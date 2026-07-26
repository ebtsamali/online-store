# orders — plan overview

Entry point for the **orders** feature. Stories execute in order by their `NN` prefix.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 07 | [07-story-orders.md](07-story-orders.md) | Order Checkout, Payment Simulation & Order History | — | Story 05 (cart/cart-crud), Story 04 (products/products-crud) |

## Dependency notes

- **Story 07 → Story 05 (cart):** checkout reads and then clears the caller's `CartItems`; it reuses the cart's `GetUserId()` claim-reading pattern and controller conventions ([../cart/05-story-cart-crud.md](../cart/05-story-cart-crud.md)).
- **Story 07 → Story 04 (products):** checkout reads `Product.Name`/`Price`/`Stock`/`IsActive` and **writes** `Product.Stock`. It adds an `OrderItem`→`Product` FK with `DeleteBehavior.Restrict`, which makes a product that appears in any order no longer hard-deletable — a **deliberate** cross-feature contract change flagged for the products-endpoint owner (retire via `IsActive = false` instead). See the story's Edge Cases.
- **Scope guard:** order cancellation / refund / restock is explicitly **out of scope**; payment is simulated (always succeeds → `Status = "paid"`), with no real gateway and no failure path.
