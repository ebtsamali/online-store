# Data model

The persisted shape of the system: seven entities in PostgreSQL, managed by EF Core code-first.

`OnlineStore.API/Data/AppDbContext.cs` is the **single** definition of the model — `DbSet`s, indexes, relationships and delete behaviour all live there. Schema changes reach the database **only** as migrations; nothing is created at runtime and the database is never modified by hand.

---

## Entities

### `User`

| Column | Type | Default | Notes |
|---|---|---|---|
| `Id` | int | identity | Primary key |
| `Name` | string | `""` | |
| `Email` | string | `""` | **Unique index** |
| `PasswordHash` | string | `""` | BCrypt hash — never a plaintext password |
| `Role` | string | **`"customer"`** | `"customer"` or `"admin"`, compared **case-sensitively** |
| `CreatedAt` | DateTime | `UtcNow` | |

The `Role` default is why registration can only ever produce a customer: `AuthController.Register` never assigns the property, so the entity default stands. Promotion is a manual database operation — see [`auth-and-roles.md`](auth-and-roles.md).

### `Product`

| Column | Type | Default | Notes |
|---|---|---|---|
| `Id` | int | identity | Primary key |
| `Name` | string | `""` | Max 100 characters (enforced by the DTO) |
| `Description` | string | `""` | |
| `Price` | decimal | — | No currency is stored anywhere in the model |
| `Stock` | int | — | Decremented at checkout |
| `IsActive` | bool | **`true`** | The soft-delete / retire flag |
| `CreatedAt` | DateTime | `UtcNow` | List ordering key (descending) |
| `ImageUrl` | string | `""` | Site-relative path, e.g. `/images/products/{guid}.jpg` |
| `CategoryId` | int | — | FK → `Category`, **Restrict** |
| `BrandId` | int | — | FK → `Brand`, **Restrict** |

`IsActive` does the real work in this model. Anonymous and non-admin callers only ever see active products, so setting it `false` withdraws a product from the storefront while leaving every historical reference intact. Both `IsActive` and `CreatedAt` are server-owned — no create request can set them.

### `Category` and `Brand`

| Column | Type | Notes |
|---|---|---|
| `Id` | int | Primary key |
| `Name` | string | 1–100 characters; uniqueness enforced in the controller, **not** by a database index |

Structurally identical. Name uniqueness is an application-level check, so a genuinely concurrent insert of the same name can produce duplicates — there is no unique index to stop it.

### `CartItem`

| Column | Type | Default | Notes |
|---|---|---|---|
| `Id` | int | identity | Primary key |
| `UserId` | int | — | FK → `User`, **Cascade** |
| `ProductId` | int | — | FK → `Product`, **Cascade** |
| `Quantity` | int | **`1`** | Incremented when the same product is added again |
| `CreatedAt` | DateTime | `UtcNow` | |

One cart per user, expressed as rows keyed by `UserId` — there is no `Cart` entity. A user has at most one row per product: adding an existing product increments `Quantity` rather than inserting a second line.

### `Order`

| Column | Type | Default | Notes |
|---|---|---|---|
| `Id` | int | identity | Primary key |
| `UserId` | int | — | FK → `User`, **Cascade** |
| `Status` | string | **`"pending"`** | Declared values: `"pending"`, `"paid"`, `"failed"` |
| `Total` | decimal | — | Sum of `UnitPrice × Quantity` at checkout |
| `CreatedAt` | DateTime | `UtcNow` | |
| `Items` | `List<OrderItem>` | empty | Navigation |

### `OrderItem`

| Column | Type | Notes |
|---|---|---|
| `Id` | int | Primary key |
| `OrderId` | int | FK → `Order`, **Cascade** |
| `ProductId` | int | FK → `Product`, **Restrict** |
| `ProductName` | string | **Snapshot** taken at checkout |
| `UnitPrice` | decimal | **Snapshot** taken at checkout |
| `Quantity` | int | |

---

## Relationships and delete behaviour

| Relationship | Behaviour | Effect |
|---|---|---|
| `Product` → `Category` | **Restrict** | A category in use by any product cannot be deleted |
| `Product` → `Brand` | **Restrict** | A brand in use by any product cannot be deleted |
| `CartItem` → `User` | **Cascade** | Deleting a user clears their cart |
| `CartItem` → `Product` | **Cascade** | Deleting a product clears it from every cart |
| `Order` → `User` | **Cascade** | Deleting a user deletes their orders |
| `OrderItem` → `Order` | **Cascade** | Deleting an order deletes its lines |
| `OrderItem` → `Product` | **Restrict** | ⚠️ A product referenced by any order cannot be deleted |

### The `OrderItem → Product` restriction

This is the one delete rule with a business consequence, and it is deliberate. `AppDbContext` states the reason:

> Restrict (not Cascade): preserve order history — a product that appears in any order can no longer be hard-deleted. Retire via `IsActive = false`.

Cascading here would silently erase lines from historical orders when an admin deleted a discontinued product, corrupting records that should be immutable.

