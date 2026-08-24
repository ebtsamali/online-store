# Story 06 — Admin dashboard home

Turn the placeholder `app/pages/admin/dashboard.vue` into a real back-office landing page: an identity header with role badge and logout, brand-coloured active sidebar state, summary cards built **only** from data the API already returns, and a recent-products table linking into admin product management.

---

## Prerequisites

- **Story 05 completed** ([`05-story-customer-home-page.md`](./05-story-customer-home-page.md)): `tailwind.config.ts` defines the `primary` scale, and `app/composables/useProducts.ts`, `app/composables/useProductImage.ts`, `app/utils/format.ts` exist. This story **reuses** them — do not duplicate a products fetch or a price formatter.
- **Story 03 completed** ([`../middelware/03-story-route-protection-middleware.md`](../middelware/03-story-route-protection-middleware.md)): `app/middleware/admin.ts` exists and `app/pages/admin/dashboard.vue` already declares `definePageMeta({ layout: "admin", middleware: "admin" })` (lines 1–3). **Keep that line exactly as it is** — this story adds content, not guards.
- **Story 02 completed** ([`../middelware/02-story-layouts.md`](../middelware/02-story-layouts.md)): `app/layouts/admin.vue` provides the sidebar and the signed-in header band.
- Backend: **no** admin-stats or order-count endpoint exists. Verified — `OnlineStore.API/Controllers/` holds only `AuthController`, `CartController`, `OrdersController`, `ProductsController`, and `OrdersController` exposes no aggregate/count action for admins. Card scope is bounded by that fact (task 3).

---

## Story Goal

1. `/admin/dashboard` renders a **data-forward** back-office page that visibly differs from the customer home page — denser spacing, tabular data, no marketing imagery.
2. The admin's **name** and **role badge** are shown from the Pinia auth store, with a working logout.
3. Summary cards show **only real numbers**: total products, active products, out-of-stock products — all derivable from `GET /api/products`. **No fabricated orders or revenue figures.**
4. A recent-products table (latest 5) with edit/delete affordances pointing at `/admin/products`.
5. The admin sidebar highlights the current route in brand `primary`.

**Not in scope:** order/revenue analytics (no endpoint), charts, product CRUD itself (`app/pages/admin/products/index.vue` is still a placeholder — links point at it, they do not implement it), user management, and any new backend endpoint.

---

## Context — Read These Files First

1. `app/pages/admin/dashboard.vue` — all 10 lines. Line 2 is the `definePageMeta` you must preserve; the `<template>` (lines 5–9) is the placeholder you replace.
2. `app/layouts/admin.vue` — the whole file, 29 lines. Note: `aside` is `w-60 bg-gray-900` (lines 7–18), nav links are plain `hover:bg-gray-800` with **no active state** (lines 10–11), the header band shows `auth.user?.email` and `auth.user?.role` (lines 21–23), and logout already exists in the sidebar footer (lines 14–16). Tasks 1–2 change this file.
3. `app/stores/auth.ts` — lines 1–5 (`AuthUser` is `{ name, email, role }` — **no id, no avatar**), lines 26–29 (`isAuthenticated`, `isAdmin` where role is compared to the lowercase string `"admin"`), lines 41–49 (`logout()` clears cookie + localStorage and returns `navigateTo("/auth/login")`).
4. `app/composables/useProducts.ts` (created in Story 05) — `useProducts(page, pageSize, key)` returning `useFetch<PagedProducts>` with `server: false`. Read the `key` argument note before calling it here.
5. `OnlineStore.API/Controllers/ProductsController.cs` — lines 25–65. Critical for task 3: line 13 puts `[Authorize(Roles = "admin")]` on the controller, `List` is `[AllowAnonymous]` (line 25), and **lines 40–43 mean an admin-authenticated request sees inactive products too, while an anonymous one does not**. This page sends no token, so its counts are the **active-only** view — see Edge Cases.
6. `OnlineStore.API/Dtos/ProductListItemDto.cs` — `Id, Name, Price, Stock, IsActive, ImageUrl`. The table columns can use nothing else. No `CreatedAt` is exposed in the list payload, so **do not** add a "Created" column.
7. `app/pages/admin/products/index.vue` — all 10 lines, still "Product management coming soon." Confirm before pointing links at it.
8. `app/components/base/Button.vue` — the `variant` / `loading` / `block` props used by the table actions and error retry.
9. Grep for `isAdmin` across `app/` — confirms whether anything besides `app/middleware/admin.ts` consumes the getter before you rely on it in the badge.

---

## Frontend Tasks

### 1 — Active sidebar state

**File: `app/layouts/admin.vue`** — lines 10–11.

