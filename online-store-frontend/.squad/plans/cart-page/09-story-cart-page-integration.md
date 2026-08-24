# Story 09 — Cart Page Integration

Replace the `app/pages/cart.vue` placeholder with a real cart page: fetch the logged-in user's cart via `GET /api/cart`, render each line item with a "Remove" action, show the running total, and provide a "Proceed to checkout" link. This is the third story in the customer-journey chain (products-catalog → product-details → **cart-page** → checkout-payment).

---

## Prerequisites

- **Story 08** ([`../product-details/08-story-product-details-page.md`](../product-details/08-story-product-details-page.md)) completed: established the codebase's first authenticated `Authorization: Bearer …` request pattern (`app/pages/products/[id].vue`'s `addToCart`, using `auth.token` directly — **the field is named `token`, not `accessToken`**) and the `useApi()` + `$fetch`/`useFetch` conventions this story reuses. This story follows the identical header-attachment pattern for the `GET` and `DELETE` cart calls rather than inventing a new one.
- Backend `GET /api/cart` and `DELETE /api/cart/{id}` are already implemented and unchanged for this story — `OnlineStore.API/Controllers/CartController.cs` lines 77–108 (`Get`) and lines 110–142 (`Delete`). **No backend change is required or permitted in this story.**
- The API must be running for the page to populate. `nuxt.config.ts` sets `apiBase` to `https://localhost:7225/api`; the API's CORS policy allows only `http://localhost:3000`, so run the frontend on the default port (same constraint documented in Stories 05, 07, and 08).
- The "Checkout and Payment Page" story (not yet planned) builds `/checkout`; this story's "Proceed to checkout" control links there without that route existing yet — see Story Goal and Edge Cases for how this is handled.

---

## Story Goal

1. `app/pages/cart.vue` (already guarded by `definePageMeta({ middleware: "auth" })`, line 2 — **unchanged**, cart viewing requires login) fetches `GET /api/cart` on load and renders each `CartItemDto` (`productName`, `price` via `formatPrice`, `quantity`, `subtotal` via `formatPrice`) plus the response's `total` (via `formatPrice`).
2. Each line has a "Remove" action that calls `DELETE /api/cart/{id}`; on success, the list and total update (via a refetch) and a success toast shows; on failure, an error toast shows and the line stays.
3. A dedicated empty-cart state (cart fetched successfully with zero items): friendly message + link to `/products` — **not** an error state.
4. A loading state (skeleton) while the initial fetch is in flight, and a distinct error state (network/500 failure) with a Retry action.
5. A "Proceed to checkout" `<NuxtLink to="/checkout">` styled as a `BaseButton`, disabled (not rendered as a functioning link) when the cart is empty. `/checkout` does not exist yet — it is built in a later, not-yet-planned story; this story links to it anyway and documents the resulting 404 as a known, out-of-scope gap (see Edge Cases).

**Not in scope:** coupon/discount codes; saved/multiple carts or guest carts (the backend has one cart per authenticated user, enforced server-side via the JWT — no client-side user-id handling needed); quantity editing (the backend has no "update quantity" endpoint, only add-which-increments via `POST /api/cart` and remove via `DELETE /api/cart/{id}` — a remove-then-re-add quantity-edit control is optional/stretch and is **not** implemented in this story to keep scope tight); building the `/checkout` route itself (separate story).

---

## Context — Read These Files First

