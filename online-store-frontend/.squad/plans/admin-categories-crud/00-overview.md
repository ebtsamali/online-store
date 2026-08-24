# admin-categories-crud — plan overview

Entry point for the **admin-categories-crud** feature: brand-new admin surface for managing `Category` and `Brand` reference entities — a list page each plus create/edit pages, mirroring the CRUD conventions established in `admin-products-crud` (Story 12).

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 13 | [13-story-admin-categories-list-create-and-edit.md](./13-story-admin-categories-list-create-and-edit.md) | Admin Categories List, Create and Edit | — | Hard-blocked by backend "Categories and Brands CRUD" story (not yet implemented); soft precedent from [admin-products-crud/12](../admin-products-crud/12-story-admin-products-list-create-and-edit.md) |

## Dependency notes

- **Hard-blocked, not just soft.** Unlike Story 12 (which shipped with numeric id inputs as a documented workaround), this story cannot be implemented at all until the backend story "Categories and Brands CRUD" (`OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md`) ships `GET/POST/PUT/DELETE /api/categories` and `/api/brands` — verified via `grep`, no `CategoriesController.cs`/`BrandsController.cs` exist in `OnlineStore.API/Controllers/` as of this plan being written. Do not begin implementation until those endpoints are confirmed live (e.g. via Swagger).
- **Brands are bundled into this same story**, not split into a separate one — the backend plan defines identical CRUD shape and rules for both entities, and mirroring the Category pages for Brand is low incremental effort once they exist.
- **No backend changes** are made or required by this feature — it only consumes the categories/brands endpoints once the backend story above has shipped them.
- Story 13 documents (but does not implement) the follow-up to upgrade `admin-products-crud`'s (Story 12) numeric `categoryId`/`brandId` inputs to `<select>` dropdowns once these endpoints exist — see Story 13's task 7 and its "Optional Follow-up" cross-reference in Story 12.
- **No test runner is configured** in this project, so Story 13 specifies manual verification plus the tests to add if Vitest + `@nuxt/test-utils` is introduced later.
- Per the intake's `## Extra notes`, this is the **last story** in the overall plan — implement after all customer-journey stories and Story 12, once the backend categories/brands endpoints are confirmed live.
