# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/orders-history/customer-orders-history/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Customer Orders History
- **Feature slug (folder under `plans/`):** `orders-history`

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
Customer Orders History
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Build `/orders` (list of the logged-in customer's past orders) and `/orders/[id]` (single order detail with line items) — the last stop in the customer journey, and the redirect target after a successful checkout.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] New page `app/pages/orders/index.vue`, `layout: 'default'`, `middleware: 'auth'`. Fetches `GET /api/orders` and lists the caller's orders newest-first (`OrderSummaryDto[]` — confirm exact fields from the backend DTO during planning: expected to include id, date, status/"paid", total). Each row links to `/orders/{id}`.
- [ ] Empty state: friendly "no orders yet" message + link to `/products`.
- [ ] New page `app/pages/orders/[id].vue`, same layout/middleware. Fetches `GET /api/orders/{id}` and renders the full `OrderDetailDto` (line items with product name/quantity/unit price snapshot, order total, status, date).
- [ ] 403/404 handling: if the order isn't the caller's own (403) or doesn't exist (404), show a clear "not found" state with a link back to `/orders` — do not show a raw error.
- [ ] Loading states for both pages (skeleton list / skeleton detail); error states with Retry.
- [ ] This is the redirect target from the checkout page on successful order placement (`/orders/{id}`) — confirm route naming matches what "Checkout and Payment Page" expects, or update that story's redirect if it shipped first with a placeholder target.
- [ ] Add an "Orders" link somewhere reachable from the customer layout (`app/layouts/default.vue` header, alongside existing Products/Cart links) so the page isn't orphaned.
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

- **Blocked by / related ids:** Coordinates with "Checkout and Payment Page" (redirect target after successful order placement).
- **Depends on code areas or other stories:** `app/layouts/default.vue` (nav link), `app/utils/format.ts`, `app/middleware/auth.ts`.

## Extra notes (optional)

- Fifth and final story in the customer-journey chain: products-catalog → product-details → cart-page → checkout-payment → **orders-history**.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.
- `GET /api/orders` (`[Authorize]`) → caller's orders only, newest-first, `OrderSummaryDto[]`.
- `GET /api/orders/{id}` (`[Authorize]`) → 200 own order (`OrderDetailDto` w/ items), 403 another user's, 404 missing.
- Exact DTO field names (`OrderSummaryDto`/`OrderDetailDto`) should be read directly from `OnlineStore.API` source during planning — the intake for the backend orders story doesn't fully spell them out.
- `OrderItem.ProductName`/`UnitPrice` are immutable snapshots taken at checkout time — display these, not live product data (a product could be edited/deactivated after the order was placed).

## Out of scope

- Reordering / "buy again" action.
- Cancellation, refunds, order status changes — no backend support.
- Admin-side view of all orders (not requested; this is the customer's own-orders view only).
