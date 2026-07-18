# Story 05 — Shopping Cart API (Add / View / Remove Items)

Implement a per-user shopping cart: a logged-in customer can add products to their cart, view the cart with a computed total, and remove items. The cart owner is always resolved from the JWT — never from the request body. This story introduces the **`CartItem`** entity (none exists yet) with FKs to `User` and `Product`, and ships one EF migration for it.

---

## Prerequisites

- **Story 03 completed** ([`../middleware/03-authorization-middleware.md`](../middleware/03-authorization-middleware.md)): the JWT validation pipeline, `AddAuthorization()`, and the Swagger **Authorize** button are already wired in [`Program.cs`](../../../Program.cs) (verified: `AddAuthentication().AddJwtBearer` lines 53–67, `AddAuthorization()` line 69, `UseAuthentication()`→`UseAuthorization()`→`MapControllers()` lines 85–89). **Do not** re-add any auth wiring — only apply `[Authorize]` attributes.
- **Story 04 completed** ([`../products/04-story-products-crud.md`](../products/04-story-products-crud.md)): the `Product` entity, `Products` `DbSet`, and `ProductsController` exist and are the precedent this story mirrors for controller/DTO/migration style. Cart items reference `Product.Id`, `Product.Name`, `Product.Price`, `Product.Stock`, `Product.IsActive`.
- **User-id claim shape (verified):** [`Services/TokenService.cs`](../../../Services/TokenService.cs) line 31 issues the user id as `JwtRegisteredClaimNames.Sub`. The JWT bearer handler maps `sub` → `ClaimTypes.NameIdentifier` inbound, so read it with `User.FindFirstValue(ClaimTypes.NameIdentifier)` and `int.TryParse` it (see [`Controllers/AuthController.cs`](../../../Controllers/AuthController.cs) `Me()` lines 83–94 — the exact precedent for reading claims). The value is a **string**.
- Confirmed already available in [`OnlineStore.API.csproj`](../../../OnlineStore.API.csproj): `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`/`.Tools`. **No new packages required.**
- PostgreSQL must be reachable at `ConnectionStrings:DefaultConnection` ([`appsettings.json`](../../../appsettings.json)) for the migration and manual verification.
- **Admin not required:** every cart endpoint is `[Authorize]` for **any** logged-in user (customer role is enough). Unlike Story 04 there is no admin-only path, so no user-promotion step is needed to test.

---

## Story Goal

Deliver a per-user shopping cart API backed by a new `CartItem` table:

1. `POST /api/cart` — **any logged-in user**. Adds a product to the caller's cart. If the product is already in the cart, **increment** its `Quantity` instead of inserting a duplicate row. Validates the product exists, is active, and that the resulting quantity does not exceed `Product.Stock`. Returns the affected item as `CartItemDto`.
2. `GET /api/cart` — **any logged-in user**. Returns all cart items belonging to the caller (resolved from the token) as `CartResponse` with a computed `Total`.
3. `DELETE /api/cart/{id}` — **any logged-in user**. Removes one item from the caller's cart. **404** if the item does not exist; **403** if it belongs to a different user. Returns **204** on success.

**Not in scope:** updating an item's quantity via a dedicated endpoint (`PUT`); clearing the whole cart; checkout / order creation; merging an anonymous/guest cart on login; decrementing `Product.Stock` when items are added (stock is only *validated*, not reserved). These are possible follow-up stories (`orders`).

---

## Context — Read These Files First

