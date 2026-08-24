# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/products-catalog/customer-product-catalog-list-filters/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Customer Product Catalog (list + filters)
- **Feature slug (folder under `plans/`):** `products-catalog`

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
Customer Product Catalog (list + filters)
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Build the `/products` page: a browsable, paginated grid of all active products, with search and (once the categories-and-brands backend story lands) category/brand filters. This is the page every "Shop"/"Products" nav link already points to across the site (home page, layout header) but which currently 404s — it's the entry point into the catalog before product details, cart, checkout.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] New page `app/pages/products/index.vue`, `layout: 'default'`, no auth middleware (public page), reachable at `/products`.
- [ ] Fetches `GET /api/products?search=&page=&pageSize=` via a new/extended composable (reuse or extend `useProducts.ts`); grid of `<ProductCard>` (already exists, already links to `/products/{id}`).
- [ ] Search input (debounced) updates the `search` query param and refetches; reflect state in the URL query string so results are shareable/back-button-friendly (`useRoute`/`useRouter` or `definePageMeta` + `useFetch` watch on route.query).
- [ ] Pagination controls (prev/next or numbered) driven by the `page`/`pageSize`/`total` fields of the paged response; disable prev on page 1 and next on the last page.
- [ ] Loading state: skeleton grid matching `ProductCard` dimensions (same pattern as `index.vue` home page). Error state: bordered notice + Retry button calling `refresh()`. Empty state (0 results): friendly "no products found" message, distinct from the error state.
- [ ] If the categories-and-brands backend story (`categoryId`/`brandId` query params on `GET /api/products`, plus `GET /api/categories`/`GET /api/brands`) has landed by the time this is implemented, add category/brand filter dropdowns wired to those params; if not yet landed, ship the page without filters and leave a clearly marked follow-up (do not block this story on the backend story).
- [ ] Nav links currently pointing at `/products` (home page hero/CTA, `default.vue` header "Products" link) continue to work unchanged — no 404 after this ships.
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

- **Blocked by / related ids:** Soft dependency on backend story "Categories and Brands CRUD" (`OnlineStore.API/.squad/stories/categories-and-brands/`) for the filter dropdowns only — the list/search/pagination part has no dependency and should ship regardless.
- **Depends on code areas or other stories:** `app/composables/useProducts.ts`, `app/components/ProductCard.vue`, `app/utils/format.ts`, `app/layouts/default.vue`, home page (`app/pages/index.vue`) for the loading/error/empty-state pattern to mirror.

## Extra notes (optional)

- This is the first of a customer-journey chain: products-catalog → product-details → cart-page → checkout-payment → orders-history. Keep the URL/query-param and composable conventions here consistent since later stories will link into this page (e.g. "back to products").

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.
- `GET /api/products` (public, `AllowAnonymous`): query `search?`, `page=1`, `pageSize=20` (clamped 1–100 server-side); response `{ items: ProductListItemDto[], page, pageSize, total }`; `ProductListItemDto { id, name, price, stock, isActive, imageUrl }`. Non-admin callers only ever see `isActive=true` items (already enforced server-side).
- Use `useApi()` for the base URL (includes `/api`), `useProductImage()` to resolve `imageUrl`.
- Follow existing conventions: `useFetch` with `server:false`, unique `key`, Tailwind only (no arbitrary hex), spacious customer-UI spacing (`p-8`, `rounded-2xl`, `space-y-16` style already used in `index.vue`).

## Out of scope

- Sorting (price/name/newest) — not requested, skip unless trivial.
- Category/brand filters if the backend story hasn't landed yet (see Dependencies) — ship list+search+pagination only in that case.
- Wishlist/favorites, product comparison, infinite scroll (use pagination, not infinite scroll).
