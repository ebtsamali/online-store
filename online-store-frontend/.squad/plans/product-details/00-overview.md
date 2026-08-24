# product-details — plan overview

Entry point for the **product-details** feature: the public `/products/[id]` product detail page — full product info plus an authenticated add-to-cart action. Second link in the customer-journey chain: products-catalog → **product-details** → cart-page → checkout-payment → orders-history. `app/components/ProductCard.vue` already links here from both the home page and the products catalog page.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 08 | [08-story-product-details-page.md](./08-story-product-details-page.md) | Product Details Page | — | home-page Story 05, products-catalog Story 07 |

## Dependency notes

- **Story 08 reuses the shared foundation from Stories 05 and 07** — `app/composables/useProductImage.ts`, `app/utils/format.ts`, the `primary` Tailwind theme, `<BaseButton>`, and the loading/error three-state UI pattern. It extends `app/composables/useProducts.ts` with a new `useProduct(id)` export and `ProductDetail` interface rather than forking a second product-fetch composable.
- **No backend changes** are made or required by this feature — it consumes the existing `[AllowAnonymous]` `GET /api/products/{id}` (`OnlineStore.API/Controllers/ProductsController.cs` lines 67–91) and the existing `[Authorize]` `POST /api/cart` (`OnlineStore.API/Controllers/CartController.cs` lines 23–75) unchanged.
- **Establishes the first authenticated frontend request pattern** in the codebase — no prior story sends an `Authorization: Bearer …` header; Story 08 adds it inline for the add-to-cart call (`app/stores/auth.ts`'s `token` field) rather than introducing a shared HTTP client wrapper, since this is the only authenticated call in the app so far.
- Story 08 fixes the pre-existing 404 on every `ProductCard`'s `/products/${product.id}` link (`app/components/ProductCard.vue` line 16, called out as a known gap in both Story 05 and Story 07's plans). Story 07's own product-details link is explicitly deferred to this story.
- **Return-path preservation after login is out of scope.** Clicking "Add to cart" while logged out redirects to `/auth/login` with no query param carrying a way back to the product; `app/pages/auth/login.vue` always redirects to `/` after a successful login. Adding that is a separate, `login.vue`-scoped change for a future story.
- **No test runner is configured** in this project, so Story 08 specifies manual verification plus the tests to add if Vitest + `@nuxt/test-utils` is introduced later.
