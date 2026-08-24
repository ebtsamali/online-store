# products-catalog — plan overview

Entry point for the **products-catalog** feature: the public `/products` catalog page — a browsable, paginated, searchable grid of active products. This is the entry point into the catalog before product details, cart, and checkout, and the first link in the customer-journey chain: products-catalog → product-details → cart-page → checkout-payment → orders-history.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 07 | [07-story-customer-product-catalog-list-filters.md](./07-story-customer-product-catalog-list-filters.md) | Customer Product Catalog (list + filters) | — | home-page Story 05 |

## Dependency notes

- **Story 07 reuses the shared foundation from home-page Story 05** — `app/composables/useProducts.ts` (extended with an optional `search` param), `app/composables/useProductImage.ts`, `app/utils/format.ts`, `app/components/ProductCard.vue`, and the `primary` Tailwind theme. It does not fork a second product-fetch composable or price formatter.
- **Soft dependency on a backend story, not blocking.** The backend story "Categories and Brands CRUD" (`OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md`) is planned but **not implemented** — verified, `ProductsController.List` has no `categoryId`/`brandId` params yet and no `CategoriesController`/`BrandsController` exist. Story 07 ships list + search + pagination now and documents the category/brand filter UI as an explicit, separate follow-up section, not part of its Done Criteria.
- **No backend changes** are made or required by this feature — it consumes the existing `[AllowAnonymous]` `GET /api/products` (`OnlineStore.API/Controllers/ProductsController.cs` lines 25–65) unchanged in shape, only adding the already-supported `search` parameter to the frontend composable.
- Story 07 fixes the pre-existing 404 on every `to="/products"` link (`app/pages/index.vue`, `app/layouts/default.vue`) that Story 05 documented as a known, visible gap. `ProductCard`'s `/products/{id}` link remains a 404 until a future product-details story lands — out of scope here.
- **No test runner is configured** in this project, so Story 07 specifies manual verification plus the tests to add if Vitest + `@nuxt/test-utils` is introduced later.
