# Online Store — Frontend

The Nuxt 4 application: a customer storefront **and** the `/admin` management area, in one app with server-side rendering enabled. It talks to the [ASP.NET Core API](../OnlineStore.API/) over HTTP and holds no data of its own.

For repository-wide context see the [root README](../README.md); for getting everything running see [`docs/local-setup.md`](../docs/local-setup.md).

---

## Requirements and install

Node.js 20+ (verified on v24.13.0) and pnpm (verified on 10.28.0).

```bash
pnpm install
```

`postinstall` runs `nuxt prepare`, which generates `.nuxt/` — the typed auto-import registry, route table and middleware types. If your editor reports unknown auto-imports, run `pnpm install` (or `npx nuxt prepare`) again.

---

## Scripts

| Script | Command | What it does |
|---|---|---|
| `dev` | `pnpm dev` | Dev server with HMR on **http://localhost:3000** |
| `build` | `pnpm build` | Production build into `.output/` |
| `preview` | `pnpm preview` | Serves the production build locally |
| `generate` | `pnpm generate` | Static prerender (not used by this project's workflow) |

**Port 3000 is load-bearing.** The API's CORS policy admits only `http://localhost:3000`. If that port is already taken, Nuxt silently starts on another one and then *every* API call fails with a CORS error in the browser while the API logs show nothing at all. Free port 3000 rather than accepting the fallback.

---

## Configuring the API base URL

The one place the backend URL is defined:

```ts
// nuxt.config.ts
runtimeConfig: {
  public: {
    apiBase: "https://localhost:7225/api",
  },
},
```

Read it through the [`useApi()`](app/composables/useApi.ts) composable — never hard-code a URL in a page or component:

```ts
const apiBase = useApi();
await $fetch(`${apiBase}/products`);
```

Because it lives under `runtimeConfig.public`, it can be overridden at runtime without rebuilding, using Nuxt's standard environment-variable convention (`NUXT_` + the config path, upper-cased):

```bash
NUXT_PUBLIC_API_BASE=http://localhost:5016/api pnpm dev
```

The default value pairs with the API's **`https`** launch profile. If you run the API's `http` profile instead, override `apiBase` to `http://localhost:5016/api` — see the profile matrix in [`docs/local-setup.md`](../docs/local-setup.md).

---

## Project structure

Everything lives under `app/` (Nuxt 4's default source directory).

| Directory | Contents |
|---|---|
| [`app/pages/`](app/pages/) | File-based routes. Each page sets its layout and guards via `definePageMeta`. |
| [`app/layouts/`](app/layouts/) | `default.vue` (storefront chrome) and `admin.vue` (admin sidebar + header). |
| [`app/middleware/`](app/middleware/) | Route guards — see [Route protection](#route-protection). |
| [`app/components/base/`](app/components/base/) | Shared form primitives: `Button`, `Form`, `Input`, `Select`. Auto-imported as `BaseButton`, `BaseForm`, `BaseInput`, `BaseSelect`. |
| [`app/components/admin/`](app/components/admin/) | Admin-only widgets: `StatCard`, `RecentProductsTable`. |
| [`app/components/`](app/components/) | `ProductCard.vue` — the storefront product tile. |
| [`app/composables/`](app/composables/) | Data-fetching wrappers — see [Data fetching](#data-fetching). |
| [`app/stores/`](app/stores/) | Pinia stores. Only `auth.ts` today. |
| [`app/plugins/`](app/plugins/) | `auth.ts` — hydrates the auth store before route middleware runs. |
| [`app/utils/`](app/utils/) | `validation.ts` (zod schemas) and `format.ts` (currency formatting). |

---

## Routing and layouts

| Route | Page | Layout | Guard |
|---|---|---|---|
| `/` | `pages/index.vue` | `default` | — |
| `/products` | `pages/products/index.vue` | `default` | — |
| `/products/:id` | `pages/products/[id].vue` | `default` | — |
| `/cart` | `pages/cart.vue` | `default` | `auth` |
| `/checkout` | `pages/checkout.vue` | `default` | `auth` |
| `/orders` | `pages/orders/index.vue` | `default` | `auth` |
| `/orders/:id` | `pages/orders/[id].vue` | `default` | `auth` |
| `/auth/login` | `pages/auth/login.vue` | none (`layout: false`) | `guest` |
| `/auth/register` | `pages/auth/register.vue` | none (`layout: false`) | `guest` |
| `/admin/login` | `pages/admin/login.vue` | none (`layout: false`) | `guest` |
| `/admin/dashboard` | `pages/admin/dashboard.vue` | `admin` | `admin` |
| `/admin/products` | `pages/admin/products/index.vue` | `admin` | `admin` |
| `/admin/products/new` | `pages/admin/products/new.vue` | `admin` | `admin` |
| `/admin/products/:id/edit` | `pages/admin/products/[id]/edit.vue` | `admin` | `admin` |
| `/admin/categories` | `pages/admin/categories/index.vue` | `admin` | `admin` |
| `/admin/categories/new` | `pages/admin/categories/new.vue` | `admin` | `admin` |
| `/admin/categories/:id/edit` | `pages/admin/categories/[id]/edit.vue` | `admin` | `admin` |
| `/admin/brands` | `pages/admin/brands/index.vue` | `admin` | `admin` |
| `/admin/brands/new` | `pages/admin/brands/new.vue` | `admin` | `admin` |
| `/admin/brands/:id/edit` | `pages/admin/brands/[id]/edit.vue` | `admin` | `admin` |

Both are declared per page in one call:

```ts
definePageMeta({ layout: "admin", middleware: "admin" });
```

The three authentication pages use `layout: false` because both layouts render session chrome (a logout button, the admin sidebar with the user's name and role) that makes no sense around a login form.

---

## Route protection

Four middleware files. One is **global** — it runs on every route with no opt-in — and three are named and applied per page.

| File | Scope | Rule |
|---|---|---|
| [`role-area.global.ts`](app/middleware/role-area.global.ts) | **Global** | For authenticated users only, the admin and customer areas are mutually exclusive: an admin outside `/admin/*` is sent to `/admin/dashboard`; a customer inside `/admin/*` is sent to `/`. Logged-out visitors pass straight through. |
| [`admin.ts`](app/middleware/admin.ts) | Per page | Logged out → `/admin/login`. Authenticated non-admin → `/`. |
| [`auth.ts`](app/middleware/auth.ts) | Per page | Logged out → `/auth/login`. |
| [`guest.ts`](app/middleware/guest.ts) | Per page | An authenticated visitor cannot see a login or register form: admin → `/admin/dashboard`, customer → `/`. |

Two things are worth understanding before editing any of them:

- **Area separation has to be global.** Three pages (`/`, `/products`, `/products/:id`) declare no middleware at all, so a per-page rule can never cover them. That is why `role-area.global.ts` exists rather than the logic living inside `auth.ts`.
- **The global guard deliberately ignores logged-out visitors.** It returns early when there is no session, leaving `auth.ts` and `admin.ts` to own the logged-out redirects and keeping the public pages public.

The full behaviour matrix, including logout targets and the reasoning behind each redirect, is in [`docs/auth-and-roles.md`](../docs/auth-and-roles.md).

> These guards are **user experience, not security**. They can be bypassed by editing a cookie. Every protected endpoint is enforced server-side by the API.

---

## State and persistence

[`app/stores/auth.ts`](app/stores/auth.ts) is a Pinia store holding `token` and `user` (`name`, `email`, `role`), with two getters used throughout the app: `isAuthenticated` (`!!token`) and `isAdmin` (`role === "admin"`, compared exactly).

Sessions persist in **two** places:

- A cookie named `auth` holding `{ token, user }` — `maxAge` 7 days, `sameSite: "lax"`, `path: "/"`. The cookie is the important one: it is readable on the server, which is what lets route guards resolve correctly during SSR.
- A mirror in `localStorage`, used as a fallback on the client.

[`app/plugins/auth.ts`](app/plugins/auth.ts) hydrates the store from those sources. It is **universal** — deliberately *not* named `auth.client.ts`. Plugins run before route middleware, so on a direct navigation to `/admin/dashboard` the server has already populated the store by the time the guard runs. A client-only plugin would leave the server store empty and every guarded page would redirect a logged-in user on first load.

`logout()` clears state and then navigates, landing an admin on `/admin/login` and a customer on `/auth/login`. **The order matters**: state is cleared *before* navigating, because the destination pages carry the `guest` guard — navigating first would let the guard see a live session and bounce the user straight back.

---

## Data fetching

Composables in [`app/composables/`](app/composables/) wrap Nuxt's `useFetch`, so pages stay free of URL construction:

| Composable | Endpoint |
|---|---|
| `useApi()` | Returns `apiBase`; used by everything else |
| `useProducts(page, pageSize, key, search?)` | `GET /api/products` (paged) |
| `useProduct(id)` | `GET /api/products/{id}` |
| `useCategories()` | `GET /api/categories` |
| `useBrands()` | `GET /api/brands` |
| `useCart()` | `GET /api/cart` |
| `useOrders()` | `GET /api/orders` |
| `useProductImage()` | Builds an absolute image URL (see below) |

Two conventions to respect:

**Pass a distinct `key` per call site.** `useProducts` takes an explicit cache key. Two pages sharing one key share one cached payload, so a filtered list can overwrite an unfiltered one.

**`server: false` is a workaround, not a preference.** `useProducts` and `useProduct` disable server-side fetching because the .NET dev server uses a **self-signed HTTPS certificate** that Nitro's SSR fetch rejects. Removing the flag without first trusting the certificate reintroduces SSR fetch failures. The fix — `dotnet dev-certs https --trust`, or switching to the `http` profile — is in [`docs/local-setup.md`](../docs/local-setup.md).

`useProductImage()` exists because images are **not** served under `/api`: they are static files on the API host. It strips a trailing `/api` from `apiBase` to recover the origin, then resolves a stored path such as `/images/products/{guid}.jpg` against it. Absolute URLs are passed through unchanged, and `null`/empty input returns `null` so callers can render a placeholder.

---

## Forms, validation and toasts

Forms are built from the `Base*` primitives and validated client-side with [zod](https://zod.dev) before any request is sent:

```ts
const parsed = loginSchema.safeParse(form);
if (!parsed.success) {
  errors.value = fieldErrors(parsed.error);
  return;
}
```

[`app/utils/validation.ts`](app/utils/validation.ts) holds the schemas — `loginSchema`, `registerSchema`, `productFormSchema`, `nameFormSchema` — plus `fieldErrors()`, which flattens a `ZodError` into `{ field: firstMessage }`. First message wins, because the design shows a single line under each input; `BaseInput` takes that string as its `error` prop.

User feedback uses [`vue-sonner`](https://vue-sonner.vercel.app) (`toast.success(...)` / `toast.error(...)`); its stylesheet is registered globally in `nuxt.config.ts`.

> **Known gap:** `registerSchema` accepts any password of 6+ characters, but the API requires **8+ with at least one uppercase letter and one digit**. A password such as `abcdef` passes client validation and is then rejected by the API with a 400. See [`docs/auth-and-roles.md`](../docs/auth-and-roles.md#known-gaps).

Prices are rendered through `formatPrice()` in [`app/utils/format.ts`](app/utils/format.ts). The API returns bare decimals with no currency field anywhere, so USD is a **frontend assumption** — change the ISO code in that one function if the backend ever returns one.

---

## Styling

Tailwind CSS via the `@nuxtjs/tailwindcss` module; no separate PostCSS setup and no `content` paths to maintain (the module supplies them).

[`tailwind.config.ts`](tailwind.config.ts) extends the palette with a `primary` scale, brand `#1C3684`. Use the tokens (`bg-primary`, `text-primary-600`) rather than raw hex.

> **Outstanding cleanup**, noted in the config file itself: `app/pages/auth/login.vue`, `app/pages/auth/register.vue` and `app/components/base/Input.vue` still carry `#1b3a6b` / `#e8edf9` literals in their markup. Converting them to tokens is a separate pass.

---

## Building

```bash
pnpm build      # → .output/
pnpm preview    # serve the build
```

The build is a Nitro server bundle. It can be run directly without the Nuxt CLI:

```bash
node .output/server/index.mjs
```

which is a convenient way to check SSR behaviour and route guards against the real server rather than the dev server.

---

## Testing

**No test runner is configured** — there is no `test` script in `package.json` and no testing library installed.

Verification today is:

1. `pnpm build` must be clean.
2. The manual **Verification Steps** in the relevant story under [`.squad/plans/`](.squad/plans/). Each plan lists concrete scenarios to click through, including the role/route combinations the guards are supposed to enforce.

If a test runner is added later, the plans' **Test Plan** sections already specify the unit and component tests each feature expects.
