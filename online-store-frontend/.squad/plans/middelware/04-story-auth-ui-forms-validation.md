# Story 04 — Auth UI redesign, global form components, zod validation & toasts

Replace the functional-but-bare `/auth/login` and `/auth/register` forms from Story 01 with a designed card UI matching the supplied reference, built on **reusable global components** (`BaseForm`, `BaseInput`, `BaseButton`), validated with **zod**, and reporting outcomes through **toast** notifications instead of inline-only text.

---

## Prerequisites

- **Story 01 completed** ([`01-story-auth-store-persistence.md`](./01-story-auth-store-persistence.md)): the `auth` store, the universal `auth` plugin, and the two auth pages this story rewrites.
- **Story 02 completed** ([`02-story-layouts.md`](./02-story-layouts.md)): `app.vue` renders through `<NuxtLayout>`; the `Toaster` mounts there so toasts are available on every route.
- Asset already present: **`public/images/logo.jpg`** — the brand mark that replaces the plain text heading.

---

## Story Goal

1. Login and register pages match the reference design: page-centred **white rounded card** with a soft shadow, **logo image** at the top, bold title, stacked label→input pairs with **filled light-blue inputs** (no visible border until focus), a **full-width navy submit button**, and a **show/hide password eye toggle**.
2. Three **global** components under `app/components/base/` — `BaseForm`, `BaseInput`, `BaseButton` — auto-imported as `<BaseForm>`, `<BaseInput>`, `<BaseButton>` and reused by both pages.
3. **zod** schemas own all client-side validation; per-field messages render under the offending input.
4. **Toasts** (`vue-sonner`) report success and failure; the API's error message text surfaces there.

**Not in scope:** password reset (the reference's "forgot password" link is rendered but points nowhere yet), social login, remember-me, i18n/RTL (the design is matched in **English/LTR** per the user's decision), and restyling the layouts/home page.

---

## Library decisions (and why *not* shadcn)

The ask allowed shadcn "if needed". It is **not** needed here and is deliberately skipped:

- `shadcn-vue` is a code-generator on top of **reka-ui** + `class-variance-authority` + `tailwind-merge` + `clsx`, and its Nuxt integration wants the `shadcn-nuxt` module plus a `components.json` and a CSS-variable theme rewrite. That is a large, opinionated footprint whose main payoff — generated primitives — is redundant when Goal 2 explicitly requires *our own* `BaseInput`/`BaseButton`/`BaseForm` API.
- The one shadcn piece with real value is its toaster, which is itself a wrapper around **`vue-sonner`** — so we depend on `vue-sonner` directly and skip the rest.

Installed in this story:

| Package | Version | Purpose |
|---|---|---|
| `zod` | 4.x | schema validation (Goal 3) |
| `vue-sonner` | 2.x | toast notifications (Goal 4) |

> `zod` v4 note: use `z.string().min(1, …)` + `.email(…)`. Read errors from `result.error.issues` (each `{ path, message }`) — `.errors` is not the v4 accessor.
>
> `vue-sonner` v2 note: the stylesheet must be registered explicitly (`vue-sonner/style.css` in `nuxt.config.ts` `css`), and `<Toaster />` must be mounted **once**, in `app.vue`.

---

## Context — Read These Files First

1. `app/pages/auth/login.vue` / `app/pages/auth/register.vue` (Story 01) — the `$fetch` calls, the response shapes, and the `auth.login(...)` handoff are **kept as-is**; only presentation, validation, and error reporting change.
2. `app/app.vue` (Story 02) — `<NuxtLayout><NuxtPage /></NuxtLayout>`; add `<Toaster />` as a sibling so it is outside the swapped page tree.
3. `nuxt.config.ts` — `@nuxtjs/tailwindcss` v6 ⇒ **Tailwind v3**, so arbitrary values (`bg-[#e8edf9]`) are available and no `@theme` block is needed. There is no `tailwind.config.js`; the palette is expressed with arbitrary hex values rather than by extending a theme, keeping this story config-free.
4. `app/components/` — currently empty. Nuxt auto-imports components from it, and a **subdirectory prefixes the name**: `app/components/base/Input.vue` → `<BaseInput>`. Name the files `Input.vue`/`Button.vue`/`Form.vue` inside `base/`, **not** `BaseInput.vue` (that would auto-import as `<BaseBaseInput>`).

