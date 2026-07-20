# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/products/add-product/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Product Image Upload
- **Feature slug (folder under `plans/`):** `products`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `add-product` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Story`
- **Status:** ``
- **Assignee:** ``
- **Labels:** `products`, `media`

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Add product image upload
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Task: Add Product with Image (Single Multipart Endpoint)

Description:
Update the Create Product endpoint to accept the product data AND the 
product image together in a single request, using multipart/form-data. 
This avoids the problem of "orphaned images" — images uploaded to the 
server that never get linked to an actual product (e.g. if the admin 
closes the page before saving).

The image is only saved to the server at the exact moment the product 
itself is saved — never before.

---

Endpoint (Updated):

POST /api/products
Content-Type: multipart/form-data

Access: [Authorize(Roles = "admin")]

Form fields (multipart, not JSON body):
- name (string, required)
- description (string)
- price (decimal, required)
- stock (int, required)
- categoryId (int, required)
- brandId (int, required)
- image (file, required) — the product image (jpg/png/webp)

---

Backend implementation notes:

1. Change the endpoint signature to accept form data instead of a JSON 
   body, using [FromForm] and IFormFile for the image:

   [HttpPost]
   public async Task<IActionResult> Create([FromForm] CreateProductFormRequest request)

   public class CreateProductFormRequest
   {
       public string Name { get; set; } = string.Empty;
       public string Description { get; set; } = string.Empty;
       public decimal Price { get; set; }
       public int Stock { get; set; }
       public int CategoryId { get; set; }
       public int BrandId { get; set; }
       public IFormFile Image { get; set; } = null!;
   }

2. Validate the image before saving:
   - Allowed extensions: .jpg, .jpeg, .png, .webp
   - Max file size: 5 MB (adjust as needed)
   - Reject with 400 Bad Request if validation fails

3. Save the image to disk only after all other validations pass 
   (category exists, brand exists, image is valid):
   - Generate a unique file name (e.g. Guid.NewGuid() + original extension) 
     to avoid name collisions.
   - Save to: wwwroot/images/products/{fileName}
   - Store the resulting relative URL (e.g. /images/products/{fileName}) 
     in the Product entity's ImageUrl field.

4. Wrap the "save image" and "save product to database" steps so that if 
   saving the product to the database fails for any reason, the 
   already-written image file should be deleted (rollback), to avoid 
   leaving an orphaned file on disk.

5. Ensure static file serving is enabled so uploaded images are publicly 
   accessible:
   app.UseStaticFiles(); // must be present before app.MapControllers();

6- handle return the image in the get by id also in update product

---

Entity update: Product
- Add new field: ImageUrl (string, required)

DTO update: ProductDetailDto / ProductListItemDto
- Include ImageUrl in both response DTOs so the frontend can display 
  the product image.

---

Validation rules:
- Name: required, max 100 characters
- Price: required, must be greater than 0
- Stock: required, must be 0 or greater
- CategoryId: required, must reference an existing category
- BrandId: required, must reference an existing brand
- Image: required, must be .jpg/.jpeg/.png/.webp, max 5 MB

Error handling:
- 400 Bad Request → validation errors (invalid fields, invalid image 
  type/size, category/brand not found)
- 401 Unauthorized → returned automatically by [Authorize] if no/invalid token
- 403 Forbidden → returned automatically if token role isn't "admin"
- 500 Internal Server Error → "Something went wrong" (and rollback any 
  saved image file if the database save fails)
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] Admin can create a product by sending form data + image in a single 
      request.
- [ ] The image is only written to disk if the product is successfully 
      saved to the database (no orphaned files on failure).
- [ ] Invalid image types or oversized images are rejected with a clear 
      error message.
- [ ] The saved product includes a valid, publicly accessible ImageUrl.
- [ ] Product listing and detail endpoints return the ImageUrl field.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None. | |

*(Add rows per file. If none, write "None.")*

---

## Dependencies

- **Blocked by / related ids:** `products-crud` — this story extends the existing `ProductsController` and `Product` entity (implemented in commit 764f5ce). `jwt-auth` — admin authorization must be enabled (commit bfc25f2).
- **Depends on code areas or other stories:** `Entities/Product.cs`, `Controllers/ProductsController.cs`, `Dtos/ProductDetailDto.cs`, `Dtos/ProductListItemDto.cs`, `Data/AppDbContext.cs` (new `ProductImages` DbSet + relationship), `Program.cs` (`UseStaticFiles`, upload options).

## Extra notes (optional)

- A new EF Core migration IS required (new `ProductImage` table + FK/cascade). Provider is PostgreSQL (list endpoint uses `EF.Functions.ILike`).
- Follow existing conventions: controller under `Controllers/`, DTOs as records under `Dtos/`, try/catch returning `{ message }` on 500, server owns `CreatedAt`/`IsActive`-style fields.
- Static file serving: ensure `wwwroot/uploads/` exists and is git-ignored (don't commit uploaded binaries).

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `c#`.
- .NET 8 minimal-hosting + controllers; EF Core with `AppDbContext` on PostgreSQL; auth via `[Authorize(Roles = "admin")]` (JWT bearer configured in `Program.cs`).
- Use `IFormFile`/`IFormFileCollection` for multipart uploads; validate `ContentType` and `Length` before saving; consider `[RequestSizeLimit]` / form options for the upload action.

## Out of scope

- Cloud/object storage (S3, Azure Blob) — local `wwwroot` filesystem storage only for now.
- Image resizing, thumbnails, or format conversion.
- CDN or signed-URL delivery.
- Reordering images beyond the single primary flag.
- Changes to product create/update/delete behavior (owned by `products-crud`).
