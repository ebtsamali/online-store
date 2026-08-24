# Story 08 — Categories and Brands CRUD

Add full CRUD for the `Category` and `Brand` reference entities (`GET`/`POST`/`PUT`/`DELETE` under `/api/categories` and `/api/brands`), mirroring `ProductsController`'s conventions exactly, plus optional `categoryId`/`brandId` filters on the existing `GET /api/products` list endpoint.

---

## Prerequisites

- **Story 04 completed** ([`../products/04-story-products-crud.md`](../products/04-story-products-crud.md)): introduced the `Category`/`Brand` entities, their `DbSet`s, and the `Product.CategoryId`/`Product.BrandId` FKs with `DeleteBehavior.Restrict` (verified in [`Data/AppDbContext.cs`](../../../Data/AppDbContext.cs) lines 27–37). This story adds the missing CRUD surface for those two entities — no new tables, no new FK columns.
- **Story 03 completed** ([`../middleware/03-authorization-middleware.md`](../middleware/03-authorization-middleware.md)): JWT auth + `AddAuthorization()` already wired in [`Program.cs`](../../../Program.cs) (`AddAuthentication().AddJwtBearer` lines 54–68, `AddAuthorization()` line 70, `UseAuthentication()`→`UseAuthorization()`→`MapControllers()` lines 91–95). **Do not** touch auth wiring — only apply `[Authorize]`/`[AllowAnonymous]` attributes on the new controllers.
- **Admin promotion still required to test writes** (from Story 03/04): `AuthController.Register` hardcodes `Role = "customer"` — promote a user via SQL and re-login to mint an admin token before testing POST/PUT/DELETE.
- **Precedent for tone/structure:** [`../products/04-story-products-crud.md`](../products/04-story-products-crud.md) (entity/DTO/controller pattern for this exact area) and [`Controllers/ProductsController.cs`](../../../Controllers/ProductsController.cs) (the controller this story mirrors line-for-line for the CRUD shape, try/catch, and status codes).

---

## Story Goal

1. `GET /api/categories` — **public**, returns all categories `{ id, name }[]` ordered by name.
2. `POST /api/categories` — **admin only**, body `{ name }`, returns **201** with the created DTO; **400** on empty or duplicate (case-insensitive) name.
3. `PUT /api/categories/{id}` — **admin only**, body `{ name }`, returns **200** on success; **404** if missing; **400** on empty or duplicate name.
4. `DELETE /api/categories/{id}` — **admin only**, returns **204** on success; **404** if missing; **409** if any `Product` still references the category (never an unhandled 500 from the FK `Restrict` constraint).
5. The same four endpoints, identical shape and rules, for `Brand` under `/api/brands`.
6. `GET /api/products` gains optional `categoryId`/`brandId` query parameters that filter the existing paged query, combinable with the existing `search` parameter. Omitting both params leaves current behaviour unchanged.
7. All new endpoints project to DTOs only — the `Category`/`Brand` EF entities are never returned directly.

**Not in scope:** pagination on the categories/brands list endpoints (small reference lists — return all); image/icon upload for categories or brands; changes to the Product create/update forms beyond the two new optional filter params on the list endpoint; soft-delete (deletion stays a hard delete, blocked by the existing `Restrict` FK and surfaced as a **409**, not a 500).

---

## Context — Read These Files First