**The practical consequence:** `DELETE /api/products/{id}` fails for any product that has ever been ordered. The database refuses it, EF throws `DbUpdateException`, and the controller's catch-all converts that into `500 { "message": "Something went wrong" }`.

> **Known gap:** that should be a `409` with an explanatory message. `CategoriesController.Delete` handles its equivalent case properly, returning `409 { "message": "Cannot delete category: it is still used by one or more products" }`. The products controller does not have the matching pre-check, so the failure is indistinguishable from a genuine server error. Fixing it is a follow-up; the correct behaviour today is to **retire the product with `IsActive = false`** rather than deleting it.

Note the asymmetry with carts: `CartItem → Product` **cascades**, because a cart is transient. Deleting a product simply removes it from everyone's cart, which is the desired outcome.

---

## Constraints

**Unique index on `User.Email`** — the only database-level uniqueness constraint in the model. It gives registration two defences: an explicit existence check first, and the index as a backstop when two concurrent requests both pass that check. `AuthController.Register` catches the resulting `DbUpdateException` and returns the same `409 { "message": "Email already exists" }` as the checked path, so the race is invisible to clients.

Category and brand name uniqueness has **no** such index — it is enforced only in the controllers, and is therefore racy.

---

## Order lifecycle

```
(cart)  ──checkout──▶  pending  ──payment simulation──▶  paid
```

`Order.Status` is created as `"pending"` and set to `"paid"` within the same request. The entity comment declares a third value, `"failed"`, but **no code path ever writes it**: the payment step is `await Task.Delay(1500)` followed by an unconditional assignment to `"paid"`.

Treat `"failed"` as reserved for a future real gateway, not as a state the system currently produces. There is no cancellation, refund, or fulfilment tracking.

Checkout is transactional. Every cart line is validated *before* the transaction opens, so a rejected checkout creates no order and changes no stock. Once inside, the order insert, the stock decrements and the cart clearing commit together or not at all.

---

## Snapshotting

`OrderItem` stores `ProductName` and `UnitPrice` as copies rather than reading them through `ProductId`.

This is what makes order history stable. Renaming a product, repricing it, or retiring it leaves past orders exactly as the customer saw them, and an order's `Total` always equals the sum of its own lines.

Two consequences to keep in mind:

- **`ProductId` is for traceability, not display.** Render `ProductName` and `UnitPrice` from the `OrderItem`. Joining to `Product` to show a name would defeat the snapshot and show today's value on a historical order.
- **Carts behave differently.** `GET /api/cart` reads names and prices **live** from `Product`, so a cart tracks price changes right up to checkout — the moment the snapshot is taken.

---

## Migrations

Applied in this order:

| # | Migration | Added |
|---|---|---|
| 1 | `20260711160101_InitialCreate` | `Products` table |
| 2 | `20260712214535_AddUser` | `Users` table + unique index on `Email` |
| 3 | `20260715232924_AddCategoryBrandAndProductFks` | `Categories` and `Brands` tables; `Product.CategoryId`/`BrandId` columns, foreign keys and indexes |
| 4 | `20260718163616_AddCartItem` | `CartItems` table + indexes |
| 5 | `20260719112746_AddProductImageUrl` | `Product.ImageUrl` column |
| 6 | `20260720101845_AddOrders` | `Orders` and `OrderItems` tables + indexes |

Working with migrations (run in `OnlineStore.API/`):

```bash
dotnet ef database update          # apply everything pending
dotnet ef migrations add <Name>    # scaffold from current model changes
dotnet ef migrations list          # show applied vs pending
dotnet ef migrations remove        # drop the last migration (only if unapplied)
```

`Migrations/AppDbContextModelSnapshot.cs` records the cumulative model and is **generated** — never hand-edit it. If it drifts from the migrations, `migrations add` produces wrong diffs.

Each migration has a `Down()` method, so `dotnet ef database update <PreviousMigration>` can roll back. Rolling back past migration 5 or 6 **drops columns and tables**, destroying data — take a backup first.

---

## Image storage

`Product.ImageUrl` holds a **site-relative URL**, not a filesystem path — for example `/images/products/3f2a8c91-….jpg`.

| Aspect | Detail |
|---|---|
| Directory | `OnlineStore.API/wwwroot/images/products/` |
| Filename | A fresh GUID plus the original extension; the client's filename never reaches disk |
| Allowed types | `.jpg`, `.jpeg`, `.png`, `.webp` |
| Maximum size | 5 MB |
| Served by | `UseStaticFiles()` — **not** under `/api` |

Because images sit outside `/api`, the frontend resolves them by stripping the `/api` suffix from `apiBase` to recover the origin (`useProductImage()`).

File writes are ordered so a failure never leaves an orphan or a broken row:

- **Create** — validate the image, verify the category and brand exist, then write the file. If the save then fails, the file is deleted.
- **Update** — write the new file, save, and delete the replaced file only after the save succeeds. A failed save removes the new file and leaves the original.
- **Delete** — remove the row first, then the file, so a failed delete cannot leave a row pointing at a missing image.

The directory is tracked in git via `.gitkeep`. Files uploaded locally are real files on disk and are **not** cleaned up by dropping the database — resetting the database leaves them orphaned behind.
