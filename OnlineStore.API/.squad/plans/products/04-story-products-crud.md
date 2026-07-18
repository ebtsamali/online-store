# Story 04 — Product Catalog CRUD API (with Category & Brand)

Implement the full products API: public browse (paginated + searchable list, single detail) and admin-only create/update/delete. This story also introduces the **Category** and **Brand** entities that `Product` references via FK, since neither exists yet in the codebase.

---

## Prerequisites

- **Story 03 completed** ([`../middleware/03-authorization-middleware.md`](../middleware/03-authorization-middleware.md)): the JWT validation pipeline, `AddAuthorization()`, and the Swagger **Authorize** button are already wired in [`Program.cs`](../../../Program.cs) (verified: `AddAuthentication().AddJwtBearer` lines 53–67, `AddAuthorization()` line 69, `UseAuthentication()`→`UseAuthorization()`→`MapControllers()` lines 85–89). **Do not** re-add any auth wiring — only apply `[Authorize]` attributes.
- **Authorization matrix is pre-specified** by Story 03 (its "Deferred — Authorization matrix" table). This story is the one that finally applies it to real endpoints.
- **Admin-user gap (from Story 03):** `AuthController.Register` hardcodes `Role = "customer"` — there is **no** code path that creates an admin. To verify admin-only endpoints you must promote a user in the DB and re-login (see Verification step 8).
- Confirmed already available in [`OnlineStore.API.csproj`](../../../OnlineStore.API.csproj): `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 (line 21), `Microsoft.EntityFrameworkCore.Design`/`.Tools` 10.0.9 (lines 13–20). **No new packages required.**
- PostgreSQL must be reachable at `ConnectionStrings:DefaultConnection` ([`appsettings.json`](../../../appsettings.json)) for the migration and manual verification.
- **Precedent for tone/structure:** [`../auth/01-user-registration-api.md`](../auth/01-user-registration-api.md) (entity + DTO + controller + migration pattern) and [`../middleware/03-authorization-middleware.md`](../middleware/03-authorization-middleware.md) (auth attributes, claim/role behaviour).

---

## Story Goal

Deliver a complete Product Catalog API backed by Category and Brand reference data:

1. `GET /api/products` — **public**, paginated + searchable list of **active** products (admins see all). Returns `ProductListItemDto[]` with paging metadata.
2. `GET /api/products/{id}` — **public**, full `ProductDetailDto`, or **404** `"Product not found"`.
3. `POST /api/products` — **admin only**, validates input (including that `CategoryId`/`BrandId` reference existing rows), returns **201** with `ProductDetailDto`.
4. `PUT /api/products/{id}` — **admin only**, updates a product, returns **200** with `ProductDetailDto` or **404**.
5. `DELETE /api/products/{id}` — **admin only**, hard delete, returns **204** or **404**.
6. Introduce `Category` and `Brand` entities, add `CategoryId`/`BrandId` FKs to `Product`, and ship one EF migration for all schema changes.

**Not in scope:** category/brand CRUD endpoints (this story only creates the tables + a minimal seed for FK validation — see task 6); product images/media; soft-delete (DELETE is a hard delete per intake); sorting beyond newest-first; cart/order integration; auth wiring changes (done in Story 03).

---

## Context — Read These Files First

1. [`Entities/Product.cs`](../../../Entities/Product.cs) — 12 lines. Current fields: `Id`, `Name`, `Description`, `Price` (`decimal`), `Stock`, `IsActive` (default `true`), `CreatedAt` (`= DateTime.UtcNow`). **There are no `CategoryId`/`BrandId` fields yet** — you add them here. This is the entity style to mirror (namespace `OnlineStore.API.Entities`, `string` props `= string.Empty`).
2. [`Entities/User.cs`](../../../Entities/User.cs) — 11 lines. Same shape; mirror for `Category`/`Brand`.
3. [`Data/AppDbContext.cs`](../../../Data/AppDbContext.cs) — 21 lines. `Products` `DbSet` (line 12) and `Users` `DbSet` (line 13) already exist; `OnModelCreating` (lines 15–20) already configures the unique `User.Email` index. You will add `Categories`/`Brands` `DbSet`s and the two FK relationships here.
4. [`Controllers/AuthController.cs`](../../../Controllers/AuthController.cs) — the controller style to mirror exactly: `[ApiController]`, `[Route("api/auth")]`, constructor-injected `AppDbContext _db`, `async Task<IActionResult>`, try/catch → `StatusCode(500, new { message = "Something went wrong" })`. Note `Me()` (lines 83–94) shows role-aware claim reading and `[Authorize]` usage.
5. [`Dtos/RegisterRequest.cs`](../../../Dtos/RegisterRequest.cs) — DTO style: `record` with **plain** DataAnnotations on positional params (`[Required, StringLength(50, MinimumLength = 3)]`) — `[ApiController]` auto-returns **400** `ValidationProblemDetails` on failure. Mirror this (do **not** use the `[property:]` form; the repo uses the plain form and it compiles).
6. [`Dtos/RegisterResponse.cs`](../../../Dtos/RegisterResponse.cs) / [`Dtos/AuthResponse.cs`](../../../Dtos/AuthResponse.cs) — one-line response `record`s; mirror for product DTOs.
7. [`Migrations/20260711160101_InitialCreate.cs`](../../../Migrations/20260711160101_InitialCreate.cs) — read `Up` (lines 15–31) for the exact Npgsql column conventions this repo generates: PK `type: "integer"` + `NpgsqlValueGenerationStrategy.IdentityByDefaultColumn`, `string`→`type: "text"`, `decimal`→`type: "numeric"`, `bool`→`type: "boolean"`, `DateTime`→`type: "timestamp with time zone"`. Your new migration is **auto-generated** and should match this shape plus FK constraints. **Do not hand-write it.**
8. [`Program.cs`](../../../Program.cs) — CORS `"Frontend"` policy for `http://localhost:3000` already registered (lines 42–48, `UseCors` line 83). No change needed. Auth pipeline lines 85–89 — do not touch.
9. [`Properties/launchSettings.json`](../../../Properties/launchSettings.json) — API at `http://localhost:5016`; Swagger at `/swagger`. Use for manual verification.