1. [`Controllers/ProductsController.cs`](../../../Controllers/ProductsController.cs) — 240 lines. This is the controller shape to mirror exactly: `[ApiController]`, `[Route("api/products")]`, controller-level `[Authorize(Roles = "admin")]` (line 13) with `[AllowAnonymous]` on the GET actions (lines 25, 67), constructor-injected `AppDbContext _db` (lines 16–23), every action wrapped in `try { … } catch { return StatusCode(500, new { message = "Something went wrong" }); }` (e.g. lines 32–64, 97–143). The `List` action (lines 27–65) is what you extend with `categoryId`/`brandId` filters — note the existing `search` filter pattern at line 47 (`EF.Functions.ILike`) and the paging/projection block at lines 50–57.
2. [`Entities/Category.cs`](../../../Entities/Category.cs) — 8 lines. `Id` (`int`) and `Name` (`string`, `= string.Empty`). No other fields; nothing to add here.
3. [`Entities/Brand.cs`](../../../Entities/Brand.cs) — 8 lines. Identical shape to `Category`.
4. [`Data/AppDbContext.cs`](../../../Data/AppDbContext.cs) — 71 lines. `Categories`/`Brands` `DbSet`s already registered (lines 14–15). The `Restrict` FK relationships are configured at lines 27–37 (`Product.CategoryId`/`Product.BrandId` → `Categories`/`Brands`, `OnDelete(DeleteBehavior.Restrict)`). **Do not** change these relationships — the delete-blocked behaviour is intentional and this story must surface it as a clean 409, not remove it.
5. [`Entities/Product.cs`](../../../Entities/Product.cs) — 19 lines. `CategoryId` (line 14) and `BrandId` (line 15) are plain `int` scalars (not nullable) — every product has both, so the new list filters compare directly against these columns with no null handling needed.
6. [`Dtos/ProductListItemDto.cs`](../../../Dtos/ProductListItemDto.cs) — 1 line: `record ProductListItemDto(int Id, string Name, decimal Price, int Stock, bool IsActive, string ImageUrl)`. Unchanged by this story — the new query params only affect which rows are selected, not the projected shape.
7. [`Dtos/RegisterRequest.cs`](../../../Dtos/RegisterRequest.cs) — 16 lines. DTO style to mirror for the new `Category`/`Brand` request DTOs: `record` with **plain** DataAnnotations on positional params (`[Required, StringLength(50, MinimumLength = 3)]` at lines 6–7); `[ApiController]` auto-returns a **400** `ValidationProblemDetails` on annotation failure — do **not** use the `[property:]` attribute form.
8. [`../products/04-story-products-crud.md`](../products/04-story-products-crud.md) — read in full. This is the sibling story that created `Category`/`Brand`/the FK relationships and the `ProductsController` conventions this story reuses verbatim (DTO-only projections, controller-level `[Authorize(Roles="admin")]` + per-action `[AllowAnonymous]`, the `StatusCode(500, new { message = "Something went wrong" })` catch-all, and the FK-existence check pattern at its task 5 `POST`/`PUT` — `if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId)) return BadRequest(...)`).
9. [`Program.cs`](../../../Program.cs) — CORS `"Frontend"` policy for `http://localhost:3000` already registered (lines 42–48, `UseCors` line 89). No change needed for this story.

---

## Backend Tasks

### 1 — Create the Category DTOs

**Create file: `Dtos/CategoryDto.cs`**

```csharp
namespace OnlineStore.API.Dtos;

public record CategoryDto(int Id, string Name);
```

**Create file: `Dtos/CategoryRequest.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record CategoryRequest(
    [Required, StringLength(100, MinimumLength = 1)]
    string Name
);
```

Used for both create and update — the shape is identical (`{ name }`) per the intake.

### 2 — Create the Brand DTOs

**Create file: `Dtos/BrandDto.cs`**

```csharp
namespace OnlineStore.API.Dtos;

public record BrandDto(int Id, string Name);
```

**Create file: `Dtos/BrandRequest.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record BrandRequest(
    [Required, StringLength(100, MinimumLength = 1)]
    string Name
);
```

### 3 — Create `Controllers/CategoriesController.cs`

**Create file: `Controllers/CategoriesController.cs`**

Mirror [`Controllers/ProductsController.cs`](../../../Controllers/ProductsController.cs) exactly: `[ApiController]`, `[Route("api/categories")]`, controller-level `[Authorize(Roles = "admin")]`, constructor-injected `AppDbContext _db` only (no `IImageStorageService` — that dependency is product-image-specific and unrelated here).

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.API.Data;
using OnlineStore.API.Dtos;
using OnlineStore.API.Entities;

