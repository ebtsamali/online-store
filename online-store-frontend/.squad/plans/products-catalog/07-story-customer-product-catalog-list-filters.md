# Story 07 — Customer Product Catalog (list + filters)

Build `app/pages/products/index.vue`: a public, paginated grid of active products backed by `GET /api/products`, with a debounced search box and URL-synced query state. Category/brand filter dropdowns are described as an explicit, separate follow-up section because the backend does not support them yet.

---

## Prerequisites

- **Story 05 completed** ([`../home-page/05-story-customer-home-page.md`](../home-page/05-story-customer-home-page.md)): created the shared foundation this story reuses — `app/composables/useProducts.ts`, `app/composables/useProductImage.ts`, `app/utils/format.ts`, `app/components/ProductCard.vue`, and the `primary` Tailwind theme in `tailwind.config.ts`. This story does **not** re-implement any of them from scratch; it extends `useProducts.ts`.
- Backend `GET /api/products` is already implemented, `[AllowAnonymous]`, and unchanged for this story — `OnlineStore.API/Controllers/ProductsController.cs` lines 25–65. **No backend change is required or permitted in this story.**
- **Soft dependency, not blocking:** the backend story "Categories and Brands CRUD" (`OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md`) has been **planned** (adds `GET /api/categories`, `GET /api/brands`, and optional `categoryId`/`brandId` query params on `GET /api/products`) but is **not yet implemented in code** — verified: `ProductsController.List` (lines 27–65) has no `categoryId`/`brandId` parameters today, and `OnlineStore.API/Controllers/` contains no `CategoriesController` or `BrandsController` file yet. This story ships the list/search/pagination page now; see `## Optional Follow-up — Category/Brand Filters` for the filter UI, which is explicitly out of the Done Criteria.
- The API must be running for the grid to populate. `nuxt.config.ts` points `apiBase` at `https://localhost:7225/api`; the API's CORS policy allows only `http://localhost:3000`, so run the frontend on the default port (same constraint documented in Story 05).

---

## Story Goal

1. A new public page at `/products` (`app/pages/products/index.vue`) that lists all active products in a paginated grid, reusing `<ProductCard>`.
2. A debounced search input that filters by name via the existing `search` query param on `GET /api/products`.
3. Pagination controls (prev/next) driven by the `page`/`pageSize`/`total` fields of the paged response, with prev disabled on page 1 and next disabled on the last page.
4. Search and page state reflected in the URL query string (`?search=&page=`) so results are shareable and back-button-friendly.
5. Loading (skeleton grid), error (retry), and empty (no-results) states, mirroring the pattern already used in `app/pages/index.vue`.
6. The nav links that already point at `/products` (home page hero/CTA, `default.vue` header) stop 404ing.

**Not in scope:** category/brand filter dropdowns (see the explicitly separate, non-blocking follow-up section below), sorting, wishlist/favorites, product comparison, and infinite scroll. `app/pages/products/[id].vue` (product detail) is a separate, later story — `<ProductCard>` already links to `/products/{id}` and that link continues to 404 until that story lands; this story does not fix it.

---

## Context — Read These Files First

