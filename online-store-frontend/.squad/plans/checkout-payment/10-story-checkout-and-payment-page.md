# Story 10 — Checkout and Payment Page

Build `/checkout`: a review-and-confirm page reached from the cart page's "Proceed to checkout" link. The backend checkout endpoint takes no body — it validates the cart server-side, simulates a 1.5s "payment" that always succeeds, and returns the created order. This page is primarily an order review plus a single confirm action, then a redirect to the order's detail view. This is the fourth story in the customer-journey chain (products-catalog → product-details → cart-page → **checkout-payment** → orders-history).

---

## Prerequisites

- **Story 09** ([`../cart-page/09-story-cart-page-integration.md`](../cart-page/09-story-cart-page-integration.md)) completed: established `app/composables/useCart.ts` (the `useCart()` composable, `CartItem`/`CartResponse` interfaces, `GET /api/cart` with `Authorization: Bearer ${auth.token}`, `server: false`) and `app/pages/cart.vue`'s "Proceed to checkout" `<NuxtLink to="/checkout">`. This story **reuses `useCart()` unchanged** for the order-summary fetch on `/checkout` rather than duplicating a second cart-fetching composable — same data shape, same loading/error/empty branches, adapted from a mutable cart view to a read-only review.
- Backend `POST /api/orders/checkout` is already implemented and unchanged for this story — `OnlineStore.API/Controllers/OrdersController.cs` lines 23–99 (`Checkout`). **No backend change is required or permitted in this story.**
- The "Customer Orders History" story (not yet planned) builds the order-detail route this page redirects to after success. As of this story, no `app/pages/orders/` directory exists in the frontend (confirmed by directory listing) — the redirect target `/orders/{id}` does **not** exist yet. This story still implements the `navigateTo(`/orders/${dto.id}`)` call; it will 404 until the orders-history story lands. This is documented, expected, and not a defect (see Edge Cases).
- The API must be running for the page to function. `nuxt.config.ts` sets `apiBase` to `https://localhost:7225/api`; the API's CORS policy allows only `http://localhost:3000`, so run the frontend on the default port (same constraint documented in Stories 05, 07, 08, and 09).

---

## Story Goal

