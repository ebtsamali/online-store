# Cart Page Integration — Overview

Third story in the customer-journey chain (products-catalog → product-details → **cart-page** → checkout-payment → orders-history). Replaces the `app/pages/cart.vue` placeholder with a real cart view backed by `GET /api/cart` and `DELETE /api/cart/{id}`.

| NN | File | Title | Depends on |
|----|------|-------|------------|
| 09 | [09-story-cart-page-integration.md](./09-story-cart-page-integration.md) | Cart Page Integration | [Story 08 — Product Details Page](../product-details/08-story-product-details-page.md) (authenticated-fetch pattern) |
