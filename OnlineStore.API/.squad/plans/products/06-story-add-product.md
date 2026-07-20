# Story 06 — Add Product with Image (Single Multipart Endpoint)

Extend the existing Products API so an admin creates a product **and** uploads its image in **one** `multipart/form-data` request. The image file is written to disk **only** when the product row is successfully saved — never before, and rolled back (file deleted) if the DB save fails — so the server never accumulates **orphaned images**. `GET`, `GET /{id}`, and `PUT` all surface the image via a new `Product.ImageUrl`.

---

## Prerequisites

- **Story 04 completed** ([`04-story-products-crud.md`](04-story-products-crud.md)): `Controllers/ProductsController.cs` (`[Route("api/products")]`, `[Authorize(Roles = "admin")]` at controller level, `[AllowAnonymous]` GETs), the `Product` entity, and DTOs `ProductDetailDto`/`ProductListItemDto`/`CreateProductRequest`/`UpdateProductRequest` all exist (verified this run). This story **modifies** them; it does not recreate them.
- **Story 03 completed** ([`../middleware/03-authorization-middleware.md`](../middleware/03-authorization-middleware.md)): JWT + `[Authorize(Roles = "admin")]` pipeline already wired in [`Program.cs`](../../../Program.cs) (verified: `UseAuthentication()`→`UseAuthorization()`→`MapControllers()` lines 85–89). **Do not** touch auth wiring.
- **Admin promotion still required to test writes** (from Story 03/04): `AuthController.Register` hardcodes `Role = "customer"`; promote a user via SQL and **re-login** to mint an admin token.
- **No new NuGet packages.** `UseStaticFiles` and `IFormFile` are in-box (`Microsoft.NET.Sdk.Web`); `IWebHostEnvironment` is already available. `Swashbuckle.AspNetCore` 10.2.3 renders `multipart/form-data` for `IFormFile` params (verified [`OnlineStore.API.csproj`](../../../OnlineStore.API.csproj) lines 10–24). EF `Design`/`Tools` 10.0.9 present for the migration.
- **Precedent for tone/structure:** [`04-story-products-crud.md`](04-story-products-crud.md) (this same controller/DTO/migration area); [`Services/TokenService.cs`](../../../Services/TokenService.cs) is the injected-service pattern to mirror for the new image-storage service.

---

## Story Goal

1. `POST /api/products` — **admin only**, changes from a JSON body to **`multipart/form-data`**: accepts product fields **plus** a required image file, validates everything, writes the file, creates the product with `ImageUrl` set, and returns **201** `ProductDetailDto` (now including `imageUrl`).
2. **Anti-orphan guarantee:** the image is persisted to disk only in the same operation that saves the product; if `SaveChangesAsync` throws, the just-written file is deleted (rollback).
3. `PUT /api/products/{id}` — **admin only**, also becomes `multipart/form-data` with an **optional** replacement image: if a file is supplied, validate + save it, repoint `ImageUrl`, and delete the previous file; if omitted, keep the existing image.
4. `GET /api/products` and `GET /api/products/{id}` return `imageUrl` in their DTOs.
5. Static file serving enabled so `/images/products/{file}` is publicly reachable.

**Not in scope:** multiple images per product (single `ImageUrl` only — no `ProductImage` table); image resizing/thumbnails/format conversion; cloud/object storage (local `wwwroot` only); CDN/signed URLs; changing GET filtering/paging or auth wiring.

> **Note on intake drift:** the intake's *Dependencies / Extra notes / Out of scope* sections still mention a multi-image `ProductImage` table + `IsPrimary`. Those are **superseded** by the intake **Description**, which specifies a **single** `ImageUrl` field on `Product`. This plan follows the Description. No `ProductImage` table, no new `DbSet`.

---

## Context — Read These Files First