1. `app/composables/useProducts.ts` — all 29 lines. `useProducts(page, pageSize, key)` currently accepts only `page`/`pageSize`/`key` and calls `useFetch` with `query: { page, pageSize }` (line 22–23), `server: false` (line 26), and a `default` returning `{ items: [], page, pageSize, total: 0 }` (line 27). This story extends the signature to also accept and forward an optional `search` term — the backend already supports `search` (`ProductsController.List` line 28, `[FromQuery] string? search`) but the composable does not send it yet.
2. `app/composables/useProductImage.ts` — all 11 lines. Reuse `useProductImage()` unchanged inside `<ProductCard>` (already wired there); this page does not call it directly.
3. `app/components/ProductCard.vue` — all 48 lines. Already renders image/placeholder, name, `formatPrice`, stock/out-of-stock pill, and links to `/products/{id}`. Reuse as-is — no changes.
4. `app/utils/format.ts` — all 11 lines. `formatPrice` — reuse unchanged; nothing to add here.
5. `app/pages/index.vue` — lines 1–2 (`useProducts(1, 8, "home-new-arrivals")` call) and lines 52–93 (the three-state grid: `pending` skeleton at lines 53–65, `error` notice with a `refresh()` retry button at lines 68–76, empty state at lines 79–84, loaded grid at lines 87–93). This is the exact loading/error/empty pattern to mirror on `/products`, adapted to a taller grid and with pagination controls appended.
6. `app/layouts/default.vue` — line 10, the header "Products" `<NuxtLink to="/products">` that currently 404s (no `app/pages/products/` directory exists yet). This story's page is the fix; no change to `default.vue` itself is needed.
7. `app/composables/useApi.ts` — all 4 lines. `useApi()` returns the base **including** `/api`; the new/extended `useProducts` call builds on the same base, unchanged.
8. `OnlineStore.API/Controllers/ProductsController.cs` — lines 25–65 (`List` action). Confirms: `search` (line 28), `page` (line 29, defaults to 1, clamped `Math.Max(page, 1)` at line 34), `pageSize` (line 30, defaults to 20, clamped `Math.Clamp(pageSize, 1, 100)` at line 35) are the only query parameters today; `search` matches via `EF.Functions.ILike` (line 47, case-insensitive substring); non-admin callers see only `IsActive` products (lines 40–43); response envelope is `new { items, page, pageSize, total }` (line 59).
9. `.squad/plans/home-page/05-story-customer-home-page.md` — sibling plan for tone/structure and the precedent this story follows for `useProducts`/`ProductCard`/loading-error-empty states (its task 5 and task 7.2).
10. Grep for `to="/products"` across `app/` before starting, to confirm every existing nav target this story must stop 404ing: `app/pages/index.vue` (hero CTA, "View all products" link, closing CTA) and `app/layouts/default.vue` (header link). `app/components/ProductCard.vue`'s `/products/${product.id}` link is a **different**, still-unimplemented route (`app/pages/products/[id].vue`) and is unaffected by this story.

---

## Frontend Tasks

### 1 — Extend `useProducts` to accept an optional search term

**File: `app/composables/useProducts.ts`**

Add an optional `search` parameter and forward it in the `useFetch` query, and widen the `key` so callers building a dynamic key (per page/search) still get correct de-duplication. Keep the existing two-arg call sites (`useProducts(1, 8, "home-new-arrivals")` in `app/pages/index.vue`) working — make `search` optional and appended after the existing params:

```ts
export const useProducts = (
  page: number,
  pageSize: number,
  key: string,
  search?: string,
) => {
  const apiBase = useApi();
  return useFetch<PagedProducts>(`${apiBase}/products`, {
    key,
    query: { page, pageSize, search: search || undefined },
    // Client-only: the .NET dev server uses a self-signed HTTPS certificate,
    // which the Nitro server rejects during SSR.
    server: false,
    default: (): PagedProducts => ({ items: [], page, pageSize, total: 0 }),
  });
};
```

`search: search || undefined` keeps an empty string out of the query string entirely (an empty `search=` still reaches the backend's `string.IsNullOrWhiteSpace` check at `ProductsController.cs` line 45 and is harmless there, but omitting it keeps the URL and the network request clean when there is no search term). `ProductListItem` and `PagedProducts` (lines 1–15) are unchanged — the response shape does not change.

### 2 — Create the products catalog page

**Create file: `app/pages/products/index.vue`**

No `definePageMeta` — inherits the `default` layout implicitly (same convention as `app/pages/index.vue`) and stays public with no auth middleware.

Page state, driven by the URL query string via `useRoute`/`useRouter`:

```vue
<script setup lang="ts">
const route = useRoute();
const router = useRouter();

const page = ref(Number(route.query.page) || 1);
const searchInput = ref(String(route.query.search || ""));
const search = ref(String(route.query.search || ""));
const pageSize = 12;

let debounceTimer: ReturnType<typeof setTimeout> | undefined;
watch(searchInput, (value) => {
  if (debounceTimer) clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => {
    search.value = value;
    page.value = 1;
  }, 400);
});

watch([page, search], ([newPage, newSearch]) => {
  router.push({
    query: {
      ...(newPage > 1 ? { page: newPage } : {}),
      ...(newSearch ? { search: newSearch } : {}),
    },
  });
});

const { data, pending, error, refresh } = useProducts(
  page.value,
  pageSize,
  "products-catalog",
  search.value,
);

watch([page, search], () => {
  refresh();
});
</script>
```

Use a plain `key` string (`"products-catalog"`) — unlike the home page's `useFetch`, this page is the only caller of this particular `key`, so there is no cross-page cache collision to avoid (see Story 05's `useProducts.ts` comment on distinct keys per call site).

