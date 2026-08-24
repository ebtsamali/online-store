# Customer Orders History — Overview

Fifth and final story in the customer-journey chain (products-catalog → product-details → cart-page → checkout-payment → **orders-history**). Builds `/orders` (the customer's own past orders, newest first) and `/orders/{id}` (single order detail with line items) — the redirect target the checkout page (Story 10) already calls on a successful order placement.

| NN | File | Title | Depends on |
|----|------|-------|------------|
| 11 | [11-story-customer-orders-history.md](./11-story-customer-orders-history.md) | Customer Orders History | [Story 10 — Checkout and Payment Page](../checkout-payment/10-story-checkout-and-payment-page.md) (its `navigateTo(`/orders/${id}`)` redirect, which this story's `/orders/[id].vue` resolves) |