namespace OnlineStore.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize(Roles = "admin")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> List()
    {
        try
        {
            var items = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto(c.Id, c.Name))
                .ToListAsync();

            return Ok(items);
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryRequest request)
    {
        try
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Name is required" });
            }

            if (await _db.Categories.AnyAsync(c => EF.Functions.ILike(c.Name, name)))
            {
                return BadRequest(new { message = "A category with this name already exists" });
            }

            var category = new Category { Name = name };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(List), new CategoryDto(category.Id, category.Name));
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
    {
        try
        {
            var category = await _db.Categories.FindAsync(id);
            if (category is null)
            {
                return NotFound(new { message = "Category not found" });
            }

            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Name is required" });
            }

            if (await _db.Categories.AnyAsync(c => c.Id != id && EF.Functions.ILike(c.Name, name)))
            {
                return BadRequest(new { message = "A category with this name already exists" });
            }

            category.Name = name;
            await _db.SaveChangesAsync();

            return Ok(new CategoryDto(category.Id, category.Name));
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var category = await _db.Categories.FindAsync(id);
            if (category is null)
            {
                return NotFound(new { message = "Category not found" });
            }

            // Pre-check the Restrict FK instead of letting SaveChangesAsync throw a
            // DbUpdateException — turns an unhandled 500 into a clean 409.
            if (await _db.Products.AnyAsync(p => p.CategoryId == id))
            {
                return Conflict(new { message = "Cannot delete category: it is still used by one or more products" });
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }
}
```

> `CreatedAtAction(nameof(List), …)` follows the same "point at a GET action" pattern as `ProductsController.Create` (`CreatedAtAction(nameof(GetById), …)`); unlike products there is no `GetById` action for categories, so it points at `List` — the route value argument is ignored by `List` (it takes no parameters) but the **201** status and `Location` header behaviour are preserved, which is what the acceptance criteria require.

### 4 — Create `Controllers/BrandsController.cs`

**Create file: `Controllers/BrandsController.cs`**

Identical structure to `CategoriesController` from task 3, with every `Category`/`Categories`/`categories` swapped for `Brand`/`Brands`/`brands`, and the delete pre-check querying `_db.Products.AnyAsync(p => p.BrandId == id)`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.API.Data;
using OnlineStore.API.Dtos;
using OnlineStore.API.Entities;

namespace OnlineStore.API.Controllers;

[ApiController]
[Route("api/brands")]
[Authorize(Roles = "admin")]
public class BrandsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BrandsController(AppDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> List()
    {
        try
        {
            var items = await _db.Brands
                .OrderBy(b => b.Name)
                .Select(b => new BrandDto(b.Id, b.Name))
                .ToListAsync();

            return Ok(items);
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BrandRequest request)
    {
        try
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Name is required" });
            }

            if (await _db.Brands.AnyAsync(b => EF.Functions.ILike(b.Name, name)))
            {
                return BadRequest(new { message = "A brand with this name already exists" });
            }

            var brand = new Brand { Name = name };
            _db.Brands.Add(brand);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(List), new BrandDto(brand.Id, brand.Name));
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] BrandRequest request)
    {
        try
        {
            var brand = await _db.Brands.FindAsync(id);
            if (brand is null)
            {
                return NotFound(new { message = "Brand not found" });
            }

            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Name is required" });
            }

            if (await _db.Brands.AnyAsync(b => b.Id != id && EF.Functions.ILike(b.Name, name)))
            {
                return BadRequest(new { message = "A brand with this name already exists" });
            }

            brand.Name = name;
            await _db.SaveChangesAsync();

            return Ok(new BrandDto(brand.Id, brand.Name));
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var brand = await _db.Brands.FindAsync(id);
            if (brand is null)
            {
                return NotFound(new { message = "Brand not found" });
            }

            if (await _db.Products.AnyAsync(p => p.BrandId == id))
            {
                return Conflict(new { message = "Cannot delete brand: it is still used by one or more products" });
            }

            _db.Brands.Remove(brand);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }
}
```

### 5 — Add `categoryId`/`brandId` filters to `GET /api/products`

**File: `Controllers/ProductsController.cs`**

Extend the `List` action signature (currently line 27–30) with two new optional query parameters, and add two `Where` clauses to the query pipeline (currently built at lines 37–48, between the existing active-only filter at line 42 and the `search` filter at lines 45–48):

```csharp
public async Task<IActionResult> List(
    [FromQuery] string? search,
    [FromQuery] int? categoryId,
    [FromQuery] int? brandId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
```

```csharp
if (categoryId.HasValue)
{
    query = query.Where(p => p.CategoryId == categoryId.Value);
}

if (brandId.HasValue)
{
    query = query.Where(p => p.BrandId == brandId.Value);
}
```

Place these two blocks after the existing active-only filter (line 42: `if (!User.IsInRole("admin")) query = query.Where(p => p.IsActive);`) and before the existing `search` filter (lines 45–48), so `search`, `categoryId`, and `brandId` all combine with `AND` semantics — matching the intake's "combinable with `search`" requirement. Do not change the `total`/paging/projection block (lines 50–57) or the response shape (line 59) — only the query-building section changes.