---

## Product rules (from story)

- **Current behaviour:** no `ProductsController` exists (verified — `Controllers/` contains only `AuthController.cs`). `Product` has no category/brand. Nothing serves the catalog.
- **New behaviour:** public browse is paginated, searchable by name, and **active-only** for anonymous/customer callers; **admins see inactive products too**. Writes are admin-only. Every product references an existing `Category` and `Brand`.

---

## Backend Tasks

### 1 — Create the `Category` and `Brand` entities

**Create file: `Entities/Category.cs`**

```csharp
namespace OnlineStore.API.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

**Create file: `Entities/Brand.cs`**

```csharp
namespace OnlineStore.API.Entities;

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

### 2 — Add FKs to `Product`

**File: `Entities/Product.cs`**

Add the two FK scalar properties (and optional navigation properties). Keep existing fields unchanged:

```csharp
public int CategoryId { get; set; }
public int BrandId { get; set; }

public Category? Category { get; set; }
public Brand? Brand { get; set; }
```

> Navigations are **nullable** so the executor is not forced to always `Include` them; they are populated only where needed (detail read).

### 3 — Register entities + relationships — `Data/AppDbContext.cs`

**File: `Data/AppDbContext.cs`**

Add after the `Users` `DbSet` (line 13):

```csharp
public DbSet<Category> Categories => Set<Category>();
public DbSet<Brand> Brands => Set<Brand>();
```

Extend `OnModelCreating` (keep the existing `User.Email` unique index) with the two FK relationships. Use **`DeleteBehavior.Restrict`** so a category/brand in use cannot be deleted out from under products:

