# Story 05 — Brand theme & customer home page

Give the project a real Tailwind theme (**primary `#1C3684`**) and replace the placeholder `app/pages/index.vue` with a marketing-forward customer home page: hero, "New arrivals" grid backed by `GET /api/products`, a static category teaser, and a closing CTA band.

---

## Prerequisites

- **Story 02 completed** ([`../middelware/02-story-layouts.md`](../middelware/02-story-layouts.md)): `app/layouts/default.vue` exists and `app/app.vue` renders through `<NuxtLayout>`. This page uses the `default` layout **implicitly** — do **not** add `definePageMeta({ layout: … })` to `index.vue`.
- **Story 04 completed** ([`../middelware/04-story-auth-ui-forms-validation.md`](../middelware/04-story-auth-ui-forms-validation.md)): the global `BaseButton` component exists at `app/components/base/Button.vue` and hardcodes the brand navy as `#1b3a6b`. This story moves that colour into the Tailwind theme — see task 1.
- Backend `GET /api/products` is already implemented and `[AllowAnonymous]` — `OnlineStore.API/Controllers/ProductsController.cs` lines 25–65. **No backend change is required or permitted in this story.**
- The API must be running for the grid to populate. `nuxt.config.ts` points `apiBase` at `https://localhost:7225/api`; the API's CORS policy allows only `http://localhost:3000` (`OnlineStore.API/Program.cs` line 45), so run the frontend on the default port.

---

## Story Goal

1. A **Tailwind theme** with `primary` = `#1C3684` plus tint/shade steps, so brand colour stops being copy-pasted hex literals.
2. `app/pages/index.vue` becomes a real home page with four sections: **hero**, **new arrivals**, **category teaser**, **closing CTA band**.
3. The new-arrivals grid renders live data from `GET /api/products?page=1&pageSize=8` with explicit **loading**, **error**, and **empty** states.
4. Reusable presentation pieces (`ProductCard`, a price formatter, a product-image URL helper) that Story 06 and the future `/products` catalog page reuse instead of re-implementing.

**Not in scope:** the `/products` catalog page itself, category/brand listing from the API (no endpoint exists — the teaser is **static**), search wiring, cart interaction from the card, and any change to `app/layouts/default.vue` beyond the colour swap in task 2.

---

## Context — Read These Files First

1. `app/pages/index.vue` — the whole file is 5 lines: a centred `<h1>` with `text-blue-600`. You replace it entirely.
2. `app/components/base/Button.vue` — lines 19–24, the `variants` record. Brand navy is hardcoded three times as `#1b3a6b` / `#16305a` / `#e8edf9` / `#f0f3fa`. Task 2 replaces these with theme tokens.
3. `app/layouts/default.vue` — lines 7–15: the header already links to `/products` and `/cart`, and line 8 uses `text-blue-600`. Note **there is no `app/pages/products/` directory yet**, so that link 404s today (pre-existing, see Edge Cases).
4. `app/composables/useApi.ts` — all 4 lines. `useApi()` returns the **base including `/api`** (`https://localhost:7225/api`). Product `imageUrl` values are **relative to the API host, not to `/api`** — task 4 handles this.
5. `app/pages/auth/login.vue` — lines 23–41: the established `$fetch` + `try/catch/finally` + `toast` pattern. Match it, but see task 5 for why the home page uses `useFetch`, not `$fetch`.
6. `OnlineStore.API/Controllers/ProductsController.cs` — lines 25–65. Confirm: non-admin callers see **only `IsActive` products** (lines 40–43), ordering is `OrderByDescending(p => p.CreatedAt)` (line 53), `pageSize` is clamped to 1–100 (line 35), and the envelope is `new { items, page, pageSize, total }` (line 59).
7. `OnlineStore.API/Dtos/ProductListItemDto.cs` — the **entire** list payload per item: `Id, Name, Price, Stock, IsActive, ImageUrl`. There is **no description, category, brand, or rating field** — do not design a card that needs one.
8. `OnlineStore.API/Services/ImageStorageService.cs` — line 62 returns `$"/{RelativeDir}/{fileName}"`, i.e. a **relative** path like `/images/products/{guid}.png`, served by `app.UseStaticFiles()` (`Program.cs` line 87).
9. Grep for `#1b3a6b` across `app/` before task 2 — that grep is your exact worklist of hardcoded brand hexes.
10. `.squad/plans/middelware/04-story-auth-ui-forms-validation.md` — the design-token table and component conventions this story follows (`app/components/base/Input.vue` auto-imports as `<BaseInput>`, **not** `<BaseBaseInput>`).