1. [`Entities/Product.cs`](../../../Entities/Product.cs) — 18 lines. Fields the cart reads: `Id`, `Name`, `Price` (`decimal`), `Stock` (`int`), `IsActive` (`bool`). Mirror the entity style for `CartItem` (namespace `OnlineStore.API.Entities`, `string` props `= string.Empty`, `CreatedAt = DateTime.UtcNow`).
2. [`Entities/User.cs`](../../../Entities/User.cs) — 11 lines. `CartItem.UserId` FKs to `User.Id`.
3. [`Data/AppDbContext.cs`](../../../Data/AppDbContext.cs) — 36 lines. `DbSet`s at lines 12–15; `OnModelCreating` (lines 17–35) configures the unique `User.Email` index and the two `Product`→`Category`/`Brand` FK relationships with `DeleteBehavior.Restrict`. You add a `CartItems` `DbSet` and two FK relationships here, following the **same pattern** (task 3).
4. [`Controllers/ProductsController.cs`](../../../Controllers/ProductsController.cs) — 192 lines. The exact controller style to mirror: `[ApiController]`, `[Route("api/…")]`, constructor-injected `AppDbContext _db`, `async Task<IActionResult>`, per-action `try/catch` → `StatusCode(500, new { message = "Something went wrong" })`, `NotFound(new { message = "…" })`, `BadRequest(new { message = "…" })`, `[HttpGet("{id:int}")]` route constraints, private `ToDetail` mapper. Note the FK-existence `AnyAsync` check (lines 95–103) — the same technique validates the product on add.
5. [`Controllers/AuthController.cs`](../../../Controllers/AuthController.cs) — `Me()` (lines 83–94) is the **only** existing example of reading the current user from claims. Copy its `using System.Security.Claims;` + `User.FindFirstValue(ClaimTypes.NameIdentifier)` approach.
6. [`Dtos/CreateProductRequest.cs`](../../../Dtos/CreateProductRequest.cs) — DTO style: `record` with **plain** DataAnnotations on positional params (`[Range(1, int.MaxValue, ErrorMessage = "…")]`). `[ApiController]` auto-returns **400** `ValidationProblemDetails` on annotation failure. Mirror this form (not `[property:]`).
7. [`Dtos/ProductDetailDto.cs`](../../../Dtos/ProductDetailDto.cs) / [`Dtos/AuthResponse.cs`](../../../Dtos/AuthResponse.cs) — one-line response `record`s; mirror for the cart DTOs.
8. [`Migrations/20260715232924_AddCategoryBrandAndProductFks.cs`](../../../Migrations/20260715232924_AddCategoryBrandAndProductFks.cs) — the most recent migration; read its `Up` for the exact Npgsql conventions this repo generates for a new table with FKs (`CreateTable` with `Id` identity, `CreateIndex` on FK columns, `AddForeignKey`). Your new migration is **auto-generated** and should match this shape. **Do not hand-write it.** This is also the migration you roll back to if needed.
9. [`Program.cs`](../../../Program.cs) — CORS `"Frontend"` policy for `http://localhost:3000` (lines 42–48, `UseCors` line 83) and the auth pipeline (lines 85–89) are already correct. **No change needed.**
10. [`Properties/launchSettings.json`](../../../Properties/launchSettings.json) — API at `http://localhost:5016`; Swagger at `/swagger`. Use for manual verification.

---

## Backend Tasks

### 1 — Create the `CartItem` entity

**Create file: `Entities/CartItem.cs`**

```csharp
namespace OnlineStore.API.Entities;

public class CartItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Product? Product { get; set; }
}
```

> Navigations are **nullable** so the executor is not forced to always `Include` them; the add/list logic loads the `Product` explicitly where needed.

### 2 — Register the `DbSet` — `Data/AppDbContext.cs`

**File: `Data/AppDbContext.cs`**

Add after the `Brands` `DbSet` (line 15):

```csharp
public DbSet<CartItem> CartItems => Set<CartItem>();
```

### 3 — Register relationships — `Data/AppDbContext.cs`

**File: `Data/AppDbContext.cs`**

Extend `OnModelCreating` (keep the existing `User.Email` index and the two `Product` FK relationships). Add the two `CartItem` FKs. Use **`DeleteBehavior.Cascade`** so a user's cart is cleaned up if the user is deleted and a product's cart references are removed if the product is hard-deleted (Story 04 DELETE is a hard delete):

```csharp
modelBuilder.Entity<CartItem>()
    .HasOne(ci => ci.User)
    .WithMany()
    .HasForeignKey(ci => ci.UserId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<CartItem>()
    .HasOne(ci => ci.Product)
    .WithMany()
    .HasForeignKey(ci => ci.ProductId)
    .OnDelete(DeleteBehavior.Cascade);
```