```csharp
modelBuilder.Entity<Product>()
    .HasOne(p => p.Category)
    .WithMany()
    .HasForeignKey(p => p.CategoryId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Product>()
    .HasOne(p => p.Brand)
    .WithMany()
    .HasForeignKey(p => p.BrandId)
    .OnDelete(DeleteBehavior.Restrict);
```

### 4 — Create the DTOs

**Create file: `Dtos/ProductListItemDto.cs`**

```csharp
namespace OnlineStore.API.Dtos;

public record ProductListItemDto(int Id, string Name, decimal Price, int Stock, bool IsActive);
```

**Create file: `Dtos/ProductDetailDto.cs`**

```csharp
namespace OnlineStore.API.Dtos;

public record ProductDetailDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId,
    int BrandId,
    bool IsActive,
    DateTime CreatedAt);
```

**Create file: `Dtos/CreateProductRequest.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record CreateProductRequest(
    [Required, StringLength(100)]
    string Name,

    string Description,

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    decimal Price,

    [Range(0, int.MaxValue, ErrorMessage = "Stock must be 0 or greater.")]
    int Stock,

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId is required.")]
    int CategoryId,

    [Range(1, int.MaxValue, ErrorMessage = "BrandId is required.")]
    int BrandId
);
```

**Create file: `Dtos/UpdateProductRequest.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record UpdateProductRequest(
    [Required, StringLength(100)]
    string Name,

    string Description,

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    decimal Price,

    [Range(0, int.MaxValue, ErrorMessage = "Stock must be 0 or greater.")]
    int Stock,

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId is required.")]
    int CategoryId,

    [Range(1, int.MaxValue, ErrorMessage = "BrandId is required.")]
    int BrandId,

    bool IsActive
);
```

> `Description` intentionally has no `[Required]` — it is optional per intake. Annotation failures produce the **400** per-field response automatically via `[ApiController]`; the "reference an existing category/brand" check is a runtime DB check (task 5, POST/PUT), not an annotation.

### 5 — Create `Controllers/ProductsController.cs`

**Create file: `Controllers/ProductsController.cs`**

