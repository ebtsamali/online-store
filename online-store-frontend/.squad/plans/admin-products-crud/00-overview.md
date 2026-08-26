# admin-products-crud — plan overview

Entry point for the **admin-products-crud** feature: replaces the `app/pages/admin/products/index.vue` placeholder with a real, paginated, searchable admin product list (including inactive products, with working Edit/Delete), and adds the two missing pages — create (`/admin/products/new`) and edit (`/admin/products/[id]/edit`) — both built against the backend's `multipart/form-data` product endpoints.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 12 | [12-story-admin-products-list-create-and-edit.md](./12-story-admin-products-list-create-and-edit.md) | Admin Products List, Create and Edit | — | None |
| 15 | [15-story-product-category-brand-selects.md](./15-story-product-category-brand-selects.md) | Admin Add/Edit Product: Category and Brand as Select Inputs | — | Story 12 |

## Dependency notes

- **Resolved by Story 15.** Story 12 originally shipped with plain numeric category/brand id inputs because the backend "Categories and Brands CRUD" story had not landed (`OnlineStore.API/Controllers/CategoriesController.cs`/`BrandsController.cs` did not exist yet). That backend has since landed, and Story 15 replaces the numeric inputs on both the create and edit product forms with `<select>` dropdowns populated from `GET /api/categories`/`GET /api/brands`, closing out the follow-up Story 12 explicitly deferred.
- **No backend changes** are made or required by this feature — it consumes the existing `[Authorize(Roles = "admin")]` `GET /api/products`, `POST /api/products`, `PUT /api/products/{id}`, and `DELETE /api/products/{id}` (`OnlineStore.API/Controllers/ProductsController.cs`) exactly as they exist today.
- Story 12 fixes, for this new page only, the known gap already documented on `app/pages/admin/dashboard.vue` (its `useProducts` calls send no `Authorization` header, so admin stats there are actually the anonymous/active-only view) — the new list page attaches the bearer token from the start. The dashboard file itself is not touched by this story.
- **No test runner is configured** in this project, so Story 12 specifies manual verification plus the tests to add if Vitest + `@nuxt/test-utils` is introduced later.
