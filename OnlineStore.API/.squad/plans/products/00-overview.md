# products — plan overview

Entry point for the **products** feature. Stories execute in order by their `NN` prefix.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 04 | [04-story-products-crud.md](04-story-products-crud.md) | Product Catalog CRUD API (with Category & Brand) | — | Story 03 (middleware/jwt-auth) |

## Dependency notes

- **Story 04 → Story 03:** applies the authorization matrix defined in [../middleware/03-authorization-middleware.md](../middleware/03-authorization-middleware.md) to the real product endpoints (`[Authorize(Roles="admin")]` on POST/PUT/DELETE, public GETs). The JWT pipeline + Swagger Authorize button must already be in place.
- **Story 04 introduces Category & Brand:** neither entity existed before; this story adds both tables, the `Product` FKs, and one EF migration (`AddCategoryBrandAndProductFks`). Category/Brand CRUD endpoints are **not** in scope and remain a possible follow-up story.
- **Admin testing:** verifying admin-only writes requires manually promoting a user to `role=admin` and re-logging in (no admin-registration path exists — see Story 03).