Add an active class to both `<NuxtLink>`s so the current section is obvious. Nuxt applies `router-link-active` / `router-link-exact-active` automatically; bind an explicit class instead of styling globals:

```vue
<NuxtLink
  to="/admin/dashboard"
  class="px-3 py-2 rounded hover:bg-gray-800"
  active-class="bg-primary text-white hover:bg-primary-600"
>
  Dashboard
</NuxtLink>
```

Apply the same `active-class` to the Products link. **Do not** use `exact-active-class` on `/admin/products` — a future `/admin/products/3` must keep the parent highlighted.

### 2 — Identity header with role badge

**File: `app/layouts/admin.vue`** — replace the header band (lines 21–23).

Show **name first** (the story asks for the admin's name; the current markup shows only email), then email as secondary text, then a role badge, then a logout button:

```vue
<header class="border-b bg-white px-6 py-3 flex items-center justify-between">
  <div class="flex flex-col leading-tight">
    <span class="text-sm font-semibold text-gray-900">{{ auth.user?.name ?? "Admin" }}</span>
    <span class="text-xs text-gray-500">{{ auth.user?.email }}</span>
  </div>
  <div class="flex items-center gap-3">
    <span class="rounded-full bg-primary-50 px-2.5 py-1 text-xs font-medium uppercase tracking-wide text-primary">
      {{ auth.user?.role }}
    </span>
    <button class="text-sm text-gray-600 hover:text-primary" @click="auth.logout()">Log out</button>
  </div>
</header>
```

The sidebar-footer logout (lines 14–16) **stays** — the header action is an addition, and both call the same `auth.logout()`.

### 3 — Summary cards component

**Create file: `app/components/admin/StatCard.vue`** — auto-imports as `<AdminStatCard>`.

- Props: `label: string`, `value: string | number`, `hint?: string`, `loading?: boolean`.
- Dense back-office styling: white card, `rounded-xl border border-gray-200 p-4` (**not** the customer page's `p-8`/`rounded-2xl`), label in `text-xs uppercase tracking-wide text-gray-500`, value in `text-2xl font-semibold text-gray-900`, optional `hint` in `text-xs text-gray-400`.
- `loading` → a `h-8 w-16 animate-pulse rounded bg-gray-100` bar in place of the value, so the card does not flash `0` before data lands.

**File: `app/pages/admin/dashboard.vue`** — render exactly **three** cards, each from a value the API actually returns:

| Card | Source | Note |
|---|---|---|
| Total products | `data.total` from the paged envelope | `total` is the **filtered** count, not a global count — see Edge Cases |
| Active products | count of `items.filter(p => p.isActive)` | derived from the fetched page only |
| Out of stock | count of `items.filter(p => p.stock <= 0)` | same page-scope caveat |

To keep the derived cards honest, fetch **two** pages: `useProducts(1, 5, "admin-recent-products")` for the table and `useProducts(1, 100, "admin-product-stats")` for the counts (100 is the API's clamp ceiling, `ProductsController.cs` line 35). Set each derived card's `hint` to "of first 100" so the number is never presented as a full-catalog aggregate.

**Do not add order, revenue, or customer-count cards.** No endpoint exists for them. Instead render one dashed-border placeholder tile reading "Orders & revenue — pending stats endpoint" and add a template comment naming the follow-up: an admin stats endpoint in `OnlineStore.API`.

### 4 — Recent products table

**Create file: `app/components/admin/RecentProductsTable.vue`** — auto-imports as `<AdminRecentProductsTable>`.

- Props: `products: ProductListItem[]`, `loading?: boolean`.
- `<table class="w-full text-sm">` inside `<div class="overflow-x-auto rounded-xl border border-gray-200 bg-white">`.
- Columns: **thumbnail** (`h-10 w-10 rounded object-cover`, via `useProductImage()`), **Name**, **Price** (`formatPrice`, `text-right tabular-nums`), **Stock** (`text-right`), **Status**, **Actions**.
- Status cell: `isActive` → green pill "Active", else grey pill "Inactive"; `stock <= 0` additionally shows an amber "Out of stock" pill. Both pills can appear.
- Actions cell: **Edit** as `<NuxtLink to="/admin/products">` and **Delete** as a `<button>` that is **`disabled`** with `title="Product CRUD arrives with the admin products story"`. **Do not** call `DELETE /api/products/{id}` from this page — destructive actions belong in the CRUD story with confirmation UI.
- `loading` → 5 skeleton rows. Empty `products` → a single full-width row, "No products yet."
- Header row `bg-gray-50 text-xs uppercase tracking-wide text-gray-500`; body rows `divide-y divide-gray-100 hover:bg-gray-50`.

### 5 — Assemble the page

**File: `app/pages/admin/dashboard.vue`** — replace the template, keep line 2 verbatim.

```vue
<script setup lang="ts">
definePageMeta({ layout: "admin", middleware: "admin" });

const auth = useAuthStore();
const stats = useProducts(1, 100, "admin-product-stats");
const recent = useProducts(1, 5, "admin-recent-products");
</script>
```

Structure, top to bottom:

1. Page title row: `<h1 class="text-xl font-semibold">Dashboard</h1>` plus a one-line greeting using `auth.user?.name`. Denser than the customer page — `text-xl`, not `text-4xl`.
2. Card grid: `grid gap-4 sm:grid-cols-2 lg:grid-cols-4` (three stat cards + the pending-stats placeholder tile).
3. Section heading "Recent products" with a "Manage products" `NuxtLink` to `/admin/products` on the right.
4. `<AdminRecentProductsTable>`.
5. A single error notice above the grid when **either** fetch errors: "Couldn't load product data." plus a **Retry** `BaseButton` calling both `refresh` functions. Never render a raw error object.

Page wrapper spacing `space-y-6` — deliberately tighter than Story 05's `space-y-16`, reinforcing the back-office density contrast the story requires.

---

## Backend Tasks

**No backend changes required.** Every number on this page comes from the existing `GET /api/products`. The absent stats endpoint is recorded as a follow-up in task 3, **not** implemented here.

---

## Edge Cases & Failure Modes

- **`total` is a filtered count, not a catalog count.** `ProductsController.List` computes `total` *after* applying the `IsActive` and `search` filters (lines 40–50). This page sends no `search`, but it also sends **no Authorization header**, so `User.IsInRole("admin")` is false server-side and `total` counts **active products only**. Label the card "Total products" and set its `hint` to "active products" — do **not** present it as the full catalog including inactive rows.
- **Consequence for the "Active products" card:** with the anonymous view, every returned item has `isActive === true`, so that count equals the number of items returned, not a meaningful ratio. Keep the card (it becomes accurate the moment the token is attached) and make the `hint` state the scope. **Flagged uncertainty:** attaching `auth.token` as an `Authorization: Bearer` header on the admin fetches would make all three cards reflect the true admin view. That requires a fetch-header change in `useProducts`, which Story 05 did not build. **Confirm with the user** before adding it; if approved, add an optional `headers` argument rather than a second composable.
- **Derived counts are page-scoped.** `pageSize=100` is the API ceiling (`Math.Clamp(pageSize, 1, 100)`, line 35); a catalog above 100 products makes the derived cards a sample. The `hint` text must say "of first 100" — a silently-truncated aggregate on a dashboard is worse than no card.
- **`auth.user` is null on a hard refresh before hydration.** The store is restored by `app/plugins/auth.ts`; every template read uses `auth.user?.…` with a fallback string so the header never renders "undefined".
- **Role casing.** `isAdmin` compares to the lowercase `"admin"` (`app/stores/auth.ts` line 28). The badge renders `auth.user?.role` raw with a CSS `uppercase` class — **do not** hand-write "Admin", and do not add a second role comparison with different casing.
- **Logout from two places.** Both the header button and the sidebar footer call `auth.logout()`, which clears state and returns `navigateTo("/auth/login")` (lines 41–49). Double-clicking is harmless — the second call clears already-empty state.
- **Non-admin reaching the route.** `app/middleware/admin.ts` (Story 03) redirects before this page renders. This page adds **no** client-side role check of its own; the server's `[Authorize(Roles = "admin")]` on the controller (line 13) remains the real enforcement for mutations.
- **SSR and the dev certificate.** `useProducts` sets `server: false` (Story 05), so both fetches are client-only and the .NET self-signed cert cannot break the SSR pass. Do not override it here.
- **Two fetches, one key each.** `"admin-product-stats"` and `"admin-recent-products"` must differ from Story 05's `"home-new-arrivals"`; a duplicated `key` makes `useFetch` return the other page's cached payload with the wrong `pageSize`.
- **API down.** Both fetches error; the single combined notice renders once, not twice, and the cards show their `loading`/dash state rather than `0`.
- **Empty catalog.** `total: 0` renders `0` in the cards (correct, not an error) and the table's "No products yet" row.
- **Missing thumbnails.** `useProductImage()` returns `null` for an empty `imageUrl`; the table cell renders a neutral `bg-gray-100` square, never a broken image.
- **Delete is intentionally inert.** The disabled button exists so the eventual CRUD story has a place to wire into. If it were enabled without a confirm dialog it would allow one-click destruction of a product from a summary screen.

---

## Test Plan

**No test runner is configured** (`package.json` defines only `build`, `dev`, `generate`, `preview`, `postinstall`; no Vitest dependency). Verification is **manual** — see Verification Steps. When Vitest + `@nuxt/test-utils` is introduced, add:

1. **Component — `AdminStatCard`:** renders `label`/`value`/`hint`; `loading: true` renders the pulse bar and **not** the value.
2. **Component — `AdminRecentProductsTable`:** 5 products → 5 rows; `isActive: false` → "Inactive" pill; `stock: 0` → "Out of stock" pill; empty array → the "No products yet" row; `loading` → 5 skeleton rows; the Delete button is `disabled`.
3. **Component — `pages/admin/dashboard.vue`** with `useProducts` mocked: asserts **two** calls with `pageSize` 100 and 5 and distinct keys; an errored fetch renders exactly one error notice; derived card values match the mocked `items`.
4. **Unit — derived counts:** active and out-of-stock counts over a fixture containing `stock: 0`, `stock: 12`, and `isActive: false` rows.
5. **Integration — guard regression:** navigating to `/admin/dashboard` as a customer redirects (Story 03 behaviour, must not change).

---

## Verification Steps

1. **Frontend builds:** in `online-store-frontend/`, `pnpm build` succeeds — this catches auto-import naming errors (`app/components/admin/StatCard.vue` → `<AdminStatCard>`, **not** `<AdminAdminStatCard>`).
2. **Backend builds:** `dotnet build` in `OnlineStore.API/` — unchanged; run it only to confirm no C# was touched.
3. **Admin path:** `pnpm dev`, log in as an admin, go to `/admin/dashboard`. The header shows the admin's **name** on the first line, email below, an uppercase role badge in `#1C3684`-tinted styling, and a working "Log out".
4. **Sidebar active state:** on `/admin/dashboard` the Dashboard link is filled brand navy; navigating to `/admin/products` moves the highlight.
5. **Cards:** three real cards plus the dashed "Orders & revenue — pending stats endpoint" tile. Cross-check "Total products" against the `total` field in the network response for `?page=1&pageSize=100`.
6. **Two requests, correct params:** the network tab shows requests for `pageSize=100` and `pageSize=5` — not one request reused for both.
7. **Table:** up to 5 newest products with thumbnails resolving from `https://localhost:7225/images/products/…` (no `/api`), right-aligned formatted prices, correct status pills, "Edit" navigating to `/admin/products`, and "Delete" visibly disabled.
8. **Loading state:** throttle to Slow 3G and reload — stat values show pulse bars (never `0`) and the table shows 5 skeleton rows.
9. **Error state:** stop the API and reload — exactly **one** error notice, no unhandled overlay; **Retry** re-issues both requests after the API restarts.
10. **Density contrast:** side by side with `/`, the dashboard is visibly tighter (`space-y-6` vs `space-y-16`, `text-xl` vs `text-4xl`, bordered white cards vs full-bleed brand bands).
11. **Regression — guards:** log out and hit `/admin/dashboard` directly → redirected to `/auth/login`; log in as a customer → redirected by `app/middleware/admin.ts`. Both unchanged from Story 03.
12. **Regression — customer home:** `/` still renders Story 05's sections and still issues its `pageSize=8` request.

---

## Done Criteria

- [ ] `app/pages/admin/dashboard.vue` keeps `definePageMeta({ layout: "admin", middleware: "admin" })` **unchanged** and renders title row, card grid, and recent-products table.
- [ ] `app/layouts/admin.vue` header shows **name**, email, uppercase role badge, and a logout action; the sidebar-footer logout still works.
- [ ] Both sidebar links use `active-class` with `primary` styling; `/admin/products/*` would keep the Products link highlighted.
- [ ] `app/components/admin/StatCard.vue` and `app/components/admin/RecentProductsTable.vue` exist and are consumed by the page.
- [ ] Exactly three data-backed cards, each sourced from `GET /api/products`; every derived card carries a `hint` disclosing its scope.
- [ ] **No** order, revenue, or customer-count numbers appear anywhere; the missing-endpoint follow-up is represented by the placeholder tile and a code comment.
- [ ] The table's Delete action is disabled and no `DELETE` request can be issued from this page.
- [ ] Loading, error, and empty states are each reachable and verified.
- [ ] `useProducts` is reused from Story 05 with **distinct** cache keys; no second products fetch composable was created.
- [ ] Dashboard density is visibly distinct from the customer home page; no arbitrary `bg-[#…]` or raw `px` values introduced.
- [ ] `pnpm build` succeeds and the Story 03 route guards still redirect correctly for logged-out and customer users.
