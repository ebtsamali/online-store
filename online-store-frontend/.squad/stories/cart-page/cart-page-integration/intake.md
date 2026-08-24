# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/cart-page/cart-page-integration/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Cart Page Integration
- **Feature slug (folder under `plans/`):** `cart-page`

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
Cart Page Integration
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Replace the `app/pages/cart.vue` placeholder ("Cart contents coming soon") with a real cart page: list the logged-in user's cart items, let them remove a line, show the running total, and proceed to checkout. This is a required stepping stone between product-details (adds items) and checkout-payment (consumes the cart).
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] `app/pages/cart.vue` (already guarded by `middleware: 'auth'`) fetches `GET /api/cart` on load and renders each `CartItemDto` (product name, unit price, quantity, subtotal) plus the response's `total`.
- [ ] Each line has a "Remove" action calling `DELETE /api/cart/{id}`; on success, refetch/optimistically update the list and total; show a toast on success/failure.
- [ ] Empty cart state: friendly message + link to `/products` (not an error state).
- [ ] Loading state while fetching; error state (network/500) with Retry.
- [ ] "Proceed to checkout" button/link to a new `/checkout` route (built in the next story) — disable it when the cart is empty.
- [ ] No client-side quantity-edit control is required unless trivial to add (backend has no "update quantity" endpoint — only add, which increments, and remove; if quantity editing is added, implement it via remove + re-add through `POST /api/cart`, but this is optional/stretch, not required).
- [ ] Cart is scoped to the logged-in user only (already enforced server-side via JWT) — no client-side user-id handling needed.
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

- **Blocked by / related ids:** None hard; third in the customer-journey chain after "Product Details Page" (which is how items get added to the cart in the first place).
- **Depends on code areas or other stories:** `app/pages/cart.vue` (existing stub), `app/middleware/auth.ts`, `app/utils/format.ts`, `app/stores/auth.ts`. Feeds "Checkout and Payment Page" (the checkout button target).

## Extra notes (optional)

- Third story in the customer-journey chain: products-catalog → product-details → **cart-page** → checkout-payment → orders-history.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.
- `GET /api/cart` (`[Authorize]`) → `CartResponse { items: CartItemDto[], total }`; `CartItemDto { id, productId, productName, price, quantity, subtotal }`.
- `DELETE /api/cart/{id}` → 204 if it's the caller's own item, 403 if not (shouldn't happen from this UI since only own items are listed), 404 if missing.
- Follow existing conventions: `useFetch`/`server:false` for the GET, `$fetch` for the DELETE mutation, toasts via `vue-sonner`, Tailwind only.

## Out of scope

- Coupon/discount codes.
- Saved/multiple carts, guest carts (backend has none of this — one cart per authenticated user).
- Quantity editing beyond the optional stretch noted above.