1. `app/pages/cart.vue` — all 10 lines. Current placeholder: `definePageMeta({ middleware: "auth" })` (line 2) and a static "Cart contents coming soon." message (line 8). This story replaces lines 5–9 (the template body) and the `<script setup>` block, keeping the `definePageMeta` call unchanged.
2. `app/middleware/auth.ts` — all 6 lines. `useAuthStore().isAuthenticated` gates the whole page (redirects to `/auth/login` if falsy) — confirms `cart.vue`'s existing `middleware: "auth"` is the correct and sufficient guard; no additional in-component auth check is needed (unlike the product-details page, where only the add-to-cart *action* was gated because viewing was public).
3. `app/stores/auth.ts` — all 73 lines. `state.token` (line 8, `token: string | null`) is the raw JWT to send as `Authorization: Bearer ${auth.token}` — same field Story 08 used, **not** `accessToken`.
4. `app/utils/format.ts` — all 11 lines. `formatPrice(value: number): string` — reuse unchanged for `price`, `subtotal`, and `total`.
5. `app/composables/useApi.ts` — all 4 lines. `useApi()` returns the base **including** `/api`; build `${apiBase}/cart` for the `GET` and `${apiBase}/cart/${id}` for the `DELETE`.
6. [`../product-details/08-story-product-details-page.md`](../product-details/08-story-product-details-page.md), Frontend Tasks §2 (the `addToCart` function in the code block) — the exact `try`/`catch`/`finally` + `headers: { Authorization: \`Bearer ${auth.token}\` }` + `toast.success`/`toast.error(e?.data?.message ?? …)` shape to mirror for this story's `removeItem` mutation. Story 08 established this as the codebase's first authenticated request; this story is the second, following the identical pattern rather than reinventing it.
7. `app/pages/index.vue` — lines 52–76. The `pending` skeleton (lines 53–65) and `error` notice + `BaseButton` "Retry" calling `refresh()` (lines 68–76) pattern to adapt for the cart's loading/error states (as a vertical list layout instead of a card grid).
8. `app/components/base/Button.vue` — all 62 lines. `<BaseButton>` `variant` prop (`primary`/`secondary`/`ghost`, lines 19–24) and `:disabled`/`:loading` props (lines 7–8, 30) — use `variant="secondary"` for each line's "Remove" action (matching the Retry-button precedent in `app/pages/index.vue` line 74) and the default `primary` variant for "Proceed to checkout"; `:disabled="true"` when the cart is empty renders the greyed-out, non-interactive state (`Button.vue` line 34, `disabled:cursor-not-allowed disabled:opacity-60`) without a page-specific style.
9. `OnlineStore.API/Controllers/CartController.cs` — lines 77–108 (`Get`) and lines 110–142 (`Delete`). `Get`: `[Authorize]` at the class level (line 13); returns `401` via `Unauthorized()` if the JWT claim is missing (lines 82–86, unreachable in practice since `cart.vue`'s `auth` middleware already redirects unauthenticated visitors before this page renders); on success returns **`200 OK`** with `new CartResponse(items, total)` (line 102) where `items` is ordered by `ci.Id` (line 90) and each item's `subtotal` is computed server-side as `Price * Quantity` (lines 96–97); on any unhandled exception, `500 { message: "Something went wrong" }` (lines 104–107). `Delete`: route is `[HttpDelete("{id:int}")]` (line 110, the `:int` constraint means a non-numeric `id` never reaches this action); looks the item up by its cart-item `id` (not `productId`) via `_db.CartItems.FindAsync(id)` (line 121); returns `404 { message: "Cart item not found" }` if missing (lines 122–125); returns `403` via bare `Forbid()` (**no JSON body**) if `item.UserId != userId` (lines 128–131 — unreachable from this UI since every rendered "Remove" button's `id` comes from the user's own fetched cart, but the frontend must not assume a body is present on this status); on success returns **`204 No Content`** (line 136, no response body); on any unhandled exception, `500 { message: "Something went wrong" }` (lines 138–141).
10. `OnlineStore.API/Dtos/CartItemDto.cs` — all 9 lines. `record CartItemDto(int Id, int ProductId, string ProductName, decimal Price, int Quantity, decimal Subtotal)` — serialized camelCase by the API's default JSON settings (confirmed by the `ProductDetailDto` → frontend field-name mapping in Story 08's Context item 13), so the frontend object shape is `{ id, productId, productName, price, quantity, subtotal }`. `id` is the **cart item's** id (the one to send to `DELETE /api/cart/{id}`), not the product's id — `productId` is a separate field, present but not needed by this page's UI (no per-line link to the product is required by the acceptance criteria).
11. `OnlineStore.API/Dtos/CartResponse.cs` — all 3 lines. `record CartResponse(List<CartItemDto> Items, decimal Total)` — serialized as `{ items, total }`.

---

## Frontend Tasks

### 1 — Add a cart composable

**File: `app/composables/useProducts.ts`**

No changes to this file — it is product-catalog-specific (`useProducts`/`useProduct`, `ProductListItem`/`ProductDetail`) and unrelated to cart data. Instead, create a new sibling composable file dedicated to cart state, matching the one-composable-per-domain convention already established by `useProducts.ts` and `useProductImage.ts`.

**Create file: `app/composables/useCart.ts`**