1. [`Entities/Product.cs`](../../../Entities/Product.cs) — 18 lines. Fields end at `CreatedAt` (line 11), then FKs (`CategoryId`/`BrandId`) + nullable navs (lines 13–17). **Add `ImageUrl` here** after line 11. Mirror the `string … = string.Empty;` style.
2. [`Controllers/ProductsController.cs`](../../../Controllers/ProductsController.cs) — 191 lines. Key regions to edit:
   - `List` projection `new ProductListItemDto(...)` — **line 53**.
   - `Create` action — **lines 90–125** (currently `Create(CreateProductRequest request)`, FK checks lines 95–103, entity build lines 106–114, `SaveChangesAsync` line 117, `CreatedAtAction` line 119).
   - `Update` action — **lines 127–165** (FK checks 138–146, field copy 148–154, save 157).
   - `Delete` action — **lines 167–187** (`Remove` + save 178–179).
   - `ToDetail` mapper — **lines 189–190**.
   - Constructor injects only `AppDbContext _db` (lines 15–20) — you will add the image service + web-host env.
3. [`Dtos/ProductDetailDto.cs`](../../../Dtos/ProductDetailDto.cs) — positional `record`, 9 params ending `DateTime CreatedAt` (line 11). Add `string ImageUrl`.
4. [`Dtos/ProductListItemDto.cs`](../../../Dtos/ProductListItemDto.cs) — one line: `record ProductListItemDto(int Id, string Name, decimal Price, int Stock, bool IsActive)`. Add `string ImageUrl`.
5. [`Dtos/CreateProductRequest.cs`](../../../Dtos/CreateProductRequest.cs) / [`Dtos/UpdateProductRequest.cs`](../../../Dtos/UpdateProductRequest.cs) — the current JSON `record`s with DataAnnotations. They are **replaced** for the write endpoints by new `[FromForm]` request classes (task 3). You may delete them once no longer referenced (grep first).
6. [`Services/TokenService.cs`](../../../Services/TokenService.cs) — 45 lines. The `interface I…Service` + `class …Service` shape and constructor-injection style to mirror for the new `IImageStorageService` (task 2). Registered in [`Program.cs`](../../../Program.cs) line 50 (`AddScoped<ITokenService, TokenService>()`).
7. [`Program.cs`](../../../Program.cs) — pipeline lines 71–91. Insert `app.UseStaticFiles();` (task 6) and register the image service near line 50. **Do not** reorder `UseAuthentication`/`UseAuthorization`/`MapControllers`.
8. [`Migrations/20260711160101_InitialCreate.cs`](../../../Migrations/20260711160101_InitialCreate.cs) — read `Up` for the Npgsql column conventions (`string`→`type: "text"`); your `AddProductImageUrl` migration is **auto-generated** and should add one `text` column with a default. **Do not hand-write it.**

---

## Product rules (from story)

- **Current behaviour:** `POST`/`PUT /api/products` consume `application/json`; `Product` has no image; responses carry no image field.
- **New behaviour:** `POST` requires `multipart/form-data` with a **required** image; `PUT` accepts `multipart/form-data` with an **optional** image; every product row has an `ImageUrl`; all read DTOs expose it. The image lands on disk **only** alongside a successful DB write.

---

## Backend Tasks

### 1 — Add `ImageUrl` to the `Product` entity

**File: `Entities/Product.cs`** — add after `CreatedAt` (line 11):

```csharp
public string ImageUrl { get; set; } = string.Empty;
```

> Kept **non-nullable with an empty-string default** so existing rows migrate cleanly (see task 5 migration default) and new products always set it in `Create`.

### 2 — Create the image-storage service

Mirror the `ITokenService`/`TokenService` shape. This centralises validation + disk I/O so `Create` and `Update` share it.

**Create file: `Services/ImageStorageService.cs`**

