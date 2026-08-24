# Story 11 — Customer Orders History

Build `/orders` (the logged-in customer's own past orders, newest first) and `/orders/[id]` (a single order's detail with line items). This is the fifth and final story in the customer-journey chain (products-catalog → product-details → cart-page → checkout-payment → **orders-history**), and it closes the loop opened by Story 10's checkout success redirect: `app/pages/checkout.vue`'s `placeOrder()` already calls `navigateTo(`/orders/${order.id}`)` today, targeting a route that does not exist until this story lands.

---

## Prerequisites

- **Story 10** ([`../checkout-payment/10-story-checkout-and-payment-page.md`](../checkout-payment/10-story-checkout-and-payment-page.md)) completed: `app/pages/checkout.vue`'s `placeOrder()` success handler already calls `await navigateTo(`/orders/${order.id}`)` (Story 10, Frontend Tasks §2) and documents that this 404s until this story ships (Story 10, Edge Cases: "`/orders/{id}` does not exist yet"). This story's `app/pages/orders/[id].vue` is exactly the file that makes that redirect resolve; **no change to `checkout.vue` is needed** — the route naming (`/orders/{id}`, numeric id) already matches what this story builds.
- Backend `GET /api/orders` and `GET /api/orders/{id}` are already implemented and unchanged for this story — `OnlineStore.API/Controllers/OrdersController.cs` lines 101–124 (`Get`) and lines 126–158 (`GetById`). **No backend change is required or permitted in this story.**
- The API must be running for either page to populate. `nuxt.config.ts` sets `apiBase` to `https://localhost:7225/api`; the API's CORS policy allows only `http://localhost:3000`, so run the frontend on the default port (same constraint documented in Stories 05, 07, 08, 09, and 10).

---

## Story Goal

1. `app/pages/orders/index.vue` (new page), `definePageMeta({ layout: "default", middleware: "auth" })`, fetches `GET /api/orders` and lists the caller's own orders newest-first: each row shows `id`, `createdAt` (formatted), `status`, and `total` (via `formatPrice`). Each row links to `/orders/{id}`.
2. Empty state (fetched successfully, zero orders): a friendly "no orders yet" message plus a link to `/products` — not the error state, matching the `/cart` empty-state precedent (Story 09).
3. `app/pages/orders/[id].vue` (new page), same `layout`/`middleware`, fetches `GET /api/orders/{id}` and renders the full order: `id`, `status`, `createdAt` (formatted), each line item's `productName`/`quantity`/`unitPrice` (via `formatPrice`)/`subtotal` (via `formatPrice`), and the order `total` (via `formatPrice`).
4. A dedicated not-found state on `/orders/[id]` covering **both** "order does not exist" (`404`) and "order belongs to another user" (`403`) — the acceptance criteria requires the same "not found, here's a link back to `/orders`" treatment for both, and the `403` response carries no JSON body to distinguish it from a 404 anyway (see Context item 10 and Edge Cases).
5. Loading states for both pages (skeleton list for `/orders`, skeleton detail for `/orders/[id]`); a distinct generic error state (network/500) with a Retry action on both, following the `app/pages/index.vue` / `/cart` / `/checkout` precedent.
6. Add an "Orders" nav link to `app/layouts/default.vue`'s header, alongside the existing "Products" and "Cart" links, visible only when `auth.isAuthenticated` (mirrors the existing conditional "Log in" vs. "Log out" control in the same header) so the page is reachable and not orphaned.