**Note on `useFetch` reactivity:** `useProducts` as extended in task 1 takes plain values, not refs, so the `page`/`search` values captured at the initial call do not automatically re-trigger `useFetch`. The `watch([page, search], () => refresh())` block above re-issues the request on change; `refresh()` re-runs the same `useFetch` call, but because `query` was built from the plain values passed at setup time, `refresh()` alone will **not** pick up new `page`/`search` values. To make this correct, pass **refs** to a version of `useProducts` that accepts reactive query params, or inline the `useFetch` call directly in the page with `query` set to a `computed`. Prefer the latter for this page specifically — call `useFetch` directly in `app/pages/products/index.vue` with:

```ts
const { data, pending, error, refresh } = useFetch<PagedProducts>(
  `${useApi()}/products`,
  {
    key: "products-catalog",
    query: computed(() => ({
      page: page.value,
      pageSize,
      search: search.value || undefined,
    })),
    server: false,
    watch: [page, search],
    default: (): PagedProducts => ({ items: [], page: page.value, pageSize, total: 0 }),
  },
);
```

This keeps `PagedProducts`/`ProductListItem` imported from `useProducts.ts` (do not redefine them) but calls `useFetch` directly so `query` stays reactive via `computed` and Nuxt's built-in `watch` option handles refetching — do not also keep the manual `watch([page, search], () => refresh())` block if using this approach, to avoid a double fetch. Task 1's extension to `useProducts.ts` is still required for the home page's use case to stay symmetric and for any future non-reactive caller, but this page uses the inline reactive form.

Template, in this order:

1. **Heading** — `<h1>` "Products" (`text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl`, matching `app/pages/index.vue` line 44 style) plus a search input below it: `<input v-model="searchInput" type="search" placeholder="Search products..." />` styled with existing Tailwind utility conventions (border, `rounded-lg`, `px-4 py-2`, `focus:ring-primary` — no arbitrary hex, matching the `focus:ring-primary` pattern already used in `app/components/base/Button.vue` line 33).
2. **Loading** — `v-if="pending"`: a skeleton grid identical in structure to `app/pages/index.vue` lines 53–65 (`grid gap-6 sm:grid-cols-2 lg:grid-cols-4`, `animate-pulse` cards), sized to `pageSize` (12) skeleton cards instead of 8.
3. **Error** — `v-else-if="error"`: bordered notice matching `app/pages/index.vue` lines 68–76 ("We couldn't load products right now." + a `BaseButton` "Retry" calling `refresh()`).
4. **Empty** — `v-else-if="data && data.items.length === 0"`: a distinct friendly message, e.g. "No products found. Try a different search." (distinct wording from the error state and from the home page's "No products yet — check back soon." since this is a filtered/searched view, not an empty catalog).
5. **Loaded grid** — `v-else`: `grid gap-6 sm:grid-cols-2 lg:grid-cols-4` of `<ProductCard :product="product" :key="product.id" v-for="product in data?.items ?? []" />`, same as `app/pages/index.vue` lines 87–93.
6. **Pagination controls** — below the grid, rendered whenever `data.total > 0`: a prev/next pair. Prev is disabled (and does not decrement) when `page.value <= 1`; next is disabled (and does not increment) when `page.value * pageSize >= data.total`. Use `<BaseButton variant="secondary" :disabled="...">` for both, with a page indicator between them (e.g. "Page {{ page }} of {{ Math.max(1, Math.ceil((data?.total ?? 0) / pageSize)) }}").

### 3 — No changes to `default.vue` or `ProductCard.vue`

**No changes required** to `app/layouts/default.vue` — its existing header link at line 10 (`<NuxtLink to="/products">Products</NuxtLink>`) resolves correctly once task 2's page file exists; no edit needed there. **No changes required** to `app/components/ProductCard.vue` — it is reused exactly as built in Story 05.

---

## Optional Follow-up — Category/Brand Filters (NOT required for this story, backend not ready)

This section is **explicitly out of the Done Criteria** below. Do not implement it as part of this story; it is documented so a later story can pick it up once the backend lands.

- The backend story "Categories and Brands CRUD" (`OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md`) plans `GET /api/categories`, `GET /api/brands` (both public, return `{ id, name }[]`), and optional `categoryId`/`brandId` query params on `GET /api/products` — **none of this exists in the codebase yet** (verified: `OnlineStore.API/Controllers/` has no `CategoriesController.cs` or `BrandsController.cs`, and `ProductsController.List` at lines 27–65 has no `categoryId`/`brandId` parameters).
- Once that backend story ships, add two `<select>` dropdowns (category, brand) above the grid in `app/pages/products/index.vue`, each backed by a `useFetch` call to `GET /api/categories` / `GET /api/brands`, wired to `categoryId`/`brandId` refs that join the existing `page`/`search` reactive query object in task 2's `computed(() => ({ ... }))`.
- Until then, ship this story without any filter UI — no disabled/greyed-out dropdown placeholders, since a UI element for a non-existent capability is worse than no UI element (same principle Story 05 applied to the static category teaser on the home page).

---

## Edge Cases & Failure Modes

- **Search race / stale response ordering:** typing quickly fires the debounced 400 ms timer once per pause, but a slow network could still resolve an older search after a newer one if the user types, waits >400 ms, then types again before the first request resolves. `useFetch`'s `watch`/`key`-based refetch (task 2) replaces `data`/`pending`/`error` on each new call; there is no manual promise-ordering fix in this story — flag this as a known risk to test manually (see Test Plan) rather than adding a cancellation token, since `useFetch` does not expose one directly.
- **Page out of range after a search narrows results:** if the user is on page 3 and then searches for a term with only 1 page of results, `page.value` is reset to 1 by the `searchInput` watcher in task 2 before the new fetch fires — verified by the `page.value = 1` line inside the debounce callback. Without that reset, the backend's `Skip((page - 1) * pageSize)` (`ProductsController.cs` line 54) would silently return an empty `items` array for an out-of-range page while `total` is still nonzero, which would look like a bug, not the empty state.
- **`/products` and the pre-existing home-page CTA 404:** Story 05 documented that every `to="/products"` link 404s until this story lands (`app/pages/index.vue` hero/CTA links, `app/layouts/default.vue` line 10). After this story, all of those resolve; the **only** remaining 404 target is `ProductCard`'s `/products/{id}` link, which is a separate, later story — do not attempt to stub `app/pages/products/[id].vue` here.
- **Empty catalog vs. no search results:** `data.items.length === 0` covers both "the catalog has zero active products" and "this search matched nothing." The empty-state copy in task 2 step 4 is worded generically enough to cover both cases without over-claiming ("no products found" rather than "no products yet"), since the page cannot distinguish the two without an extra request.
- **`pageSize=12` is within the server clamp** (`Math.Clamp(pageSize, 1, 100)`, `ProductsController.cs` line 35) — safe as chosen; not configurable by the user in this story.
- **SSR vs. self-signed dev certificate:** identical to Story 05's finding — `server: false` (kept in both the extended `useProducts.ts` and the inline `useFetch` in task 2) avoids the Nitro server making the request during SSR, where Node would reject the API's self-signed HTTPS certificate.
- **Browser back/forward through paginated/searched state:** because `page`/`search` are pushed into `route.query` (task 2), the browser back button restores the prior query string; the page's `ref`s are seeded from `route.query` only on initial mount (`Number(route.query.page) || 1"`, `String(route.query.search || "")`) and do **not** re-sync from `route.query` on a back/forward navigation within this story — that is a known gap; a fully round-trippable implementation would add a `watch(() => route.query, ...)` to resync the refs, but the intake only requires the query string to be shareable/back-button-friendly for a **fresh load** of a URL, not live resync during in-page back/forward. Flag this as a follow-up if reviewers expect live resync.
- **Non-numeric or negative `page` in the URL** (e.g. a hand-edited `?page=abc` or `?page=-5`): `Number(route.query.page) || 1` in task 2 falls back to `1` for `NaN` (from `"abc"`), but **not** for a valid negative number like `-5`, which would be sent to the backend and get `Math.Max(page, 1)`-clamped server-side (`ProductsController.cs` line 34) to `1` anyway — the page still renders correctly, just via server-side clamping rather than client-side validation.

---

## Test Plan

**No test runner is configured** in this project — confirmed by Story 05 (`package.json` has `build`/`dev`/`generate`/`preview`/`postinstall` only, no Vitest dependency), still true as of this story. Verification is manual (see Verification Steps). If Vitest + `@nuxt/test-utils` is introduced later, these are the tests to add, matching the structure of `.squad/plans/home-page/05-story-customer-home-page.md`'s Test Plan section:

1. **Unit — `useProducts`:** calling with a `search` argument includes `search` in the `useFetch` query options; calling with `search` omitted or `""` omits `search` from the query (i.e., `search: undefined`).
2. **Component — `app/pages/products/index.vue`** with `useFetch` mocked: `pending` renders 12 skeleton cards; `error` renders the retry notice with no grid; `items: []` renders the empty-search-results message; a full page of items renders that many `ProductCard`s; the prev button is disabled when `page === 1`; the next button is disabled when `page * pageSize >= total`.
3. **Component — pagination boundary:** `total: 12, pageSize: 12, page: 1` disables next (last page reached exactly); `total: 13, pageSize: 12, page: 1` enables next.
4. **Smoke — URL sync:** typing into the search box and waiting past the debounce updates `route.query.search`; clicking next/prev updates `route.query.page`.

---

## Verification Steps

1. **Frontend builds:** in `online-store-frontend/`, run `pnpm build`. Must succeed — a wrong auto-import name for `<ProductCard>` or `<BaseButton>` fails here.
2. **Route resolves:** with `pnpm dev` running, open `/products` directly — no 404, the page renders inside the existing header/footer from `default.vue`.
3. **Nav links fixed:** click "Shop now" and "View all products" on `/` (`app/pages/index.vue`) and the header "Products" link (`app/layouts/default.vue` line 10) — all land on `/products` without a 404.
4. **Data path:** with the API running, load `/products` and confirm the network tab shows a request to `https://localhost:7225/api/products?page=1&pageSize=12` (no `search` param on first load), returning `{ items, page, pageSize, total }`.
5. **Search:** type a product name fragment into the search box; after the debounce, confirm exactly one new request fires with `search=<term>` and `page=1` (reset even if you had navigated to page 2+ beforehand), and the URL updates to `?search=<term>`.
6. **Pagination:** with more than 12 active products seeded, click "Next" — confirm the URL updates to `?page=2`, a new request fires with `page=2`, prev becomes enabled, and next disables once the last page is reached (`page * pageSize >= total`).
7. **Loading state:** throttle the network to Slow 3G and reload `/products` — 12 skeleton cards render in the grid positions before data lands.
8. **Error state:** stop the API and reload `/products` — the retry notice renders, no unhandled error overlay; clicking Retry re-issues the request once the API is back.
9. **Empty state:** search for a term that matches nothing — confirm the "no products found" message renders, distinct from the error notice.
10. **Shareable URL:** copy a URL like `/products?search=foo&page=2`, open it in a fresh tab — confirm the search box is pre-filled with "foo" and the grid loads page 2 of that search directly (no interim page-1/no-search flash beyond the initial fetch).
11. **Backend unchanged:** `dotnet build` in `OnlineStore.API/` — expected unchanged, this story touches no C#.
12. **Responsive:** at 375 px the grid is one column and pagination controls stack without overflow; at 1280 px it is four columns.

---

## Done Criteria

- [ ] `app/pages/products/index.vue` exists, has no `definePageMeta`, is public (no auth middleware), and is reachable at `/products`.
- [ ] The page fetches `GET /api/products?search=&page=&pageSize=` through the extended `useProducts.ts` or an equivalent reactive `useFetch` call, and renders a grid of `<ProductCard>`.
- [ ] A debounced search input updates the `search` query param, resets to page 1, refetches, and is reflected in the URL query string.
- [ ] Prev/next pagination controls are driven by `page`/`pageSize`/`total`; prev is disabled on page 1, next is disabled on the last page.
- [ ] Loading (skeleton grid sized to the page's `pageSize`), error (bordered notice + Retry calling `refresh()`), and empty (distinct "no products found" wording) states are each reachable and verified.
- [ ] Category/brand filters are **not** implemented in this story — the follow-up section above documents them for later, and no disabled/placeholder filter UI is added.
- [ ] Every existing `to="/products"` link (`app/pages/index.vue` hero/CTA/"View all products", `app/layouts/default.vue` header) resolves without a 404.
- [ ] `pnpm build` succeeds; no arbitrary `bg-[#…]`/`text-[#…]` or raw `px` spacing introduced.
