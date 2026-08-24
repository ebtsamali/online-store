# home-page — plan overview

Entry point for the **home-page** feature: the two landing experiences that sit on top of the `middelware` groundwork — a marketing-forward **customer home page** (`default` layout, public) and a data-forward **admin dashboard home** (`admin` layout, guarded). Stories execute in order by their `NN` prefix.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 05 | [05-story-customer-home-page.md](./05-story-customer-home-page.md) | Brand theme & customer home page | — | middelware Stories 02, 04 |
| 06 | [06-story-admin-dashboard-home.md](./06-story-admin-dashboard-home.md) | Admin dashboard home | — | Story 05; middelware Stories 02, 03 |

## Dependency notes

- Execute **strictly in order** — Story 05 stops for confirmation before Story 06 begins.
- **Story 05 owns the shared foundation.** It creates `tailwind.config.ts` (the `primary` scale with brand `#1C3684`), `app/composables/useProducts.ts`, `app/composables/useProductImage.ts`, `app/utils/format.ts`, and `app/components/ProductCard.vue`. Story 06 **reuses** all of them; it must not fork a second products fetch or price formatter.
- **Brand colour change is intentional and cross-cutting.** The existing code hardcodes navy `#1b3a6b` (`app/components/base/Button.vue` lines 19–24, the auth pages, `app/components/base/Input.vue`). Story 05 converts `BaseButton` and the `default` layout wordmark to theme tokens and **explicitly leaves the auth pages for a follow-up** — expect a visible two-navy period until that follow-up lands.
- **Backend is read-only for this feature.** Both stories consume only the existing `[AllowAnonymous]` `GET /api/products` (`OnlineStore.API/Controllers/ProductsController.cs` lines 25–65, envelope `{ items, page, pageSize, total }`). No C# changes are planned or permitted.
- **Two backend gaps bound the scope**, both confirmed against `OnlineStore.API/Controllers/`:
  - No `GET /api/categories` or `GET /api/brands` → the home page's category teaser is **static** (Story 05, task 7.3).
  - No admin stats or order-count endpoint → the dashboard shows **product-derived numbers only**, with a placeholder tile instead of fabricated orders/revenue (Story 06, task 3). Both warrant follow-up backend stories.
- **Known caveat carried into Story 06:** the dashboard's product counts are the *anonymous* view (active products only), because `useProducts` sends no `Authorization` header while `ProductsController.List` keys its `IsActive` filter on `User.IsInRole("admin")` (lines 40–43). Story 06 discloses the scope in each card's `hint` and flags attaching the bearer token as a **decision for the user**, not an assumed change.
- Both stories link to `/products` and `/products/{id}`, which **do not exist yet** — the catalog page is a separate feature. Those links 404 until it lands (already true of the header link in `app/layouts/default.vue` line 10).
- **No test runner is configured** in this project, so both stories specify manual verification plus the tests to add if Vitest + `@nuxt/test-utils` is introduced later.
