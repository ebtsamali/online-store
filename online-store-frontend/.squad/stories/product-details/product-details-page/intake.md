# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/product-details/product-details-page/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Product Details Page
- **Feature slug (folder under `plans/`):** `product-details`

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
Product Details Page
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Build the product detail page at `/products/[id]`, which `ProductCard` (used on both the home page and the products catalog page) already links to. Shows full product info and lets the customer add it to their cart with a chosen quantity.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] New page `app/pages/products/[id].vue`, `layout: 'default'`, no auth middleware (public — anyone can view a product, login only required to add to cart per backend's `[Authorize]` on `POST /api/cart`).
- [ ] Fetches `GET /api/products/{id}` (public); renders image (via `useProductImage`, with placeholder for missing image, matching `ProductCard`'s placeholder pattern), name, description, price (via `formatPrice`), stock, and an out-of-stock indicator when `stock === 0`.
- [ ] 404 handling: if the API returns 404 (missing or inactive-and-non-admin product), show a clear "product not found" state with a link back to `/products` — do not show a raw error.
- [ ] Quantity selector (stepper or number input) clamped between 1 and the product's `stock` (disable/hide entirely if `stock === 0`).
- [ ] "Add to cart" button: if `!isAuthenticated` (from `useAuthStore`), redirect to `/auth/login` (optionally preserving a return path) instead of calling the API; if authenticated, calls `POST /api/cart { productId, quantity }` and shows a success toast (`vue-sonner`) on 201, or an error toast on failure (400 insufficient stock, etc. — surface `e?.data?.message` when present).
- [ ] Loading state while fetching product details (skeleton); error state (non-404 failures) with Retry.
- [ ] Link/breadcrumb back to `/products`.
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

- **Blocked by / related ids:** None hard; conceptually follows "Customer Product Catalog (list + filters)" in the customer journey but does not require it to be finished first (`ProductCard` already links here from the home page today).
- **Depends on code areas or other stories:** `app/components/ProductCard.vue`, `app/composables/useProductImage.ts`, `app/utils/format.ts`, `app/stores/auth.ts`, `app/utils/validation.ts` (none needed for validation here, but `fieldErrors` pattern if any form is added). Feeds into "Cart Page Integration" (customer must be able to reach cart after adding here).

## Extra notes (optional)

- Second story in the customer-journey chain: products-catalog → **product-details** → cart-page → checkout-payment → orders-history.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.
- `GET /api/products/{id}` → `ProductDetailDto { id, name, description, price, stock, categoryId, brandId, isActive, createdAt, imageUrl }` (no category/brand *names*, only ids — until the categories-and-brands backend story lands there's nothing to resolve them to; just don't display raw ids to the customer).
- `POST /api/cart` (`[Authorize]`, any role) body `{ productId, quantity }` → 201 `CartItemDto`; 400 if qty≤0 or exceeds available stock; 404 if product missing/inactive.
- `useApi()` for base URL, `$fetch` (not `useFetch`) for the add-to-cart mutation since it's a one-off POST, in a `try/catch/finally` with a `loading` ref — mirror the `auth/login.vue` pattern.

## Out of scope

- Reviews/ratings, related products, image galleries (backend only exposes a single `imageUrl`).
- Editing/deleting the product from this page (that's the admin CRUD story).