---

## Design tokens taken from the reference

| Token | Value | Applied to |
|---|---|---|
| Page background | `#f5f6fa` | auth page wrapper |
| Card | white, `rounded-2xl`, `shadow-lg`, `max-w-md`, `p-8`–`p-10` | form card |
| Input fill | `#e8edf9` | `BaseInput` default state |
| Brand navy | `#1b3a6b` | title, submit button, focus ring |
| Muted link | `#8a94a6` | "forgot password" |
| Error | `#dc2626` (`text-red-600`) | field messages |

---

## Implementation tasks

### 1 — Install dependencies

```bash
pnpm add zod vue-sonner
```

### 2 — Register the toast stylesheet

**File: `nuxt.config.ts`** — add the `css` array (leave `modules`/`runtimeConfig` untouched):

```ts
css: ["vue-sonner/style.css"],
```

### 3 — Mount the toaster once

**File: `app/app.vue`**

```vue
<script setup lang="ts">
import { Toaster } from "vue-sonner";
</script>

<template>
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
  <Toaster position="top-center" rich-colors />
</template>
```

> Mounted outside `<NuxtLayout>` so a layout change never unmounts in-flight toasts.

### 4 — Global `BaseButton`

**Create file: `app/components/base/Button.vue`**

A `variant`/`block`/`loading` button. `loading` implies `disabled` and swaps in a spinner, so pages never manage both flags.

### 5 — Global `BaseInput`

**Create file: `app/components/base/Input.vue`**

- `v-model` via `modelValue` + `update:modelValue`.
- Props: `label`, `type`, `placeholder`, `error`, `autocomplete`, `required`.
- `type="password"` renders an **eye toggle** that flips the effective input type locally (the `type` prop itself is never mutated).
- `error` swaps the fill for a red ring and renders the message below; `aria-invalid` + `aria-describedby` wire the message to the field for screen readers.
- Generates a stable id with Vue's `useId()` so the `<label for>` association survives SSR hydration.

### 6 — Global `BaseForm`

**Create file: `app/components/base/Form.vue`**

A thin `<form>` wrapper that owns the one thing every form here repeats: `@submit.prevent` → emit `submit`, plus consistent vertical rhythm. Kept deliberately small — validation state stays in the page, because each page's zod schema and API call differ.

### 7 — Shared zod schemas

**Create file: `app/utils/validation.ts`**

```ts
import { z } from "zod";

export const loginSchema = z.object({
  email: z.string().min(1, "Email is required").email("Enter a valid email address"),
  password: z.string().min(1, "Password is required"),
});

export const registerSchema = z.object({
  name: z.string().min(2, "Name must be at least 2 characters"),
  email: z.string().min(1, "Email is required").email("Enter a valid email address"),
  password: z.string().min(6, "Password must be at least 6 characters"),
});

// Flattens a zod v4 error into { field: firstMessage } for BaseInput's `error` prop.
export function fieldErrors(error: z.ZodError): Record<string, string> { … }
```

> `fieldErrors` keeps the **first** message per field — matching the design, which shows a single line under each input.

### 8 — Rewrite the login page

**File: `app/pages/auth/login.vue`** — same store/API behaviour as Story 01, new shell:

- Validate with `loginSchema.safeParse(...)` **before** the request; on failure set field errors and **do not** call the API.
- Success → `auth.login(...)`, `toast.success("Welcome back, {name}")`, `navigateTo("/")`.
- Failure → `toast.error(...)` with the API message when present, else the generic 401 text.
- `definePageMeta({ layout: false })` so the card is centred on a bare page rather than nested inside the store header/footer — the reference is a standalone screen.

### 9 — Rewrite the register page

**File: `app/pages/auth/register.vue`** — same structure with `registerSchema`, the extra `name` field, and Story 01's rule preserved: **register returns no token**, so on success `toast.success` then `navigateTo("/auth/login")` — never touch the store.

---

## Edge Cases & Failure Modes

