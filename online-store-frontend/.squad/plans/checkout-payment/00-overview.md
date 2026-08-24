# Checkout and Payment Page — Overview

Fourth story in the customer-journey chain (products-catalog → product-details → cart-page → **checkout-payment** → orders-history). Builds `/checkout`: an order review + single "Place order" confirm action backed by `POST /api/orders/checkout`, redirecting to the not-yet-built `/orders/{id}` detail view on success.

| NN | File | Title | Depends on |
|----|------|-------|------------|
| 10 | [10-story-checkout-and-payment-page.md](./10-story-checkout-and-payment-page.md) | Checkout and Payment Page | [Story 09 — Cart Page Integration](../cart-page/09-story-cart-page-integration.md) (`useCart()` composable, entry link from `/cart`) |