**Not in scope:** reordering / "buy again" (no backend support); cancellation, refunds, or order-status changes (no backend endpoint for any of these); an admin-side view of all orders (this is the customer's own-orders view only — `GetById`'s `order.UserId != userId` check, `OrdersController.cs` line 147, and `Get`'s `Where(o => o.UserId == userId)`, line 113, both scope every response to the caller); editing `checkout.vue`'s redirect (already correct, see Prerequisites).

---

## Context — Read These Files First

1. `OnlineStore.API/Controllers/OrdersController.cs` lines 101–124 (`Get`) — `[HttpGet]` under the class-level `[Route("api/orders")]` and `[Authorize]` (lines 11–13), so the full path is `GET /api/orders`. Returns bare `401 Unauthorized()` if the JWT claim is missing (lines 106–110, unreachable in practice since this story's `auth` middleware already redirects unauthenticated visitors before the page renders). On success, queries `_db.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt)` (lines 112–114) — already newest-first server-side, no client-side sort needed — and projects each row into `new OrderSummaryDto(o.Id, o.Status, o.Total, o.CreatedAt)` (line 115), returning `200` with the array (line 118). On any unhandled exception, `500 { message: "Something went wrong" }` (lines 120–123).
2. `OnlineStore.API/Controllers/OrdersController.cs` lines 126–158 (`GetById`) — `[HttpGet("{id:int}")]` (line 126, the `:int` constraint means a non-numeric id in the URL never reaches this action and 404s at the routing layer with no JSON body, same pattern as `ProductsController.GetById` in Story 08). Returns bare `401 Unauthorized()` if the JWT claim is missing (lines 131–135, unreachable per the `auth` middleware guard). Looks the order up by id with `.Include(o => o.Items)` (lines 137–139); returns `404 { message: "Order not found" }` if it does not exist (lines 141–144); if it exists but `order.UserId != userId` (line 147, "A user must never view another user's order"), returns a **bare `Forbid()` with no JSON body** (line 149) — same shape as `CartController.Delete`'s 403 documented in Story 09. On success, returns `200` with `ToDetail(order)` (line 152). On any unhandled exception, `500 { message: "Something went wrong" }` (lines 154–157).
3. `OnlineStore.API/Controllers/OrdersController.cs` lines 166–179 (`ToDetail`) — the exact mapping this story's rendering must match: `new OrderDetailDto(order.Id, order.Status, order.Total, order.CreatedAt, order.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity)).ToList())` — confirms `subtotal` on each item is always `unitPrice * quantity` computed server-side, matching what is already rendered.
4. `OnlineStore.API/Dtos/OrderSummaryDto.cs` — all 7 lines. `record OrderSummaryDto(int Id, string Status, decimal Total, DateTime CreatedAt)`, serialized camelCase (same convention confirmed for every other DTO in Stories 08–10), so the frontend list-item shape is `{ id, status, total, createdAt }`. There is **no `items` field on this DTO** — the list page must not attempt to render line items per row.
5. `OnlineStore.API/Dtos/OrderDetailDto.cs` — all 8 lines. `record OrderDetailDto(int Id, string Status, decimal Total, DateTime CreatedAt, List<OrderItemDto> Items)` — frontend shape `{ id, status, total, createdAt, items }`. Already confirmed identical in Story 10, Context item 11 (used there for the checkout response).
6. `OnlineStore.API/Dtos/OrderItemDto.cs` — all 9 lines. `record OrderItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal)` — frontend shape `{ productId, productName, unitPrice, quantity, subtotal }`. Note the field is `unitPrice`, **not** `price` (the field name `CartItemDto` uses) — already flagged in Story 10, Context item 12; this story is the first to actually render these item fields, so get the name right: `item.unitPrice`, not `item.price`.
7. `app/composables/useProducts.ts` — all 29 lines. The `useProducts`/`useProduct` pattern (`useApi()` for the base URL, `useFetch` with `server: false`, a distinct `key` per call site, no `default:` when the caller must distinguish "loading" from "loaded, empty/zero") this story's new `useOrders`/`useOrder` composable follows exactly, as a new sibling file dedicated to orders data (matching the one-composable-per-domain convention `useCart.ts` already established in Story 09).
8. `app/composables/useCart.ts` (created in Story 09) — the most recent precedent for a domain-specific composable file: exported interfaces (`CartItem`/`CartResponse`) plus a `useCart()` function returning `useFetch` with `key`, an `Authorization: Bearer ${auth.token}` header, and `server: false`. This story's `useOrders()` and `useOrder(id)` both need the same `Authorization` header (unlike the public `useProducts`/`useProduct`), since `GET /api/orders` and `GET /api/orders/{id}` are both `[Authorize]`-gated.
9. `app/pages/products/[id].vue` pattern, documented in `.squad/plans/product-details/08-story-product-details-page.md`, Frontend Tasks §2 (lines 92–150 of that file) — the exact dynamic-route shape this story's `app/pages/orders/[id].vue` follows: `route.params.id` read via `useRoute()`, a client-side `isValidId = Number.isInteger(rawId) && rawId > 0` guard that renders the not-found state without ever issuing a request for a non-numeric/non-positive id, `isNotFound = computed(() => (error.value as any)?.statusCode === 404)` driving a dedicated not-found branch distinct from the generic error branch, and a `pending`/`isNotFound`/`error`/loaded `v-else-if` chain in that order.
10. `.squad/plans/cart-page/09-story-cart-page-integration.md`, Edge Cases (the "403 Forbid on `DELETE /api/cart/{id}`" bullet) — documents that a bare `Forbid()` response carries no JSON body, so `error.value.data` is empty and only `error.value.statusCode` can be inspected. This story's `isNotFound` check on `/orders/[id]` must therefore be `statusCode === 404 || statusCode === 403` (both cases render the identical "not found" UI per this story's acceptance criteria — see Story Goal item 4), not a message-based check.
11. `app/pages/index.vue` lines 52–76 — the `pending` skeleton and `error`-notice-with-Retry pattern already adapted three times (Story 09 for `/cart`, Story 10 for `/checkout`) that both new pages' loading/error branches follow again here.
12. `app/pages/cart.vue` empty-state branch (Story 09, Frontend Tasks §2 step 4: "Your cart is empty." + a `<NuxtLink to="/products">` styled as a `BaseButton`) — the exact pattern `/orders/index.vue`'s "no orders yet" empty state (Story Goal item 2) reuses, swapping the copy and keeping the same link target (`/products`, since there is nothing order-specific to link to from an empty list).
13. `app/utils/format.ts` — all 11 lines. `formatPrice(value: number): string` — reuse unchanged for `total`, `unitPrice`, and `subtotal`. There is no existing date-formatting helper in this file or elsewhere under `app/utils/` or `app/composables/` (confirmed: `formatPrice` is the file's only export) — this story adds date formatting inline per page (see Frontend Tasks §3) rather than inventing a new shared utility for a single date field used in two places, to keep the change minimal; a `formatDate` helper can be extracted later if a third date-rendering need appears.
14. `app/composables/useApi.ts` — all 4 lines. `useApi()` returns the base including `/api`; build `${apiBase}/orders` and `${apiBase}/orders/${id}`.
15. `app/stores/auth.ts` — all 73 lines. `state.token` (line 8) is the raw JWT for `Authorization: Bearer ${auth.token}`; `isAuthenticated` getter (line 27, `!!state.token`) is what gates the new "Orders" nav link's visibility.
16. `app/middleware/auth.ts` — all 6 lines. `useAuthStore().isAuthenticated` gates both new pages, same guard `/cart` and `/checkout` use — no additional in-component auth check needed.
17. `app/layouts/default.vue` — all 25 lines. Lines 9–13: the `<nav>` block with `<NuxtLink to="/products">Products</NuxtLink>`, `<NuxtLink to="/cart">Cart</NuxtLink>`, then the `v-if="!auth.isAuthenticated"` "Log in" link vs. `v-else` "Log out" button. This story adds an "Orders" link into this same `<nav>`, gated by `v-if="auth.isAuthenticated"` (an order history only makes sense for a logged-in customer, unlike "Products", which is public).

---

## Frontend Tasks

### 1 — Add an orders composable

**Create file: `app/composables/useOrders.ts`**

```ts
export interface OrderSummary {
  id: number;
  status: string;
  total: number;
  createdAt: string;
}

export interface OrderItem {
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
  subtotal: number;
}

export interface OrderDetail {
  id: number;
  status: string;
  total: number;
  createdAt: string;
  items: OrderItem[];
}

export const useOrders = () => {
  const apiBase = useApi();
  const auth = useAuthStore();
  return useFetch<OrderSummary[]>(`${apiBase}/orders`, {
    key: "orders",
    headers: { Authorization: `Bearer ${auth.token}` },
    // Same reason as useCart/useProducts: the .NET dev server's self-signed
    // HTTPS cert is rejected by Nitro's SSR fetch.
    server: false,
  });
};

export const useOrder = (id: number) => {
  const apiBase = useApi();
  const auth = useAuthStore();
  return useFetch<OrderDetail>(`${apiBase}/orders/${id}`, {
    key: `order-${id}`,
    headers: { Authorization: `Bearer ${auth.token}` },
    server: false,
  });
};
```

No `default:` option on either call — same reasoning as `useProduct` (Story 08) and `useCart` (Story 09): the pages must be able to tell "still loading" (`data.value` is `undefined`) apart from "loaded, zero orders" (`useOrders`, an empty array) or "loaded, not found" (`useOrder`, never resolves to a value on a 404/403 — the `error` ref is set instead).

### 2 — Create the orders list page

**Create file: `app/pages/orders/index.vue`**

```vue
<script setup lang="ts">
definePageMeta({ layout: "default", middleware: "auth" });

const { data: orders, pending, error, refresh } = useOrders();

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}
</script>
```

Template, in this order:

1. **Heading** — `<h1>Your Orders</h1>` (`text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl`, matching the heading scale used on `/cart` and `/checkout`).
2. **Loading** — `v-if="pending"`: a skeleton list, the same `animate-pulse`/`bg-gray-100` stacked-row convention as `/cart` (Story 09) and `/checkout` (Story 10), reused here for order rows.
3. **Error** — `v-else-if="error"`: the same bordered notice + `BaseButton` "Retry" (`@click="refresh()"`) pattern as `/cart`, `/checkout`, and `app/pages/index.vue` lines 68–76.
4. **Empty** — `v-else-if="orders && orders.length === 0"`: a friendly message, e.g. "You haven't placed any orders yet." plus a `<NuxtLink to="/products">` styled as a `BaseButton` ("Browse products") — **not** the error state, matching `/cart`'s empty-state precedent (Story 09, Frontend Tasks §2 step 4).
5. **Loaded** — `v-else-if="orders"`: a vertical list (`space-y-4`), one row per order wrapped in `<NuxtLink :to="`/orders/${order.id}`">`: `Order #{{ order.id }}` (`font-medium text-gray-900`), `formatDate(order.createdAt)` (`text-sm text-gray-500`), `order.status` (a small pill/badge, e.g. `text-sm font-medium text-primary`), and `formatPrice(order.total)` (`font-semibold text-primary`) right-aligned.

### 3 — Create the order detail page

**Create file: `app/pages/orders/[id].vue`**

```vue
<script setup lang="ts">
definePageMeta({ layout: "default", middleware: "auth" });

const route = useRoute();

const rawId = Number(route.params.id);
// A non-numeric or non-positive id can never match the backend's
// `[HttpGet("{id:int}")]` route — treat it as "not found" without a request.
const isValidId = Number.isInteger(rawId) && rawId > 0;

const { data: order, pending, error, refresh } = isValidId
  ? useOrder(rawId)
  : { data: ref(null), pending: ref(false), error: ref({ statusCode: 404 }), refresh: () => {} };

// Both "order not found" (404) and "another user's order" (403, bare Forbid
// with no JSON body) render the identical not-found UI — see Context item 10.
const isNotFound = computed(() => {
  const code = (error.value as any)?.statusCode;
  return code === 404 || code === 403;
});

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}
</script>
```

Template, in this order:

1. **Back link** — `<NuxtLink to="/orders" class="text-sm font-medium text-primary hover:underline">← Back to orders</NuxtLink>`, always rendered above every state below (loading, not-found, error, loaded), matching the breadcrumb precedent on `/products/[id]` (Story 08).
2. **Loading** — `v-if="pending"`: a skeleton detail block (heading-line + a few row placeholders), same `animate-pulse`/`bg-gray-100`/`bg-gray-200` convention as the other pages.
3. **Not found** — `v-else-if="isNotFound"`: a bordered notice, e.g. "We couldn't find this order." plus a `<NuxtLink to="/orders">` styled as a `BaseButton` ("Back to orders") — **do not** render `error.value.message` or any raw error object, and do not attempt to distinguish the 404 and 403 cases in the copy (per Story Goal item 4 and Context item 10, the 403 has no body to read a distinguishing message from anyway).
4. **Error (non-404/403)** — `v-else-if="error"`: bordered notice matching `app/pages/index.vue` lines 68–76 ("We couldn't load this order right now." + `BaseButton` "Retry" calling `refresh()`).
5. **Loaded** — `v-else-if="order"`:
   - `<h1>Order #{{ order.id }}</h1>` (same heading scale as the list page), with `order.status` shown as a pill/badge next to it and `formatDate(order.createdAt)` below (`text-sm text-gray-500`).
   - A **read-only** vertical list of line items (`space-y-4`), one row per `order.items`: `productName` (`font-medium text-gray-900`), `formatPrice(item.unitPrice)` × `item.quantity` (`text-sm text-gray-500`) — note the field is `item.unitPrice`, not `item.price` (Context item 6) — and `formatPrice(item.subtotal)` (`font-semibold text-primary`), same row shape as `/cart`'s and `/checkout`'s loaded rows but with no interactive control.
   - Below the list: `<p class="text-lg font-semibold">Total: {{ formatPrice(order.total) }}</p>`.

### 4 — Add the "Orders" nav link

**File: `app/layouts/default.vue`**

In the `<nav>` block (lines 9–13), add an "Orders" link between "Cart" (line 11) and the login/logout control (lines 12–13), gated on `auth.isAuthenticated` (an order history only makes sense once logged in, unlike "Products"/"Cart" which stay visible either way):

```vue
<NuxtLink v-if="auth.isAuthenticated" to="/orders">Orders</NuxtLink>
```

No other changes to this file — the `<script setup>` block (line 2, `const auth = useAuthStore();`) already exposes `auth` to the template; no new import or state is needed.

### 5 — No changes to `app/pages/checkout.vue`, `app/middleware/auth.ts`, or the backend

**No changes required** to `app/pages/checkout.vue` — its `navigateTo(`/orders/${order.id}`)` call (Story 10) already targets the exact route this story builds; the route naming matches with no placeholder to update.
**No changes required** to `app/middleware/auth.ts` — reused exactly as-is.
**No backend changes** — `OrdersController.Get` and `OrdersController.GetById` are used exactly as they exist today (Context items 1–3 above).

---

## Edge Cases & Failure Modes

- **Non-numeric or non-positive `id` in the URL** (e.g. `/orders/abc`, `/orders/-1`, `/orders/0`): `app/pages/orders/[id].vue`'s `isValidId` check (Frontend Tasks §3) catches this client-side and renders the not-found state without ever issuing a request — matches the backend anyway, since `[HttpGet("{id:int}")]` (`OrdersController.cs` line 126) never matches those URLs and 404s at the routing layer with no JSON body. Identical pattern to `/products/[id]` in Story 08.
- **Viewing another user's order (`/orders/{id}` for a valid, existing order that belongs to someone else):** `GetById` returns a bare `403 Forbid()` with **no JSON body** (`OrdersController.cs` line 149) — `error.value.data` is empty, so `isNotFound` (Frontend Tasks §3) checks `statusCode` only, never a message field, and renders the identical not-found UI as a genuine 404. This is the acceptance criteria's explicit requirement ("if the order isn't the caller's own (403) or doesn't exist (404), show a clear 'not found' state") — the frontend must not and cannot distinguish the two cases, and does not try to.
- **Order does not exist at all:** `404 { message: "Order not found" }` (`OrdersController.cs` lines 141–144) — also covered by the same `isNotFound` branch; the `message` field is present here but intentionally unused, to keep both 404 and 403 rendering through one code path.
- **Redirect from `/checkout` landing here immediately after a successful order:** Story 10's `navigateTo(`/orders/${order.id}`)` fires right after the `201` response; this page's own `GET /api/orders/{id}` fetch is a fresh request (not fed the checkout response directly), so it always reflects the committed database state — no race, since Story 10's checkout transaction already committed (`OrdersController.cs` lines 89–90) before the `201`/redirect happens.
- **Empty orders list for a brand-new customer:** `orders.value` resolves to `[]` (not `undefined`) once the fetch completes — the empty-state branch (Frontend Tasks §2 step 4) renders, not the loading skeleton or the error notice, matching the `/cart` empty-state precedent (Story 09).
- **401 on `GET /api/orders` or `GET /api/orders/{id}` from a stale/expired token** (the `auth` middleware only checks `!!state.token`, not validity — same gap documented for `/cart` in Story 09 and `/checkout` in Story 10): the bare `401 Unauthorized()` (`OrdersController.cs` lines 108, 133, no JSON body) is captured by `useFetch`'s `error` ref and renders the generic error state (Frontend Tasks §2 step 3 / §3 step 4), since `statusCode` is `401`, not `404`/`403`, so it does not hit the not-found branch on the detail page. No dedicated "session expired" state is built in this story, matching the scope decision Stories 09 and 10 already made for the equivalent case.
- **500 on either endpoint** (any unhandled exception, `OrdersController.cs` lines 120–123 and 154–157): body is `{ message: "Something went wrong" }`; the generic error state renders (message not shown to the user, same as every other page's error branch in this codebase — only Retry-and-generic-copy, no raw backend text surfaced).
- **`OrderSummaryDto` has no `items` field:** the list page (Frontend Tasks §2) renders only `id`/`status`/`total`/`createdAt` per row — attempting to show line-item counts or previews on the list page is not possible without an extra per-row fetch, which this story does not add (out of scope; the detail page is the only place items render).
- **`OrderItemDto.UnitPrice`/`ProductName` are immutable snapshots, not live product data:** per the intake's explicit note, these fields are copied at checkout time (`OrdersController.cs` lines 61–72, `Checkout`) and never updated afterward — the detail page renders `item.productName`/`item.unitPrice` directly with no product-lookup call, so a since-edited or since-deactivated product still shows correctly as it was at the time of purchase; no additional handling needed.
- **SSR vs. self-signed dev certificate:** identical to Stories 05/07/08/09/10 — `server: false` on both `useOrders` and `useOrder` (Frontend Tasks §1) avoids the Nitro server making the request during SSR, where Node would reject the API's self-signed HTTPS certificate.

---

## Test Plan

**No test runner is configured** in this project — confirmed by Stories 05, 07, 08, 09, and 10 (`package.json` has `build`/`dev`/`generate`/`preview`/`postinstall` only, no Vitest dependency), still true as of this story. Verification is manual (see Verification Steps). If Vitest + `@nuxt/test-utils` is introduced later, these are the tests to add, matching the structure of `.squad/plans/checkout-payment/10-story-checkout-and-payment-page.md`'s Test Plan section:

1. **Unit — `useOrders`/`useOrder`:** `useOrders()` builds `${apiBase}/orders` with the `Authorization` header and `server: false`; `useOrder(id)` builds `${apiBase}/orders/${id}` with the same header, `server: false`, and a `key` unique per id (`order-${id}`).
2. **Component — `app/pages/orders/index.vue`** with `useOrders` mocked: `pending` renders the skeleton; a non-404 `error` renders the generic error notice with a working Retry (`refresh()` called on click); `data: []` renders the empty state with a link to `/products` (not the error notice); `data: [...]` renders one row per order, newest-first order preserved from the mocked array, each row linking to `/orders/{id}`.
3. **Component — `app/pages/orders/[id].vue`** with `useOrder` mocked: `pending` renders the skeleton; an error with `statusCode: 404` or `statusCode: 403` renders the identical not-found state (not the generic error notice) with a link back to `/orders`; an error with another `statusCode` (e.g. `500`) renders the generic error notice with a working Retry; loaded data renders `id`/`status`/`createdAt` plus one read-only row per item (`productName`, `formatPrice(unitPrice)` × `quantity`, `formatPrice(subtotal)`) and the formatted `total`.
4. **Component — invalid id:** mounting `/orders/[id].vue` with a non-numeric or non-positive `route.params.id` never calls `useOrder`/`$fetch` and renders the not-found state directly.
5. **Component — nav link:** `app/layouts/default.vue` renders the "Orders" link only when `auth.isAuthenticated` is `true`; it is absent when `false`.

---

## Verification Steps

1. **Frontend builds:** in `online-store-frontend/`, run `pnpm build`. Must succeed — a wrong auto-import name for `<BaseButton>`, a bad `useOrders`/`useOrder` usage, or a missing `definePageMeta` fails here.
2. **Auth guard:** while logged out, navigate to `/orders` or `/orders/1` directly — redirected to `/auth/login` (same guard as `/cart`/`/checkout`); the "Orders" nav link is also absent from the header.
3. **Checkout redirect closes the loop:** while logged in with items in the cart, complete `/checkout`'s "Place order" flow (Story 10) — the success redirect to `/orders/{id}` now resolves to this story's detail page instead of 404ing, rendering the just-placed order's items and `status: "paid"`.
4. **List data path:** while logged in with prior orders, open `/orders` — the network tab shows a request to `https://localhost:7225/api/orders` with an `Authorization: Bearer <token>` header; rows render newest-first with `id`, formatted date, `status`, and formatted `total`; clicking a row navigates to `/orders/{id}`.
5. **Empty list:** with an account that has no orders, open `/orders` — the "no orders yet" message renders with a working link to `/products`, not the error notice.
6. **Detail data path:** click into an order from the list — the network tab shows a request to `https://localhost:7225/api/orders/{id}`; the page renders the order's `id`/`status`/formatted `createdAt`, one row per item (`productName`, formatted `unitPrice` × `quantity`, formatted `subtotal`), and the formatted `total`.
7. **Not-found (missing order):** navigate to `/orders/999999` (or any id that does not exist) — the dedicated not-found state renders with a working link back to `/orders`, not a raw error.
8. **Not-found (invalid id):** navigate to `/orders/abc` — same not-found state, and confirm in the network tab that no request to `/api/orders/abc` fires.
9. **Not-found (another user's order):** log in as a second user and navigate to the first user's known order id (e.g. from step 6's URL) — the identical not-found state renders (not a distinct "forbidden" message), confirming the 403-and-404 cases are visually indistinguishable per the acceptance criteria.
10. **Loading states:** throttle the network to Slow 3G and reload both `/orders` and `/orders/{id}` — the respective skeletons render before data lands.
11. **Error states:** stop the API and reload both pages — the generic error notice renders with a Retry button on each; clicking Retry re-issues the respective request once the API is back.
12. **Nav link:** while logged in, confirm "Orders" appears in the header alongside "Products" and "Cart" and navigates to `/orders`; while logged out, confirm it is absent.
13. **Backend unchanged:** `dotnet build` in `OnlineStore.API/` — expected unchanged, this story touches no C#.
14. **Responsive:** at 375 px both pages' rows/line items stack in a single column with no horizontal overflow; at 1280 px both read comfortably within the page's max-width.

---

## Done Criteria

- [ ] `app/pages/orders/index.vue` exists with `definePageMeta({ layout: "default", middleware: "auth" })`, fetches `GET /api/orders` via a new `useOrders` composable, and lists the caller's orders newest-first (`id`, formatted `createdAt`, `status`, formatted `total`), each row linking to `/orders/{id}`.
- [ ] An empty orders list renders a friendly "no orders yet" message with a link to `/products`, distinct from the error state.
- [ ] `app/pages/orders/[id].vue` exists with the same `layout`/`middleware`, fetches `GET /api/orders/{id}` via a new `useOrder` composable, and renders the full order (`id`, `status`, formatted `createdAt`, each item's `productName`/formatted `unitPrice`/`quantity`/formatted `subtotal`, formatted `total`).
- [ ] A 403 (another user's order) and a 404 (missing order) on `/orders/[id]` both render the identical "not found" state with a link back to `/orders` — no raw error is shown for either.
- [ ] Loading skeletons render on both pages while their respective fetch is in flight; a distinct generic error state (network/500) renders with a Retry button on both.
- [ ] `app/layouts/default.vue` shows an "Orders" link in the header, visible only when `auth.isAuthenticated`, alongside the existing "Products" and "Cart" links.
- [ ] The checkout success redirect (`app/pages/checkout.vue`, Story 10) now resolves to a real page instead of 404ing — confirmed by completing a checkout end-to-end and landing on the correct order's detail view.
- [ ] `pnpm build` succeeds; no arbitrary `bg-[#…]`/`text-[#…]` or raw `px` spacing introduced.

**STOP HERE. Report to the user and wait for confirmation.** This is the fifth and final story in the customer-journey chain (products-catalog → product-details → cart-page → checkout-payment → orders-history).