---

## Frontend Tasks

### 1 — Create the Tailwind theme

**Create file: `tailwind.config.ts`** (frontend **root**, next to `nuxt.config.ts`).

There is currently **no** Tailwind config file in the repo — `@nuxtjs/tailwindcss` 6.14.0 is running on its defaults with `tailwindcss` **3.4.19**. The module auto-detects `tailwind.config.{js,ts}` in the root and merges its own `content` paths, so **do not** hand-write a `content` array.

```ts
import type { Config } from "tailwindcss";

export default {
  theme: {
    extend: {
      colors: {
        primary: {
          50: "#eef2fb",
          100: "#dbe3f5",
          200: "#b8c6ea",
          300: "#8fa4dc",
          400: "#4f6cbb",
          500: "#1C3684", // brand
          600: "#182f73",
          700: "#142862",
          800: "#101f4d",
          900: "#0c1839",
          DEFAULT: "#1C3684",
        },
      },
    },
  },
} satisfies Config;
```

`DEFAULT` makes bare `bg-primary` / `text-primary` valid alongside `bg-primary-600`. Use the scale for hover/active states — **do not** reintroduce arbitrary `bg-[#…]` values in any file this story touches.

### 2 — Retire the hardcoded brand hexes

**File: `app/components/base/Button.vue`** — replace the `variants` record (lines 19–24) with theme tokens, keeping the three variant keys and every other class unchanged:

```ts
const variants: Record<string, string> = {
  primary: "bg-primary text-white hover:bg-primary-600 focus:ring-primary",
  secondary: "bg-primary-100 text-primary hover:bg-primary-200 focus:ring-primary",
  ghost: "bg-transparent text-primary hover:bg-primary-50 focus:ring-primary",
};
```

**File: `app/layouts/default.vue`** — line 8, swap `text-blue-600` for `text-primary` on the wordmark. Leave the rest of the layout alone.

> The old navy was `#1b3a6b`; the story mandates `#1C3684`. This is an intentional, visible colour shift on the auth pages too. `app/pages/auth/login.vue` line 57 and `app/pages/auth/register.vue` still carry `#1b3a6b`/`#e8edf9` literals in page markup and in `app/components/base/Input.vue` — converting those is **out of scope here**; leave them and note the follow-up in a comment above the `colors` block in `tailwind.config.ts`.

### 3 — Price formatting helper

**Create file: `app/utils/format.ts`**

```ts
// The backend exposes `decimal` amounts with no currency field anywhere in the
// API (verified: no currency/ISO code in any DTO or entity), so the currency
// here is a frontend assumption. Change the ISO code in one place if the
// backend later returns one.
export function formatPrice(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
  }).format(value);
}
```

Place it in `app/utils/` (not `composables/`) — `app/utils/validation.ts` establishes that convention, and Nuxt auto-imports both.

### 4 — Product image URL helper

**Create file: `app/composables/useProductImage.ts`**

`imageUrl` from the API is host-relative (`/images/products/x.png`) and served by the **API**, not by Nuxt. `useApi()` returns a base **ending in `/api`**, so naive concatenation yields `…/api/images/products/x.png` → 404.

```ts
// Product images live on the API host's wwwroot, not under /api.
export const useProductImage = () => {
  const apiBase = useApi();
  const origin = apiBase.replace(/\/api\/?$/, "");

  return (imageUrl: string | null | undefined): string | null => {
    if (!imageUrl) return null;
    if (/^https?:\/\//i.test(imageUrl)) return imageUrl;
    return `${origin}${imageUrl.startsWith("/") ? "" : "/"}${imageUrl}`;
  };
};
```

Returning `null` for a missing image lets the card render a placeholder instead of a broken `<img>`.

### 5 — Shared product types + fetch composable

**Create file: `app/composables/useProducts.ts`**

Type the envelope exactly as `ProductsController.List` returns it, then wrap `useFetch` so Story 06 reuses one definition.

