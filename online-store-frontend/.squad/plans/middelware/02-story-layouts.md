# Story 02 — Customer & admin layouts

Introduce two Nuxt layouts — a customer-facing `default` (no sidebar) and an `admin` layout (with a sidebar) — and switch `app.vue` to render through `<NuxtLayout>` so pages can pick their layout via `definePageMeta({ layout: '…' })`.

---

## Prerequisites

- **Story 01 completed** ([`01-story-auth-store-persistence.md`](./01-story-auth-store-persistence.md)): the `auth` store exists. The `admin` layout's sidebar shows a logout control that calls `useAuthStore().logout()`.

---

## Story Goal

1. `app/layouts/default.vue` — simple header/footer, **no sidebar**, applied automatically to all customer pages.
2. `app/layouts/admin.vue` — a distinct structure with a **sidebar** (Dashboard, Products) plus a logout action.
3. `app/app.vue` wraps `<NuxtPage />` in `<NuxtLayout>` so layouts take effect.

**Not in scope:** route protection (Story 03), real page content for products/cart/admin (Story 03 creates only the minimal protected pages; catalog/cart UIs are separate features).

---

## Context — Read These Files First

1. `app/app.vue` — currently `<div><NuxtPage /></div>`. You will replace the body with `<NuxtLayout>`. `NuxtLayout`/`NuxtPage`/`NuxtLink` are **auto-imported** components.
2. `.nuxt/tsconfig.app.json` — confirms source root `app/`; layouts therefore live in **`app/layouts/`** (Nuxt's convention, auto-discovered — no config needed).
3. `app/pages/index.vue` — the existing home page. It has **no** `definePageMeta`, so it will use the `default` layout automatically once Story goal 3 is done. No change required here.
4. `nuxt.config.ts` — Tailwind is enabled (`@nuxtjs/tailwindcss`), so the utility classes below are available.

---

## Implementation tasks

### 1 — Wrap the app in a layout outlet

**File: `app/app.vue`**

Replace the entire file with:

```vue
<template>
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
</template>
```

> Without `<NuxtLayout>`, `definePageMeta({ layout })` has no effect. The `default` layout is applied automatically to any page that does not set one.

### 2 — Customer layout (no sidebar)

**Create file: `app/layouts/default.vue`**

```vue
<script setup lang="ts">
const auth = useAuthStore();
</script>

<template>
  <div class="min-h-screen flex flex-col">
    <header class="border-b px-6 py-4 flex items-center justify-between">
      <NuxtLink to="/" class="text-xl font-bold text-blue-600">Online Store</NuxtLink>
      <nav class="flex items-center gap-4 text-sm">
        <NuxtLink to="/products">Products</NuxtLink>
        <NuxtLink to="/cart">Cart</NuxtLink>
        <NuxtLink v-if="!auth.isAuthenticated" to="/auth/login">Log in</NuxtLink>
        <button v-else class="text-red-600" @click="auth.logout()">Log out</button>
      </nav>
    </header>

    <main class="flex-1 p-6">
      <slot />
    </main>

    <footer class="border-t px-6 py-4 text-center text-sm text-gray-500">
      © Online Store
    </footer>
  </div>
</template>
```

> The `/products` link is a placeholder route (that page is a separate feature); it is fine for it to 404 until then. The important structural fact is **no sidebar**.

### 3 — Admin layout (with sidebar)

**Create file: `app/layouts/admin.vue`**

```vue
<script setup lang="ts">
const auth = useAuthStore();
</script>

<template>
  <div class="min-h-screen flex">
    <aside class="w-60 shrink-0 bg-gray-900 text-gray-100 flex flex-col">
      <div class="px-4 py-4 text-lg font-bold border-b border-gray-700">Admin</div>
      <nav class="flex-1 flex flex-col p-2 gap-1 text-sm">
        <NuxtLink to="/admin/dashboard" class="px-3 py-2 rounded hover:bg-gray-800">Dashboard</NuxtLink>
        <NuxtLink to="/admin/products" class="px-3 py-2 rounded hover:bg-gray-800">Products</NuxtLink>
      </nav>
      <div class="p-2 border-t border-gray-700">
        <button class="w-full text-left px-3 py-2 rounded hover:bg-gray-800" @click="auth.logout()">
          Log out
        </button>
      </div>
    </aside>

    <div class="flex-1 flex flex-col">
      <header class="border-b px-6 py-4 text-sm text-gray-600">
        Signed in as {{ auth.user?.email }} ({{ auth.user?.role }})
      </header>
      <main class="flex-1 p-6">
        <slot />
      </main>
    </div>
  </div>
</template>
```

> This structure is **completely separate** from `default.vue`: full-height flex row, dark sidebar on the left, content on the right. That satisfies the "distinct admin area" requirement independently of the route guard (Story 03).

---

## Edge Cases & Failure Modes

- **Layout not applied:** if `app.vue` is not wrapped in `<NuxtLayout>` (task 1 skipped), no layout renders regardless of `definePageMeta`. Verify the header/footer appear on `/`.
- **`auth.user` is null before login:** the admin header uses optional chaining (`auth.user?.email`) so it renders blank rather than throwing when the store is empty; in practice the admin layout is only reached via the admin guard (Story 03), which requires a logged-in admin.
- **Logout from a layout button:** `auth.logout()` returns `navigateTo('/auth/login')`; calling it from the sidebar/header button works because the store action performs the navigation itself.
- **Hydration mismatch on auth-conditional nav:** the `default` header shows "Log in" vs "Log out" based on `auth.isAuthenticated`. Because the store is hydrated from the cookie on the **server** (Story 01 plugin), the server and client render the same branch — no hydration warning. If a warning appears, confirm the `auth` plugin is universal (not `.client`).

---

## Test Plan

No test runner is configured (see Story 01). Manual verification only. If component testing is added later:

1. **Component — `default.vue`:** renders header + footer, contains **no** `<aside>`; shows "Log in" when unauthenticated and "Log out" when authenticated.
2. **Component — `admin.vue`:** renders an `<aside>` sidebar with Dashboard/Products links and a logout button.

---

## Verification Steps

1. **Frontend runs:** `pnpm dev` in `online-store-frontend/`.
2. **Default layout:** open `/` → header ("Online Store", nav links) and footer are present, **no sidebar**. The existing home heading still shows inside `<main>`.
3. **Admin layout (temporary check):** temporarily add `definePageMeta({ layout: 'admin' })` to `app/pages/index.vue`, reload → the dark **sidebar** with Dashboard/Products appears and the default header/footer are gone. **Remove that temporary line** before finishing (real admin pages arrive in Story 03).
4. **Logout button:** while logged in (from Story 01), click "Log out" in the header → redirected to `/auth/login`, store cleared.

---

## Done Criteria

- [ ] `app/app.vue` renders `<NuxtPage />` inside `<NuxtLayout>`.
- [ ] `app/layouts/default.vue` exists with header/footer and **no sidebar**; applied automatically to customer pages (acceptance: customer pages use default layout, no sidebar).
- [ ] `app/layouts/admin.vue` exists with a **sidebar** and a distinct structure (acceptance: admin pages use a separate layout with a sidebar).
- [ ] No leftover temporary `definePageMeta` on `index.vue`.

---

**STOP HERE. Report to the user and wait for confirmation before proceeding to Story 03.**