- Namespace `OnlineStore.API.Controllers`; `[ApiController]`, `[Route("api/products")]`, inherit `ControllerBase`.
- Constructor-inject `AppDbContext _db` (mirror `AuthController` lines 16–23).
- Usings: `using Microsoft.AspNetCore.Authorization;`, `using Microsoft.AspNetCore.Mvc;`, `using Microsoft.EntityFrameworkCore;`, `using OnlineStore.API.Data;`, `using OnlineStore.API.Dtos;`, `using OnlineStore.API.Entities;`.
- **Controller-level:** `[Authorize(Roles = "admin")]`, then mark the two GET actions `[AllowAnonymous]` (per Story 03's recommendation for a controller mixing public reads and admin writes).

**Actions:**

#### `GET /api/products` — `[AllowAnonymous] [HttpGet]`

Signature: `public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)`

1. Clamp paging: `page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 100);`
2. Start `IQueryable<Product> query = _db.Products;`
3. **Active-only unless admin:** `if (!User.IsInRole("admin")) query = query.Where(p => p.IsActive);`
4. **Search by name (case-insensitive):** `if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));` (`ILike` is Npgsql-specific and case-insensitive.)
5. `var total = await query.CountAsync();`
6. Page + project (newest first): `.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(p => new ProductListItemDto(p.Id, p.Name, p.Price, p.Stock, p.IsActive)).ToListAsync();`
7. Return `Ok(new { items, page, pageSize, total });`

#### `GET /api/products/{id}` — `[AllowAnonymous] [HttpGet("{id:int}")]`

1. `var p = await _db.Products.FindAsync(id);`
2. If `null` → `return NotFound(new { message = "Product not found" });`
3. **Active-only unless admin:** if `!p.IsActive && !User.IsInRole("admin")` → also `NotFound(new { message = "Product not found" })` (don't leak inactive products to the public).
4. Return `Ok(ToDetail(p));` (see private mapper below).

#### `POST /api/products` — `[HttpPost]` (admin only via controller attribute)

1. Wrap in try/catch → `StatusCode(500, new { message = "Something went wrong" })`.
2. **FK validation:** `if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId)) return BadRequest(new { message = "Category not found" });` and the same for `Brands`/`BrandId`.
3. Build entity — **server owns `IsActive`/`CreatedAt`** (leave to entity defaults; do not read from client):

```csharp
var product = new Product
{
    Name = request.Name,
    Description = request.Description,
    Price = request.Price,
    Stock = request.Stock,
    CategoryId = request.CategoryId,
    BrandId = request.BrandId
};
_db.Products.Add(product);
await _db.SaveChangesAsync();
```

4. Return **201** pointing at the detail route:
   `return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDetail(product));`
   *(Ensure the `GET /{id}` action method is named `GetById` to match `nameof`.)*

#### `PUT /api/products/{id}` — `[HttpPut("{id:int}")]` (admin only)

1. try/catch → 500. `var product = await _db.Products.FindAsync(id);` if `null` → **404** `"Product not found"`.
2. FK validation for `CategoryId`/`BrandId` (same as POST) → **400** if missing.
3. Update `Name`, `Description`, `Price`, `Stock`, `CategoryId`, `BrandId`, `IsActive`. **Do not touch `CreatedAt`.**
4. `await _db.SaveChangesAsync();` return `Ok(ToDetail(product));`

#### `DELETE /api/products/{id}` — `[HttpDelete("{id:int}")]` (admin only)

1. try/catch → 500. `var product = await _db.Products.FindAsync(id);` if `null` → **404** `"Product not found"`.
2. `_db.Products.Remove(product); await _db.SaveChangesAsync();` return `NoContent();` (**204**).

#### Private mapper

```csharp
private static ProductDetailDto ToDetail(Product p) =>
    new(p.Id, p.Name, p.Description, p.Price, p.Stock, p.CategoryId, p.BrandId, p.IsActive, p.CreatedAt);
```

> **Never return the `Product` entity directly** — always project to a DTO (acceptance requirement).

### 6 — Generate and apply the EF migration (+ minimal seed for FK validation)

From `OnlineStore.API/`:

```powershell
dotnet ef migrations add AddCategoryBrandAndProductFks
dotnet ef database update
```

Inspect the generated `Migrations/*_AddCategoryBrandAndProductFks.cs`: it should `CreateTable("Categories", …)` and `CreateTable("Brands", …)` (each `Id` identity + `Name text`), `AddColumn<int>("CategoryId"/"BrandId", "Products")`, plus `CreateIndex` + `AddForeignKey` with `onDelete: ReferentialAction.Restrict`. **Do not hand-write it.** If `dotnet ef` is missing: `dotnet tool install --global dotnet-ef`.

> **Existing-rows caveat:** if the `Products` table already has rows, adding non-nullable `CategoryId`/`BrandId` FK columns will fail or orphan them. Since this is early-stage dev, the simplest path is to ensure at least one `Category` and one `Brand` row exist and that any existing products point at valid ids. Seed one of each for verification:
> ```sql
> INSERT INTO "Categories" ("Name") VALUES ('General');
> INSERT INTO "Brands" ("Name") VALUES ('Generic');
> ```
> If products already exist, either truncate them or backfill their `CategoryId`/`BrandId` to the seeded ids before `database update`.

### 7 — Apply the authorization matrix

Verify against Story 03's matrix: GET list + GET by id are `[AllowAnonymous]`; POST/PUT/DELETE inherit `[Authorize(Roles = "admin")]` from the controller. `login`/`register` remain public and untouched. **No `Program.cs` change** — the pipeline is already correct.

---

## Edge Cases & Failure Modes

- **Non-existent CategoryId/BrandId on POST/PUT:** annotation `[Range(1, …)]` only rejects `<= 0`; a value like `999` passes annotations but has no row. The runtime `AnyAsync` check (task 5) returns **400** `"Category not found"` / `"Brand not found"`. Without it, `SaveChangesAsync` throws a `DbUpdateException` (FK violation) → caught by the generic catch → **500**, which is the wrong status. The explicit check is **required**.
- **Inactive product visibility:** anonymous/customer callers must not see `IsActive = false` products in either the list or by-id (`GET /{id}` returns 404 for them). Admins (valid token with `role=admin`) see all. Enforced in tasks 5 (list `Where` + by-id guard) via `User.IsInRole("admin")`.
- **`GET` with a token but non-admin role:** the GET actions are `[AllowAnonymous]`, so a customer token is fine — but `User.IsInRole("admin")` is `false`, so they correctly get active-only. A malformed/expired token on a public GET is ignored (anonymous), not a 401.
- **Paging bounds:** `page <= 0` or `pageSize <= 0`/huge values are clamped (`page>=1`, `pageSize` in `[1,100]`) to avoid negative `Skip` and unbounded reads.
- **Search injection / wildcards:** `EF.Functions.ILike` is parameterized (no SQL injection). Note user-supplied `%`/`_` act as wildcards — acceptable for a simple search; flag if literal matching is later required.
- **`decimal` precision:** `Price` maps to Postgres `numeric` (unbounded) per `InitialCreate`. No precision loss; no explicit `HasPrecision` needed unless a fixed scale is later required (**uncertainty flagged** — intake doesn't specify).
- **Admin promotion required to test writes:** `register` always creates `role=customer` (Story 03). Promote via SQL and **re-login** to mint a token carrying `role=admin` (old tokens keep the old role until expiry — 7 days).
- **Restrict delete on referenced Category/Brand:** `DeleteBehavior.Restrict` means deleting a category/brand still referenced by products throws — intentional. Category/Brand deletion endpoints are out of scope anyway.
- **`CreatedAt` immutability on PUT:** the update must not overwrite `CreatedAt`; only the six mutable fields + `IsActive` change.

---

## Test Plan

No test project exists yet (noted in Stories 02–03). If/when one is added (e.g. `OnlineStore.API.Tests` with `WebApplicationFactory`), add:

1. **List — public sees only active** (`ProductsListTests`): seed active + inactive products, `GET /api/products` with no token → response `items` contains only active; `total` matches active count.
2. **List — admin sees all:** same seed, admin Bearer token → inactive products included.
3. **List — search + paging:** seed >pageSize products; `GET /api/products?search=mouse&page=1&pageSize=5` → filtered by name (case-insensitive), at most 5 items, correct `total`.
4. **Detail — 404:** `GET /api/products/999999` → **404** `"Product not found"`. Inactive product as anonymous → **404**; as admin → **200**.
5. **Create — admin 201:** admin token, valid body with seeded `CategoryId`/`BrandId` → **201**, `ProductDetailDto`, `IsActive=true`, `CreatedAt` set by server.
6. **Create — 400 on bad FK:** admin token, `CategoryId=999` → **400** `"Category not found"`.
7. **Create — 400 on validation:** admin token, empty `Name` / `Price=0` / negative `Stock` → **400** per-field errors.
8. **Create — 401/403:** no token → **401**; customer token → **403**.
9. **Update — admin 200, CreatedAt unchanged:** capture `CreatedAt`, PUT changes, assert body updated and `CreatedAt` equal.
10. **Delete — admin 204 then 404:** DELETE existing → **204**; GET same id → **404**. DELETE missing id → **404**.

Until a test project exists, the manual matrix in **Verification Steps** is the acceptance gate — record results in the PR. Reference existing manual-verification style in [`../auth/01-user-registration-api.md`](../auth/01-user-registration-api.md).

---

## Migration / Rollback

- **Forward:** `dotnet ef migrations add AddCategoryBrandAndProductFks` → `dotnet ef database update`. Creates `Categories`, `Brands`, adds `CategoryId`/`BrandId` (+ indexes + FKs) to `Products`.
- **Half-applied risk:** if `Products` has pre-existing rows, `database update` fails when adding non-nullable FK columns with no default. Mitigate by seeding a `Category`/`Brand` and backfilling (or truncating `Products`) **before** update (task 6 caveat).
- **Rollback:** `dotnet ef database update <PreviousMigrationName>` (the `AddUser` migration `20260712214535_AddUser`) reverts, then `dotnet ef migrations remove` drops the migration files. Rolling back drops the new columns/tables — back up any seeded catalog data first.

---

## Verification Steps

1. **Backend builds:** `dotnet build` in `OnlineStore.API/` — succeeds, no warnings.
2. **Migration applied:** `dotnet ef database update` completes; `Categories`, `Brands` tables and `Products.CategoryId`/`BrandId` FK columns exist.
3. **Seed reference data:** insert one `Category` ("General") and one `Brand` ("Generic"); note their ids.
4. **Backend runs:** `dotnet run --launch-profile http`, open `http://localhost:5016/swagger`.
5. **Public list (no token):** `GET /api/products` → **200**, `{ items, page, pageSize, total }`; only active products shown.
6. **Search + paging:** `GET /api/products?search=mou&page=1&pageSize=5` → filtered, ≤5 items.
7. **Public writes blocked:** `POST /api/products` with no token → **401**.
8. **Admin path:** promote a user — `UPDATE "Users" SET "Role"='admin' WHERE "Email"='<you>';` — log in **again**, click Swagger **Authorize**, paste the new token.
9. **Create (201):** `POST /api/products` `{ "name":"Wireless Mouse","description":"Ergo","price":19.99,"stock":120,"categoryId":<id>,"brandId":<id> }` → **201**, body includes server-set `isActive:true` + `createdAt`.
10. **Create bad FK (400):** same body with `categoryId:999` → **400** `"Category not found"`.
11. **Customer forbidden (403):** with a **customer** token, `POST /api/products` → **403**.
12. **Get by id (200/404):** `GET /api/products/{createdId}` → **200** detail; `GET /api/products/999999` → **404** `"Product not found"`.
13. **Update (200):** `PUT /api/products/{id}` changing price/stock → **200**, `createdAt` unchanged.
14. **Delete (204):** `DELETE /api/products/{id}` → **204**; repeat → **404**.
15. **CORS:** preflight from `http://localhost:3000` to `/api/products` passes (existing `"Frontend"` policy).

---

## Done Criteria

- [ ] `Category` and `Brand` entities exist; `Product` has `CategoryId`/`BrandId` FKs; one migration creates the tables + FK columns (acceptance: products reference category/brand).
- [ ] `GET /api/products` returns a paginated, name-searchable list; anonymous/customer see **active only**, admin sees all.
- [ ] `GET /api/products/{id}` returns detail or **404** `"Product not found"` (and 404 for inactive to non-admins).
- [ ] `POST /api/products` with an **admin** token creates a product (**201**, server-set `IsActive`/`CreatedAt`), returns `ProductDetailDto`.
- [ ] `POST`/`PUT` return **400** `"Category not found"`/`"Brand not found"` for non-existent FKs, and per-field **400** for invalid input.
- [ ] `PUT /api/products/{id}` updates (admin, **200**) without changing `CreatedAt`; **404** for missing id.
- [ ] `DELETE /api/products/{id}` hard-deletes (admin, **204**); **404** for missing id.
- [ ] Admin-only actions return **401** without a token and **403** with a customer token.
- [ ] Endpoints never expose the raw `Product` entity — always a DTO.
- [ ] `dotnet build` succeeds; migration applies cleanly.

---

**STOP HERE. Report to the user and wait for confirmation before proceeding to any follow-up story (e.g. Category/Brand CRUD or Cart).**