```ts
export interface ProductListItem {
  id: number;
  name: string;
  price: number;
  stock: number;
  isActive: boolean;
  imageUrl: string;
}

export interface PagedProducts {
  items: ProductListItem[];
  page: number;
  pageSize: number;
  total: number;
}

export const useProducts = (page: number, pageSize: number, key: string) => {
  const apiBase = useApi();
  return useFetch<PagedProducts>(`${apiBase}/products`, {
    key,
    query: { page, pageSize },
    // Client-only: the .NET dev server uses a self-signed HTTPS certificate,
    // which the Nitro server rejects during SSR (see Edge Cases).
    server: false,
    default: (): PagedProducts => ({ items: [], page, pageSize, total: 0 }),
  });
};
```

Use `useFetch` (not `$fetch`) so `pending`/`error` come free and the request is de-duplicated by `key`. Pass a distinct `key` per call site (`"home-new-arrivals"` here, `"admin-recent-products"` in Story 06) — a shared key would make the two pages share one cached payload.

### 6 — Product card component

**Create file: `app/components/ProductCard.vue`**

Auto-imports as `<ProductCard>` (top-level `components/`, so no `Base` prefix).

- Props: `product: ProductListItem`.
- Renders, in this order: image (fixed **`aspect-square`**, `object-cover`, `rounded-t-xl`), name (`line-clamp-2`, `font-medium`), `formatPrice(product.price)` in `text-primary font-semibold`, and a stock line.
- Image falls back to a neutral `bg-primary-50` block with the product's initial when `useProductImage()` returns `null`; add `loading="lazy"` and a real `alt={{ product.name }}`.
- `product.stock <= 0` → render an "Out of stock" pill (`bg-gray-100 text-gray-500`) instead of the stock count. Do **not** hide the card.
- Wrap the whole card in `<NuxtLink :to="`/products/${product.id}`">` with `hover:shadow-md transition` — **the target route does not exist yet** (see Edge Cases); it is the correct eventual destination and keeps the card markup stable.
- No "Add to cart" button — cart wiring is a separate feature.

### 7 — Rebuild the home page

**File: `app/pages/index.vue`** — replace the entire file. No `definePageMeta` (inherits `default` layout, stays public — **do not** add `middleware`).

```vue
<script setup lang="ts">
const { data, pending, error } = useProducts(1, 8, "home-new-arrivals");
</script>
```

Then four sections, in order:

#### 7.1 Hero

Full-bleed band, `bg-primary` with white text, `py-16 sm:py-24`, `rounded-2xl`. Contents: an eyebrow line, an `<h1>` (`text-4xl sm:text-5xl font-bold tracking-tight`) carrying the brand message, one supporting paragraph in `text-primary-100`, and two CTAs — `<BaseButton>` "Shop now" linking to `/products` and a `ghost`/outline secondary "Browse new arrivals" anchoring to `#new-arrivals`. Wrap the primary button in `<NuxtLink to="/products">`; `BaseButton` renders a `<button>` and has **no** `to` prop.

#### 7.2 New arrivals

`<section id="new-arrivals">` with a heading "New arrivals" and a subhead stating these are the **latest** products (the API orders by `CreatedAt` descending — there is **no** featured/best-seller flag in the backend, so **do not** label them "Featured" or "Best sellers").

Grid: `grid gap-6 sm:grid-cols-2 lg:grid-cols-4`. Three explicit states:

- `pending` → 8 skeleton cards (`animate-pulse`, `aspect-square bg-primary-50` + two grey bars), same grid, so layout does not jump.
- `error` → a bordered notice, "We couldn't load products right now." plus a **Retry** `BaseButton` calling `useFetch`'s `refresh`. Never render a raw error object.
- `data.items.length === 0` → "No products yet — check back soon."

Below the grid, a "View all products" link to `/products`.

#### 7.3 Category teaser (static)

**There is no `GET /api/categories` or `GET /api/brands` endpoint** — verified: `OnlineStore.API/Controllers/` contains only `AuthController`, `CartController`, `OrdersController`, `ProductsController`. Render a **hardcoded** 3–4 tile row (icon/initial block + label, `bg-primary-50`, `rounded-xl`) that links to `/products`, and put a comment in the template stating the tiles are static pending a categories endpoint. **Do not** invent a `/api/categories` call.