No FK-existence validation is needed for these two params: an unknown `categoryId`/`brandId` simply yields zero matching products (correct, not an error) since they only narrow an `IQueryable<Product>` filter — unlike `POST`/`PUT`, which write a FK and must reject invalid references up front.

---

## Edge Cases & Failure Modes

- **Empty/whitespace name on create or update:** `[Required, StringLength(100, MinimumLength = 1)]` on `CategoryRequest.Name`/`BrandRequest.Name` rejects a `null` or missing field via `[ApiController]`'s automatic `ValidationProblemDetails` **400**, but a value of `"   "` passes the annotation (it is non-null, non-empty). The explicit `string.IsNullOrWhiteSpace(name)` check after `Trim()` in both `Create` and `Update` (tasks 3–4) catches this and returns **400** `"Name is required"`.
- **Case-insensitive duplicate name:** `"Electronics"` and `"electronics"` must be treated as the same name. `EF.Functions.ILike(c.Name, name)` (no `%` wildcards) does an exact case-insensitive match in Postgres — used identically to the existing wildcard search in `ProductsController.List` line 47, but without wildcards here since this is an equality check, not a substring search.
- **Duplicate check on update must exclude the row being updated:** without the `c.Id != id` guard in `Update`, renaming "Electronics" to itself (unchanged case) would incorrectly report a duplicate. Enforced in task 3/4 `Update` via `c.Id != id && EF.Functions.ILike(...)`.
- **Delete blocked by referencing products:** `Data/AppDbContext.cs` lines 27–37 configure `Product.CategoryId`/`BrandId` with `DeleteBehavior.Restrict`, so deleting a still-referenced `Category`/`Brand` would otherwise throw a `DbUpdateException` on `SaveChangesAsync`, caught by the generic `catch` and surfaced as an unhelpful **500**. The pre-check `_db.Products.AnyAsync(p => p.CategoryId == id)` / `p.BrandId == id` in `Delete` (tasks 3–4) avoids the exception entirely and returns a clean **409** with a descriptive message, per the acceptance criteria ("409/400, not 500").
- **Delete/update of a non-existent id:** `FindAsync(id)` returns `null` for both `Category` and `Brand`; both actions return **404** before touching `SaveChangesAsync`.
- **`categoryId=0` or a negative/non-existent id on `GET /api/products`:** no product has `CategoryId <= 0` (the column is a non-nullable identity FK starting at 1 — see `Entities/Product.cs` line 14), so the filter simply yields an empty `items` array with `total: 0` — not an error. This is intentional: the filter narrows an existing query rather than validating a reference.
- **Both `categoryId`/`brandId` omitted:** the two new `if (x.HasValue)` blocks in task 5 are no-ops, so `GET /api/products` behaves exactly as before this story — required by the acceptance criteria ("no change to existing behavior when the params are omitted").
- **Non-admin/anonymous GET with a malformed or expired token:** `[AllowAnonymous]` on `List` in both new controllers means the request still succeeds; ASP.NET Core simply treats the caller as unauthenticated rather than returning 401, matching the existing `ProductsController` GET behaviour.

---

## Test Plan