```ts
export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  price: number;
  quantity: number;
  subtotal: number;
}

export interface CartResponse {
  items: CartItem[];
  total: number;
}

export const useCart = () => {
  const apiBase = useApi();
  const auth = useAuthStore();
  return useFetch<CartResponse>(`${apiBase}/cart`, {
    key: "cart",
    headers: { Authorization: `Bearer ${auth.token}` },
    // Same reason as useProducts/useProduct: the .NET dev server's
    // self-signed HTTPS cert is rejected by Nitro's SSR fetch.
    server: false,
  });
};
```

No `default:` option — same reasoning as `useProduct` in Story 08 (Frontend Tasks §1): the page must be able to tell "still loading" (`data.value` is `undefined`) apart from "loaded, zero items" (`data.value` is `{ items: [], total: 0 }`) to choose between the loading skeleton and the empty-cart state.

### 2 — Rewrite the cart page

**File: `app/pages/cart.vue`**

Keep `definePageMeta({ middleware: "auth" })` (line 2) unchanged. Replace the rest:

```vue
<script setup lang="ts">
import { toast } from "vue-sonner";

definePageMeta({ middleware: "auth" });

const apiBase = useApi();
const auth = useAuthStore();

const { data: cart, pending, error, refresh } = useCart();

const removingId = ref<number | null>(null);

async function removeItem(id: number) {
  removingId.value = id;
  try {
    await $fetch(`${apiBase}/cart/${id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${auth.token}` },
    });
    toast.success("Item removed from cart");
    await refresh();
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not remove this item");
  } finally {
    removingId.value = null;
  }
}
</script>
```

Template, in this order:

1. **Heading** — `<h1>Your Cart</h1>` (`text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl`, matching the heading scale in `app/pages/index.vue` line 44), replacing the current bare `<h1 class="text-2xl font-bold">Your Cart</h1>` (line 7).
2. **Loading** — `v-if="pending"`: a skeleton list mirroring `app/pages/index.vue` lines 53–65's `animate-pulse`/`bg-gray-200` conventions, but as stacked row placeholders (e.g. 3 rows of `h-16 rounded-lg bg-gray-100 animate-pulse`) instead of a card grid, since the cart is a list, not a grid.
3. **Error** — `v-else-if="error"`: bordered notice matching `app/pages/index.vue` lines 68–76 ("We couldn't load your cart right now." + `BaseButton` "Retry" calling `refresh()`).
4. **Empty** — `v-else-if="cart && cart.items.length === 0"`: a distinct friendly message, e.g. "Your cart is empty." plus a `<NuxtLink to="/products">` styled as a `BaseButton` ("Browse products") — **not** the error notice, matching the acceptance criteria's explicit "not an error state."
5. **Loaded** — `v-else-if="cart"`:
   - A vertical list of line items (`space-y-4`), one row per `cart.items`: `productName` (`font-medium text-gray-900`), `formatPrice(item.price)` × `item.quantity` (e.g. `{{ formatPrice(item.price) }} × {{ item.quantity }}`, `text-sm text-gray-500`), `formatPrice(item.subtotal)` (`font-semibold text-primary`), and a `<BaseButton variant="secondary" :loading="removingId === item.id" @click="removeItem(item.id)">Remove</BaseButton>`.
   - Below the list: the running total, `<p class="text-lg font-semibold">Total: {{ formatPrice(cart.total) }}</p>`.
   - `<NuxtLink to="/checkout">` wrapping `<BaseButton :disabled="cart.items.length === 0">Proceed to checkout</BaseButton>` — the `:disabled` binding is redundant with this branch already requiring `cart.items.length > 0` (the empty state above intercepts the zero-item case first), but is kept explicit per the acceptance criteria's "disable it when the cart is empty" wording, and protects against a future refactor that merges the empty and loaded branches.

### 3 — No changes to `app/layouts/default.vue`, `app/middleware/auth.ts`, or the backend

**No changes required** to `app/layouts/default.vue` — the page inherits the header/footer via the default layout with no page-specific layout wiring needed (same as `cart.vue` today).
**No changes required** to `app/middleware/auth.ts` — reused exactly as-is.
**No backend changes** — `CartController.Get` and `CartController.Delete` are used exactly as they exist today (Context items 9–11 above).

---

## Edge Cases & Failure Modes

- **`/checkout` does not exist yet:** the "Proceed to checkout" link (Frontend Tasks §2 step 5) points at `/checkout`, which has no corresponding `app/pages/checkout.vue` file as of this story — clicking it while the cart is non-empty 404s. This is explicitly accepted per the intake ("a route that doesn't exist yet — a separate story builds it... note this explicitly, don't block on it") and is **not** a defect to fix here; the "Checkout and Payment Page" story builds that route next in the customer-journey chain.
- **Removing the last item in the cart:** after `removeItem` resolves and `refresh()` re-fetches, `cart.value.items.length` becomes `0` and the template branch (Frontend Tasks §2 step 4) switches from the loaded list to the empty-cart state on the same page load — no full page reload needed, since `refresh()` re-runs the same `useFetch` and Vue's reactivity re-evaluates the `v-else-if` chain.
- **Remove request for an item that was already removed elsewhere** (e.g. a second browser tab, or a double-click before `removingId` disables the button): `CartController.Delete` returns `404 { message: "Cart item not found" }` (`CartController.cs` lines 122–125) — surfaced via `e?.data?.message` in the `catch` block (Frontend Tasks §2), showing "Cart item not found" instead of the generic fallback. `removingId` is set before the request and cleared in `finally`, so `BaseButton`'s `:loading`/implicit `disabled` (`Button.vue` line 30) blocks a second click on the *same* row while its request is in flight, but does not block other rows' buttons.
- **403 Forbid on `DELETE /api/cart/{id}`:** `Forbid()` (`CartController.cs` line 130) returns a bare `403` with **no JSON body** — `e?.data?.message` is `undefined` in this case, so the generic fallback string ("Could not remove this item") is shown. Documented as unreachable from this UI (every rendered "Remove" button's `id` comes from the user's own fetched `cart.items`), but the fallback covers it defensively, same pattern as Story 08's model-validation-400 edge case.
- **401 on `GET /api/cart` from a stale/expired token still present in the cookie:** `cart.vue`'s `auth` middleware (`app/middleware/auth.ts`) only checks `!!state.token` (`auth.ts` line 27), not token validity, so a stale token still passes the client-side guard and reaches the page; the subsequent `GET /api/cart` request then fails server-side with a bodyless `401` (`CartController.cs` lines 82–86), which `useCart`'s `error` ref captures — the generic error state (Frontend Tasks §2 step 3) renders, since there is no dedicated "session expired, log in again" state distinct from a network/500 failure in this story's scope.
- **Concurrent double-click on "Remove" across two different rows:** each row's `removingId === item.id` check (Frontend Tasks §2 step 5) is per-row, so clicking "Remove" on two different lines in quick succession fires two independent `$fetch` calls — both resolve independently and each triggers its own `refresh()`; the last `refresh()` to resolve reflects the final server state correctly since `useCart` always re-fetches the full list rather than patching client-side.
- **SSR vs. self-signed dev certificate:** identical to Stories 05/07/08 — `server: false` in `useCart` (Frontend Tasks §1) avoids the Nitro server making the request during SSR, where Node would reject the API's self-signed HTTPS certificate.
- **`Authorization` header on a `server: false` `useFetch`:** since `useCart`'s `GET` only ever runs client-side (`server: false`), `auth.token` is always read from the client-side Pinia store state (already hydrated from the `auth` cookie by `auth.ts`'s `loadFromStorage`, per Story 08's established pattern) — there is no SSR-time header-attachment concern to handle here, matching Story 08's `useProduct`/product-details precedent.

---

## Test Plan

**No test runner is configured** in this project — confirmed by Stories 05, 07, and 08 (`package.json` has `build`/`dev`/`generate`/`preview`/`postinstall` only, no Vitest dependency), still true as of this story. Verification is manual (see Verification Steps). If Vitest + `@nuxt/test-utils` is introduced later, these are the tests to add, matching the structure of `.squad/plans/product-details/08-story-product-details-page.md`'s Test Plan section:

1. **Unit — `useCart`:** the `useFetch` call targets `${apiBase}/cart`, sets `headers: { Authorization: `Bearer ${auth.token}` }` from the auth store, and passes `server: false`.
2. **Component — `app/pages/cart.vue`** with `useCart` mocked: `pending` renders the skeleton; a non-404 `error` renders the generic error notice with a working Retry (`refresh()` called on click); `data: { items: [], total: 0 }` renders the empty-cart state (not the error notice) with a link to `/products`; `data: { items: [...], total }` renders one row per item plus the formatted total.
3. **Component — remove action:** clicking "Remove" on a row calls `$fetch` with `method: "DELETE"` against `${apiBase}/cart/{id}` and the `Authorization` header, then calls `refresh()` on success and shows a success toast; a rejected `$fetch` with `e.data.message` set shows that exact string in the error toast, and a rejection with no `data.message` shows the generic fallback string; the clicked row's button shows `:loading` only for that row's `removingId`, not for other rows.
4. **Component — checkout link:** the "Proceed to checkout" control is `:disabled` (or not rendered as an active link) when `cart.items.length === 0`, and links to `/checkout` when the cart has items.

---

## Verification Steps

1. **Frontend builds:** in `online-store-frontend/`, run `pnpm build`. Must succeed — a wrong auto-import name for `<BaseButton>` or a bad `useCart` export fails here.
2. **Auth guard unchanged:** while logged out, navigate to `/cart` directly — redirected to `/auth/login` (unchanged behavior from the existing placeholder).
3. **Data path:** while logged in with items in the cart (add some via `/products/{id}` from Story 08 first), open `/cart` — the network tab shows a request to `https://localhost:7225/api/cart` with an `Authorization: Bearer <token>` header, returning `{ items, total }`; the page renders one row per item with `productName`, formatted unit price, quantity, formatted subtotal, and the formatted `total`.
4. **Loading state:** throttle the network to Slow 3G and reload `/cart` — the skeleton renders before data lands.
5. **Empty-cart state:** with an account that has no cart items, open `/cart` — the "Your cart is empty" message renders with a working link to `/products`, not the error notice.
6. **Error state:** stop the API and reload `/cart` — the generic error notice renders with a Retry button; clicking Retry re-issues the request once the API is back.
7. **Remove success:** click "Remove" on any line — the network tab shows a `DELETE` to `https://localhost:7225/api/cart/{id}` with the `Authorization` header and a `204` response; a success toast appears; the row disappears and the total updates without a full page reload.
8. **Remove to empty:** remove every item one at a time — after the last removal, the page switches from the loaded list to the empty-cart state.
9. **Remove failure (already removed):** remove an item, then click "Remove" again quickly on the same row before the first request resolves (or manually delete the same cart-item id via a second request) — an error toast shows "Cart item not found" (the backend's exact message).
10. **Checkout link:** with a non-empty cart, confirm "Proceed to checkout" is an active link targeting `/checkout` (it 404s — expected per Edge Cases, since that route is not built yet); with an empty cart, confirm the control is disabled/non-interactive.
11. **Backend unchanged:** `dotnet build` in `OnlineStore.API/` — expected unchanged, this story touches no C#.
12. **Responsive:** at 375 px the line items and total stack in a single column with no horizontal overflow; at 1280 px the list reads comfortably within the page's max-width.

---

## Done Criteria

- [ ] `app/pages/cart.vue` keeps its existing `middleware: "auth"` guard, fetches `GET /api/cart` via a new `useCart` composable in `app/composables/useCart.ts`, and renders each `CartItem` (`productName`, formatted `price`, `quantity`, formatted `subtotal`) plus the formatted `total`.
- [ ] Each line has a "Remove" action calling `DELETE /api/cart/{id}` with the `Authorization: Bearer <token>` header; on success it refetches the cart and shows a success toast; on failure it shows an error toast (`e?.data?.message` when present, generic fallback otherwise) and leaves the line in place.
- [ ] A dedicated empty-cart state (fetched, zero items) renders a friendly message and a link to `/products` — distinct from the error state.
- [ ] A loading skeleton renders while the initial fetch is in flight; a distinct error state (network/500) renders with a Retry button calling `refresh()`.
- [ ] "Proceed to checkout" links to `/checkout` and is disabled/non-interactive when the cart is empty; the route not existing yet is documented, not fixed, in this story.
- [ ] No quantity-edit control is added (out of scope per the intake's stretch note).
- [ ] `pnpm build` succeeds; no arbitrary `bg-[#…]`/`text-[#…]` or raw `px` spacing introduced.

**STOP HERE. Report to the user and wait for confirmation before proceeding to the next story in the customer-journey chain (checkout-payment).**