#### 7.4 Closing CTA band

`bg-primary-50` band, `rounded-2xl`, centred: short headline, one line of copy, and a single "Browse full catalog" `BaseButton` inside a `<NuxtLink to="/products">`. Static — **no** newsletter form (a form with no endpoint is worse than no form).

Spacing between sections: `space-y-16 sm:space-y-24` on the page wrapper. Use Tailwind spacing tokens only — **no arbitrary `px` values**.

---

## Backend Tasks

**No backend changes required.** `GET /api/products` already returns everything this page consumes, already allows anonymous callers, and already filters to active products for them.

---

## Edge Cases & Failure Modes

- **SSR vs the .NET self-signed dev certificate:** the API dev URL is `https://localhost:7225` (`nuxt.config.ts` `apiBase`). During SSR the Nitro **server** performs the fetch, and Node rejects the untrusted dev certificate (`UNABLE_TO_VERIFY_LEAF_SIGNATURE`), which would surface as a hard error on the public home page. Mitigated by `server: false` in task 5, so the grid fetches on the client only. If SSR data is wanted later, trust the dev cert instead of disabling verification globally.
- **`/products` does not exist.** `app/pages/` contains only `index.vue`, `cart.vue`, `auth/`, and `admin/`. Every "Shop now" / "View all" CTA — and the pre-existing header link at `app/layouts/default.vue` line 10 — 404s until the catalog story lands. Keep the links: they are the correct targets, and the 404 is pre-existing and visible, not silent. Same applies to `ProductCard`'s `/products/{id}` link.
- **API down / CORS mismatch:** browser blocks the request and `useFetch` sets `error`; the `error` branch in 7.2 must render. Serving the frontend on any port other than **3000** produces exactly this, because `Program.cs` line 45 allows only `http://localhost:3000`.
- **Empty catalog:** a fresh database returns `{ items: [], total: 0 }` — the empty-state branch renders, not a bare grid. The `default` in `useProducts` guarantees `data.items` is an array even before the first response, so `data.items.length` never throws on `undefined`.
- **Missing or absolute `imageUrl`:** `ImageStorageService` returns a relative path, but a seeded row can hold `""` or a full URL. `useProductImage` returns `null` for empty (→ placeholder) and passes `http(s)://` values through untouched.
- **Long product names:** `ProductListItemDto.Name` has no length cap in the list payload; `line-clamp-2` plus the fixed `aspect-square` image keeps every card the same height so the grid does not stagger.
- **Zero-decimal and large prices:** `Price` is `decimal` and arrives as a JSON number; `formatPrice` pins two fraction digits so `10` renders as `$10.00`, and thousands separators come from `Intl`.
- **Currency is an assumption.** No currency or locale exists anywhere in the API (verified across all DTOs and entities). `formatPrice` hardcodes `USD`. **Confirm with the product owner** — if it should be SAR, change the single ISO code in `app/utils/format.ts`.
- **`pageSize=8` is within the clamp** (`Math.Clamp(pageSize, 1, 100)`, line 35). A future change to a value above 100 would be silently clamped, not rejected.
- **Admin viewing the home page** sees the same list as anonymous users, because the `IsActive` filter is keyed on `User.IsInRole("admin")` (lines 40–43) and this page sends **no** Authorization header. That is intended: the customer home page must show the customer's view.
- **Colour regression risk:** after task 2, any element still using `#1b3a6b` sits next to `#1C3684` and the mismatch is visible. Grep for `#1b3a6b` after the change and list the remaining files in your report — do **not** silently convert the auth pages.
- **Tailwind config not picked up:** the module reads `tailwind.config.ts` at **startup**. Adding the file requires restarting `pnpm dev`; without a restart, `bg-primary` produces no CSS and elements render transparent.

---

## Test Plan

**No test runner is configured** in this project — `package.json` has `build`/`dev`/`generate`/`preview`/`postinstall` only, and no Vitest dependency. Verification for this story is **manual** (see Verification Steps). If Vitest + `@nuxt/test-utils` is added later, these are the tests to write:

1. **Unit — `app/utils/format.ts`:** `formatPrice(0)` → `"$0.00"`; `formatPrice(1234.5)` → `"$1,234.50"`.
2. **Unit — `useProductImage`:** relative `/images/products/a.png` gains the API **origin** and not `/api`; an absolute `https://…` URL passes through; `""`/`null`/`undefined` return `null`.
3. **Component — `ProductCard`:** renders name and formatted price; `stock: 0` renders the "Out of stock" pill and no stock count; a null image renders the placeholder block, not an `<img>`.
4. **Component — `pages/index.vue`** with `useFetch` mocked: `pending` renders 8 skeletons; `error` renders the retry notice and no grid; `items: []` renders the empty state; 8 items render 8 `ProductCard`s.
5. **Smoke:** mount the page and assert the query string is `page=1&pageSize=8` — the number the copy ("New arrivals") depends on.

Match the structure of the test-plan sections in [`../middelware/04-story-auth-ui-forms-validation.md`](../middelware/04-story-auth-ui-forms-validation.md) if the runner is introduced.

---

## Verification Steps

1. **Frontend builds:** in `online-store-frontend/`, run `pnpm build`. It must succeed — Nuxt resolves auto-imports at build time, so a wrong component name (`<ProductCard>` vs the file path) fails here rather than at runtime.
2. **Theme active:** restart `pnpm dev`, open `/`, and inspect the hero — computed background must be `rgb(28, 54, 132)` (`#1C3684`). If it is transparent, `tailwind.config.ts` was added without a dev-server restart.
3. **Backend builds:** `dotnet build` in `OnlineStore.API/` — expected unchanged, this story touches no C#. Run it only to confirm you did not.
4. **Data path:** with the API running, load `/` and confirm the network tab shows one request to `https://localhost:7225/api/products?page=1&pageSize=8` returning `{ items, page, pageSize, total }`, and that up to 8 cards render newest-first.
5. **Images:** product images resolve against `https://localhost:7225/images/products/…` (**no** `/api` segment) and render; a product with an empty `imageUrl` shows the placeholder block.
6. **Loading state:** throttle the network to Slow 3G and reload — 8 skeleton cards appear in the final grid positions and the page does not shift when data lands.
7. **Error state:** stop the API and reload — the retry notice renders, no unhandled error overlay, and clicking **Retry** re-issues the request once the API is back.
8. **Empty state:** point `apiBase` at an empty catalog (or filter to none) and confirm "No products yet" renders instead of an empty grid.
9. **Responsive:** at 375 px the grid is one column and the hero copy does not overflow; at 1280 px it is four columns.
10. **Regression — layout:** the `default` header/footer still render around the page, the wordmark is now navy `#1C3684`, and `/cart` still redirects to `/auth/login` when logged out (Story 03 guards untouched).
11. **Regression — buttons:** `/auth/login` still renders its card and the submit button is now `#1C3684`; the password eye toggle and validation messages still work (Story 04 behaviour unchanged).

---

## Done Criteria

- [ ] `tailwind.config.ts` exists at the frontend root and defines `primary` with `DEFAULT: "#1C3684"` plus the tint/shade scale.
- [ ] `app/components/base/Button.vue` contains **no** `#1b3a6b`/`#16305a`/`#e8edf9`/`#f0f3fa` literals; all three variants use `primary-*` tokens.
- [ ] `app/layouts/default.vue` wordmark uses `text-primary` instead of `text-blue-600`.
- [ ] `app/pages/index.vue` renders hero, new arrivals, static category teaser, and closing CTA band — and still declares **no** `definePageMeta`, so it stays public on the `default` layout.
- [ ] The new-arrivals grid is populated from `GET /api/products?page=1&pageSize=8` and is labelled as newest/new arrivals, **not** "featured".
- [ ] Loading (skeletons), error (retry), and empty states are each reachable and verified.
- [ ] `app/composables/useProducts.ts`, `app/composables/useProductImage.ts`, `app/utils/format.ts`, and `app/components/ProductCard.vue` exist and are used by the page.
- [ ] Product images load from the API **origin** without an `/api` segment.
- [ ] No arbitrary `bg-[#…]`/`text-[#…]` values or raw `px` spacing introduced in any file this story touches.
- [ ] `pnpm build` succeeds; the remaining `#1b3a6b` occurrences (auth pages, `BaseInput`) are reported as a known follow-up, not silently changed.

**STOP HERE. Report to the user and wait for confirmation before proceeding to Story 06.**
