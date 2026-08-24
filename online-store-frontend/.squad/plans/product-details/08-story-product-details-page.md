# Story 08 — Product Details Page

Build `app/pages/products/[id].vue`: a public product detail page backed by `GET /api/products/{id}`, showing full product info and letting an authenticated customer add it to their cart with a chosen quantity via `POST /api/cart`. `app/components/ProductCard.vue` (used on both the home page and the products catalog page) already links to `/products/${product.id}` (line 16) — this story is the page that link resolves to.

---

## Prerequisites

- **Story 05** ([`../home-page/05-story-customer-home-page.md`](../home-page/05-story-customer-home-page.md)) and **Story 07** ([`../products-catalog/07-story-customer-product-catalog-list-filters.md`](../products-catalog/07-story-customer-product-catalog-list-filters.md)) completed: created and extended the shared foundation this story reuses — `app/composables/useProducts.ts`, `app/composables/useProductImage.ts`, `app/utils/format.ts`, the `primary` Tailwind theme, and the loading/error/empty three-state UI pattern. This story does not fork a new image-resolution or price-formatting helper.
- Backend `GET /api/products/{id}` and `POST /api/cart` are already implemented and unchanged for this story — `OnlineStore.API/Controllers/ProductsController.cs` lines 67–91 and `OnlineStore.API/Controllers/CartController.cs` lines 23–75. **No backend change is required or permitted in this story.**
- The API must be running for the page to populate. `nuxt.config.ts` sets `apiBase` to `https://localhost:7225/api`; the API's CORS policy allows only `http://localhost:3000`, so run the frontend on the default port (same constraint documented in Stories 05 and 07).
- This is the **first authenticated (`Authorization: Bearer …`) frontend request in the codebase** — verified: no file under `app/` sends an `Authorization` header today (grepped for `Authorization`/`Bearer` across `app/`, no matches). `app/pages/auth/login.vue`'s `$fetch` call (lines 25–30) is unauthenticated; `app/plugins/auth.ts` only loads the token into the Pinia store, it never attaches it to a request. This story establishes that pattern for the add-to-cart call; there is no existing helper to reuse or extend.

---

## Story Goal

