# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/checkout-payment/checkout-and-payment-page/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Checkout and Payment Page
- **Feature slug (folder under `plans/`):** `checkout-payment`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Checkout and Payment Page
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Build `/checkout`: a review-and-confirm page reached from the cart page's "Proceed to checkout" button. The backend checkout endpoint takes no body (no shipping address, no real payment form — it's a simulated 1.5s "payment" that always succeeds) so this page is primarily an order review + a single confirm action, then redirect to the new order's detail/confirmation view.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] New page `app/pages/checkout.vue`, `layout: 'default'`, `middleware: 'auth'`.
- [ ] On load, fetch `GET /api/cart` and render a read-only order summary (items, quantities, line subtotals, total) — same data shape as the cart page. If the cart is empty, redirect back to `/cart` (or show an empty state with a link) rather than allowing checkout.
- [ ] "Place order" button calls `POST /api/orders/checkout` (no body). Show a pending/processing state while it's in flight — the backend simulates a ~1.5s payment delay, so the button must be disabled and show a spinner/loading label during the request (reuse `BaseButton`'s `loading` prop) to avoid double-submits.
- [ ] On success (201 `OrderDetailDto`), show a success toast and redirect to the order's detail view (or `/orders` if a dedicated order-detail route doesn't exist yet — coordinate with the "Customer Orders History" story on the final route; `/orders/{id}` is preferred if that story builds it).
- [ ] On failure (400 `"Cart is empty"` / `"Insufficient stock for {name}"` / `"Product {id} unavailable"`), show the exact backend message in an error toast (no partial state — cart is untouched per backend guarantee, so just let the user retry or go back to `/cart` to fix quantities).
- [ ] No payment form fields of any kind (no card number, no address) — do not build UI that implies real payment is collected, since none is processed; if a "payment method" visual is desired for realism, label it clearly as a simulated/demo flow.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| *(e.g. `attachments/flow.png`)* | *(e.g. UX flow)* |

*(Add rows per file. If none, write "None.")*

---

## Dependencies

- **Blocked by / related ids:** Depends on "Cart Page Integration" for the entry point (`/cart`'s "Proceed to checkout" link) and ideally coordinates with "Customer Orders History" for the post-success redirect target (`/orders/{id}`).
- **Depends on code areas or other stories:** `app/pages/cart.vue`, `app/components/base/Button.vue` (loading prop), `app/utils/format.ts`, `app/stores/auth.ts`.

## Extra notes (optional)

- Fourth story in the customer-journey chain: products-catalog → product-details → cart-page → **checkout-payment** → orders-history. If "Customer Orders History" hasn't shipped yet when this is implemented, redirect to `/orders` (even if that page doesn't exist yet) is acceptable — the two stories should land close together.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.
- `POST /api/orders/checkout` (`[Authorize]`, no body): validates every cart line inside one DB transaction (stock + active), decrements stock, simulates payment (`Task.Delay(1500)`, always succeeds), clears the cart, returns 201 `OrderDetailDto`. 400 on any validation failure with no partial side effects.
- `OrderDetailDto` shape not fully specified in this intake — read the actual DTO in `OnlineStore.API` (`OrdersController`/DTOs folder) during planning to get exact field names before wiring the redirect/detail view.
- Use `$fetch` (not `useFetch`) for the checkout POST — it's a one-off mutation with a `loading` ref, in `try/catch/finally`, mirroring `auth/login.vue`.

## Out of scope

- Real payment gateway integration (Stripe, PayPal, etc.) — backend simulates payment only.
- Shipping address collection — backend has no such field on Order/OrderItem.
- Order cancellation/refund UI — no backend endpoint exists for either.