> **Contrast with Story 04:** `Product`→`Category`/`Brand` used `Restrict` (don't delete reference data in use). Here `Cascade` is correct: cart rows are disposable and must not block deleting a user or product. This is a **deliberate** difference — flag it in the PR.

### 4 — Create the DTOs

**Create file: `Dtos/AddToCartRequest.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record AddToCartRequest(
    [Range(1, int.MaxValue, ErrorMessage = "ProductId is required.")]
    int ProductId,

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    int Quantity
);
```

> `[Range(1, …)]` on `Quantity` makes `Quantity <= 0` fail annotation validation → automatic **400** via `[ApiController]`, satisfying "400 if Quantity <= 0" without a manual check.

**Create file: `Dtos/CartItemDto.cs`**

```csharp
namespace OnlineStore.API.Dtos;

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    decimal Subtotal);
```

**Create file: `Dtos/CartResponse.cs`**

```csharp
namespace OnlineStore.API.Dtos;

public record CartResponse(List<CartItemDto> Items, decimal Total);
```

### 5 — Create `Controllers/CartController.cs`

**Create file: `Controllers/CartController.cs`**

- Namespace `OnlineStore.API.Controllers`; `[ApiController]`, `[Route("api/cart")]`, `[Authorize]` at the **controller level** (every action requires a valid token; no `[AllowAnonymous]` anywhere).
- Constructor-inject `AppDbContext _db` (mirror `ProductsController` lines 15–20).
- Usings: `using System.Security.Claims;`, `using Microsoft.AspNetCore.Authorization;`, `using Microsoft.AspNetCore.Mvc;`, `using Microsoft.EntityFrameworkCore;`, `using OnlineStore.API.Data;`, `using OnlineStore.API.Dtos;`, `using OnlineStore.API.Entities;`.

**Private helper — resolve the current user id from the token (never from the body):**

```csharp
private int? GetUserId()
{
    var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
    return int.TryParse(raw, out var id) ? id : null;
}
```

> Returns `int?` so a token missing/with an unparseable `sub` is handled explicitly (→ **401**) rather than throwing.

**Actions:**

#### `POST /api/cart` — `[HttpPost]`

Signature: `public async Task<IActionResult> Add(AddToCartRequest request)`

1. Wrap the body in `try/catch` → `StatusCode(500, new { message = "Something went wrong" })`.
2. `var userId = GetUserId(); if (userId is null) return Unauthorized();`
3. Load the product: `var product = await _db.Products.FindAsync(request.ProductId);`
   - If `null` **or** `!product.IsActive` → `return NotFound(new { message = "Product not found" });` (do not let customers add inactive products).
4. Find an existing cart row for this user + product:
   `var existing = await _db.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == request.ProductId);`
5. Compute the **resulting** quantity and validate stock **once** against the final total:
   ```csharp
   var newQuantity = (existing?.Quantity ?? 0) + request.Quantity;
   if (newQuantity > product.Stock)
       return BadRequest(new { message = "Requested quantity exceeds available stock" });
   ```
6. Upsert:
   ```csharp
   CartItem item;
   if (existing is null)
   {
       item = new CartItem
       {
           UserId = userId.Value,
           ProductId = product.Id,
           Quantity = request.Quantity
       };
       _db.CartItems.Add(item);
   }
   else
   {
       existing.Quantity = newQuantity;
       item = existing;
   }
   await _db.SaveChangesAsync();
   ```
7. Return the affected item mapped with the product's current name/price:
   `return Ok(ToDto(item, product));` (private mapper below). *(Intake allows 200 or 201; use **200** since an add can be an update to an existing row.)*

#### `GET /api/cart` — `[HttpGet]`

Signature: `public async Task<IActionResult> Get()`

1. `try/catch` → 500. `var userId = GetUserId(); if (userId is null) return Unauthorized();`
2. Query the caller's items, joined to the product for name/price, projected to `CartItemDto`:
   ```csharp
   var items = await _db.CartItems
       .Where(ci => ci.UserId == userId)
       .OrderBy(ci => ci.Id)
       .Select(ci => new CartItemDto(
           ci.Id,
           ci.ProductId,
           ci.Product!.Name,
           ci.Product.Price,
           ci.Quantity,
           ci.Product.Price * ci.Quantity))
       .ToListAsync();
   ```
   *(The `ci.Product!.Name` projection makes EF emit a JOIN — no explicit `Include` needed inside a `Select`.)*
3. `var total = items.Sum(i => i.Subtotal);`
4. `return Ok(new CartResponse(items, total));`

#### `DELETE /api/cart/{id}` — `[HttpDelete("{id:int}")]`

Signature: `public async Task<IActionResult> Delete(int id)`

1. `try/catch` → 500. `var userId = GetUserId(); if (userId is null) return Unauthorized();`
2. `var item = await _db.CartItems.FindAsync(id);`
3. If `null` → `return NotFound(new { message = "Cart item not found" });`
4. **Ownership check:** `if (item.UserId != userId) return Forbid();` — a user must never delete another user's item. `Forbid()` yields **403** (per intake).
5. `_db.CartItems.Remove(item); await _db.SaveChangesAsync(); return NoContent();` (**204**).

#### Private mapper

```csharp
private static CartItemDto ToDto(CartItem item, Product product) =>
    new(item.Id, product.Id, product.Name, product.Price, item.Quantity, product.Price * item.Quantity);
```

> **Never return the `CartItem` entity directly** — always project to a DTO, so `UserId`/navigation graphs are not serialized.

### 6 — Generate and apply the EF migration

From `OnlineStore.API/`:

```powershell
dotnet ef migrations add AddCartItem
dotnet ef database update
```

Inspect the generated `Migrations/*_AddCartItem.cs`: it should `CreateTable("CartItems", …)` with `Id` identity, `UserId`/`ProductId` `integer`, `Quantity` `integer`, `CreatedAt` `timestamp with time zone`, plus `CreateIndex` on `UserId` and `ProductId` and two `AddForeignKey` constraints with `onDelete: ReferentialAction.Cascade`. **Do not hand-write it.** If `dotnet ef` is missing: `dotnet tool install --global dotnet-ef`.

---

## Edge Cases & Failure Modes

- **Adding a product already in the cart:** must **increment** the existing row's `Quantity`, not insert a duplicate (task 5 step 4–6, enforced by the `FirstOrDefaultAsync` lookup). Verified by acceptance criterion 2.
- **Stock validation against the resulting total, not the delta:** if the cart already holds 3 of a product with `Stock = 5`, adding 3 more must fail (3 + 3 > 5), even though the delta (3) alone is ≤ stock. Enforced by computing `newQuantity` first (task 5 step 5). A naive "request.Quantity > product.Stock" check would wrongly allow it.
- **`Quantity <= 0`:** rejected at the annotation layer (`[Range(1, …)]`) → automatic **400** before the action runs.
- **Inactive / non-existent product on add:** both return **404** `"Product not found"` (task 5 step 3). Prevents adding products hidden from the catalog.
- **Missing / unparseable `sub` claim:** `GetUserId()` returns `null` → **401** `Unauthorized()`. Guards against a valid-signature token that somehow lacks the id.
- **Deleting another user's item:** returns **403** `Forbid()` (task 5 DELETE step 4), never **204**. **Info-leak tradeoff (flagged):** returning **403** for an existing-but-not-yours item reveals that the id exists, whereas **404** for both cases would hide it. The intake explicitly asks for **403**, so follow it; note the alternative in the PR if the team prefers 404-for-both.
- **Deleting a non-existent item:** **404** `"Cart item not found"` (task 5 DELETE step 3).
- **Product deleted while in a cart:** `DeleteBehavior.Cascade` (task 3) removes the orphaned cart rows automatically, so `GET /api/cart`'s `ci.Product!.Name` projection never dereferences a missing product. Without cascade, the join would drop the row or throw. **Uncertainty flagged:** if the team later wants "product removed" placeholders in the cart instead of silent deletion, revisit this delete behaviour.
- **Concurrent add of the same product (two requests):** both could pass the `FirstOrDefaultAsync` check and insert two rows before either commits — a rare duplicate. No unique index on `(UserId, ProductId)` is added in this story (keeps it minimal); if duplicates become a problem, a unique composite index + upsert-on-conflict is the follow-up. **Flagged, out of scope.**
- **`decimal` precision:** `Price` maps to Postgres `numeric` (unbounded); `Subtotal`/`Total` are computed in-memory (`GET`) or via SQL (`Select`) — no precision loss.

---

## Test Plan

No test project exists yet (noted in Stories 02–04). If/when one is added (e.g. `OnlineStore.API.Tests` with `WebApplicationFactory`), add:

1. **Add — 200 new row** (`CartAddTests`): logged-in user adds an active product with `quantity = 2` → **200**, `CartItemDto` with `subtotal = price * 2`; one `CartItems` row for that user.
2. **Add — increments existing:** add the same product twice (2 then 3) → single row with `quantity = 5`, no duplicate.
3. **Add — 400 over stock:** product `stock = 5`, add `quantity = 6` → **400** `"Requested quantity exceeds available stock"`; also add 3 then 3 → second call **400**.
4. **Add — 400 bad quantity:** `quantity = 0` / negative → **400** (annotation).
5. **Add — 404 inactive/missing product:** `productId` of an inactive product or a non-existent id → **404** `"Product not found"`.
6. **Add — 401 no token:** no `Authorization` header → **401**.
7. **Get — only own items:** seed cart items for user A and user B; `GET /api/cart` as A → only A's items; `total` equals the sum of A's subtotals.
8. **Get — 401 no token:** → **401**.
9. **Delete — 204 own item:** A deletes A's item → **204**; item gone from `GET /api/cart`.
10. **Delete — 403 other user's item:** A attempts to delete B's item id → **403**; B's item still present.
11. **Delete — 404 missing:** delete a non-existent id → **404** `"Cart item not found"`.

Until a test project exists, the manual matrix in **Verification Steps** is the acceptance gate — record results in the PR. Reference the manual-verification style in [`../products/04-story-products-crud.md`](../products/04-story-products-crud.md).

---

## Migration / Rollback

- **Forward:** `dotnet ef migrations add AddCartItem` → `dotnet ef database update`. Creates the `CartItems` table (+ FK indexes + FKs). No existing table is altered, so there is **no backfill risk** (unlike Story 04's added non-nullable FK columns).
- **Half-applied risk:** minimal — a brand-new table with no dependents. If `database update` fails midway, re-run it; the migration is idempotent at the DDL level.
- **Rollback:** `dotnet ef database update AddCategoryBrandAndProductFks` (migration `20260715232924_AddCategoryBrandAndProductFks`) reverts, dropping `CartItems`, then `dotnet ef migrations remove` deletes the migration files. Rolling back discards all cart data — back it up first if it matters.

---

## Verification Steps

1. **Backend builds:** `dotnet build` in `OnlineStore.API/` — succeeds, no warnings.
2. **Migration applied:** `dotnet ef database update` completes; a `CartItems` table with `UserId`/`ProductId` FK columns exists.
3. **Seed data:** ensure at least one active product exists with a known `Stock` (use Story 04's `POST /api/products` as admin, or insert directly). Note its `id`.
4. **Backend runs:** `dotnet run --launch-profile http`, open `http://localhost:5016/swagger`.
5. **Auth required:** `GET /api/cart` with no token → **401**.
6. **Log in:** `POST /api/auth/login` as a customer, copy the token, click Swagger **Authorize**, paste it.
7. **Add (200):** `POST /api/cart` `{ "productId": <id>, "quantity": 2 }` → **200**, `CartItemDto` with `subtotal = price * 2`.
8. **Add again → increments:** `POST /api/cart` same `productId`, `quantity: 1` → **200**, same item `id`, `quantity: 3` (not a new row).
9. **Over-stock (400):** `POST /api/cart` `{ "productId": <id>, "quantity": 9999 }` → **400** `"Requested quantity exceeds available stock"`.
10. **Bad quantity (400):** `{ "productId": <id>, "quantity": 0 }` → **400** (validation).
11. **Inactive/missing product (404):** `{ "productId": 999999, "quantity": 1 }` → **404** `"Product not found"`.
12. **View cart (200):** `GET /api/cart` → **200**, `{ items: [...], total }`; `total` equals the sum of `subtotal`s; only this user's items appear.
13. **Ownership on delete (403):** log in as a **second** user, note one of their cart item ids is different; as the first user call `DELETE /api/cart/{secondUsersItemId}` → **403**.
14. **Delete own (204):** `DELETE /api/cart/{ownItemId}` → **204**; `GET /api/cart` no longer lists it.
15. **Delete missing (404):** `DELETE /api/cart/999999` → **404** `"Cart item not found"`.
16. **CORS:** preflight from `http://localhost:3000` to `/api/cart` passes (existing `"Frontend"` policy).

---

## Done Criteria

- [ ] `CartItem` entity exists with `UserId`/`ProductId` FKs; `CartItems` `DbSet` registered; one migration (`AddCartItem`) creates the table + FK indexes/constraints.
- [ ] `POST /api/cart` adds a product to the **caller's** cart (user resolved from the JWT, not the body); returns `CartItemDto`.
- [ ] Adding a product already in the cart **increments** its quantity — no duplicate row.
- [ ] `POST /api/cart` returns **400** when the resulting quantity exceeds `Product.Stock`, and **404** for a missing/inactive product.
- [ ] `GET /api/cart` returns only the caller's items as `CartResponse` with a correct computed `Total`.
- [ ] `DELETE /api/cart/{id}` removes the caller's own item (**204**), returns **403** for another user's item, and **404** for a non-existent item.
- [ ] Every cart endpoint returns **401** without a valid token.
- [ ] Endpoints never expose the raw `CartItem` entity — always a DTO.
- [ ] `dotnet build` succeeds; migration applies cleanly.

---

**STOP HERE. Report to the user and wait for confirmation before proceeding to any follow-up story (e.g. `orders` / checkout).**