1. A new public page at `/products/[id]` (`app/pages/products/[id].vue`), default layout, **no auth middleware** — anyone can view a product; only adding to cart requires login (mirrors the backend's `[AllowAnonymous]` on `GetById` vs. `[Authorize]` on `CartController`).
2. Fetches `GET /api/products/{id}` and renders: image (via `useProductImage`, with the same initial-letter placeholder pattern `ProductCard` uses), name, description, price (via `formatPrice`), stock count, and an "Out of stock" indicator when `stock === 0`.
3. A quantity selector (stepper) clamped to `[1, stock]`, hidden/disabled entirely when `stock === 0`.
4. An "Add to cart" button: if the customer is not authenticated, redirect to `/auth/login` instead of calling the API; if authenticated, `POST /api/cart` with `{ productId, quantity }`, success toast on resolution, error toast (surfacing `e?.data?.message` when present, generic fallback otherwise) on failure.
5. Distinct loading (skeleton), not-found (404 → "product not found" + link back to `/products`), and error (other failures, with Retry) states.
6. A link back to `/products`.

**Not in scope:** reviews/ratings, related products, an image gallery (the backend exposes only a single `imageUrl` — `ProductDetailDto`, `OnlineStore.API/Dtos/ProductDetailDto.cs` lines 3–13), editing/deleting the product (the admin CRUD story), category/brand **names** (only `categoryId`/`brandId` are in the DTO — do not render raw ids to the customer; there is nothing to resolve them to until the categories-and-brands backend story lands), and preserving a post-login return path (see Edge Cases — `app/pages/auth/login.vue` has no query-param redirect handling today; adding one is a separate, `login.vue`-scoped change, not part of this story).

---

## Context — Read These Files First

1. `app/components/ProductCard.vue` — all 48 lines. Line 16: `:to="`/products/${product.id}`"` is the exact link target this story implements. Lines 8–10 and 19–32 are the image/placeholder pattern (`useProductImage()` resolver, `v-if="imageSrc"` image vs. initial-letter fallback `div`) to mirror on the detail page. Line 38 (`formatPrice(product.price)`) and lines 39–45 (out-of-stock pill vs. "N in stock" text) are the price/stock rendering precedent.
2. `app/composables/useProductImage.ts` — all 11 lines. `useProductImage()` returns a resolver `(imageUrl) => string | null`; reuse unchanged, called once at setup with the fetched product's `imageUrl`.
3. `app/composables/useProducts.ts` — all 29 lines. `ProductListItem`/`PagedProducts` interfaces and the `useProducts` composable pattern (lines 19–29): `useApi()` for the base URL, `useFetch` with `server: false` (line 26, comment lines 24–25 explain why — the .NET dev server's self-signed HTTPS cert is rejected during SSR) and a distinct `key` per call site. This story adds a sibling `useProduct(id)` export to this same file following the identical shape, returning the full `ProductDetailDto` instead of a paged list.
4. `app/composables/useApi.ts` — all 4 lines. `useApi()` returns the base **including** `/api`; build both the `GET` and `POST` URLs on top of it (`${apiBase}/products/${id}`, `${apiBase}/cart`).
5. `app/utils/format.ts` — all 11 lines. `formatPrice(value: number): string` — reuse unchanged for the price display.
6. `app/stores/auth.ts` — all 73 lines. `isAuthenticated` getter (line 27, `!!state.token`) gates the add-to-cart call. `state.token` (line 8, `token: string | null`) is the raw JWT string to send as `Authorization: Bearer ${auth.token}` — **note the field is named `token`, not `accessToken`**.
7. `app/pages/auth/login.vue` — all 42 lines. Lines 12–41 are the `loading` ref + `try/catch/finally` + `$fetch` pattern to mirror for the add-to-cart mutation: `loading.value = true` before the call (line 23), `$fetch<T>(url, { method: "POST", body })` inside `try` (lines 25–30), `toast.error(e?.data?.message ?? <fallback>)` in `catch` (lines 35–37), `loading.value = false` in `finally` (line 39). The add-to-cart call additionally needs a `headers: { Authorization: ... }` option that this existing call does not use (see Prerequisites — no precedent for this in the codebase).
8. `app/middleware/auth.ts` — all 6 lines. Confirms the middleware only redirects unauthenticated users to `/auth/login`; the product-details page must **not** register this middleware (`definePageMeta({ middleware: "auth" })`) since viewing is public — only the add-to-cart *action* is gated, done in-component via `auth.isAuthenticated`, exactly like `app/pages/cart.vue` line 2 gates the whole page (which this page must **not** do).
9. `app/pages/index.vue` — lines 52–93. The three-state (`pending` skeleton / `error` retry / loaded) pattern to adapt: skeleton lines 53–65, error notice + `BaseButton` "Retry" lines 68–76, loaded content lines 87–93. This page adds a fourth state (404 "not found") that `index.vue` does not need, since the home page never fetches a single resource by a client-supplied id.
10. `app/components/base/Button.vue` — all 62 lines. `<BaseButton>` variants (`primary`/`secondary`/`ghost`, lines 19–24), `:loading`/`:disabled` props (lines 7–8, disables and shows a spinner, lines 30 and 39–59) — use for "Add to cart" (`:loading="addingToCart"`) and the Retry button.
11. `.squad/plans/products-catalog/07-story-customer-product-catalog-list-filters.md` — sibling plan for tone/structure and the loading/error/empty precedent this story extends with a 404 state.
12. `OnlineStore.API/Controllers/ProductsController.cs` — lines 67–91 (`GetById`). `[AllowAnonymous]` (line 67), route `[HttpGet("{id:int}")]` (line 68 — the `:int` constraint means a non-numeric id in the URL never reaches this action; ASP.NET's router returns a bodyless 404 for the unmatched route). Returns `404 { message: "Product not found" }` both when the product does not exist (lines 74–77) and when it exists but `!IsActive` and the caller is not an admin (lines 80–83) — the frontend cannot and must not distinguish these two cases. On success, `ToDetail(product)` (line 238, used at line 85) returns a `ProductDetailDto`.
13. `OnlineStore.API/Dtos/ProductDetailDto.cs` — all 13 lines. Exact shape: `{ id, name, description, price, stock, categoryId, brandId, isActive, createdAt, imageUrl }` (`int`, `string`, `string`, `decimal`, `int`, `int`, `int`, `bool`, `DateTime`, `string`). No category/brand names — do not render `categoryId`/`brandId` anywhere in the UI.
14. `OnlineStore.API/Controllers/CartController.cs` — lines 23–75 (`Add`). `[Authorize]` at the class level (line 13, any authenticated role). Looks up the product by `request.ProductId` (line 34); returns `404 { message: "Product not found" }` if missing **or inactive** (line 35, `product is null || !product.IsActive`). Validates `newQuantity` (existing cart quantity + requested quantity) against `product.Stock` and returns `400 { message: "Requested quantity exceeds available stock" }` if it would exceed stock (lines 44–48). On success returns **`200 OK`** with a `CartItemDto` (line 69, `return Ok(ToDto(item, product));`) — **not 201```; there is no `CreatedAtAction`/`Created` call anywhere in this action. Detect success by the `$fetch` promise resolving without throwing, not by inspecting a status code.
15. `OnlineStore.API/Dtos/AddToCartRequest.cs` — all 11 lines. `record AddToCartRequest(int ProductId, int Quantity)` — both fields carry `[Range(1, int.MaxValue)]` data-annotation validation. A `ProductId ≤ 0` or `Quantity ≤ 0` in the POST body fails **ASP.NET's automatic model-state validation** before the action body runs, producing a `400` with the framework's default `ValidationProblemDetails` JSON (`{ errors: { Quantity: [...] }, ... }`), **not** the `{ message: "..." }` shape every other 400/404 in this API uses — `e?.data?.message` will be `undefined` for this specific failure. The quantity stepper (task 3) already clamps to `≥ 1` client-side, so this path should be unreachable in normal use; still fall back to a generic error string when `e?.data?.message` is absent (task 4).
16. `OnlineStore.API/Dtos/CartItemDto.cs` — all 9 lines. Response shape on success: `{ id, productId, productName, price, quantity, subtotal }`. Not required for the toast (a static "Added to cart" message is enough), but confirms there is no field to double-check quantity against.

---

## Frontend Tasks

### 1 — Add a single-product composable

**File: `app/composables/useProducts.ts`**

Add a `ProductDetail` interface matching `ProductDetailDto` (task 13 above) and a `useProduct` export, alongside the existing `ProductListItem`/`PagedProducts`/`useProducts`:

```ts
export interface ProductDetail {
  id: number;
  name: string;
  description: string;
  price: number;
  stock: number;
  categoryId: number;
  brandId: number;
  isActive: boolean;
  createdAt: string;
  imageUrl: string;
}

export const useProduct = (id: number) => {
  const apiBase = useApi();
  return useFetch<ProductDetail>(`${apiBase}/products/${id}`, {
    key: `product-${id}`,
    // Same reason as useProducts: the .NET dev server's self-signed HTTPS
    // cert is rejected by Nitro's SSR fetch.
    server: false,
  });
};
```

Do not add a `default:` option — unlike `useProducts`, the caller must be able to tell "still loading" apart from "no data because the id was never valid," and a fetched-but-empty default would blur that (task 2 relies on `data.value` being `undefined`/`null` until the first successful response).

### 2 — Create the product details page

**Create file: `app/pages/products/[id].vue`**

No `definePageMeta` — inherits the `default` layout implicitly (same convention as `app/pages/index.vue` and `app/pages/products/index.vue`) and stays public with no auth middleware.

```vue
<script setup lang="ts">
import { toast } from "vue-sonner";

const route = useRoute();
const auth = useAuthStore();
const apiBase = useApi();
const resolveImage = useProductImage();

const rawId = Number(route.params.id);
// A non-numeric or non-positive id can never match the backend's
// `[HttpGet("{id:int}")]` route — treat it as "not found" without a request.
const isValidId = Number.isInteger(rawId) && rawId > 0;

const { data: product, pending, error, refresh } = isValidId
  ? useProduct(rawId)
  : { data: ref(null), pending: ref(false), error: ref({ statusCode: 404 }), refresh: () => {} };

const quantity = ref(1);
const addingToCart = ref(false);

const imageSrc = computed(() => resolveImage(product.value?.imageUrl));
const initial = computed(() => product.value?.name.trim().charAt(0).toUpperCase() ?? "");

const isNotFound = computed(() => (error.value as any)?.statusCode === 404);

watch(product, (p) => {
  if (p) quantity.value = Math.min(quantity.value, Math.max(p.stock, 1));
});

function incrementQuantity() {
  if (product.value) quantity.value = Math.min(quantity.value + 1, product.value.stock);
}
function decrementQuantity() {
  quantity.value = Math.max(quantity.value - 1, 1);
}

async function addToCart() {
  if (!product.value) return;
  if (!auth.isAuthenticated) {
    await navigateTo("/auth/login");
    return;
  }

  addingToCart.value = true;
  try {
    await $fetch(`${apiBase}/cart`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.token}` },
      body: { productId: product.value.id, quantity: quantity.value },
    });
    toast.success("Added to cart");
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not add this item to your cart");
  } finally {
    addingToCart.value = false;
  }
}
</script>
```

Template, in this order:

1. **Breadcrumb/back link** — `<NuxtLink to="/products" class="text-sm font-medium text-primary hover:underline">← Back to products</NuxtLink>`, always rendered above every state below (loading, not-found, error, and loaded), matching the "View all products →" link precedent in `app/pages/index.vue` line 96.
2. **Loading** — `v-if="pending"`: a skeleton mirroring `app/pages/index.vue` lines 53–65 but as a single two-column block (image placeholder left, text-line placeholders right) instead of a grid of cards, using the same `animate-pulse` / `bg-gray-200` utility classes.
3. **Not found** — `v-else-if="isNotFound"`: a bordered notice, e.g. "We couldn't find this product." plus a `<NuxtLink to="/products">` styled as a `BaseButton` ("Browse products") — **do not** render `error.value.message` or any raw error object; this is a dedicated, clear state, not the generic error state.
4. **Error (non-404)** — `v-else-if="error"`: bordered notice matching `app/pages/index.vue` lines 68–76 ("We couldn't load this product right now." + `BaseButton` "Retry" calling `refresh()`).
5. **Loaded** — `v-else-if="product"`: two-column layout (`grid gap-8 sm:grid-cols-2`):
   - Left: image via `imageSrc`/`initial` fallback, same `v-if="imageSrc"` / `v-else` pattern as `ProductCard.vue` lines 19–32, sized `aspect-square w-full rounded-xl` (no `object-cover` crop needed at this larger size, but keep `object-cover` for consistency).
   - Right: `<h1>{{ product.name }}</h1>` (`text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl`, matching the heading scale in `app/pages/index.vue` line 44), `<p>` description (`text-gray-600`, preserve line breaks with `whitespace-pre-line` since `Description` is a plain `string` with no rendering hints from the API), `<p class="font-semibold text-primary text-xl">{{ formatPrice(product.price) }}</p>`.
   - Stock indicator: `v-if="product.stock <= 0"` an "Out of stock" pill (same classes as `ProductCard.vue` line 41) **and hide the quantity selector and Add to cart button entirely** in that case (per acceptance criteria — disable/hide entirely when `stock === 0`; this plan hides, matching the "hide entirely" phrasing); otherwise `<p>{{ product.stock }} in stock</p>` (`ProductCard.vue` line 45 style) plus the quantity stepper and Add to cart button.
   - Quantity stepper: two `<button>`s (`decrementQuantity`/`incrementQuantity`) flanking a numeric display bound to `quantity`, decrement disabled when `quantity <= 1`, increment disabled when `quantity >= product.stock`. Do not reuse `<BaseInput>` here — it is a string-`modelValue`, label+error-focused component styled for the auth forms (`#1b3a6b` palette, `app/components/base/Input.vue` lines 2–13, 30–53); a plain stepper matching the `primary` theme and `BaseButton` styling is simpler and avoids a string↔number `modelValue` conversion for no benefit.
   - `<BaseButton :loading="addingToCart" @click="addToCart">{{ auth.isAuthenticated ? "Add to cart" : "Log in to add to cart" }}</BaseButton>` — label reflects the redirect behavior so the click target sets the right expectation before the redirect happens.

### 3 — No changes to `ProductCard.vue`, `default.vue`, or the backend

**No changes required** to `app/components/ProductCard.vue` — its `/products/${product.id}` link (line 16) already targets this page correctly once task 2's page file exists; the comment on line 14 ("`/products/{id}` does not exist yet; it is the correct eventual target") becomes stale and should be deleted as part of this story since the route now exists.
**No changes required** to `app/layouts/default.vue` — the page inherits the header/footer via the default layout with no page-specific layout wiring needed.
**No backend changes** — `ProductsController.GetById` and `CartController.Add` are used exactly as they exist today (tasks 12–16 above).

---

## Edge Cases & Failure Modes

- **Non-numeric or non-positive `id` in the URL** (e.g. `/products/abc`, `/products/-1`, `/products/0`): task 2's `isValidId` check catches this client-side and renders the not-found state without ever issuing a request — matches the backend behavior anyway, since `[HttpGet("{id:int}")]` (`ProductsController.cs` line 68) never matches those URLs and would 404 at the routing layer with no JSON body.
- **404 with no JSON body vs. 404 with `{ message }`:** the not-found state (task 2 step 3) is driven purely by `error.value.statusCode === 404`, never by reading `error.value.data.message` — this correctly covers both the JSON `{ message: "Product not found" }` the controller returns (`ProductsController.cs` lines 76, 82) and the bodyless 404 from an unmatched `{id:int}` route.
- **Product exists but is inactive, viewed by a non-admin (anonymous or logged-in customer):** `GetById` returns the same `404 { message: "Product not found" }` as a genuinely missing product (`ProductsController.cs` lines 80–83) — the frontend shows the identical not-found state in both cases; there is no way (and no need) to tell them apart client-side.
- **Stock changes between page load and clicking "Add to cart"** (another customer buys the last units): the client-side clamp on `quantity` was correct at load time but the backend re-validates against current stock (`CartController.cs` lines 44–48) and returns `400 { message: "Requested quantity exceeds available stock" }` if it no longer fits — the `catch` block in `addToCart` (task 2) surfaces that exact message via `e?.data?.message`; no client-side re-fetch of stock is attempted before the POST.
- **Automatic model-validation 400 (malformed body only)** — see Context item 15: `Quantity ≤ 0` or `ProductId ≤ 0` in the POST body short-circuits to ASP.NET's default `ValidationProblemDetails`, which has no top-level `message` field, so `e?.data?.message` is `undefined` and the fallback string ("Could not add this item to your cart") is shown instead. Unreachable in normal use since the stepper clamps `quantity >= 1` and `product.value.id` is always a positive int from the fetched DTO, but the fallback covers it defensively.
- **Unauthenticated add-to-cart click:** redirects to `/auth/login` (task 2, `addToCart`) with **no return-path preservation** — after logging in, `login.vue` (lines 32–34) unconditionally `navigateTo("/")`s to the home page, not back to this product. This is a known, explicitly out-of-scope gap for this story (see Story Goal — "Not in scope"); do not modify `login.vue` to add redirect-query handling as part of this story.
- **Race between the page's own `auth.isAuthenticated` check and an expired/invalid token still present in the cookie:** if the stored token is stale, the client-side check still passes (`isAuthenticated` only checks `!!state.token`, `auth.ts` line 27) and the `POST /api/cart` request is sent with a bad `Authorization` header; the backend's `[Authorize]` middleware then rejects it before the action runs, and `$fetch` throws — surfaced through the same generic `catch` block and fallback message, since a 401 from the auth middleware carries no `{ message }` body either.
- **Concurrent double-click on "Add to cart":** `addingToCart` is set to `true` before the request and the button is `:loading`/implicitly disabled via `BaseButton`'s `disabled || loading` (`Button.vue` line 30), so a second click during an in-flight request is blocked at the UI level; no debounce needed beyond this.
- **`description` containing newlines:** rendered with `whitespace-pre-line` (task 2 step 5) so backend-authored line breaks in `Description` are preserved rather than collapsed to a single line.
- **SSR vs. self-signed dev certificate:** identical to Stories 05/07 — `server: false` in `useProduct` (task 1) avoids the Nitro server making the request during SSR, where Node would reject the API's self-signed HTTPS certificate.

---

## Test Plan

**No test runner is configured** in this project — confirmed by Stories 05 and 07 (`package.json` has `build`/`dev`/`generate`/`preview`/`postinstall` only, no Vitest dependency), still true as of this story. Verification is manual (see Verification Steps). If Vitest + `@nuxt/test-utils` is introduced later, these are the tests to add, matching the structure of `.squad/plans/products-catalog/07-story-customer-product-catalog-list-filters.md`'s Test Plan section:

1. **Unit — `useProduct`:** calling with a numeric id builds the URL `${apiBase}/products/${id}` and passes `server: false`; the `key` is unique per id (`product-${id}`).
2. **Component — `app/pages/products/[id].vue`** with `useProduct` mocked: `pending` renders the skeleton; a `404` error renders the not-found state (not the generic error notice); a non-404 error renders the generic error notice with a working Retry (`refresh()` called on click); a loaded product with `stock === 0` hides the quantity stepper and Add to cart button and shows the "Out of stock" pill; a loaded product with `stock > 0` shows both, with the stepper clamped between 1 and `stock`.
3. **Component — quantity stepper boundaries:** `stock: 1` disables both increment (already at max) and decrement (already at min) simultaneously; `stock: 5, quantity: 5` disables increment only; `quantity: 1` disables decrement only.
4. **Component — add-to-cart auth gate:** with `auth.isAuthenticated` false, clicking "Add to cart" calls `navigateTo("/auth/login")` and does **not** call `$fetch`; with it true, clicking calls `$fetch` with `method: "POST"`, the `Authorization` header set from `auth.token`, and `body: { productId, quantity }`.
5. **Component — add-to-cart error surfacing:** a rejected `$fetch` with `e.data.message` set shows that exact string in the error toast; a rejection with no `data.message` shows the generic fallback string.

---

## Verification Steps

1. **Frontend builds:** in `online-store-frontend/`, run `pnpm build`. Must succeed — a wrong auto-import name for `<BaseButton>` or a bad `useProduct` export fails here.
2. **Route resolves from `ProductCard`:** with `pnpm dev` running and the API up, open `/` or `/products` and click any product card — lands on `/products/{id}` with no 404, product details render inside the existing header/footer.
3. **Data path:** confirm the network tab shows a request to `https://localhost:7225/api/products/{id}` returning the `ProductDetailDto` shape (`id, name, description, price, stock, categoryId, brandId, isActive, createdAt, imageUrl`).
4. **Loading state:** throttle the network to Slow 3G and reload a product page — the skeleton renders before data lands.
5. **Not-found state:** navigate to `/products/999999` (or any id that does not exist) — the dedicated "couldn't find this product" state renders with a working link back to `/products`, not a raw error or the generic error notice. Also try `/products/abc` — same result, no network request fires (check the network tab).
6. **Inactive product:** as an admin, mark a product inactive (via the admin UI once available, or directly in the database), then visit its `/products/{id}` while logged out or as a non-admin — confirms the same not-found state (not a raw 404 page).
7. **Error state:** stop the API and reload a valid product's page — the generic error notice renders with a Retry button; clicking Retry re-issues the request once the API is back.
8. **Quantity clamp:** on a product with `stock` (e.g., `5`), confirm the stepper cannot go below 1 or above 5; on a product with `stock === 0`, confirm the stepper and "Add to cart" button are not rendered at all, and the "Out of stock" pill is.
9. **Unauthenticated add-to-cart:** while logged out, click "Log in to add to cart" — redirected to `/auth/login`, no `POST /api/cart` request is sent (check the network tab).
10. **Authenticated add-to-cart success:** while logged in, pick a quantity, click "Add to cart" — a success toast ("Added to cart") appears, and the network tab shows a `POST` to `https://localhost:7225/api/products/{id}`-sibling `.../cart` with `Authorization: Bearer <token>` and a `200` response.
11. **Authenticated add-to-cart failure (stock exceeded):** set a product's stock to `1` in the database while another cart already holds it (or pick a quantity greater than available stock if the stepper's clamp is bypassed for testing), click "Add to cart" — an error toast shows "Requested quantity exceeds available stock" (the backend's exact message).
12. **Backend unchanged:** `dotnet build` in `OnlineStore.API/` — expected unchanged, this story touches no C#.
13. **Responsive:** at 375 px the image/details stack into one column; at 1280 px they sit side by side per the `sm:grid-cols-2` breakpoint.

---

## Done Criteria

- [ ] `app/pages/products/[id].vue` exists, has no `definePageMeta`, is public (no auth middleware), and is reachable at `/products/{id}` from every existing `ProductCard` link.
- [ ] The page fetches `GET /api/products/{id}` via a new `useProduct` export in `app/composables/useProducts.ts`, and renders image (with placeholder), name, description, `formatPrice`-formatted price, stock, and an out-of-stock indicator when `stock === 0`.
- [ ] A dedicated not-found state (distinct from the generic error state) renders on a 404 response, with a link back to `/products` — no raw error is shown.
- [ ] A generic error state (non-404 failures) renders with a Retry button calling `refresh()`.
- [ ] A quantity stepper is clamped to `[1, stock]` and is hidden entirely, along with the "Add to cart" button, when `stock === 0`.
- [ ] "Add to cart" redirects to `/auth/login` when `!auth.isAuthenticated`; when authenticated, it `POST`s to `/api/cart` with `Authorization: Bearer <token>` and `{ productId, quantity }`, showing a success toast on resolution and an error toast (`e?.data?.message` when present, generic fallback otherwise) on rejection.
- [ ] A visible link/breadcrumb back to `/products` is present in every state (loading, not-found, error, loaded).
- [ ] The stale comment on `ProductCard.vue` line 14 ("does not exist yet") is removed.
- [ ] `pnpm build` succeeds; no arbitrary `bg-[#…]`/`text-[#…]` or raw `px` spacing introduced.

**STOP HERE. Report to the user and wait for confirmation before proceeding to the next story in the customer-journey chain (cart-page).**