```csharp
using Microsoft.AspNetCore.Http;

namespace OnlineStore.API.Services;

public interface IImageStorageService
{
    /// <summary>Validates the file; throws InvalidImageException (→ 400) on failure. No-op on success.</summary>
    void Validate(IFormFile file);

    /// <summary>Writes the file under wwwroot/images/products and returns the public relative URL (e.g. /images/products/{guid}.png).</summary>
    Task<string> SaveAsync(IFormFile file);

    /// <summary>Deletes a previously-saved file given its public relative URL. Safe to call if the file is missing.</summary>
    void Delete(string relativeUrl);
}

public class InvalidImageException : Exception
{
    public InvalidImageException(string message) : base(message) { }
}

public class ImageStorageService : IImageStorageService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
    private const string RelativeDir = "images/products";

    private readonly IWebHostEnvironment _env;

    public ImageStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public void Validate(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new InvalidImageException("Image is required.");

        if (file.Length > MaxBytes)
            throw new InvalidImageException("Image must be 5 MB or smaller.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidImageException("Image must be a .jpg, .jpeg, .png, or .webp file.");
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        // WebRootPath is null until wwwroot exists — ensure the folder.
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var targetDir = Path.Combine(webRoot, "images", "products");
        Directory.CreateDirectory(targetDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(targetDir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/{RelativeDir}/{fileName}";
    }

    public void Delete(string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return;

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }
}
```

**Register it** in [`Program.cs`](../../../Program.cs) next to the token service (after line 50):

```csharp
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
```

### 3 — Multipart request DTOs

The JSON `record`s can't bind `IFormFile`; use plain classes with `IFormFile` and the same DataAnnotations (annotations still fire on form binding, producing the automatic **400** `ValidationProblemDetails` via `[ApiController]`).

**Create file: `Dtos/CreateProductFormRequest.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace OnlineStore.API.Dtos;

public class CreateProductFormRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock must be 0 or greater.")]
    public int Stock { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId is required.")]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "BrandId is required.")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Image is required.")]
    public IFormFile Image { get; set; } = null!;
}
```

**Create file: `Dtos/UpdateProductFormRequest.cs`** — identical fields **plus** `IsActive`, but the image is **optional** (`IFormFile?`, no `[Required]`):

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace OnlineStore.API.Dtos;

public class UpdateProductFormRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock must be 0 or greater.")]
    public int Stock { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId is required.")]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "BrandId is required.")]
    public int BrandId { get; set; }

    public bool IsActive { get; set; }

    public IFormFile? Image { get; set; }
}
```

> Grep for `CreateProductRequest` / `UpdateProductRequest` after switching the controller; if unreferenced, delete `Dtos/CreateProductRequest.cs` and `Dtos/UpdateProductRequest.cs`.

### 4 — Add `ImageUrl` to the read DTOs

**File: `Dtos/ProductDetailDto.cs`** — append a param:

```csharp
public record ProductDetailDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId,
    int BrandId,
    bool IsActive,
    DateTime CreatedAt,
    string ImageUrl);
```

**File: `Dtos/ProductListItemDto.cs`**:

```csharp
public record ProductListItemDto(int Id, string Name, decimal Price, int Stock, bool IsActive, string ImageUrl);
```

### 5 — Rewire the controller

**File: `Controllers/ProductsController.cs`**

**5a — Constructor:** inject the image service alongside `_db`:

```csharp
private readonly AppDbContext _db;
private readonly IImageStorageService _images;

public ProductsController(AppDbContext db, IImageStorageService images)
{
    _db = db;
    _images = images;
}
```

Add `using OnlineStore.API.Services;`.

**5b — `List` projection (line 53):** include `ImageUrl` last:

```csharp
.Select(p => new ProductListItemDto(p.Id, p.Name, p.Price, p.Stock, p.IsActive, p.ImageUrl))
```

**5c — `ToDetail` mapper (lines 189–190):** include `ImageUrl` last:

```csharp
private static ProductDetailDto ToDetail(Product p) =>
    new(p.Id, p.Name, p.Description, p.Price, p.Stock, p.CategoryId, p.BrandId, p.IsActive, p.CreatedAt, p.ImageUrl);