- **`layout: false` and the toaster:** because `<Toaster />` lives in `app.vue` *outside* `<NuxtLayout>`, disabling the layout on the auth pages does **not** remove toasts. Mounting it inside `default.vue` instead would silently break every toast on these pages.
- **Component-name collision:** `app/components/base/Input.vue` auto-imports as `<BaseInput>`. Naming the file `BaseInput.vue` inside `base/` yields `<BaseBaseInput>` and the template silently renders nothing. Verified against the generated `.nuxt/components.d.ts`.
- **SSR id mismatch:** hand-rolling input ids from `Math.random()` produces different server/client values and a hydration warning; `useId()` is SSR-stable.
- **Double submit:** `loading` is set before the `await` and cleared in `finally`, and `BaseButton` treats `loading` as `disabled`, so a second click cannot fire a duplicate request.
- **Errors must clear:** stale field errors are wiped at the start of every submit; otherwise a corrected field keeps showing its old message.
- **zod v4 error shape:** `error.issues`, not `error.errors`; `issue.path` is an array, so the field key is `issue.path[0]` and needs a `String(...)` cast for the record key.
- **API error text vs generic message:** login intentionally shows the backend's generic *"Invalid email or password"* (it does not distinguish unknown email from wrong password — Story 01). Register **does** surface `e.data.message` because a 409 *"Email already exists"* is actionable.
- **`$fetch` network failure:** if the API is down, `e.data` is `undefined`; every handler falls back to a static string rather than rendering `undefined` in a toast.
- **Logo aspect ratio:** `logo.jpg` is a wide lockup; it is sized by height (`h-16 w-auto`) and centred so a different asset does not distort.
- **Guards unaffected:** this story touches only presentation/validation. `/auth/login` stays **unguarded** (Story 03 relies on it as a public redirect target) — do not add `middleware` to these pages.

---

## Test Plan

No test runner is configured (see Story 01). Manual verification only. If Vitest + `@nuxt/test-utils` is added later:

1. **Unit — schemas:** `loginSchema` rejects empty/malformed email and empty password; `registerSchema` enforces name ≥ 2 and password ≥ 6.
2. **Unit — `fieldErrors`:** maps a multi-issue `ZodError` to one message per field, first-wins.
3. **Component — `BaseInput`:** renders the label, emits `update:modelValue`, toggles `type` on eye click, shows `error` and sets `aria-invalid`.
4. **Component — `BaseButton`:** `loading` renders the spinner and sets `disabled`.
5. **Component — pages:** invalid submit shows field errors and issues **no** `$fetch`; mocked 401 triggers `toast.error`; mocked success calls `auth.login` and navigates.

---

## Verification Steps

1. `pnpm build` succeeds (catches auto-import name errors, which are build-time in Nuxt).
2. **Login visual:** `/auth/login` renders the centred white card with the logo, no store header/footer, filled light-blue inputs, and a navy full-width button.
3. **Client validation:** submit empty → "Email is required" / "Password is required" under the fields and **no** network request in DevTools; type a malformed email → "Enter a valid email address".
4. **Password toggle:** click the eye → characters become visible, icon flips, click again → masked.
5. **Failure toast:** with the API running, submit wrong credentials → red toast "Invalid email or password"; the form stays filled and re-submittable.
6. **Success toast:** correct credentials → green toast, redirect to `/`, header shows "Log out".
7. **Register:** short password → inline message; duplicate email → red toast with the API's "Email already exists"; a fresh user → green toast then `/auth/login`.
8. **Regression:** `/cart` still redirects to `/auth/login` logged out, and the login page reached that way still renders and works (Story 03 guards intact).

---

## Done Criteria

- [ ] Both auth pages match the reference: white card, `public/images/logo.jpg`, labels above filled inputs, navy full-width button, password eye toggle.
- [ ] `<BaseForm>`, `<BaseInput>`, `<BaseButton>` exist under `app/components/base/`, are globally auto-imported, and are used by **both** pages.
- [ ] All client-side validation goes through zod schemas in `app/utils/validation.ts`; invalid submits never hit the API.
- [ ] Success and error paths on both pages raise toasts; `<Toaster />` is mounted once in `app.vue`.
- [ ] Story 01 behaviour preserved: login populates the store and lands on `/`; register stores nothing and lands on `/auth/login`.
- [ ] `pnpm build` is clean.

---

**Feature complete. Report results to the user with the acceptance-criteria checklist filled in.**
