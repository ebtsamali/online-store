# admin-products-crud — plan overview

Entry point for the **admin-products-crud** feature: replaces the `app/pages/admin/products/index.vue` placeholder with a real, paginated, searchable admin product list (including inactive products, with working Edit/Delete), and adds the two missing pages — create (`/admin/products/new`) and edit (`/admin/products/[id]/edit`) — both built against the backend's `multipart/form-data` product endpoints.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 12 | [12-story-admin-products-list-create-and-edit.md](./12-story-admin-products-list-create-and-edit.md) | Admin Products List, Create and Edit | — | None |

## Dependency notes

- **Soft dependency on a backend story, not blocking.** The backend story "Categories and Brands CRUD" (`OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md`) is planned but **not implemented** — verified, `OnlineStore.API/Controllers/` has no `CategoriesController.cs`/`BrandsController.cs` and there is no `GET /api/categories`/`GET /api/brands` endpoint. Story 12 ships the create/edit forms now with plain numeric category/brand id inputs and documents the `<select>` upgrade as an explicit, separate follow-up, not part of its Done Criteria.
- **No backend changes** are made or required by this feature — it consumes the existing `[Authorize(Roles = "admin")]` `GET /api/products`, `POST /api/products`, `PUT /api/products/{id}`, and `DELETE /api/products/{id}` (`OnlineStore.API/Controllers/ProductsController.cs`) exactly as they exist today.
- Story 12 fixes, for this new page only, the known gap already documented on `app/pages/admin/dashboard.vue` (its `useProducts` calls send no `Authorization` header, so admin stats there are actually the anonymous/active-only view) — the new list page attaches the bearer token from the start. The dashboard file itself is not touched by this story.
- **No test runner is configured** in this project, so Story 12 specifies manual verification plus the tests to add if Vitest + `@nuxt/test-utils` is introduced later.
