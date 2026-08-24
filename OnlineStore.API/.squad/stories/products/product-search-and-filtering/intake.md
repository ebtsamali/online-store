# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/products/product-search-and-filtering/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `products`

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
Product search and filtering
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
# Task: Category/Brand Listing & Admin Dashboard Stats API

## Context

Two gaps were flagged while building the frontend home page and admin dashboard:

1. `Category` and `Brand` entities already exist (introduced in `products/04-story-products-crud.md` as FK targets on `Product`), but there is **no endpoint to list them**. The frontend can reference a category/brand by id on a product, but has no way to show category/brand names, filters, or a picker.
2. The admin dashboard needs summary numbers (orders, revenue) that don't exist yet. `Order`/`OrderItem` (from `orders/07-story-orders.md`) hold this data, but there's no aggregate endpoint — only per-user order history.

This story adds both as **read-only, additive** endpoints. No existing entity, table, or endpoint changes — this is new surface area only.

---

## Part 1 — Category & Brand Listing

### Endpoints

- `GET /api/categories` — `[AllowAnonymous]`. Returns all categories: `[{ id, name }]`. No pagination needed (reference data, expected to be small).
- `GET /api/brands` — `[AllowAnonymous]`. Same shape: `[{ id, name }]`.

### Notes

- Mirror the existing `ProductsController` conventions: `[ApiController]`, `[Route("api/categories")]` / `[Route("api/brands")]`, constructor-injected `AppDbContext`, try/catch → `StatusCode(500, new { message = "Something went wrong" })`.
- Project to a small DTO (`CategoryDto(int Id, string Name)`, `BrandDto(int Id, string Name)`) — don't return the entity directly.
- These are **read-only** for now. Category/Brand create/edit/delete stays out of scope (as already noted in the products plan) unless a future story asks for it.
- Optional follow-on (not required for this story): extend `GET /api/products` to accept `?categoryId=` / `?brandId=` filters, so the frontend can filter the catalog by the values returned here. Flag this as a possible next story rather than building it now, unless you want it bundled in.

### Acceptance criteria

- [ ] `GET /api/categories` returns 200 with all categories, publicly accessible (no auth required).
- [ ] `GET /api/brands` returns 200 with all brands, publicly accessible.
- [ ] Both return an empty array (not an error) when no rows exist yet.

---

## Part 2 — Admin Dashboard Stats

### Endpoint

- `GET /api/admin/stats` — `[Authorize(Roles = "admin")]`. Returns a single summary object, e.g.:

```json
{
  "totalProducts": 42,
  "totalOrders": 17,
  "totalRevenue": 1289.50,
  "ordersByStatus": { "pending": 2, "paid": 15, "failed": 0 }
}
```

### Notes

- `totalProducts` — `_db.Products.CountAsync()` (all products, active or not, since this is an admin view).
- `totalOrders` — `_db.Orders.CountAsync()`.
- `totalRevenue` — sum of `Total` across orders with `Status == "paid"` (`_db.Orders.Where(o => o.Status == "paid").SumAsync(o => o.Total)`, defaulting to `0` if none). Don't count pending/failed orders as revenue.
- `ordersByStatus` — group `Orders` by `Status` and count each — useful for a small breakdown card, but optional if you want to keep the first version minimal (`totalProducts`/`totalOrders`/`totalRevenue` alone unblocks the dashboard cards already planned).
- Mirror the `AuthController.Me()` precedent for role-protected admin-only reads: `[Authorize(Roles = "admin")]` at the action or controller level, same error-body conventions as elsewhere.
- This is a **single aggregate read**, not a general reporting/analytics system — keep it to the fields the dashboard actually needs; don't over-build filtering/date-range support unless asked.

### Acceptance criteria

- [ ] `GET /api/admin/stats` returns 200 with `totalProducts`, `totalOrders`, `totalRevenue` for an authenticated admin.
- [ ] A non-admin (or unauthenticated) request returns **403**/**401** respectively — same authorization behavior as other admin-only routes.
- [ ] `totalRevenue` only counts orders with `Status == "paid"`.
- [ ] Endpoint returns sensible zero-values (not an error) when there are no products/orders yet.

---

## Out of Scope

- Category/Brand create, update, delete endpoints.
- Product filtering by category/brand on `GET /api/products` (flagged as a possible follow-up, not required here).
- Date-range or time-series analytics (e.g. "revenue this month") — only current-state totals.
- Order cancellation/refund stats — no such states exist yet per the orders story.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```

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

- **Blocked by / related ids:** (tracker ids only; optional short note)
- **Depends on code areas or other stories:**

## Extra notes (optional)

- Anything not captured above (e.g. chat context) — keep short.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `c#`.

## Out of scope

- What this story explicitly does **not** cover:
