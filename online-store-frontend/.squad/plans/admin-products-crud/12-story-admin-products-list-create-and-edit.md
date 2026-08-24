# Story 12 — Admin Products List, Create and Edit

Replace the `app/pages/admin/products/index.vue` placeholder ("Product management coming soon") with a real, paginated, searchable admin product list that includes inactive products, wires up the currently-disabled Edit/Delete actions in `AdminRecentProductsTable`, and adds the two missing pages — create and edit — both submitting through the backend's `multipart/form-data` endpoints.

---

## Prerequisites

- None. This story stands on its own; it does not depend on any other `.squad/plans/` story landing first.
- Backend `GET /api/products`, `POST /api/products`, `PUT /api/products/{id}`, `DELETE /api/products/{id}` are already implemented and unchanged for this story — `OnlineStore.API/Controllers/ProductsController.cs`. **No backend change is required or permitted in this story.**
- **Soft dependency, not blocking:** the backend story "Categories and Brands CRUD" (`OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md`) is planned but **not implemented** — verified: `OnlineStore.API/Controllers/` contains no `CategoriesController.cs` or `BrandsController.cs`, and there is no `GET /api/categories`/`GET /api/brands` endpoint anywhere in the codebase. Both create and edit forms in this story use **plain numeric id inputs** for category and brand; upgrading to a `<select>` populated from a categories/brands endpoint is an explicit out-of-scope follow-up (see `## Optional Follow-up — Category/Brand Selects` below), not something this story builds.
- The API must be running for any of these pages to work. `nuxt.config.ts` sets `apiBase` to `https://localhost:7225/api`; the API's CORS policy allows only `http://localhost:3000`, so run the frontend on the default port (same constraint documented in Stories 05, 07–11).
- The customer-facing `/products` catalog page described in `.squad/plans/products-catalog/07-story-customer-product-catalog-list-filters.md` (search input + prev/next pagination UX, debounced search reflected in the URL) has **not yet been implemented in code** — verified: no `app/pages/products/index.vue` file exists in the current tree. This story's admin list therefore follows that story's **documented UX pattern** (debounced search input, prev/next pagination driven by `page`/`pageSize`/`total`, loading/error/empty states) rather than an existing running page, since none exists yet to copy pixel-for-pixel.

---

## Story Goal

1. `app/pages/admin/products/index.vue` (`layout: 'admin'`, `middleware: 'admin'`, unchanged from today) fetches `GET /api/products?page=&pageSize=&search=` **with the admin's `Authorization: Bearer <token>` header attached**, so the response includes inactive products (`ProductsController.List`, `OnlineStore.API/Controllers/ProductsController.cs` line 40: `if (!User.IsInRole("admin")) query = query.Where(p => p.IsActive);` — the header is what makes the JWT's `admin` role claim visible to this check; the existing dashboard fetch (`app/pages/admin/dashboard.vue`, `useProducts` calls at lines 9–21) sends no such header today and is documented as a known, NOT-to-be-repeated gap — see Context item 5).
2. The existing `AdminRecentProductsTable.vue` is reused/extended as the list table: its disabled Edit link (currently `to="/admin/products"`, a no-op) becomes a real link to `/admin/products/{id}/edit`, and its disabled Delete button becomes a real action that calls `DELETE /api/products/{id}` behind a confirmation dialog, with a toast reporting the result.
3. Because the backend hard-deletes and returns a generic `500 { message: "Something went wrong" }` if the product is referenced by any `OrderItem` (an unhandled FK `Restrict` violation caught by the controller's blanket `catch` — `ProductsController.cs` lines 211–236, no dedicated 4xx exists for this case), the frontend must **not** surface that raw 500. Catch it generically and show a message suggesting deactivation instead (edit → set `isActive` to `false`) rather than deleting.
4. A debounced search input plus prev/next pagination controls above/below the table, following the UX pattern documented in `.squad/plans/products-catalog/07-story-customer-product-catalog-list-filters.md` (Frontend Tasks §2): search reflected in the URL query string, page reset to 1 on a new search term, prev disabled on page 1, next disabled once `page * pageSize >= total`.
5. An "Add product" button/link to `/admin/products/new`.
6. `app/pages/admin/products/new.vue` (new page): a create form (name, description, price, stock, category id, brand id, required image file) that client-side validates text/number fields via a new Zod schema (following the `app/utils/validation.ts` pattern) and the image file's extension (`.jpg`/`.jpeg`/`.png`/`.webp`) and size (≤ 5 MB) before submitting, to fail fast and match the backend's `ImageStorageService` limits exactly (`OnlineStore.API/Services/ImageStorageService.cs` lines 24–25). Submits as `multipart/form-data` via `POST /api/products`. On success: toast + redirect to `/admin/products`.
7. `app/pages/admin/products/[id]/edit.vue` (new page): the same form pre-filled from `GET /api/products/{id}`, image input optional (existing image kept if not replaced; current image shown as a preview), plus an `isActive` toggle. Submits as `multipart/form-data` via `PUT /api/products/{id}`.
8. Both forms show per-field validation errors (`fieldErrors()` + `BaseInput`'s `error` prop) and a submit-loading state (`BaseButton`'s `loading` prop) that disables the submit button while the request — including the image upload — is in flight, to prevent double-submits.

**Not in scope:** bulk actions (bulk delete/activate); image cropping/editing UI (raw file upload only); a category/brand management UI (a separate "Admin Categories List, Create and Edit" story); the `<select>` upgrade for category/brand inputs described in the follow-up section below; fixing the dashboard's own missing-Authorization-header gap (`app/pages/admin/dashboard.vue`) — this story only guarantees the **new** list page attaches the header from the start, it does not touch the dashboard file.

---

## Context — Read These Files First

1. `app/pages/admin/products/index.vue` — all 10 lines. The current stub: `definePageMeta({ layout: "admin", middleware: "admin" })` plus a "Product management coming soon" placeholder. This story replaces the template body entirely but keeps the same `definePageMeta` call unchanged.
2. `app/components/admin/RecentProductsTable.vue` — all 116 lines. The table this story extends: `resolveImage = useProductImage()` (line 14) already resolves `product.imageUrl` for the `<img>` at lines 55–61; the status pills (`Active`/`Inactive`/`Out of stock`, lines 70–89) are reused unchanged. The Edit link (lines 93–98) currently points at `to="/admin/products"` (a no-op back to the list itself) and must become `:to="`/admin/products/${product.id}/edit`"`. The Delete button (lines 101–108) is `disabled` with a comment ("Inert on purpose: destructive actions belong in the CRUD story...") — this story removes the `disabled` attribute and the comment, and wires a real `@click` handler that the page passes down via a new `delete` emit (see Frontend Tasks §2). The component takes `products: ProductListItem[]` and `loading?: boolean` as props (lines 4–12) — both are reused unchanged; no new prop is needed for the loading/error/empty grid states, since those live in the page, not the table.
3. `app/components/admin/StatCard.vue` — all 25 lines. Not reused directly by this story (no new stat cards are added to the products list page), but read to confirm the existing admin-component styling conventions (`rounded-xl border border-gray-200 bg-white p-4`-style card shells) this story's search/pagination controls should visually match.
4. `app/layouts/admin.vue` — all 56 lines. The sidebar's "Products" `<NuxtLink to="/admin/products" active-class="bg-primary text-white hover:bg-primary-600">` (lines 18–24) already targets the route this story builds out — **no change needed here**; the comment on line 17 ("`active-class`, not `exact-active-class`: `/admin/products/3` must keep this highlighted") confirms the sidebar link is already designed to stay highlighted on the new `/admin/products/new` and `/admin/products/{id}/edit` sub-routes, since `active-class` matches on path prefix.
5. `app/pages/admin/dashboard.vue` — all 100 lines, specifically lines 9–21 (`useProducts(1, 100, "admin-product-stats")` and `useProducts(1, 5, "admin-recent-products")`). Confirms the known gap directly: neither call passes an `Authorization` header, so — combined with `ProductsController.List`'s `!User.IsInRole("admin")` check (`ProductsController.cs` line 40) — the dashboard's stats and "recent products" table are actually computed from the **anonymous/active-only** view, not the true admin view including inactive products. This story's new list-page fetch must **not** repeat this: it needs its own header-attaching fetch (see Frontend Tasks §1), because the existing `useProducts` composable (Context item 8) takes no `search` or auth parameter and cannot express this without a breaking signature change this story avoids by building the request inline in the page instead.
6. `app/middleware/admin.ts` — all 10 lines. `defineNuxtRouteMiddleware`: redirects to `/auth/login` if `!auth.isAuthenticated` (line 4), to `/` if authenticated but not admin (`!auth.isAdmin`, lines 6–8). Reused unchanged as `middleware: "admin"` on `index.vue` (already present) and on both new pages (`new.vue`, `[id]/edit.vue`).
7. `app/utils/validation.ts` — all 34 lines. The Zod pattern this story's new create/edit schema follows: a `z.object({...})` schema (see `loginSchema`/`registerSchema`, lines 3–18) plus the shared `fieldErrors(error: z.ZodError): Record<string, string>` helper (lines 25–34), which flattens a `ZodError` to `{ field: firstMessage }` for `BaseInput`'s `error` prop — first issue per field wins (line 29: `if (key && !(key in out))`). This story adds a new exported schema (e.g. `productFormSchema`) to this same file; it does not create a separate validation file.
8. `app/composables/useProducts.ts` — all 29 lines. Exports `ProductListItem` (`{ id, name, price, stock, isActive, imageUrl }`, lines 1–8), `PagedProducts` (`{ items, page, pageSize, total }`, lines 10–15), and `useProducts(page, pageSize, key)` (lines 19–29), which builds a plain (non-reactive, non-authenticated) `useFetch` with `query: { page, pageSize }` and `server: false`. This story's admin list page does **not** call `useProducts` as-is (it needs `search`, an `Authorization` header, and reactive re-fetching on page/search change, none of which the current signature supports) — it imports the `ProductListItem`/`PagedProducts` types from this file but calls `useFetch` directly inside the page (see Frontend Tasks §1), matching the same "call `useFetch` directly for a reactive, page-specific need" precedent already used for the customer catalog page in `.squad/plans/products-catalog/07-story-customer-product-catalog-list-filters.md` (Frontend Tasks §2, the `computed(() => ({...}))`/`watch: [page, search]` block).
9. `app/composables/useProductImage.ts` — all 11 lines. `useProductImage()` returns a resolver that turns a relative `imageUrl` (e.g. `/images/products/{guid}.png`) into an absolute URL against the API host's origin (stripping the trailing `/api`), or passes through an already-absolute `http(s)://` URL unchanged. Reused unchanged inside `AdminRecentProductsTable.vue` (already wired, Context item 2) and needed again on the edit page to render the existing image preview from `GET /api/products/{id}`'s `imageUrl` field.
10. `app/stores/auth.ts` — all 74 lines. `state.token` (line 8) is the raw JWT string for `Authorization: Bearer ${auth.token}`; `isAuthenticated`/`isAdmin` getters (lines 27–28) back `middleware/admin.ts`. This story's list-page fetch and both forms' submit calls all need `auth.token` for the header.
11. `app/components/base/Button.vue` — all 63 lines. `<BaseButton>` props: `type` (`"button" | "submit" | "reset"`, default `"button"`), `variant` (`"primary" | "secondary" | "ghost"`), `block`, `loading` (renders a spinner and — combined with `disabled`, line 30: `:disabled="disabled || loading"` — blocks re-submission while `true`), `disabled`. Reused unchanged for every button in this story (Retry, prev/next, "Add product", both forms' submit buttons, the delete-confirmation dialog's confirm/cancel buttons).
12. `app/components/base/Input.vue` — all 108 lines. `<BaseInput>` props: `modelValue`, `label`, `type` (default `"text"`), `placeholder`, `error`, `autocomplete`, `required`. The `error` prop (bound from `errors.<field>`, e.g. line 66 in `login.vue`) renders a red-ring state plus an `<p id="{id}-error">` message (lines 50–52, 104–106) — exactly the per-field error contract both new forms use with `fieldErrors()`. Note there is **no dedicated `type="number"` numeric-formatting behavior** here beyond the native `<input type="number">` — price/stock/category id/brand id inputs bind `v-model` to a form field typed as `string` in the reactive form object (matching `login.vue`'s pattern) and are coerced to `number` only by the Zod schema's `z.coerce.number()` at submit time, not live.
13. `app/components/base/Form.vue` — all 9 lines. `<BaseForm>` wraps a `<form novalidate @submit.prevent="emit('submit')">` — reused unchanged around both new forms, matching `login.vue`'s usage (`<BaseForm class="mt-8" @submit="onSubmit">`).
14. `app/pages/auth/login.vue` — all 96 lines, specifically the `onSubmit` shape (lines 14–41): `errors.value = {}` reset, `schema.safeParse(form)`, `fieldErrors(parsed.error)` on failure, `loading.value = true` before the request, `try/catch/finally` around the `$fetch` call with `toast.error(...)` on failure and `toast.success(...)` + `navigateTo(...)` on success, `loading.value = false` in `finally`. Both new pages' submit handlers follow this exact shape, swapping the JSON `$fetch` body for a `FormData` multipart body.
15. `app/pages/index.vue` — lines 52–93. The three-state (`pending`/`error`/empty/loaded) rendering pattern already reused by every list-style page in this codebase (Stories 07–11): skeleton (`animate-pulse` placeholders), a bordered error notice with a `BaseButton` "Retry" calling `refresh()`, a distinct empty-state message, then the loaded content. The admin products list page's loading/error/empty states follow this same structure, adapted to the table shape (`AdminRecentProductsTable`'s own internal `loading`/`products.length === 0` branches, Context item 2, already provide the skeleton-rows and "No products yet." empty states — the page only needs its own `error` branch, since the table component does not render one).
16. `.squad/plans/products-catalog/07-story-customer-product-catalog-list-filters.md`, Frontend Tasks §2 — the debounced-search-plus-URL-sync and prev/next-pagination pattern this story's admin list page copies: a 400 ms debounce timer resetting `page` to 1 on a new search term (its code block using `watch(searchInput, ...)` with `setTimeout`), a `computed(() => ({ page, pageSize, search }))` query object passed to `useFetch`'s `query` option with `watch: [page, search]` for reactive re-fetching, and prev/next `<BaseButton variant="secondary" :disabled="...">` pairs gated on `page.value <= 1` and `page.value * pageSize >= data.total`. This story adapts the same block to also send the `Authorization` header (Context item 5) since the admin endpoint requires it to see inactive products; the customer catalog page itself does not exist as a running file yet (verified: no `app/pages/products/index.vue` in the tree) — this plan is followed as documented pattern, not copied from a live file.
17. `OnlineStore.API/Controllers/ProductsController.cs` — all 241 lines.
    - Class-level `[Authorize(Roles = "admin")]` (line 13) with `[AllowAnonymous]` on `List` (line 25) and `GetById` (line 67) — both are reachable without a token, but `List`'s admin-only inactive-product visibility (line 40) and `GetById`'s admin-only inactive-product visibility (lines 79–83) both key off `User.IsInRole("admin")`, which is only populated when a valid admin JWT is sent — hence the `Authorization` header requirement (Story Goal item 1).
    - `List` (lines 27–65): query params `search` (optional), `page` (default `1`, clamped `Math.Max(page, 1)`, line 34), `pageSize` (default `20`, clamped `Math.Clamp(pageSize, 1, 100)`, line 35); `search` matches via `EF.Functions.ILike` (line 47, case-insensitive substring); response envelope `{ items, page, pageSize, total }` (line 59); generic `catch { return StatusCode(500, new { message = "Something went wrong" }); }` (lines 61–64).
    - `GetById` (lines 69–91): `[HttpGet("{id:int}")]`; `404 { message = "Product not found" }` if missing (lines 74–77) or if inactive and caller is not admin (lines 79–83); success returns `ToDetail(product)` (line 85); same generic `500` catch (lines 87–90).
    - `Create` (lines 93–144): `[HttpPost]`, `[Consumes("multipart/form-data")]`, binds `[FromForm] CreateProductFormRequest request`. Validates the image via `_images.Validate(request.Image)`, returning `400 { message }` on an `InvalidImageException` (lines 99–100); validates `CategoryId`/`BrandId` exist, returning `400 { message = "Category not found" }` / `400 { message = "Brand not found" }` (lines 102–110) — **not** a field-level Zod-style error, a top-level message; saves the file only after those checks pass (line 113); on a DB save failure, rolls back the just-saved file (`_images.Delete(imageUrl)`, line 134) then rethrows into the outer generic `catch` → `500`; on success, `201 CreatedAtAction(nameof(GetById), ..., ToDetail(product))` (line 138).
    - `Update` (lines 146–209): `[HttpPut("{id:int}")]`, `[Consumes("multipart/form-data")]`, binds `[FromForm] UpdateProductFormRequest request`. `404 { message = "Product not found" }` if the id does not exist (lines 152–156); same `CategoryId`/`BrandId` existence checks and `400` messages as `Create` (lines 158–166); **image is optional** — only validated/replaced `if (request.Image is not null && request.Image.Length > 0)` (lines 171–178), otherwise the existing `product.ImageUrl` is left untouched; `IsActive` is part of the payload and is written unconditionally (`product.IsActive = request.IsActive;`, line 186) — omitting it from the submitted form data means the server-model-bound default `false` is what gets sent, so the edit form **must** always include the current `isActive` value, never omit the field; on success, replaces the old image file only *after* a successful save (lines 199–201); returns `200 Ok(ToDetail(product))` (line 203); same generic `500` catch (lines 205–208).
    - `Delete` (lines 211–236): `[HttpDelete("{id:int}")]`; `404 { message = "Product not found" }` if missing (lines 216–220); hard-deletes the row and then the image file (lines 224–228); returns `204 NoContent()` (line 230) on success. **There is no FK-violation-specific catch** — if any `OrderItem` references the product, `_db.SaveChangesAsync()` (line 225) throws (a `DbUpdateException` wrapping the DB's `Restrict` constraint violation), which falls straight into the method's own generic `catch { return StatusCode(500, ...); }` (lines 232–235) — identical shape to every other unhandled error, with **no distinguishing field or error code** to detect "this failed because of a referencing order" versus any other unexpected 500. The frontend cannot reliably distinguish this case from a genuine server error by inspecting the response; it must assume any 500 on this endpoint might be this case and word the message accordingly (see Edge Cases).
    - `ToDetail` (lines 238–239): `new ProductDetailDto(p.Id, p.Name, p.Description, p.Price, p.Stock, p.CategoryId, p.BrandId, p.IsActive, p.CreatedAt, p.ImageUrl)`.
18. `OnlineStore.API/Dtos/CreateProductFormRequest.cs` — all 27 lines. **Exact multipart field names for `POST /api/products`** (bound via `[FromForm]`, so these are the literal `FormData` keys the frontend must use): `Name` (`[Required, StringLength(100)]`), `Description` (no `[Required]` — empty string is valid, defaults to `string.Empty`), `Price` (`[Range(0.01, double.MaxValue)]`, error message "Price must be greater than 0."), `Stock` (`[Range(0, int.MaxValue)]`, error message "Stock must be 0 or greater."), `CategoryId` (`[Range(1, int.MaxValue)]`, error message "CategoryId is required."), `BrandId` (`[Range(1, int.MaxValue)]`, error message "BrandId is required."), `Image` (`[Required(ErrorMessage = "Image is required.")]`, type `IFormFile`).
19. `OnlineStore.API/Dtos/UpdateProductFormRequest.cs` — all 29 lines. **Exact multipart field names for `PUT /api/products/{id}`**: identical `Name`/`Description`/`Price`/`Stock`/`CategoryId`/`BrandId` constraints as `CreateProductFormRequest`, plus `IsActive` (`bool`, no attribute — always bind, defaults to `false` if the form key is absent, confirming the "always send `isActive`" rule from Context item 17's `Update` notes) and `Image` typed as `IFormFile?` (**optional** — omit the `image` form field entirely to keep the existing image, per `Update`'s `if (request.Image is not null && request.Image.Length > 0)` check).
20. `OnlineStore.API/Services/ImageStorageService.cs` — all 74 lines. `Validate` (lines 35–46): throws `InvalidImageException("Image is required.")` if the file is null or zero-length; `InvalidImageException("Image must be 5 MB or smaller.")` if `file.Length > MaxBytes` (`MaxBytes = 5 * 1024 * 1024`, line 25 — exactly 5 MB, `<=` passes); `InvalidImageException("Image must be a .jpg, .jpeg, .png, or .webp file.")` if the lowercased file extension is not in `AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" }` (line 24). Client-side validation before submit (Story Goal item 6) must match these exact three checks and messages so the UI fails fast with the same rules the server enforces, not a superset or subset of them.
21. `app/utils/format.ts` — all 11 lines. `formatPrice(value: number): string` — reused unchanged for the price column in the list table and any price display on the forms.
22. `.squad/plans/orders-history/11-story-customer-orders-history.md` — sibling plan read for tone/structure precedent (Prerequisites/Story Goal/Context/Frontend Tasks/Edge Cases/Test Plan/Verification/Done Criteria ordering, and the "read this exact file, these exact lines" citation style this document follows).

---

## Optional Follow-up — Category/Brand `<select>` Upgrade (NOT required for this story, backend not ready)

This section is **explicitly out of the Done Criteria** below. Do not implement it as part of this story; it is documented so a later story can pick it up once the backend lands.

- The backend story "Categories and Brands CRUD" (`OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md`) plans `GET /api/categories` and `GET /api/brands` (both returning `{ id, name }[]`) — **neither endpoint exists in the codebase yet** (verified: no `CategoriesController.cs`/`BrandsController.cs` under `OnlineStore.API/Controllers/`).
- Once that backend story ships, replace the plain numeric `CategoryId`/`BrandId` `<BaseInput type="number">` fields on both `new.vue` and `[id]/edit.vue` with `<select>` elements populated from those two endpoints, keeping the same form field names (`categoryId`, `brandId`) and the same Zod validation shape (`z.coerce.number().int().min(1, ...)`).
- Until then, ship this story with the numeric id inputs and a short helper label (e.g. "Category ID" with a hint like "enter the numeric category id") — no disabled/greyed-out `<select>` placeholder, matching the principle already applied to the customer catalog page's category/brand filters (`.squad/plans/products-catalog/07-story-customer-product-catalog-list-filters.md`, its own follow-up section): a UI element for a capability that cannot be looked up yet is worse than a plain, honest input.

---

## Frontend Tasks

### 1 — Rebuild the admin products list page

**File: `app/pages/admin/products/index.vue`**

Keep `definePageMeta({ layout: "admin", middleware: "admin" })` unchanged. Replace the rest of the file with a page that:

- Seeds `page`/`searchInput`/`search` refs from `route.query`, matching `.squad/plans/products-catalog/07-story-customer-product-catalog-list-filters.md`'s pattern (Context item 16), with a `pageSize` of `20` (the backend's default and mid-range clamp value, `ProductsController.cs` line 35).
- Debounces `searchInput` → `search` on a 400 ms timer, resetting `page` to `1` on every new search term, same as the referenced pattern.
- Calls `useFetch<PagedProducts>(`${useApi()}/products`, { key: "admin-products-list", query: computed(() => ({ page: page.value, pageSize, search: search.value || undefined })), headers: { Authorization: `Bearer ${useAuthStore().token}` }, server: false, watch: [page, search], default: (): PagedProducts => ({ items: [], page: page.value, pageSize, total: 0 }) })` directly (importing only the `PagedProducts`/`ProductListItem` types from `~/composables/useProducts`) — **do not** call the existing `useProducts()` composable, since it has neither a `search` parameter nor an `Authorization` header (Context item 8).
- Pushes `page`/`search` into `route.query` on change, same as the referenced pattern (`router.push({ query: { ...(page > 1 ? { page } : {}), ...(search ? { search } : {}) } })`).
- Renders, in order: heading "Products" + a search `<BaseInput type="search" v-model="searchInput" placeholder="Search products..." />` + an "Add product" `<NuxtLink to="/admin/products/new"><BaseButton>Add product</BaseButton></NuxtLink>`; an `error`-only bordered notice with a `BaseButton` "Retry" calling `refresh()` (`v-if="error"`, matching `app/pages/index.vue` lines 68–76 — placed *before* the table so the table underneath still renders its own loading/empty states when there is no error); then `<AdminRecentProductsTable :products="data?.items ?? []" :loading="pending" @delete="onDeleteRequest" />` (new `@delete` listener, task 2); then prev/next pagination controls below the table, shown whenever `(data?.total ?? 0) > 0`, identical disabled-state logic to the referenced pattern (`page.value <= 1` / `page.value * pageSize >= (data?.total ?? 0)`).

### 2 — Wire Edit/Delete into `AdminRecentProductsTable`

**File: `app/components/admin/RecentProductsTable.vue`**

- Change the Edit `<NuxtLink>` (lines 93–98) from `to="/admin/products"` to `:to="`/admin/products/${product.id}/edit`"`.
- Remove the `disabled` attribute, the `title` attribute, and the preceding HTML comment on the Delete `<button>` (lines 99–108). Add `@click="$emit('delete', product)"`. Declare `defineEmits<{ delete: [product: ProductListItem] }>()` near the existing `defineProps` call (lines 4–12).
- No other changes — the component stays a dumb list/emit component; the confirmation dialog and the actual `DELETE` call live in the page (task 3), keeping this component reusable and consistent with its current "no side effects inside the table" shape.

### 3 — Delete flow with confirmation and 500-suggests-deactivation handling

**File: `app/pages/admin/products/index.vue`** (same file as task 1)

Add an `onDeleteRequest(product: ProductListItem)` handler triggered by `AdminRecentProductsTable`'s new `delete` emit (task 2):

1. Show a confirmation dialog (a simple `window.confirm(`Delete "${product.name}"? This cannot be undone.`)` is acceptable — there is no existing modal/dialog component in `app/components/base/` to reuse, and this story does not introduce one). If not confirmed, return without calling the API.
2. On confirm, call `await $fetch(`${useApi()}/products/${product.id}`, { method: "DELETE", headers: { Authorization: `Bearer ${useAuthStore().token}` } })`.
3. On success (`204 No Content`): `toast.success(`"${product.name}" deleted.`)` and `refresh()` the list.
4. On failure: inspect `e?.statusCode` (or `e?.response?.status`, matching the `$fetch` error shape already used in `login.vue`, Context item 14). Because the backend returns an indistinguishable generic `500` for both a genuine server error and an FK-`Restrict` violation from a referencing `OrderItem` (Context item 17's `Delete` notes — there is no dedicated 4xx or distinguishing field), treat **any** non-2xx response from this endpoint as the deactivation-suggesting case: `toast.error('Could not delete "${product.name}". If it appears in any order, deactivate it instead — edit the product and turn off "Active".')`. Do not attempt to branch on status code to show a different message for "genuine 500" versus "FK violation" — the response gives no way to tell them apart.

### 4 — Add a shared product-form Zod schema

**File: `app/utils/validation.ts`**

Add, alongside the existing `loginSchema`/`registerSchema`/`fieldErrors` (lines 1–34):

```ts
export const productFormSchema = z.object({
  name: z.string().min(1, "Name is required").max(100, "Name must be 100 characters or fewer"),
  description: z.string(),
  price: z.coerce.number({ invalid_type_error: "Price must be a number" }).gt(0, "Price must be greater than 0"),
  stock: z.coerce.number({ invalid_type_error: "Stock must be a number" }).int("Stock must be a whole number").min(0, "Stock must be 0 or greater"),
  categoryId: z.coerce.number({ invalid_type_error: "Category id must be a number" }).int().min(1, "Category id is required"),
  brandId: z.coerce.number({ invalid_type_error: "Brand id must be a number" }).int().min(1, "Brand id is required"),
});

export type ProductFormInput = z.infer<typeof productFormSchema>;
```

Field names, ranges, and messages mirror `CreateProductFormRequest`/`UpdateProductFormRequest` (Context items 18–19) exactly: `name` max length 100, `price` strictly greater than 0, `stock` a non-negative integer, `categoryId`/`brandId` positive integers. `description` has no server-side `[Required]`, so no `.min()` here either. The image file and (on edit) `isActive` are validated/handled separately in each page's script (not part of this shared Zod object), since Zod does not model `File`/`FileList` cleanly and `isActive` only exists on the edit form.

### 5 — Create the "new product" page

**Create file: `app/pages/admin/products/new.vue`**

```vue
<script setup lang="ts">
definePageMeta({ layout: "admin", middleware: "admin" });

import { toast } from "vue-sonner";
import { productFormSchema, fieldErrors } from "~/utils/validation";

const apiBase = useApi();
const auth = useAuthStore();

const form = reactive({
  name: "",
  description: "",
  price: "",
  stock: "",
  categoryId: "",
  brandId: "",
});
const imageFile = ref<File | null>(null);
const imageError = ref("");
const errors = ref<Record<string, string>>({});
const loading = ref(false);

const ALLOWED_EXT = [".jpg", ".jpeg", ".png", ".webp"];
const MAX_BYTES = 5 * 1024 * 1024;

function onImageChange(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0] ?? null;
  imageError.value = "";
  if (!file) {
    imageFile.value = null;
    return;
  }
  const ext = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
  if (!ALLOWED_EXT.includes(ext)) {
    imageError.value = "Image must be a .jpg, .jpeg, .png, or .webp file.";
    imageFile.value = null;
    return;
  }
  if (file.size > MAX_BYTES) {
    imageError.value = "Image must be 5 MB or smaller.";
    imageFile.value = null;
    return;
  }
  imageFile.value = file;
}

async function onSubmit() {
  errors.value = {};

  const parsed = productFormSchema.safeParse(form);
  if (!parsed.success) {
    errors.value = fieldErrors(parsed.error);
    return;
  }
  if (!imageFile.value) {
    imageError.value = "Image is required.";
    return;
  }

  const body = new FormData();
  body.append("Name", parsed.data.name);
  body.append("Description", parsed.data.description);
  body.append("Price", String(parsed.data.price));
  body.append("Stock", String(parsed.data.stock));
  body.append("CategoryId", String(parsed.data.categoryId));
  body.append("BrandId", String(parsed.data.brandId));
  body.append("Image", imageFile.value);

  loading.value = true;
  try {
    await $fetch(`${apiBase}/products`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.token}` },
      body,
    });
    toast.success(`"${parsed.data.name}" created.`);
    await navigateTo("/admin/products");
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not create product.");
  } finally {
    loading.value = false;
  }
}
</script>
```

Template: `<BaseForm @submit="onSubmit">` wrapping `<BaseInput v-model="form.name" label="Name" :error="errors.name" />`, a plain `<textarea>` for `description` (there is no `<BaseTextarea>` component — use a bare `<textarea v-model="form.description">` styled with the same Tailwind conventions as `BaseInput`'s `<input>`, no `:error` binding needed since the schema places no constraint on it), `<BaseInput v-model="form.price" type="number" step="0.01" label="Price" :error="errors.price" />`, `<BaseInput v-model="form.stock" type="number" label="Stock" :error="errors.stock" />`, `<BaseInput v-model="form.categoryId" type="number" label="Category ID" :error="errors.categoryId" />` (with a hint that a `<select>` is a future upgrade — see the follow-up section), `<BaseInput v-model="form.brandId" type="number" label="Brand ID" :error="errors.brandId" />`, a native `<input type="file" accept=".jpg,.jpeg,.png,.webp" @change="onImageChange" />` with `imageError` rendered below it the same way `BaseInput`'s own error text renders (a `<p class="text-xs text-red-600">`), and `<BaseButton type="submit" :loading="loading">Create product</BaseButton>`. **Do not** set a `Content-Type` header on the `$fetch` call — the browser sets the multipart boundary automatically when the `body` is a `FormData` instance; setting `Content-Type: multipart/form-data` manually omits the boundary parameter and breaks server-side parsing.

### 6 — Create the "edit product" page

**Create file: `app/pages/admin/products/[id]/edit.vue`**

Same shape as `new.vue` (task 5), with these differences:

- `definePageMeta({ layout: "admin", middleware: "admin" })`, same as every other admin page.
- On mount, fetch the existing product via `useFetch` (or `$fetch` in an `onMounted`/top-level `await`) against `GET /api/products/${route.params.id}` — this endpoint is `[AllowAnonymous]` but still returns the full record including inactive products for an admin caller (Context item 17, `GetById`), so send the same `Authorization` header for consistency and correctness regardless of the product's `isActive` state. Populate `form.name`/`form.description`/`form.price`/`form.stock`/`form.categoryId`/`form.brandId` from the response's `name`/`description`/`price`/`stock`/`categoryId`/`brandId` fields (`ProductDetailDto`, Context item 17's `ToDetail`), plus a new `isActive = ref(response.isActive)`.
- Render the existing image as a preview above the file input, using `useProductImage()(response.imageUrl)` (Context item 9) for the `<img :src>`. Label the file input "Replace image (optional)" and do not set `imageError.value = "Image is required."` when no new file is chosen — an edit submits successfully with no `Image` form field at all (Context item 19: `Image` is `IFormFile?` on `UpdateProductFormRequest`, and the controller only touches the image if a new one is present).
- Add an `isActive` toggle (a labeled checkbox is sufficient — there is no existing `<BaseToggle>`/`<BaseCheckbox>` component to reuse) bound to `isActive.value`.
- In `onSubmit`, build the `FormData` the same way as `new.vue`, additionally appending `body.append("IsActive", String(isActive.value))` (always present, per Context item 19's "always send `isActive`" rule) and appending `Image` **only** `if (imageFile.value)` — when no new file was chosen, omit the key entirely so the backend keeps the existing image.
- Submit via `$fetch(`${apiBase}/products/${route.params.id}`, { method: "PUT", headers: { Authorization: ... }, body })`. On success: `toast.success(...)` + `navigateTo("/admin/products")`. On failure: `toast.error(e?.data?.message ?? "Could not update product.")`, same pattern as `new.vue`.
- A non-numeric or missing `id` route param, or a `404` from the initial `GetById` fetch, renders a small "Product not found" notice with a link back to `/admin/products` instead of the form (mirrors the not-found pattern already established for `/orders/[id]` in `.squad/plans/orders-history/11-story-customer-orders-history.md`, Frontend Tasks §3) — do not render an empty/blank form in that case.

---

## Edge Cases & Failure Modes

- **Admin fetch without the Authorization header would silently under-report inactive products** — this is the exact gap already present on the dashboard (`app/pages/admin/dashboard.vue` lines 9–21, Context item 5). This story's list-page `useFetch` call (task 1) must include `headers: { Authorization: `Bearer ${useAuthStore().token}` } }` or `ProductsController.List`'s `User.IsInRole("admin")` check (`ProductsController.cs` line 40) evaluates `false` and silently filters out inactive products with no error — a bug that looks like "the list works" but returns the wrong data.
- **Delete on a product referenced by an `OrderItem`** returns a generic `500 { message: "Something went wrong" }` (Context item 17, `Delete`) with **no field or code distinguishing it from any other unexpected 500** — task 3's `onDeleteRequest` must treat every non-2xx response from `DELETE /api/products/{id}` as potentially this case and word the error message accordingly (suggest deactivation), never surfacing the raw "Something went wrong" text.
- **`Update`'s `IsActive` is written unconditionally from the submitted form** (`ProductsController.cs` line 186) — if the edit page's `FormData` omits the `IsActive` key (e.g. a bug where the checkbox state is not appended), .NET's default model-binding for a missing `bool` form field is `false`, which would silently deactivate an active product on every edit. Task 6 must always call `body.append("IsActive", String(isActive.value))`, with no conditional around it.
- **Replacing the image on edit deletes the old file only after a successful save** (`ProductsController.cs` lines 199–201) — from the frontend's perspective this is transparent (a single `200` response either way), but it means a failed `PUT` (e.g. a validation 400) never orphans or loses the original image; no special handling is needed beyond the normal error-toast path already described in task 6.
- **Category/brand id that does not exist**: the backend returns `400 { message = "Category not found" }` or `400 { message = "Brand not found" }` (Context item 17, lines 102–110/158–166) as a **top-level message**, not a per-field Zod-style error — since the Zod schema (task 4) only validates that the id is a positive integer, not that it exists in the database (an existence check requires a request the client-side schema cannot make synchronously). Both `new.vue` and `[id]/edit.vue`'s `catch` blocks must surface this via `toast.error(e?.data?.message ?? ...)` (already the plan in tasks 5–6) rather than attempting to map it into `errors.categoryId`/`errors.brandId`.
- **Oversized or wrong-type image selected**: `onImageChange` (task 5) rejects the file client-side with the exact same three messages the backend's `ImageStorageService.Validate` (Context item 20) would produce, and never adds the rejected file to `imageFile`/the `FormData` — but a user could still bypass this via devtools or a race between selecting a valid file and the network being slow; the backend's own `_images.Validate` call (`ProductsController.cs` line 99 on create, line 173 on update) is the authoritative check either way, and its `400 { message }` is what task 5/6's generic `catch` surfaces if client-side validation is ever bypassed.
- **Double-submit from a slow image upload**: `BaseButton`'s `:disabled="disabled || loading"` (`Button.vue` line 30) combined with `loading.value = true` set before the `$fetch` call and reset only in `finally` (tasks 5–6) prevents a second click from firing a second request while the first is still uploading.
- **Non-numeric or negative `page` in the list page's URL** (e.g. hand-edited `?page=abc`): same fallback as the customer catalog pattern (Context item 16) — `Number(route.query.page) || 1` falls back to `1` for `NaN`; a valid negative number is sent to the backend and clamped server-side (`Math.Max(page, 1)`, `ProductsController.cs` line 34).
- **Search narrows results below the current page**: same `page.value = 1` reset inside the search debounce callback as the customer catalog pattern (Context item 16) — without it, `Skip((page - 1) * pageSize)` (`ProductsController.cs` line 54) would return an empty `items` array for an out-of-range page while `total` is still nonzero.
- **Editing a product whose `id` does not exist, or a non-numeric `id` in the URL**: `GET /api/products/{id}` 404s (`ProductsController.cs` lines 74–77) or, for a non-numeric id, never matches the `[HttpGet("{id:int}")]` route constraint and 404s at the routing layer with no JSON body — task 6's not-found branch covers both by checking the fetch error's `statusCode` (mirrors `.squad/plans/orders-history/11-story-customer-orders-history.md`'s `isValidId`/`isNotFound` pattern, Context item 22, adapted here for a single 404 case since there is no 403-ambiguity on this endpoint the way there was for orders).
- **SSR vs. self-signed dev certificate**: identical to every prior story — all `useFetch`/`$fetch` calls in this story's pages use `server: false` (or run inside `onMounted`/a client-only lifecycle) to avoid the Nitro server making the request during SSR, where Node would reject the API's self-signed HTTPS certificate.

---

## Test Plan

**No test runner is configured** in this project — confirmed by Stories 05, 07–11 (`package.json` has `build`/`dev`/`generate`/`preview`/`postinstall` only, no Vitest dependency), still true as of this story. Verification is manual (see Verification Steps). If Vitest + `@nuxt/test-utils` is introduced later, these are the tests to add:

1. **Unit — `productFormSchema`** (`app/utils/validation.ts`): valid input parses to the coerced-number shape; an empty `name`, a `price` of `0` or negative, a non-integer `stock`, and a `categoryId`/`brandId` of `0` or negative each produce the exact messages listed in task 4, retrievable via `fieldErrors()`.
2. **Component — `app/pages/admin/products/index.vue`** with the list `useFetch` mocked: `pending` renders the table's own skeleton rows (via `AdminRecentProductsTable`'s `loading` prop); a page-level `error` renders the bordered notice with a working Retry; `data.items: []` renders the table's own "No products yet." empty state; a full page renders one row per item; prev is disabled on page 1, next is disabled once `page * pageSize >= total`.
3. **Component — `AdminRecentProductsTable`**: the Edit link's `to` resolves to `/admin/products/{id}/edit` for each row's `id`; clicking Delete emits a `delete` event carrying the clicked row's product, with no `disabled` attribute present.
4. **Component — delete flow**: confirming the dialog and a `204` response calls `refresh()` and shows a success toast; confirming and a `500` response shows the deactivation-suggesting error toast, not the raw backend message; declining the dialog makes no network call.
5. **Component — `app/pages/admin/products/new.vue`**: submitting with invalid fields shows the corresponding `fieldErrors()` messages and makes no network call; submitting with no image selected shows "Image is required." and makes no network call; submitting with a `.gif` or an oversized file shows the matching client-side message and makes no network call; a valid submit builds a `FormData` with keys `Name`/`Description`/`Price`/`Stock`/`CategoryId`/`BrandId`/`Image` and calls `POST /api/products` with the `Authorization` header; success toasts and redirects to `/admin/products`; a `400` response with `message` toasts that exact message.
6. **Component — `app/pages/admin/products/[id]/edit.vue`**: the form pre-fills from a mocked `GET /api/products/{id}` response, including the image preview and the `isActive` toggle's initial state; submitting with no new image chosen omits the `Image` key from the built `FormData` and still includes `IsActive`; submitting with a new image includes it; a mocked `404` on the initial fetch renders the not-found branch instead of a blank form.

---

## Verification Steps

1. **Frontend builds:** in `online-store-frontend/`, run `pnpm build`. Must succeed — a wrong auto-import name for `<AdminRecentProductsTable>`/`<BaseButton>`/`<BaseInput>`/`<BaseForm>`, a bad `productFormSchema` reference, or a missing `definePageMeta` fails here.
2. **List, admin view includes inactive products:** log in as an admin (role `admin`), open `/admin/products` — the network tab shows a request to `https://localhost:7225/api/products?page=1&pageSize=20` carrying an `Authorization: Bearer <token>` header; confirm at least one inactive product (if seeded) appears in the table with the "Inactive" pill, proving the header fix works (contrast with the dashboard's list at `/admin/dashboard`, which — per the documented gap — does not show inactive products the same way).
3. **Search:** type into the search box; after the debounce, exactly one request fires with `search=<term>&page=1` and the URL updates to `?search=<term>`.
4. **Pagination:** with more than 20 products seeded, click "Next" — URL updates to `?page=2`, a new request fires, prev enables, next disables on the last page.
5. **Edit link:** click "Edit" on any row — navigates to `/admin/products/{id}/edit` (not the old no-op `/admin/products`), and the form is pre-filled with that product's current values plus its existing image preview.
6. **Delete, success path:** click "Delete" on a product with no orders referencing it, confirm the dialog — the row disappears (list refreshes) and a success toast appears.
7. **Delete, referenced-by-order path:** click "Delete" on a product known to be referenced by an existing `OrderItem` (seed one via a completed checkout if none exists) — a clear toast suggesting deactivation appears, not a raw 500 or "Something went wrong."
8. **Create, happy path:** open `/admin/products/new`, fill all fields, attach a valid `.png`/`.jpg`/`.jpeg`/`.webp` under 5 MB — submits, network tab shows a `multipart/form-data` `POST` to `/api/products` with fields `Name`/`Description`/`Price`/`Stock`/`CategoryId`/`BrandId`/`Image` and an `Authorization` header, no manually-set `Content-Type`; success toast + redirect to `/admin/products`, and the new product appears in the list.
9. **Create, client-side image validation:** attempt to submit with a `.gif` file or a file over 5 MB — the exact matching message renders inline, no network request fires.
10. **Create, field validation:** attempt to submit with an empty name, a zero/negative price, or a `0` category id — the corresponding field shows its `fieldErrors()` message, no network request fires.
11. **Create, server-side category/brand rejection:** submit with a `categoryId`/`brandId` that does not exist in the database — a toast shows "Category not found"/"Brand not found" verbatim from the backend, and the page's own fields show no fabricated field-level error for it.
12. **Edit, happy path (no new image):** open `/admin/products/{id}/edit` for an existing product, change a field, leave the image input untouched, submit — network tab shows the `PUT` request's `FormData` has no `Image` key; the existing image remains unchanged after re-opening the edit page.
13. **Edit, happy path (replace image):** same as above but choose a new valid image — the `PUT` request includes the new `Image` file; re-opening the edit page shows the new image as the preview.
14. **Edit, deactivate:** toggle `isActive` off and submit — the product disappears from the customer-facing view (if `/products` exists) but still appears in the admin list with an "Inactive" pill.
15. **Submit-loading state:** on a throttled connection (Slow 3G), click submit on either form and confirm the button shows its loading spinner and is unclickable until the request resolves.
16. **Backend unchanged:** `dotnet build` in `OnlineStore.API/` — expected unchanged, this story touches no C#.
17. **Responsive:** at 375 px the list's search/pagination controls and both forms stack in a single column with no horizontal overflow; at 1280 px all three pages read comfortably within the layout's content width.

---

## Done Criteria

- [ ] `app/pages/admin/products/index.vue` fetches `GET /api/products?page=&pageSize=&search=` with the admin's `Authorization` bearer token attached, so inactive products are included in the response.
- [ ] `AdminRecentProductsTable.vue`'s Edit link points to `/admin/products/{id}/edit` and its Delete button is enabled, emits a `delete` event, and is no longer wrapped in the old "inert on purpose" comment.
- [ ] Delete is behind a confirmation dialog; a successful delete refreshes the list and toasts; a failed delete (including the referenced-by-order 500 case) shows a clear message suggesting deactivation instead of a raw error.
- [ ] A debounced search input and prev/next pagination controls are present on the list page, reflected in the URL query string, matching the customer-catalog UX pattern.
- [ ] An "Add product" link/button on the list page navigates to `/admin/products/new`.
- [ ] `app/pages/admin/products/new.vue` exists: a form (name, description, price, stock, category id, brand id, required image) that validates text/number fields via a new `productFormSchema` (`app/utils/validation.ts`) and validates the image's type (`.jpg`/`.jpeg`/`.png`/`.webp`) and size (≤ 5 MB) client-side before submit; submits as `multipart/form-data` to `POST /api/products`; on success, toasts and redirects to `/admin/products`.
- [ ] `app/pages/admin/products/[id]/edit.vue` exists: the same form pre-filled from `GET /api/products/{id}`, image optional with a preview of the current image, plus an `isActive` toggle; submits as `multipart/form-data` to `PUT /api/products/{id}` with `IsActive` always included in the payload.
- [ ] Both forms show per-field validation errors via `fieldErrors()` + `BaseInput`'s `error` prop, and a submit-loading state via `BaseButton`'s `loading` prop that blocks double-submits.
- [ ] Category/brand inputs on both forms are plain numeric id inputs — no `<select>` — with the `<select>` upgrade documented only as an out-of-scope follow-up.
- [ ] `pnpm build` succeeds; no arbitrary `bg-[#…]`/`text-[#…]` or raw `px` spacing introduced.