No test project exists yet in this repo (same gap noted in `../products/04-story-products-crud.md`'s Test Plan). If/when one is added (e.g. `OnlineStore.API.Tests` with `WebApplicationFactory`), add:

1. **Categories — list is public and ordered:** `CategoriesListTests` — seed `Category` rows out of alphabetical order, `GET /api/categories` with no token → **200**, array ordered by `Name`.
2. **Categories — list on empty table:** no rows seeded → `GET /api/categories` → **200**, `[]` (not an error).
3. **Categories — create (201):** admin token, `POST /api/categories { "name": "Electronics" }` → **201**, body `{ id, name: "Electronics" }`.
4. **Categories — create rejects empty/duplicate name:** admin token, `{ "name": "" }` → **400**; `{ "name": "electronics" }` when `"Electronics"` already exists → **400** (case-insensitive).
5. **Categories — create is admin-only:** no token → **401**; customer token → **403**.
6. **Categories — update (200) / 404 / 400 duplicate:** admin token, `PUT /api/categories/{id} { "name": "New Name" }` → **200**; missing id → **404**; renaming to another existing category's name (any case) → **400**.
7. **Categories — delete blocked by FK (409):** seed a `Product` referencing the category, `DELETE /api/categories/{id}` as admin → **409**, no exception surfaced. Delete an unreferenced category → **204**; deleting again → **404**.
8. **Brands — mirror tests 1–7** for `/api/brands` (`BrandsListTests`/equivalent), asserting the identical status codes and message shapes.
9. **Products — filter by categoryId/brandId:** seed products across two categories/brands, `GET /api/products?categoryId={id}` → only matching products; `?brandId={id}` → only matching products; both together → intersection; combined with `?search=` → intersection of all three; no params → unchanged existing behaviour (same result as before this story).
10. **Products — filter with a non-existent id:** `GET /api/products?categoryId=999999` → **200**, `items: []`, `total: 0` (not an error).

Until a test project exists, the manual matrix in **Verification Steps** is the acceptance gate — record results in the PR, matching the manual-verification style already used in `../products/04-story-products-crud.md`.

---

## Verification Steps

1. **Backend builds:** `dotnet build` in `OnlineStore.API/` — succeeds, no new warnings.
2. **Backend runs:** `dotnet run --launch-profile http` in `OnlineStore.API/`, open `http://localhost:5016/swagger`.
3. **Public categories list:** `GET /api/categories` with no token → **200**, `[]` or existing seeded categories ordered by name.
4. **Public brands list:** `GET /api/brands` with no token → **200**.
5. **Writes blocked without a token:** `POST /api/categories { "name": "Electronics" }` with no token → **401**. Same for `/api/brands`.
6. **Admin path:** promote a user — `UPDATE "Users" SET "Role"='admin' WHERE "Email"='<you>';` — log in again, click Swagger **Authorize**, paste the new token.
7. **Create category (201):** admin token, `POST /api/categories { "name": "Electronics" }` → **201** with `{ id, name }`.
8. **Duplicate name (400):** admin token, `POST /api/categories { "name": "electronics" }` (different case) → **400**.
9. **Update category (200):** admin token, `PUT /api/categories/{id} { "name": "Consumer Electronics" }` → **200**.
10. **Delete blocked (409):** create a product referencing the category (or reuse a seeded one from `../products/04-story-products-crud.md` verification), then `DELETE /api/categories/{id}` as admin → **409**, not 500.
11. **Delete succeeds (204):** delete an unreferenced category as admin → **204**; repeat → **404**.
12. **Repeat steps 6–11 for `/api/brands`** with the equivalent brand payloads.
13. **Products filter — categoryId:** `GET /api/products?categoryId={id}` → **200**, only products in that category.
14. **Products filter — brandId:** `GET /api/products?brandId={id}` → **200**, only products of that brand.
15. **Products filter — combined with search:** `GET /api/products?search=mou&categoryId={id}` → results satisfy both filters.
16. **Products filter — no params:** `GET /api/products` (no `categoryId`/`brandId`) → identical response shape/behaviour to before this story.
17. **CORS:** preflight from `http://localhost:3000` to `/api/categories` and `/api/brands` passes (existing `"Frontend"` policy, unchanged).

---

## Done Criteria

- [ ] `GET /api/categories` — public, returns all categories `{ id, name }[]` ordered by name.
- [ ] `POST /api/categories` — admin only, **201** with created DTO; **400** on empty or duplicate (case-insensitive) name.
- [ ] `PUT /api/categories/{id}` — admin only, **200** on success; **404** if missing; **400** on empty or duplicate name.
- [ ] `DELETE /api/categories/{id}` — admin only, **204** on success; **404** if missing; **409** (never 500) if any `Product` still references it.
- [ ] The same four behaviours mirrored exactly for `Brand` under `/api/brands`.
- [ ] `GET /api/products` accepts optional `categoryId`/`brandId` query params, combinable with `search`; omitting both leaves existing behaviour unchanged.
- [ ] Every new endpoint returns a DTO (`CategoryDto`/`BrandDto`) — the `Category`/`Brand` EF entities are never serialized directly.
- [ ] Write endpoints on both new controllers carry `[Authorize(Roles = "admin")]` (via the controller-level attribute); their GET/list actions are `[AllowAnonymous]`.
- [ ] `dotnet build` succeeds with no new warnings; no EF migration is needed (no schema change).

---

**STOP HERE. Report to the user and wait for confirmation before proceeding to any follow-up story.**