1. `app/pages/checkout.vue` (new page), `definePageMeta({ layout: "default", middleware: "auth" })`, fetches `GET /api/cart` via the existing `useCart()` composable and renders a **read-only** order summary: each item's `productName`, unit `price` (via `formatPrice`), `quantity`, line `subtotal` (via `formatPrice`), and the cart's `total` (via `formatPrice`) — same fields Story 09 renders on `/cart`, but with no "Remove" action and no quantity controls.
2. If the cart is empty (`cart.value.items.length === 0` after a successful fetch), redirect back to `/cart` immediately — checkout is never reachable with nothing to order.
3. A "Place order" `<BaseButton>` calls `POST /api/orders/checkout` with no body, `Authorization: Bearer ${auth.token}` header, via `$fetch` (a one-off mutation, not `useFetch`/`useCart`). While the request is in flight, the button shows `:loading="true"` (spinner + disabled, via `BaseButton`'s existing `loading` prop, `app/components/base/Button.vue` line 7 and lines 30/40–59) so a second click cannot fire a duplicate `POST` during the backend's simulated 1.5s delay.
4. On success (**201** `OrderDetailDto` — confirmed in `OrdersController.cs` line 93, `CreatedAtAction(nameof(GetById), new { id = order.Id }, dto)`), show a success toast and call `navigateTo(`/orders/${dto.id}`)`.
5. On failure (**400** with body `{ message: "Cart is empty" }`, `{ message: "Insufficient stock for {ProductName}" }`, or `{ message: "Product {ProductId} is unavailable" }` — exact strings confirmed in `OrdersController.cs` lines 41, 50, 55), show the exact backend message (`e?.data?.message`) in an error toast. The backend performs all validation and the stock decrement inside one DB transaction before committing (lines 44–91), so a 400 leaves the cart and stock completely untouched — no partial-state cleanup is needed on the frontend; the user can retry "Place order" or navigate back to `/cart`.
6. No payment form fields of any kind — no card number input, no address input, no billing-details form. The endpoint takes no body (confirmed: `Checkout()` in `OrdersController.cs` has no request parameter), so there is nothing for such a form to collect. A static, clearly-labeled "Simulated payment — no real charge will be made." notice may be shown for realism, but it is copy only, not a form.

**Not in scope:** any real payment gateway UI or integration; shipping-address collection (no such field exists on `Order`/`OrderItem` — confirmed via `OnlineStore.API/Entities/Order.cs`/`OrderItem.cs` file listing and the `OrderDetailDto`/`OrderItemDto` shapes below, neither of which carries an address field); order cancellation/refund controls (no backend endpoint exists for either); building the `/orders/{id}` detail route itself (separate, not-yet-planned story) — this story only issues the redirect call to it.

---

## Context — Read These Files First

1. [`../cart-page/09-story-cart-page-integration.md`](../cart-page/09-story-cart-page-integration.md), Frontend Tasks §1 — the `useCart()` composable this story reuses verbatim: `export const useCart = () => { const apiBase = useApi(); const auth = useAuthStore(); return useFetch<CartResponse>(\`${apiBase}/cart\`, { key: "cart", headers: { Authorization: \`Bearer ${auth.token}\` }, server: false }); }`, plus the `CartItem`/`CartResponse` interfaces (`id`, `productId`, `productName`, `price`, `quantity`, `subtotal` on each item; `items`/`total` on the response). No changes to `app/composables/useCart.ts` in this story.
2. [`../cart-page/09-story-cart-page-integration.md`](../cart-page/09-story-cart-page-integration.md), Frontend Tasks §2 — the loading-skeleton / error-notice / empty-state branch structure (`v-if="pending"` / `v-else-if="error"` / `v-else-if="cart.items.length === 0"` / `v-else`) this story adapts: the empty branch changes from "show a message with a link" (cart page) to "redirect to `/cart`" (this page, per Story Goal item 2), because checkout has no reason to render an empty-review UI.
3. `app/components/base/Button.vue` — all 62 lines. `loading` prop (line 7, default `false`) drives `:disabled="disabled || loading"` (line 30) and the spinner SVG (lines 40–59) — confirms reusing `:loading` alone (no separate `:disabled` binding needed) both shows the spinner and blocks a double-click during the in-flight `POST`.
4. `app/stores/auth.ts` — all 73 lines. `state.token` (line 8) is the raw JWT for `Authorization: Bearer ${auth.token}` — same field every prior authenticated story used, **not** `accessToken`.
5. `app/pages/auth/login.vue` — all 96 lines, specifically lines 14–41 (`onSubmit`). The exact mutation shape this story's "Place order" handler mirrors: a `loading` ref set `true` before the call, `$fetch<T>(...)` in `try`, success path (`toast.success(...)` then `navigateTo(...)`) inside `try`, `catch (e: any)` calling `toast.error(e?.data?.message ?? <fallback>)`, and `finally { loading.value = false; }`.
6. `app/utils/format.ts` — all 11 lines. `formatPrice(value: number): string` — reuse unchanged for each item's `price`/`subtotal` and the `total`.
7. `app/composables/useApi.ts` — all 4 lines. `useApi()` returns the base including `/api`; build `${apiBase}/orders/checkout` for the `POST`.
8. `app/middleware/auth.ts` — all 6 lines. `useAuthStore().isAuthenticated` gates the page (redirects to `/auth/login` if falsy) — the same guard `/cart` uses; no additional in-component auth check needed.
9. `app/pages/index.vue` lines 52–76 — the `pending` skeleton and `error`-notice-with-Retry pattern (already adapted once for `/cart` in Story 09) that this page's loading/error branches follow for the order-summary fetch.
10. `OnlineStore.API/Controllers/OrdersController.cs` — lines 23–99 (`Checkout`). Route: `[HttpPost("checkout")]` under the class-level `[Route("api/orders")]` and `[Authorize]` (lines 11–13), so the full path is `POST /api/orders/checkout`. Reads the caller's cart (`_db.CartItems.Where(ci => ci.UserId == userId)`, lines 34–37); returns `401 Unauthorized()` (bare, no body) if the JWT claim is missing (lines 29–32, unreachable in practice since the page's `auth` middleware already redirects unauthenticated visitors); returns `400 { message = "Cart is empty" }` if the cart has zero lines (line 41); for each line, returns `400 { message = $"Product {ProductId} is unavailable" }` if the product is null or inactive (line 50) or `400 { message = $"Insufficient stock for {Product.Name}" }` if the requested quantity exceeds current stock (line 55) — **note the exact wording is "Product {id} is unavailable" (with "is"), not "Product {id} unavailable"**; on success, commits stock decrements and the new `Order` inside one transaction (lines 59–90), simulates the payment delay (`Task.Delay(1500)`, line 84, always succeeds), clears the user's cart rows (line 87), and returns `201` via `CreatedAtAction(nameof(GetById), new { id = order.Id }, dto)` (line 93) with body `dto` (the `OrderDetailDto` below); any unhandled exception returns `500 { message = "Something went wrong" }` (lines 95–98).
11. `OnlineStore.API/Dtos/OrderDetailDto.cs` — all 8 lines. `record OrderDetailDto(int Id, string Status, decimal Total, DateTime CreatedAt, List<OrderItemDto> Items)` — serialized camelCase (same convention confirmed for `CartItemDto`/`CartResponse` in Story 09's Context items 10–11), so the frontend response shape is `{ id, status, total, createdAt, items }`. `status` is the literal string `"paid"` on a successful checkout (set at `OrdersController.cs` line 85), not an enum.
12. `OnlineStore.API/Dtos/OrderItemDto.cs` — all 9 lines. `record OrderItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal)` — frontend shape `{ productId, productName, unitPrice, quantity, subtotal }`. Note the field is `unitPrice`, not `price` (the `CartItemDto` field name used on `/cart`) — the two DTOs are not identical, so this page's post-success success toast/redirect must not assume `price` exists on the returned order's items (not needed here since the page only reads `dto.id`/`dto.status` after success, but relevant if a future story renders these items).
13. `OnlineStore.API/Controllers/CartController.cs` lines 77–108 (`Get`) — reused unchanged via `useCart()`; confirms `GET /api/cart` returns `200 { items, total }` on success and a bodyless `500`/`401` on failure, matching Story 09's documented error handling this page's loading/error branches reuse.

---

## Frontend Tasks

### 1 — No changes to `app/composables/useCart.ts`

`useCart()` (Story 09, Frontend Tasks §1) is reused exactly as-is for this page's order-summary fetch. No new composable is created for the checkout review — the same `CartResponse`/`CartItem` shape and the same `GET /api/cart` endpoint back both `/cart` and `/checkout`.

### 2 — Create the checkout page

**Create file: `app/pages/checkout.vue`**

```vue
<script setup lang="ts">
import { toast } from "vue-sonner";

definePageMeta({ layout: "default", middleware: "auth" });

const apiBase = useApi();
const auth = useAuthStore();

const { data: cart, pending, error, refresh } = useCart();

const placingOrder = ref(false);

watchEffect(() => {
  if (cart.value && cart.value.items.length === 0) {
    navigateTo("/cart");
  }
});

async function placeOrder() {
  placingOrder.value = true;
  try {
    const order = await $fetch<{
      id: number;
      status: string;
      total: number;
      createdAt: string;
      items: {
        productId: number;
        productName: string;
        unitPrice: number;
        quantity: number;
        subtotal: number;
      }[];
    }>(`${apiBase}/orders/checkout`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.token}` },
    });
    toast.success("Order placed successfully");
    await navigateTo(`/orders/${order.id}`);
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not place your order");
  } finally {
    placingOrder.value = false;
  }
}
</script>
```

Template, in this order:

1. **Heading** — `<h1>Review Your Order</h1>` (`text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl`, matching the heading scale used on `/cart` per Story 09, Frontend Tasks §2 step 1).
2. **Loading** — `v-if="pending"`: a skeleton list, same `animate-pulse`/`bg-gray-100` stacked-row convention Story 09 introduced for `/cart` (Frontend Tasks §2 step 2), reused here for the order-summary rows.
3. **Error** — `v-else-if="error"`: the same bordered notice + `BaseButton` "Retry" (`@click="refresh()"`) pattern as `/cart` (Story 09, Frontend Tasks §2 step 3) and `app/pages/index.vue` lines 68–76.
4. **Redirecting (empty cart)** — `v-else-if="cart && cart.items.length === 0"`: render nothing (or a brief "Your cart is empty — redirecting…" notice) while the `watchEffect` above fires `navigateTo("/cart")`. This branch should never be visibly reached for more than an instant; it exists so the loaded-order template below is never rendered against a zero-item cart.
5. **Loaded (review + confirm)** — `v-else-if="cart"`:
   - A **read-only** vertical list of line items (`space-y-4`), one row per `cart.items`: `productName` (`font-medium text-gray-900`), `formatPrice(item.price)` × `item.quantity` (`text-sm text-gray-500`), `formatPrice(item.subtotal)` (`font-semibold text-primary`) — same fields as `/cart`'s loaded rows (Story 09, Frontend Tasks §2 step 5) but with **no** "Remove" button and no other interactive control on the row.
   - Below the list: `<p class="text-lg font-semibold">Total: {{ formatPrice(cart.total) }}</p>`.
   - A short static notice: `<p class="text-sm text-gray-500">Simulated payment — no real charge will be made.</p>` — copy only, confirms to the user this is a demo flow per the intake's explicit "no payment form fields" constraint; not an input control.
   - `<BaseButton block :loading="placingOrder" @click="placeOrder">{{ placingOrder ? "Placing order…" : "Place order" }}</BaseButton>` — no `:disabled` binding needed beyond what `loading` already provides (`Button.vue` line 30 disables while `loading` is `true`).

### 3 — No changes to `app/layouts/default.vue`, `app/middleware/auth.ts`, `app/pages/cart.vue`, or the backend

**No changes required** to `app/layouts/default.vue` — the page inherits the header/footer via the default layout, same as `/cart`.
**No changes required** to `app/middleware/auth.ts` — reused exactly as-is.
**No changes required** to `app/pages/cart.vue` — its "Proceed to checkout" link (Story 09) already points at `/checkout`; this story is what makes that link resolve to a real page instead of 404ing.
**No backend changes** — `OrdersController.Checkout` is used exactly as it exists today (Context items 10–12 above).

---

## Edge Cases & Failure Modes

- **`/orders/{id}` does not exist yet:** the success path's `navigateTo(`/orders/${order.id}`)` (Frontend Tasks §2) targets a route with no corresponding `app/pages/orders/[id].vue` file as of this story (confirmed: no `app/pages/orders*` files exist). This 404s after a successful checkout. This is explicitly accepted per the intake ("redirect to `/orders` if a dedicated order-detail route doesn't exist yet... `/orders/{id}` is preferred if that story builds it") — the order **is** created and the cart **is** cleared server-side regardless of what the frontend redirect resolves to; the "Customer Orders History" story builds that route next and is expected to land close together with this one per the intake's Extra Notes. Not a defect to fix here.
- **Empty cart reached directly via URL (`/checkout` typed or bookmarked with no items):** the `watchEffect` (Frontend Tasks §2) fires `navigateTo("/cart")` as soon as `cart.value` resolves with `items.length === 0`, before the "Place order" button is ever interactive — this also covers the case where a user empties their cart in another tab and then loads `/checkout` fresh.
- **Cart becomes empty between page load and "Place order" click** (e.g. a second tab removes the last item after `/checkout` has already rendered the loaded review with `cart.value.items.length > 0`): the frontend does **not** refetch the cart before submitting — `POST /api/orders/checkout` re-reads the cart server-side at request time (`OrdersController.cs` lines 34–37) and independently returns `400 { message: "Cart is empty" }` (line 41) if it is now empty, which the `catch` block surfaces via `e?.data?.message` verbatim. No stale-client-state bug: the backend is the source of truth for the actual checkout attempt.
- **Insufficient stock discovered at checkout time** (e.g. another user bought the last units after this page loaded): `400 { message: $"Insufficient stock for {Product.Name}" }` (`OrdersController.cs` line 55) — surfaced verbatim via `e?.data?.message`. The cart and stock are unchanged (validation runs before any write, lines 46–57), so the user can go back to `/cart` to adjust or retry "Place order" once stock is available; no client-side quantity-adjustment UI is built for this in this story (out of scope, matching Story 09's no-quantity-edit stance).
- **Product deactivated between cart-add and checkout:** `400 { message: $"Product {ProductId} is unavailable" }` (`OrdersController.cs` line 50, exact wording confirmed **"is unavailable"**, not "unavailable" as paraphrased in the intake) — surfaced verbatim via `e?.data?.message`.
- **Double-click on "Place order" during the 1.5s simulated delay:** `placingOrder.value = true` is set synchronously before the `$fetch` call and only cleared in `finally`; `BaseButton`'s `:loading` prop disables the button (`Button.vue` line 30) for the whole in-flight window, so a second click cannot fire a second `POST /api/orders/checkout` from this page. (The backend itself has no idempotency key, so this is a client-side-only safeguard — acceptable for a simulated, non-financial checkout per the intake.)
- **401 on `POST /api/orders/checkout` from a stale/expired token** (the `auth` middleware only checks `!!state.token`, not validity — same gap Story 09 documented for `/cart`'s `GET`): the bare `401 Unauthorized()` (`OrdersController.cs` line 31, no JSON body) means `e?.data?.message` is `undefined`, so the generic fallback string ("Could not place your order") is shown instead of a session-specific message. No dedicated "session expired" state is built in this story, matching Story 09's scope decision for the equivalent case on `/cart`.
- **500 on `POST /api/orders/checkout`** (any unhandled exception, `OrdersController.cs` lines 95–98): body is `{ message: "Something went wrong" }`, surfaced verbatim via `e?.data?.message` — same generic message the backend uses everywhere else, no special-casing needed.
- **`GET /api/cart` fails (network/500) on page load:** identical to Story 09's `/cart` error branch — the bordered notice with Retry renders (Frontend Tasks §2 step 3); "Place order" is never rendered in this state since it lives in the `v-else-if="cart"` branch only.
- **SSR vs. self-signed dev certificate:** identical to Stories 05/07/08/09 — `useCart()`'s `server: false` (unchanged, reused from Story 09) avoids the Nitro server making the request during SSR, where Node would reject the API's self-signed HTTPS certificate. The `POST /api/orders/checkout` mutation is fired via `$fetch` from a client-side event handler (`placeOrder`, triggered by a button click), so it never runs during SSR either.

---

## Test Plan

**No test runner is configured** in this project — confirmed by Stories 05, 07, 08, and 09 (`package.json` has `build`/`dev`/`generate`/`preview`/`postinstall` only, no Vitest dependency), still true as of this story. Verification is manual (see Verification Steps). If Vitest + `@nuxt/test-utils` is introduced later, these are the tests to add, matching the structure of `.squad/plans/cart-page/09-story-cart-page-integration.md`'s Test Plan section:

1. **Component — `app/pages/checkout.vue`** with `useCart` mocked: `pending` renders the skeleton; a non-empty, non-404 `error` renders the generic error notice with a working Retry (`refresh()` called on click); `data: { items: [], total: 0 }` triggers `navigateTo("/cart")` and does not render the "Place order" button; `data: { items: [...], total }` renders one read-only row per item (no "Remove" control present) plus the formatted total and an enabled "Place order" button.
2. **Component — place-order success:** clicking "Place order" calls `$fetch` with `method: "POST"` against `${apiBase}/orders/checkout` and the `Authorization` header, sets `:loading` on the button for the duration of the call (verify the button is `disabled` while the mocked `$fetch` promise is unresolved), then on resolution calls `toast.success` and `navigateTo` with the path `/orders/${id}` built from the mocked response's `id` field.
3. **Component — place-order failure:** a rejected `$fetch` with `e.data.message` set to each of `"Cart is empty"`, `"Insufficient stock for Widget"`, and `"Product 4 is unavailable"` shows that exact string in the error toast and does **not** call `navigateTo`; a rejection with no `data.message` shows the generic fallback string; `placingOrder` returns to `false` (button re-enabled) after any rejection.
4. **Component — empty-cart redirect:** mounting the page with `useCart` returning `{ items: [], total: 0 }` results in a `navigateTo("/cart")` call and the loaded-review template (item rows, "Place order" button) is never rendered.

---

## Verification Steps

1. **Frontend builds:** in `online-store-frontend/`, run `pnpm build`. Must succeed — a wrong auto-import name for `<BaseButton>`, a bad `useCart` usage, or a missing `definePageMeta` fails here.
2. **Auth guard:** while logged out, navigate to `/checkout` directly — redirected to `/auth/login` (same guard as `/cart`, unchanged behavior).
3. **Empty-cart redirect:** while logged in with an empty cart, navigate to `/checkout` directly — immediately redirected to `/cart` (no "Place order" button ever becomes clickable).
4. **Review data path:** while logged in with items in the cart (add some via `/products/{id}` from Story 08, confirm they show on `/cart` from Story 09 first), open `/checkout` — the network tab shows a request to `https://localhost:7225/api/cart` with an `Authorization: Bearer <token>` header; the page renders one read-only row per item (`productName`, formatted unit price, quantity, formatted subtotal — no "Remove" button) and the formatted `total`.
5. **Loading state:** throttle the network to Slow 3G and reload `/checkout` — the skeleton renders before the cart data lands.
6. **Error state:** stop the API and reload `/checkout` — the generic error notice renders with a Retry button; clicking Retry re-issues the `GET /api/cart` request once the API is back.
7. **Place order — success:** with a non-empty cart and sufficient stock, click "Place order" — the button immediately shows the spinner and becomes non-interactive; the network tab shows `POST https://localhost:7225/api/orders/checkout` with the `Authorization` header and no request body, taking roughly 1.5s, returning `201` with `{ id, status: "paid", total, createdAt, items }`; a success toast appears; the browser navigates to `/orders/{id}` (expected to 404 or redirect to a not-yet-built page — acceptable per Edge Cases, since the orders-history story has not landed).
8. **Place order — double-click guard:** click "Place order" and immediately click it again before the 1.5s delay elapses — only one `POST` request appears in the network tab; the second click has no effect while the button is disabled/loading.
9. **Place order — cart-empty failure:** manually clear the cart via a second request (or race a second tab's "Remove" action) between opening `/checkout` and clicking "Place order," then click "Place order" — the network tab shows a `400` response with `{ message: "Cart is empty" }`; an error toast shows that exact string; the button returns to its enabled, non-loading state.
10. **Place order — insufficient stock failure:** reduce a product's stock below the cart's requested quantity (e.g. via the admin product edit, if available, or by placing a competing order first) before clicking "Place order" — a `400` with `{ message: "Insufficient stock for {ProductName}" }` shows verbatim in an error toast.
11. **No payment form fields:** confirm the rendered page has no `<input>` of type `text`/`tel`/`number` (or similar) for a card number, expiry, CVV, or shipping address anywhere on `/checkout`.
12. **Backend unchanged:** `dotnet build` in `OnlineStore.API/` — expected unchanged, this story touches no C#.
13. **Responsive:** at 375 px the order-summary rows and total stack in a single column with no horizontal overflow; at 1280 px the review list and "Place order" button read comfortably within the page's max-width.

---

## Done Criteria

- [ ] `app/pages/checkout.vue` exists with `definePageMeta({ layout: "default", middleware: "auth" })`, fetches `GET /api/cart` via the reused `useCart()` composable, and renders a read-only order summary (`productName`, formatted `price`, `quantity`, formatted `subtotal` per item, plus the formatted `total`) — no "Remove" action, no quantity controls.
- [ ] An empty cart on `/checkout` redirects to `/cart` rather than rendering a "Place order" button.
- [ ] "Place order" calls `POST /api/orders/checkout` with no body and the `Authorization: Bearer <token>` header via `$fetch`; the button shows `BaseButton`'s `:loading` state (spinner, disabled) for the full duration of the request, preventing a duplicate submit.
- [ ] On success (`201 OrderDetailDto`), a success toast shows and the browser navigates to `/orders/{id}` using the response's `id` field (documented as 404ing until the orders-history story lands).
- [ ] On failure (`400` with `"Cart is empty"` / `"Insufficient stock for {name}"` / `"Product {id} is unavailable"`), the exact backend message shows in an error toast, the cart is left untouched, and the user can retry or navigate back to `/cart`.
- [ ] No payment-form input fields (card number, expiry, CVV, shipping address) exist anywhere on the page; the simulated-payment notice, if shown, is static copy only.
- [ ] `pnpm build` succeeds; no arbitrary `bg-[#…]`/`text-[#…]` or raw `px` spacing introduced.

**STOP HERE. Report to the user and wait for confirmation before proceeding to the next story in the customer-journey chain (orders-history).**
