# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/products/products-crud/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Product Catalog
- **Feature slug (folder under `plans/`):** `products`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `products-crud` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Implement Product Catalog CRUD API
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Task: Build Products API (Full CRUD)

Description:
Implement a complete set of endpoints to manage products — the core entity 
of the store. Customers should be able to browse products, while only 
admins can create, update, or delete them.

Entity: Product
- Id (int, PK)
- Name (string, required)
- Description (string)
- Price (decimal, required)
- Stock (int, required)
- CategoryId (int, FK)
- BrandId (int, FK)
- IsActive (bool, default = true)
- CreatedAt (DateTime, default = current UTC time)

DTOs:
- ProductListItemDto(int Id, string Name, decimal Price, int Stock, bool IsActive)
- ProductDetailDto(int Id, string Name, string Description, decimal Price, 
  int Stock, int CategoryId, int BrandId, bool IsActive)
- CreateProductRequest(string Name, string Description, decimal Price, 
  int Stock, int CategoryId, int BrandId)
- UpdateProductRequest(string Name, string Description, decimal Price, 
  int Stock, int CategoryId, int BrandId, bool IsActive)

---

Endpoints:

1. GET /api/products
   - Access: Public (no authentication required)
   - Description: Returns a list of all active products for customers to browse.
   - Optional query params: search (string), page (int), pageSize (int)
   - Success response (200 OK): array of ProductListItemDto

2. GET /api/products/{id}
   - Access: Public (no authentication required)
   - Description: Returns full details of a single product.
   - Success response (200 OK): ProductDetailDto
   - Error response: 404 Not Found if the product doesn't exist

3. POST /api/products
   - Access: [Authorize(Roles = "admin")]
   - Description: Creates a new product.
   - Request body: CreateProductRequest
   - Success response (201 Created): the created ProductDetailDto
   - Error response: 400 Bad Request if validation fails

4. PUT /api/products/{id}
   - Access: [Authorize(Roles = "admin")]
   - Description: Updates an existing product.
   - Request body: UpdateProductRequest
   - Success response (200 OK): the updated ProductDetailDto
   - Error response: 404 Not Found if the product doesn't exist
   - Error response: 400 Bad Request if validation fails

5. DELETE /api/products/{id}
   - Access: [Authorize(Roles = "admin")]
   - Description: Deletes a product permanently.
   - Success response: 204 No Content
   - Error response: 404 Not Found if the product doesn't exist

---

Validation rules:
- Name: required, max 100 characters
- Price: required, must be greater than 0
- Stock: required, must be 0 or greater
- CategoryId: required, must reference an existing category
- BrandId: required, must reference an existing brand

Business logic notes:
- GET endpoints should only return products where IsActive = true, unless 
  the request comes from an admin (optional — confirm if needed).
- Deleting a product should be a hard delete for now (soft delete/IsActive 
  toggle can be considered later if needed).

Error handling:
- 404 Not Found → "Product not found" (for GET by id, PUT, DELETE with 
  invalid id)
- 400 Bad Request → validation errors per field
- 401 Unauthorized → returned automatically by [Authorize] if no/invalid token
- 403 Forbidden → returned automatically if token role isn't "admin"
- 500 Internal Server Error → "Something went wrong"
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] Any user (logged in or not) can view the list of products.
- [ ] Any user (logged in or not) can view a single product's details.
- [ ] Only an admin can create a new product.
- [ ] Only an admin can update an existing product.
- [ ] Only an admin can delete a product.
- [ ] Attempting admin-only actions without admin role returns 403 Forbidden.
- [ ] Attempting admin-only actions without any token returns 401 Unauthorized.
- [ ] Validation errors are returned clearly when required fields are 
      missing or invalid.
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

- **Blocked by / related ids:** `jwt-auth` — authorization middleware must be enabled for the admin-only rules to work (already implemented per commit bfc25f2).
- **Depends on code areas or other stories:** `Entities/Product.cs` (already exists), `Data/AppDbContext.cs` (Products DbSet already registered), auth stories (`auth/login`, `auth/sign-up`) for token issuance.

## Extra notes (optional)

- The `Product` entity and `Products` DbSet already exist — no new migration is required unless a schema change is introduced.
- New ProductsController should be added under `Controllers/`, DTOs under `Dtos/`, mirroring the existing AuthController conventions.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `c#`.
- .NET 8 minimal-hosting + controllers; EF Core with `AppDbContext`; auth via `[Authorize(Roles = "admin")]` (JWT bearer already configured in `Program.cs`).

## Out of scope

- Pagination, filtering, sorting, and search on the product list.
- Image upload / product media.
- Inventory adjustments beyond the plain `Stock` field.
- Soft-delete semantics — DELETE performs a hard delete for now.
