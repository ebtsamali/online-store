# categories-and-brands — plan overview

Entry point for the **categories-and-brands** feature. Stories execute in order by their `NN` prefix.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 08 | [08-story-categories-and-brands-crud.md](08-story-categories-and-brands-crud.md) | Categories and Brands CRUD | — | Story 04 (products/products-crud) |

## Dependency notes

- **Story 08 → Story 04:** [`../products/04-story-products-crud.md`](../products/04-story-products-crud.md) introduced the `Category`/`Brand` entities, their `DbSet`s, and the `Product.CategoryId`/`BrandId` FKs with `DeleteBehavior.Restrict`. Story 08 adds the missing CRUD endpoints for those two entities (`/api/categories`, `/api/brands`) plus optional `categoryId`/`brandId` filters on `GET /api/products` — no schema changes, no new migration.