```

**5d — `Create` (replace lines 90–125):**

```csharp
[HttpPost]
[Consumes("multipart/form-data")]
public async Task<IActionResult> Create([FromForm] CreateProductFormRequest request)
{
    try
    {
        try { _images.Validate(request.Image); }
        catch (InvalidImageException ex) { return BadRequest(new { message = ex.Message }); }

        if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return BadRequest(new { message = "Category not found" });

        if (!await _db.Brands.AnyAsync(b => b.Id == request.BrandId))
            return BadRequest(new { message = "Brand not found" });

        // Write the file only after all other validation passes.
        var imageUrl = await _images.SaveAsync(request.Image);

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            ImageUrl = imageUrl
        };

        _db.Products.Add(product);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            _images.Delete(imageUrl); // rollback the orphaned file
            throw;
        }

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDetail(product));
    }
    catch
    {
        return StatusCode(500, new { message = "Something went wrong" });
    }
}
```

> **Order matters:** validate image → FK checks → **save file** → save row (with file-delete rollback on failure). This is the anti-orphan guarantee from the intake.

**5e — `Update` (replace lines 127–165):** switch to `[FromForm]`; replace image only when one is supplied.

```csharp
[HttpPut("{id:int}")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> Update(int id, [FromForm] UpdateProductFormRequest request)
{
    try
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null)
            return NotFound(new { message = "Product not found" });

        if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return BadRequest(new { message = "Category not found" });

        if (!await _db.Brands.AnyAsync(b => b.Id == request.BrandId))
            return BadRequest(new { message = "Brand not found" });

        string? newImageUrl = null;
        var oldImageUrl = product.ImageUrl;

        if (request.Image is not null && request.Image.Length > 0)
        {
            try { _images.Validate(request.Image); }
            catch (InvalidImageException ex) { return BadRequest(new { message = ex.Message }); }

            newImageUrl = await _images.SaveAsync(request.Image);
            product.ImageUrl = newImageUrl;
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.IsActive = request.IsActive;
        // CreatedAt intentionally unchanged.

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            if (newImageUrl is not null) _images.Delete(newImageUrl); // rollback the new file
            throw;
        }

        // Only after a successful save, delete the replaced file.
        if (newImageUrl is not null && !string.IsNullOrEmpty(oldImageUrl))
            _images.Delete(oldImageUrl);

        return Ok(ToDetail(product));
    }
    catch
    {
        return StatusCode(500, new { message = "Something went wrong" });
    }
}
```

**5f — `Delete` (lines 167–187):** after a successful `SaveChangesAsync()`, remove the file too (prevents orphaned files on product deletion). Capture `product.ImageUrl` **before** removing the row, then after save: `if (!string.IsNullOrEmpty(imageUrl)) _images.Delete(imageUrl);`. Keep the **204**/**404** behaviour unchanged.

### 6 — Enable static files + create the upload folder

**File: `Program.cs`** — add before `app.MapControllers();` (line 89):

```csharp
app.UseStaticFiles();
```

**Create the served folder** so `WebRootPath` resolves and the folder is tracked without committing binaries:

- Create `wwwroot/images/products/.gitkeep` (empty file).
- Add to [`.gitignore`](../../../.gitignore):

```gitignore
# Uploaded product images (keep the folder, ignore its contents)
wwwroot/images/products/*
!wwwroot/images/products/.gitkeep
```

### 7 — EF migration for the new column

From `OnlineStore.API/`:

```powershell
dotnet ef migrations add AddProductImageUrl
dotnet ef database update
```

Inspect the generated `Migrations/*_AddProductImageUrl.cs`: it should be a single `AddColumn<string>(name: "ImageUrl", table: "Products", type: "text", nullable: false, defaultValue: "")`. The `defaultValue: ""` backfills existing rows. **Do not hand-write it.** If EF is configured to emit `nullable: false` **without** a default, edit the generated migration to add `defaultValue: ""` (this is the one allowed manual tweak, to avoid failing on existing rows).

---

## Edge Cases & Failure Modes

- **Missing/empty image on POST:** `[Required]` on `CreateProductFormRequest.Image` → automatic **400**; `_images.Validate` also guards `file.Length == 0` → **400** `"Image is required."` (defence in depth). Enforced task 3 + task 2/`Validate`.
- **Wrong file type / oversized:** `Validate` throws `InvalidImageException` → **400** `"Image must be a .jpg, .jpeg, .png, or .webp file."` / `"Image must be 5 MB or smaller."` Extension is checked case-insensitively (`.ToLowerInvariant()`). Enforced task 2, caught in `Create`/`Update` (task 5d/5e).
- **DB save fails after file written (orphan risk):** the inner `try/catch` around `SaveChangesAsync` calls `_images.Delete(...)` before rethrowing, so no file is left behind. This is the core acceptance guarantee. Enforced task 5d/5e.
- **PUT without a new image:** `request.Image` is `null` → `ImageUrl` untouched, old file preserved. Enforced task 5e (`if (request.Image is not null …)`).
- **PUT replacing an image:** new file saved, row updated; old file deleted **only after** a successful save. If save fails, the **new** file is deleted and the old one is untouched — product still points at a valid file. Enforced task 5e.
- **Kestrel multipart body limit:** default request body limit (~28.6 MB) comfortably exceeds the 5 MB image cap; no `[RequestSizeLimit]` needed. If a much larger cap is ever set, revisit. **(Flagged; not changed here.)**
- **`WebRootPath` null on first run:** `wwwroot` may not exist until created; `SaveAsync`/`Delete` fall back to `ContentRootPath/wwwroot` and `SaveAsync` calls `Directory.CreateDirectory`. Task 6's `.gitkeep` guarantees the folder exists in a fresh clone.
- **Static file exposure:** `UseStaticFiles` serves everything under `wwwroot` anonymously — intended (public product images). Only `wwwroot/images/products` is used; do not place secrets under `wwwroot`.
- **Existing rows after migration:** `defaultValue: ""` means pre-existing products get an empty `ImageUrl` (frontend should treat empty as "no image"). **(Flagged** — intake marks `ImageUrl` "required" going forward but says nothing about backfilling old rows.)**
- **Swagger multipart:** `[Consumes("multipart/form-data")]` + `IFormFile` makes Swashbuckle 10.2.3 render a file-upload field. If the file field does not appear, verify the action isn't also bound to `[FromBody]`. **(Flagged for the executor to eyeball in `/swagger`.)**

---

## Test Plan

No test project exists yet (noted in Stories 02–05). If/when `OnlineStore.API.Tests` (`WebApplicationFactory`) is added:

1. **Create — 201 with image** (`ProductsImageTests`): admin token, `multipart/form-data` with valid fields + a small PNG → **201**, `ProductDetailDto` with non-empty `imageUrl`; assert the file exists under `wwwroot/images/products`.
2. **Create — image required (400):** admin token, form with **no** file → **400**.
3. **Create — bad type (400):** upload a `.txt` (or `.gif`) → **400** type message; assert **no** file written.
4. **Create — oversized (400):** upload a >5 MB file → **400** size message.
5. **Create — orphan rollback:** force `SaveChangesAsync` to throw (e.g. invalid FK bypassing the pre-check, or a mocked failure) → assert **500** *and* the written file was deleted.
6. **Create — 401/403:** no token → **401**; customer token → **403**.
7. **List / Detail expose imageUrl:** seed a product with an image → `GET /api/products` items and `GET /api/products/{id}` both include `imageUrl`.
8. **Update — replace image:** PUT with a new file → **200**, `imageUrl` changed, **old** file removed, new file present.
9. **Update — no image keeps old:** PUT without a file → **200**, `imageUrl` unchanged, file still present.
10. **Delete — removes file:** DELETE a product with an image → **204**, its file removed from disk.

Until a test project exists, the **Verification Steps** matrix is the acceptance gate — record results in the PR. Match the manual-verification style in [`04-story-products-crud.md`](04-story-products-crud.md).

---

## Migration / Rollback

- **Forward:** `dotnet ef migrations add AddProductImageUrl` → `dotnet ef database update`. Adds the non-nullable `Products.ImageUrl text` column with `defaultValue: ""`.
- **Half-applied risk:** minimal — a single additive column with a default; existing rows backfill to `""`. If the generated migration lacks the default and `Products` has rows, `database update` fails → add `defaultValue: ""` to the generated `AddColumn` and retry.
- **Rollback:** `dotnet ef database update <PreviousMigration>` (the Story 04 `AddCategoryBrandAndProductFks` migration) drops the column, then `dotnet ef migrations remove`. Rolling back the schema does **not** delete files already written under `wwwroot/images/products` — clean those manually if desired.

---

## Verification Steps

1. **Backend builds:** `dotnet build` in `OnlineStore.API/` — succeeds, no warnings (watch for the DTO positional-record call sites in `ProductsController`).
2. **Migration applied:** `dotnet ef database update` completes; `Products.ImageUrl` column exists.
3. **Backend runs:** `dotnet run --launch-profile http`; open `http://localhost:5016/swagger`.
4. **Admin path:** promote a user — `UPDATE "Users" SET "Role"='admin' WHERE "Email"='<you>';` — log in **again**, Swagger **Authorize**, paste the new token.
5. **Create (201):** `POST /api/products` as `multipart/form-data` with `name/description/price/stock/categoryId/brandId` + a `.png` image (use a seeded Category/Brand from Story 04) → **201**; response `imageUrl` like `/images/products/<guid>.png`.
6. **Image reachable:** open `http://localhost:5016/images/products/<guid>.png` in the browser → the image renders (confirms `UseStaticFiles`).
7. **Create — image required (400):** same POST with no file → **400**.
8. **Create — bad type (400):** attach a `.txt` → **400** `"Image must be a .jpg, .jpeg, .png, or .webp file."`.
9. **Create — oversized (400):** attach a >5 MB file → **400** `"Image must be 5 MB or smaller."`.
10. **List/Detail carry imageUrl:** `GET /api/products` and `GET /api/products/{id}` → `imageUrl` present.
11. **Update — replace image (200):** `PUT /api/products/{id}` with a new file → **200**, new `imageUrl`; verify the **old** file is gone from `wwwroot/images/products` and the new one exists.
12. **Update — no image (200):** `PUT /api/products/{id}` without a file → **200**, `imageUrl` unchanged.
13. **Delete (204):** `DELETE /api/products/{id}` → **204**; confirm the product's image file was removed from disk.
14. **AuthZ:** POST/PUT with no token → **401**; with a **customer** token → **403**.

---

## Done Criteria

- [ ] `Product.ImageUrl` exists; one additive migration (`AddProductImageUrl`) adds the column with a `""` default and applies cleanly.
- [ ] `POST /api/products` consumes `multipart/form-data`, requires an image, and returns **201** `ProductDetailDto` with a populated `imageUrl`.
- [ ] The image file is written **only** when the product row saves successfully; a DB failure deletes the just-written file (no orphans).
- [ ] Invalid image type/size and missing image return **400** with clear messages; no file is left on disk.
- [ ] `PUT /api/products/{id}` accepts an **optional** replacement image: supplying one repoints `ImageUrl` and deletes the previous file; omitting one keeps the current image; `CreatedAt` unchanged.
- [ ] `DELETE /api/products/{id}` removes the product's image file after the **204**.
- [ ] `GET /api/products` and `GET /api/products/{id}` include `imageUrl`.
- [ ] `app.UseStaticFiles()` serves uploaded images at `/images/products/{file}`; `wwwroot/images/products/*` is git-ignored except `.gitkeep`.
- [ ] Admin-only writes return **401** without a token and **403** with a customer token.
- [ ] `dotnet build` succeeds.

---

**STOP HERE. Report to the user and wait for confirmation before implementing.**
