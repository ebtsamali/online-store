# cart — plan overview

Entry point for the **cart** feature. Stories execute in order by their `NN` prefix.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 05 | [05-story-cart-crud.md](05-story-cart-crud.md) | Shopping Cart API (Add / View / Remove Items) | — | Story 03 (middleware/jwt-auth), Story 04 (products) |

## Dependency notes

- **Story 05 → Story 03:** every cart endpoint is `[Authorize]` for any logged-in user. The JWT pipeline + Swagger Authorize button from [../middleware/03-authorization-middleware.md](../middleware/03-authorization-middleware.md) must already be in place. No admin role is required.
- **Story 05 → Story 04:** cart items reference `Product` (`Id`, `Name`, `Price`, `Stock`, `IsActive`) and mirror the `ProductsController`/DTO/migration style from [../products/04-story-products-crud.md](../products/04-story-products-crud.md).
- **Story 05 introduces `CartItem`:** a new table with FKs to `User` and `Product` using `DeleteBehavior.Cascade` (deliberately different from Story 04's `Restrict` on reference data — cart rows are disposable). One EF migration (`AddCartItem`).
- **Out of scope (possible follow-ups):** quantity-update endpoint, clear-cart, checkout/order creation, guest-cart merge on login, and stock reservation (stock is validated on add, not decremented).
