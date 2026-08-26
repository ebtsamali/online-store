# API reference

Complete reference for the `OnlineStore.API` HTTP surface: **22 endpoints** across six controllers.

---

## Conventions

**Base path** — every route is under `/api`. With the default `https` launch profile the full base is `https://localhost:7225/api`.

**Content types** — JSON in and out, with two exceptions: `POST /api/products` and `PUT /api/products/{id}` require `multipart/form-data` because they carry a file.

**Authentication** — a JWT in the header:

```
Authorization: Bearer <token>
```

Obtain one from `POST /api/auth/login`. Tokens are valid for **7 days**.

**Authorization levels** used in the tables below:

| Level | Attribute | Meaning |
|---|---|---|
| **Anonymous** | `[AllowAnonymous]` | No token needed |
| **Authenticated** | `[Authorize]` | Any valid token |
| **Admin** | `[Authorize(Roles = "admin")]` | Token whose role claim is exactly `admin` |

**Error envelope** — deliberate failures return a single-field object:

```json
{ "message": "Product not found" }
```

**Validation errors** — DTO data annotations are enforced by `[ApiController]`, which returns `400` with a `ValidationProblemDetails` body listing each failing field:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "Email": ["The Email field is not a valid e-mail address."] }
}
```

**Unhandled exceptions** — every action catches broadly and returns `500` with `{ "message": "Something went wrong" }`. A `500` therefore carries no diagnostic detail; check the server log.

---

## Endpoint index

| # | Method | Path | Auth | Purpose |
|---|---|---|---|---|
| 1 | `POST` | `/api/auth/register` | Anonymous | Create a customer account |
| 2 | `POST` | `/api/auth/login` | Anonymous | Exchange credentials for a JWT |
| 3 | `GET` | `/api/auth/me` | Authenticated | Echo the caller's identity claims |
| 4 | `GET` | `/api/products` | Anonymous | Paged, searchable product list |
| 5 | `GET` | `/api/products/{id}` | Anonymous | Single product |
| 6 | `POST` | `/api/products` | Admin | Create a product (multipart) |
| 7 | `PUT` | `/api/products/{id}` | Admin | Update a product (multipart) |
| 8 | `DELETE` | `/api/products/{id}` | Admin | Delete a product |
| 9 | `GET` | `/api/categories` | Anonymous | List categories |
| 10 | `POST` | `/api/categories` | Admin | Create a category |
| 11 | `PUT` | `/api/categories/{id}` | Admin | Rename a category |
| 12 | `DELETE` | `/api/categories/{id}` | Admin | Delete a category |
| 13 | `GET` | `/api/brands` | Anonymous | List brands |
| 14 | `POST` | `/api/brands` | Admin | Create a brand |
| 15 | `PUT` | `/api/brands/{id}` | Admin | Rename a brand |
| 16 | `DELETE` | `/api/brands/{id}` | Admin | Delete a brand |
| 17 | `POST` | `/api/cart` | Authenticated | Add to cart (increments if present) |
| 18 | `GET` | `/api/cart` | Authenticated | The caller's cart with totals |
| 19 | `DELETE` | `/api/cart/{id}` | Authenticated | Remove a cart line |
| 20 | `POST` | `/api/orders/checkout` | Authenticated | Turn the cart into an order |
| 21 | `GET` | `/api/orders` | Authenticated | The caller's order history |
| 22 | `GET` | `/api/orders/{id}` | Authenticated | One order with its lines |

Note the pattern on Products, Categories and Brands: the controller is `[Authorize(Roles = "admin")]` at class level, and the read endpoints opt out with `[AllowAnonymous]`. Writes are admin-only by default.

---

## Auth

### `POST /api/auth/register` — Anonymous

Creates an account. The new user's role is **always `customer`** — the request cannot influence it, and no endpoint exists to create an admin (see [`auth-and-roles.md`](auth-and-roles.md)).

**Body**

| Field | Type | Rules |
|---|---|---|
| `name` | string | Required, 3–50 characters |
| `email` | string | Required, valid email address |
| `password` | string | Required, **min 8 characters, at least one uppercase letter and one digit** |

```json
{ "name": "Ada Lovelace", "email": "ada@example.com", "password": "Passw0rdX" }
```

**`201 Created`**

```json
{ "message": "Account created successfully", "name": "Ada Lovelace", "email": "ada@example.com" }
```

No token is returned — the client must call `login` afterwards.

| Status | When |
|---|---|
| `400` | Validation failure |
| `409` | `{ "message": "Email already exists" }` — also returned when a concurrent request wins the race, caught via the unique-index violation |
| `500` | Unhandled exception |

> The frontend's client-side rule is weaker than this one (6 characters, no character-class requirement), so a password can pass in the browser and be rejected here. Known gap — see [`auth-and-roles.md`](auth-and-roles.md#known-gaps).

### `POST /api/auth/login` — Anonymous

**Body**

| Field | Type | Rules |
|---|---|---|
| `email` | string | Required, valid email address |
| `password` | string | Required |

**`200 OK`**

```json
{ "token": "eyJhbGciOi…", "name": "Ada Lovelace", "email": "ada@example.com", "role": "customer" }
```

| Status | When |
|---|---|
| `400` | Validation failure |
| `401` | `{ "message": "Invalid email or password" }` |
| `500` | Unhandled exception |

The `401` is **deliberately identical** for an unknown email and a wrong password, so the endpoint cannot be used to enumerate registered accounts.

### `GET /api/auth/me` — Authenticated

Echoes the caller's claims. Useful for confirming a token's role.

**`200 OK`**

```json
{ "id": "1", "email": "ada@example.com", "role": "customer" }
```

`id` is a string — it comes straight from the `sub` claim. The values are read from the validated token, not the database, so a role changed in SQL will not appear here until the user logs in again.

| Status | When |
|---|---|
| `401` | Missing, expired or invalid token |

---

## Products

### `GET /api/products` — Anonymous

Paged list, newest first (`CreatedAt` descending).

**Query parameters**

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `search` | string | — | Case-insensitive **partial** match on name (SQL `ILIKE '%term%'`) |
| `categoryId` | int | — | Exact match |
| `brandId` | int | — | Exact match |
| `page` | int | `1` | Values below 1 are raised to 1 |
| `pageSize` | int | `20` | **Clamped to 1–100** — a larger value is silently reduced |

**Visibility rule:** anonymous and non-admin callers receive **only products with `isActive = true`**. A caller whose role is `admin` receives all products, active or not. The same URL therefore returns different result sets depending on the token — worth remembering when comparing the storefront against the admin list.

**`200 OK`**

```json
{
  "items": [
    { "id": 12, "name": "Wireless Mouse", "price": 29.99, "stock": 40, "isActive": true, "imageUrl": "/images/products/8f3c….jpg" }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 137
}
```

`total` is the count **after** filtering and before paging. `imageUrl` is a site-relative path on the API host — resolve it against the API origin, not `/api`.

| Status | When |
|---|---|
| `500` | Unhandled exception |

### `GET /api/products/{id}` — Anonymous

**`200 OK`**

```json
{
  "id": 12, "name": "Wireless Mouse", "description": "…",
  "price": 29.99, "stock": 40,
  "categoryId": 3, "brandId": 7,
  "isActive": true, "createdAt": "2026-07-19T11:27:46Z",
  "imageUrl": "/images/products/8f3c….jpg"
}
```

| Status | When |
|---|---|
| `404` | `{ "message": "Product not found" }` — also returned for an **inactive** product when the caller is not an admin |
| `500` | Unhandled exception |

### `POST /api/products` — Admin

`Content-Type: multipart/form-data`.

| Field | Type | Rules |
|---|---|---|
| `Name` | string | Required, max 100 characters |
| `Description` | string | Optional |
| `Price` | decimal | **> 0** |
| `Stock` | int | ≥ 0 |
| `CategoryId` | int | Required, must exist |
| `BrandId` | int | Required, must exist |
| `Image` | file | **Required.** `.jpg`, `.jpeg`, `.png`, `.webp`; max 5 MB |

`IsActive` and `CreatedAt` are **not** accepted — the server sets them from entity defaults (`true` and now).

**`201 Created`** — the full product detail object, with `Location` pointing at `GET /api/products/{id}`.

| Status | When |
|---|---|
| `400` | Validation failure; or `{ "message": "Category not found" }` / `{ "message": "Brand not found" }`; or an image rejection such as `{ "message": "Image must be 5 MB or smaller." }` |
| `401` / `403` | No token / not an admin |
| `500` | Unhandled exception |

Validation order matters: the image is checked, then the category and brand are confirmed to exist, and only then is the file written. If the database save fails afterwards the file is deleted, so a failed create leaves nothing behind.

### `PUT /api/products/{id}` — Admin

`Content-Type: multipart/form-data`. Same fields as create, with two differences:

| Field | Type | Rules |
|---|---|---|
| `IsActive` | bool | **Update only.** This is how a product is retired. |
| `Image` | file | **Optional.** Omit to keep the current image. |

`CreatedAt` is left unchanged.

**`200 OK`** — the updated product detail object.

| Status | When |
|---|---|
| `400` | Validation failure; unknown `CategoryId`/`BrandId`; image rejection |
| `401` / `403` | No token / not an admin |
| `404` | `{ "message": "Product not found" }` |
| `500` | Unhandled exception |

When a replacement image is supplied, the new file is written first and the old one is deleted **only after** the save succeeds. A failed save removes the new file and leaves the original intact.

### `DELETE /api/products/{id}` — Admin

Hard delete. The row is removed first, then its image file.

**`204 No Content`**

| Status | When |
|---|---|
| `401` / `403` | No token / not an admin |
| `404` | `{ "message": "Product not found" }` |
| `500` | Unhandled exception — **including the case below** |

> ⚠️ **A product that appears in any order cannot be deleted.** `OrderItem → Product` uses `DeleteBehavior.Restrict` to preserve order history, so the database refuses the delete and the resulting exception surfaces as a generic `500`, *not* the `409` you would expect (and that `DELETE /api/categories/{id}` does return for its equivalent case).
>
> **Retire the product instead:** `PUT /api/products/{id}` with `IsActive = false`. It then disappears from all customer-facing responses while order history stays intact. The misleading status code is a known gap — see [`data-model.md`](data-model.md).

---

## Categories

`Category` is `{ id, name }`. `Brands` below is identical in every respect.

### `GET /api/categories` — Anonymous

**`200 OK`** — `[{ "id": 3, "name": "Electronics" }]`

### `POST /api/categories` — Admin

**Body** — `{ "name": "Electronics" }`. Required, 1–100 characters, trimmed.

**`201 Created`** — `{ "id": 3, "name": "Electronics" }`

| Status | When |
|---|---|
| `400` | `{ "message": "Name is required" }`, or `{ "message": "A category with this name already exists" }` |
| `401` / `403` | No token / not an admin |

Note the duplicate-name case is a `400`, not a `409`.

### `PUT /api/categories/{id}` — Admin

**Body** — `{ "name": "Home & Living" }`

**`200 OK`** — the updated object.

| Status | When |
|---|---|
| `400` | Empty name, or duplicate name |
| `401` / `403` | No token / not an admin |
| `404` | `{ "message": "Category not found" }` |

### `DELETE /api/categories/{id}` — Admin

**`204 No Content`**

| Status | When |
|---|---|
| `401` / `403` | No token / not an admin |
| `404` | `{ "message": "Category not found" }` |
| `409` | `{ "message": "Cannot delete category: it is still used by one or more products" }` |

The in-use case is checked explicitly and returns a proper `409` — unlike the analogous product-in-order case, which leaks a `500`.

---

## Brands

Identical to Categories in shape, validation and status codes.

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/brands` | Anonymous |
| `POST` | `/api/brands` | Admin |
| `PUT` | `/api/brands/{id}` | Admin |
| `DELETE` | `/api/brands/{id}` | Admin |

Messages read "brand" in place of "category", e.g. `{ "message": "Brand not found" }`.

---

## Cart

One cart per user, derived from the token — there is no cart id in any route and no way to read another user's cart.

### `POST /api/cart` — Authenticated

**Adds or increments.** If the product is already in the cart, its quantity increases; a duplicate line is never created.

**Body**

| Field | Type | Rules |
|---|---|---|
| `productId` | int | ≥ 1 |
| `quantity` | int | ≥ 1 |

**`200 OK`**

```json
{ "id": 55, "productId": 12, "productName": "Wireless Mouse", "price": 29.99, "quantity": 3, "subtotal": 89.97 }
```

| Status | When |
|---|---|
| `400` | Validation failure, or `{ "message": "Requested quantity exceeds available stock" }` |
| `401` | No token |
| `404` | `{ "message": "Product not found" }` — also returned when the product exists but is **inactive** |
| `500` | Unhandled exception |

Stock is validated against the **resulting total**, not the increment. With 5 in stock and 4 already in the cart, adding 2 is rejected even though 2 ≤ 5.

There is **no update-quantity endpoint.** To reduce a quantity, `DELETE` the line and add it again.

### `GET /api/cart` — Authenticated

**`200 OK`**

```json
{
  "items": [
    { "id": 55, "productId": 12, "productName": "Wireless Mouse", "price": 29.99, "quantity": 3, "subtotal": 89.97 }
  ],
  "total": 89.97
}
```

Lines are ordered by id (insertion order). Prices and names are read **live** from the product rows, so a cart reflects price changes until checkout — after which the order snapshots them permanently.

| Status | When |
|---|---|
| `401` | No token |

### `DELETE /api/cart/{id}` — Authenticated

`{id}` is the **cart item** id, not the product id.

**`204 No Content`**

| Status | When |
|---|---|
| `401` | No token |
| `403` | The cart item belongs to another user |
| `404` | `{ "message": "Cart item not found" }` |

---

## Orders

### `POST /api/orders/checkout` — Authenticated

Converts the caller's cart into an order. **No request body.**

Sequence:

1. Load the cart; reject if empty.
2. Validate **every** line before writing anything — each product must exist, be active, and have enough stock.
3. Open a transaction.
4. Create the order, **snapshotting** each product's name and unit price into its `OrderItem`.
5. Compute `total` as the sum of `unitPrice × quantity`.
6. Decrement each product's stock.
7. Simulate a payment gateway (`await Task.Delay(1500)`), then set status `paid`.
8. Empty the cart.
9. Commit.

Step 2 is a full pre-pass: if any line fails, no order is created and no stock changes.

> **Payment is simulated and always succeeds.** There is no gateway integration, no `failed` path, and no idempotency key. The 1.5-second delay is deliberate. Expect this endpoint to take ~1.5 s.

**`201 Created`**

```json
{
  "id": 91, "status": "paid", "total": 89.97, "createdAt": "2026-08-26T10:15:00Z",
  "items": [
    { "productId": 12, "productName": "Wireless Mouse", "unitPrice": 29.99, "quantity": 3, "subtotal": 89.97 }
  ]
}
```

| Status | When |
|---|---|
| `400` | `{ "message": "Cart is empty" }`, `{ "message": "Product {id} is unavailable" }` (missing or inactive), `{ "message": "Insufficient stock for {name}" }` |
| `401` | No token |
| `500` | Unhandled exception — the transaction rolls back |

### `GET /api/orders` — Authenticated

The caller's own orders only.

**`200 OK`**

```json
[{ "id": 91, "status": "paid", "total": 89.97, "createdAt": "2026-08-26T10:15:00Z" }]
```

Summaries carry no line items — use `GET /api/orders/{id}` for those.

| Status | When |
|---|---|
| `401` | No token |

### `GET /api/orders/{id}` — Authenticated

**`200 OK`** — the same detail object `checkout` returns.

| Status | When |
|---|---|
| `401` | No token |
| `403` | The order belongs to another user |
| `404` | `{ "message": "Order not found" }` |

The `403`/`404` split is meaningful: `404` means no such order exists, `403` means it exists but is not yours.

---

## Trying it out

Swagger UI is available in Development at `/swagger`. Click **Authorize** and paste the raw token from a login response — **without** a `Bearer ` prefix, which Swagger adds itself.

With `curl`:

```bash
# Log in and capture the token
TOKEN=$(curl -sk -X POST https://localhost:7225/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"ada@example.com","password":"Passw0rdX"}' | jq -r .token)

# Anonymous read
curl -sk "https://localhost:7225/api/products?page=1&pageSize=5"

# Authenticated read
curl -sk https://localhost:7225/api/cart -H "Authorization: Bearer $TOKEN"

# Admin multipart create
curl -sk -X POST https://localhost:7225/api/products \
  -H "Authorization: Bearer $TOKEN" \
  -F "Name=Wireless Mouse" -F "Description=Ergonomic" \
  -F "Price=29.99" -F "Stock=40" \
  -F "CategoryId=3" -F "BrandId=7" \
  -F "Image=@mouse.jpg"
```

`-k` skips certificate verification against the self-signed development certificate — see [`local-setup.md`](local-setup.md#8-the-self-signed-certificate).
