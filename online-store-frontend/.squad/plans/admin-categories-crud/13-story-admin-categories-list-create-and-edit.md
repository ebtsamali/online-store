# Story 13 — Admin Categories List, Create and Edit

Add brand-new admin surface for managing the `Category` and `Brand` reference entities: a list page each, plus create/edit pages, mirroring the CRUD conventions established in Story 12 (`admin-products-crud`). No placeholder currently exists for either entity.

---

## Prerequisites

- **HARD-BLOCKED — do not start implementation yet.** This story requires the backend endpoints `GET/POST/PUT/DELETE /api/categories` and `/api/brands`, planned in [`OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md`](../../../../OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md) but **not yet implemented**: verified via `grep` that `OnlineStore.API/Controllers/` contains no `CategoriesController.cs`/`BrandsController.cs` and no `class CategoriesController`/`class BrandsController` anywhere in the codebase. Do not write any frontend code against these endpoints until the backend story ships and its endpoints are confirmed live (e.g. via Swagger at `http://localhost:5016/swagger`), per this story's own intake `## Dependencies`.
- **Story 12 completed** ([`../admin-products-crud/12-story-admin-products-list-create-and-edit.md`](../admin-products-crud/12-story-admin-products-list-create-and-edit.md)): establishes the exact admin CRUD conventions this story mirrors — confirm-before-delete via `window.confirm(...)` (its task 3, step 1), `productFormSchema` + `fieldErrors()` pattern in `app/utils/validation.ts` (its task 4), `BaseButton`'s `loading` prop blocking double-submits (its tasks 5–6), and the "plain numeric id input now, `<select>` later" precedent for a not-yet-available lookup endpoint (its "Optional Follow-up" section). Story 12 also documents, as an explicit follow-up (not in its own Done Criteria), that once `GET /api/categories`/`GET /api/brands` exist, its create/edit forms' `categoryId`/`brandId` numeric inputs should become `<select>` dropdowns — this story's own final task formalizes that same follow-up for the executor of Story 12's forms.
- `app/layouts/admin.vue`, `app/middleware/admin.ts`, `app/utils/validation.ts`, `app/composables/useApi.ts`, `app/stores/auth.ts` — read in full below (Context items 1–5); all are reused unchanged, no structural change to any of them beyond the sidebar edit in Frontend Tasks §1.
- **Last story in the overall plan** (per intake `## Extra notes`): implement after all customer-journey stories and Story 12, once the backend categories/brands endpoints are confirmed live.

---

## Story Goal

1. `app/pages/admin/categories/index.vue` (`layout: 'admin'`, `middleware: 'admin'`): fetches `GET /api/categories` with the admin bearer token attached (the endpoint is public per the backend plan, but the token is sent for consistency with every other admin fetch in this codebase); renders a table of `name` + Edit/Delete actions; an "Add category" button linking to `/admin/categories/new`. No pagination — the backend plan explicitly returns the full unpaginated list (`../../../../OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md` line 18: "small reference lists — return all").
2. `app/pages/admin/categories/new.vue`: a single-field form (`name`), Zod-validated (non-empty, capped at a reasonable max length matching the backend's own limit), submitting **JSON** (not multipart — categories have no image) via `POST /api/categories`. Toast + redirect to `/admin/categories` on success; the backend's duplicate-name `400` message surfaced verbatim on failure.
3. `app/pages/admin/categories/[id]/edit.vue`: the same single-field form, pre-filled. Since the backend plan defines no `GET /api/categories/{id}` (only `List`, `Create`, `Update`, `Delete` — confirmed absent from the backend plan's Story Goal and Backend Tasks sections), the edit page obtains the current name by re-fetching `GET /api/categories` and finding the matching `id` client-side, not via a per-id endpoint. Submits `PUT /api/categories/{id}` as JSON.
4. Delete action on the list: `DELETE /api/categories/{id}` behind a `window.confirm(...)` dialog (matching Story 12's pattern, its task 3 step 1 — no modal component exists in `app/components/base/` to reuse). The backend returns a clean `409` (not a `500`) when a `Product` still references the category — the frontend surfaces that message clearly ("cannot delete a category in use by products" or the backend's own message verbatim) rather than a generic error.
5. Add a "Categories" nav item to `app/layouts/admin.vue`'s sidebar, alongside the existing Dashboard/Products items.
6. **Brands are included in this same story**, mirrored identically: `app/pages/admin/brands/index.vue`, `app/pages/admin/brands/new.vue`, `app/pages/admin/brands/[id]/edit.vue`, calling `/api/brands` instead of `/api/categories`, plus a "Brands" sidebar item. The backend plan defines identical CRUD shape and rules for both entities (`../../../../OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md` line 22: "The same four endpoints, identical shape and rules, for `Brand` under `/api/brands`"), and the intake explicitly allows bundling them in one story since the incremental effort is low once the Category pages exist.
7. A final documented follow-up (not built by this story): once this ships, Story 12's `new.vue`/`[id]/edit.vue` numeric `categoryId`/`brandId` `<BaseInput type="number">` fields should be upgraded to `<select>` dropdowns populated from `GET /api/categories`/`GET /api/brands`.

**Not in scope:** pagination on the categories/brands list pages (small reference lists, matching the backend's own no-pagination design); category/brand images or descriptions (the backend `Category`/`Brand` entities have no such fields per the backend plan's Context item 2–3); any change to how Products select their category/brand beyond the follow-up wiring step (item 7 above) — this story does not touch `app/pages/admin/products/new.vue` or `[id]/edit.vue`.

---

## Context — Read These Files First

1. `app/layouts/admin.vue` — all 56 lines. The sidebar `<nav>` (lines 9–25) currently has two `<NuxtLink>` entries: Dashboard (lines 10–16) and Products (lines 18–24, with the comment on line 17 explaining `active-class` vs `exact-active-class` — `active-class` is used so `/admin/products/3` stays highlighted). This story inserts two more `<NuxtLink>` entries after the Products one, for `/admin/categories` and `/admin/brands`, using the exact same `class`/`active-class` attributes as the Products link (lines 20–21) so `/admin/categories/new` and `/admin/categories/{id}/edit` stay highlighted the same way.
2. `app/middleware/admin.ts` — all 10 lines. `defineNuxtRouteMiddleware`: redirects to `/auth/login` if `!auth.isAuthenticated` (line 4), to `/` if authenticated but not admin (line 7). Reused unchanged as `middleware: "admin"` on all six new pages.
3. `app/utils/validation.ts` — all 34 lines. The Zod pattern this story's new schema follows: a `z.object({...})` (lines 3–18) plus the shared `fieldErrors(error: z.ZodError): Record<string, string>` (lines 25–34) that flattens a `ZodError` to `{ field: firstMessage }`, first issue per field wins (line 29). This story adds a new exported schema (e.g. `categoryFormSchema`) to this same file, following the same shape as the existing `loginSchema`/`registerSchema`.
4. `app/composables/useApi.ts` — all 5 lines. `useApi()` returns `config.public.apiBase` (a plain string, e.g. `https://localhost:7225/api`). Used unchanged to build every fetch URL in this story (`${useApi()}/categories`, `${useApi()}/brands`).
5. `app/stores/auth.ts` — all 74 lines. `state.token` (line 8, `string | null`) is the raw JWT for `Authorization: Bearer ${auth.token}`; `isAuthenticated`/`isAdmin` getters (lines 27–28) back `middleware/admin.ts`. Every fetch in this story attaches this header the same way Story 12's pages do.
6. [`../admin-products-crud/12-story-admin-products-list-create-and-edit.md`](../admin-products-crud/12-story-admin-products-list-create-and-edit.md) — read in full. This is the sibling story whose admin-CRUD conventions this story mirrors: its task 3 (`window.confirm` delete flow, treating any non-2xx as an error to surface, though this story's `409` message IS distinguishable and should be shown verbatim — see Edge Cases), its task 4 (adding a form schema to `app/utils/validation.ts` alongside the existing ones), its tasks 5–6 (`reactive` form object with **string**-typed fields coerced to the right type only inside the Zod schema at submit time, `errors.value = {}` reset, `parsed.error` → `fieldErrors()`, `loading.value` guarding `BaseButton`'s `loading` prop, `try/catch/finally` around the request, `toast.success`/`toast.error` from `vue-sonner`, `navigateTo(...)` on success), and its Context items 11–14 (`BaseButton`, `BaseInput`, `BaseForm`, `login.vue`'s `onSubmit` shape) for the exact base-component contracts reused here (see items 7–10 below, verified independently in this run).
7. `app/components/base/Button.vue` — all 63 lines. `<BaseButton>` props: `type` (`"button" | "submit" | "reset"`, default `"button"`, lines 4/11), `variant` (`"primary" | "secondary" | "ghost"`, default `"primary"`, lines 5/12), `block` (line 6/13), `loading` (line 7/14) and `disabled` (line 8/15) both feed `:disabled="disabled || loading"` (line 30), and `loading` additionally renders the spinner `<svg>` (lines 39–59). Reused unchanged for every button in this story (Add category/brand, Edit/Delete row actions, confirm/cancel are a native `window.confirm`, both forms' submit buttons).
8. `app/components/base/Input.vue` — all 108 lines. `<BaseInput>` props: `modelValue` (default `""`), `label`, `type` (default `"text"`), `placeholder`, `error`, `autocomplete`, `required` (lines 2–13). The `error` prop renders a red-ring input state (lines 50–52) plus a `<p :id="`${id}-error`" class="text-xs text-red-600">` (lines 104–106) — this is the exact per-field error contract this story's `name` field uses with `fieldErrors()`. `modelValue`/`update:modelValue` (line 15, line 54–56) is a plain string — no numeric coercion happens in this component, matching Story 12's note (its Context item 12) that any type coercion happens only in the Zod schema at submit time.
9. `app/components/base/Form.vue` — all 9 lines. `<BaseForm>` wraps a `<form novalidate @submit.prevent="emit('submit')">` (line 6). Reused unchanged around both the category and brand create/edit forms.
10. `app/pages/auth/login.vue` — all 96 lines, specifically `onSubmit` (lines 14–41): `errors.value = {}` reset (line 15), `schema.safeParse(form)` (line 17), `fieldErrors(parsed.error)` on failure (line 19), `loading.value = true` before the request (line 23), `try { await $fetch(...) ... } catch (e: any) { toast.error(e?.data?.message ?? ...) } finally { loading.value = false }` (lines 24–40). All four new form pages (category new/edit, brand new/edit) in this story follow this exact shape, with a JSON body (not `FormData` — unlike Story 12's product forms, categories/brands carry no file upload).
11. `app/composables/useProducts.ts` — all 29 lines. Not reused directly (this story is about categories/brands, not products), but read to confirm the precedent this story's own composable choice follows: Story 12 (its Context item 8) established that a page-specific fetch with an `Authorization` header should be built with `useFetch`/`$fetch` called directly inside the page rather than wrapped in a generic composable, since the existing `useProducts` composable takes no auth parameter. This story applies the same "no new shared composable" decision for categories/brands — each of the six new pages calls `useFetch`/`$fetch` directly.
12. `.squad/plans/00-index.md` — 15 lines. Confirms the next global sequence number is `13` (highest existing entry is `12`, `admin-products-crud`).

---

## Frontend Tasks

### 1 — Add sidebar nav items

**File: `app/layouts/admin.vue`**

After the existing Products `<NuxtLink>` (lines 18–24), add two more entries, copying its `class`/`active-class` attributes exactly:

```vue
<NuxtLink
  to="/admin/categories"
  class="px-3 py-2 rounded hover:bg-gray-800"
  active-class="bg-primary text-white hover:bg-primary-600"
>
  Categories
</NuxtLink>
<NuxtLink
  to="/admin/brands"
  class="px-3 py-2 rounded hover:bg-gray-800"
  active-class="bg-primary text-white hover:bg-primary-600"
>
  Brands
</NuxtLink>
```

`active-class` (not `exact-active-class`) keeps the link highlighted on `/admin/categories/new` and `/admin/categories/{id}/edit`, matching the existing Products link's documented behavior (line 17's comment).

### 2 — Add the shared category/brand form Zod schema

**File: `app/utils/validation.ts`**

Add, alongside the existing `loginSchema`/`registerSchema`/`fieldErrors` (lines 1–34):

```ts
export const nameFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Name is required")
    .max(100, "Name must be 100 characters or fewer"),
});

export type NameFormInput = z.infer<typeof nameFormSchema>;
```

One shared schema for both Category and Brand forms — both entities have the identical single `{ name }` shape per the backend plan's `CategoryRequest`/`BrandRequest` DTOs (`../../../../OnlineStore.API/.squad/plans/categories-and-brands/08-story-categories-and-brands-crud.md` lines 63–66 and 88–91: `[Required, StringLength(100, MinimumLength = 1)] string Name` on both). The `max(100, ...)` matches the backend's `StringLength(100, ...)` exactly; the `.trim()` matches the backend's own `request.Name.Trim()` (lines 146, 180, 287, 321 of the backend plan) so the client never rejects a name that only has leading/trailing whitespace around otherwise-valid content, while the backend's own `IsNullOrWhiteSpace` check after trim (documented in the backend plan's Edge Cases) remains the authoritative guard against an all-whitespace name reaching the database.

### 3 — Create the categories list page

**Create file: `app/pages/admin/categories/index.vue`**

```vue
<script setup lang="ts">
definePageMeta({ layout: "admin", middleware: "admin" });

import { toast } from "vue-sonner";

interface CategoryDto {
  id: number;
  name: string;
}

const apiBase = useApi();
const auth = useAuthStore();

const { data, pending, error, refresh } = await useFetch<CategoryDto[]>(
  `${apiBase}/categories`,
  {
    key: "admin-categories-list",
    headers: { Authorization: `Bearer ${auth.token}` },
    server: false,
    default: (): CategoryDto[] => [],
  },
);

async function onDelete(category: CategoryDto) {
  if (!window.confirm(`Delete "${category.name}"? This cannot be undone.`)) {
    return;
  }
  try {
    await $fetch(`${apiBase}/categories/${category.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${auth.token}` },
    });
    toast.success(`"${category.name}" deleted.`);
    await refresh();
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not delete category.");
  }
}
</script>
```

Template: a heading "Categories" plus an "Add category" `<NuxtLink to="/admin/categories/new"><BaseButton>Add category</BaseButton></NuxtLink>`; an `error`-only bordered notice with a `BaseButton` "Retry" calling `refresh()` (`v-if="error"`, matching the pattern already used in `app/pages/index.vue` and Story 12's list page); while `pending`, a simple skeleton (e.g. a few `animate-pulse` rows); once loaded, if `data.length === 0` an empty-state message ("No categories yet."); otherwise a table with one row per category — `name` cell, an Edit `<NuxtLink :to="`/admin/categories/${category.id}/edit`">Edit</NuxtLink>`, and a Delete `<button @click="onDelete(category)">Delete</button>` (wrapped in `<BaseButton variant="secondary">`/similar, no `disabled` attribute — unlike Story 12's product table, there is no "inert on purpose" placeholder to remove here since this page is new).

### 4 — Create the "new category" page

**Create file: `app/pages/admin/categories/new.vue`**

```vue
<script setup lang="ts">
definePageMeta({ layout: "admin", middleware: "admin" });

import { toast } from "vue-sonner";
import { nameFormSchema, fieldErrors } from "~/utils/validation";

const apiBase = useApi();
const auth = useAuthStore();

const form = reactive({ name: "" });
const errors = ref<Record<string, string>>({});
const loading = ref(false);

async function onSubmit() {
  errors.value = {};

  const parsed = nameFormSchema.safeParse(form);
  if (!parsed.success) {
    errors.value = fieldErrors(parsed.error);
    return;
  }

  loading.value = true;
  try {
    await $fetch(`${apiBase}/categories`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.token}` },
      body: parsed.data,
    });
    toast.success(`"${parsed.data.name}" created.`);
    await navigateTo("/admin/categories");
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Could not create category.");
  } finally {
    loading.value = false;
  }
}
</script>
```

Template: `<BaseForm @submit="onSubmit">` wrapping `<BaseInput v-model="form.name" label="Name" :error="errors.name" />` and `<BaseButton type="submit" :loading="loading">Create category</BaseButton>`, matching `login.vue`'s structure (Context item 10). No `Content-Type` header is set — `$fetch`'s default JSON body handling applies since `body` here is a plain object, unlike Story 12's `FormData` case.

### 5 — Create the "edit category" page

**Create file: `app/pages/admin/categories/[id]/edit.vue`**

Same shape as `new.vue` (task 4), with these differences:

- No `GET /api/categories/{id}` endpoint exists in the backend plan (confirmed: the backend plan's Story Goal lists only `List`, `Create`, `Update`, `Delete` for `/api/categories`, and its `CreatedAtAction(nameof(List), ...)` comment at lines 233 explicitly notes "there is no `GetById` action for categories"). Fetch the full list via `useFetch<CategoryDto[]>(`${apiBase}/categories`, { headers: { Authorization: ... }, server: false })` and find the entry whose `id` matches `Number(route.params.id)`.
- A non-numeric `id` route param, or no matching entry found in the fetched list, renders a small "Category not found" notice with a `<NuxtLink to="/admin/categories">` link back to the list — do not render an empty/blank form in that case (mirrors the not-found pattern in `.squad/plans/orders-history/11-story-customer-orders-history.md` lines 145–154, 173, adapted here for a client-side "not found in the fetched list" check instead of a fetch-level 404, since there is no per-id endpoint to 404 against).
- Once found, populate `form.name = match.name` before rendering the form.
- `onSubmit` calls `PUT` instead of `POST`: `$fetch(`${apiBase}/categories/${route.params.id}`, { method: "PUT", headers: { Authorization: ... }, body: parsed.data })`. On success: `toast.success(...)` + `navigateTo("/admin/categories")`. On failure: `toast.error(e?.data?.message ?? "Could not update category.")`, same pattern as `new.vue`.

### 6 — Mirror all three pages for Brands

**Create file: `app/pages/admin/brands/index.vue`** — identical to task 3's `app/pages/admin/categories/index.vue`, with every `category`/`categories`/`Category`/`Categories` swapped for `brand`/`brands`/`Brand`/`Brands` (`CategoryDto` → `BrandDto`, `${apiBase}/categories` → `${apiBase}/brands`, "Add category" → "Add brand", link targets `/admin/brands/new` and `/admin/brands/${brand.id}/edit`).

**Create file: `app/pages/admin/brands/new.vue`** — identical to task 4, swapping `category`/`Category` for `brand`/`Brand` throughout (`${apiBase}/brands`, `/admin/brands`, "Create brand").

**Create file: `app/pages/admin/brands/[id]/edit.vue`** — identical to task 5, same swap. The backend plan confirms the identical "no `GetById` for Brand either" shape (its task 4, mirroring task 3's `CreatedAtAction(nameof(List), ...)` pattern for `BrandsController`), so the edit page uses the same "fetch the full list, find by id client-side" approach.

The `nameFormSchema` from task 2 is reused as-is for all four category/brand forms — no separate `brandFormSchema` is needed since the shape is identical.

### 7 — Document the follow-up: upgrade Story 12's numeric id inputs to `<select>`

**File: `../admin-products-crud/12-story-admin-products-list-create-and-edit.md`** (do not edit this file — it is a completed sibling story; this task is a note for whoever picks up the follow-up, recorded here since this story is what makes the follow-up actionable)

Once this story ships and `GET /api/categories`/`GET /api/brands` are live, Story 12's "Optional Follow-up — Category/Brand `<select>` Upgrade" section (its lines 66–73) becomes actionable: replace the plain numeric `CategoryId`/`BrandId` `<BaseInput type="number">` fields on `app/pages/admin/products/new.vue` and `app/pages/admin/products/[id]/edit.vue` with `<select>` elements populated from `GET /api/categories`/`GET /api/brands` (the same `CategoryDto[]`/`BrandDto[]` shape this story's list pages already fetch), keeping the same form field names (`categoryId`, `brandId`) and the same Zod validation shape (`z.coerce.number().int().min(1, ...)` in `productFormSchema`). This is **not** part of this story's own Done Criteria — it is a follow-up for Story 12's forms, tracked here because this story is the one that makes the prerequisite endpoints exist.

---

## Edge Cases & Failure Modes

- **Duplicate name on create or update:** the backend returns `400 { message: "A category with this name already exists" }` (or the `brand` equivalent) as a case-insensitive check via `EF.Functions.ILike` (backend plan lines 152, 186, 293, 327) — this is a **top-level message**, not a field-level Zod error, since the client-side schema (task 2) only checks non-empty/length, not uniqueness (an existence check requires a request the client cannot make synchronously). Both `new.vue` and `[id]/edit.vue`'s `catch` blocks surface this via `toast.error(e?.data?.message ?? ...)` — do not attempt to map it into `errors.name`.
- **Delete blocked by a referencing Product (409):** the backend returns `409 { message: "Cannot delete category: it is still used by one or more products" }` (backend plan lines 213–218; brand equivalent lines 354–357) — a clean, distinguishable status code, unlike Story 12's product-delete case where a referencing `OrderItem` produces an indistinguishable generic `500`. Task 3/6's `onDelete` must surface this `409`'s `message` verbatim via `toast.error(e?.data?.message ?? "Could not delete category.")` — it does **not** need Story 12's "treat every non-2xx as the same suggested-action case" workaround, because this backend endpoint's `409` is purpose-built and distinguishable from a genuine `500`.
- **Empty/whitespace-only name:** the shared `nameFormSchema` (task 2) rejects an empty string client-side via `.min(1, ...)` after `.trim()`, but the backend's own `IsNullOrWhiteSpace` check after its own `Trim()` (backend plan lines 147–150, 181–184, 288–291, 322–325) is the authoritative guard — client-side validation here is a fail-fast convenience, not a substitute.
- **Editing a category/brand whose id has no match in the fetched list, or a non-numeric `id` in the URL:** task 5/6's edit pages render the "not found" branch (a bordered notice + link back to the list) instead of an empty form — see task 5's not-found handling, adapted from `.squad/plans/orders-history/11-story-customer-orders-history.md`'s `isValidId`/not-found pattern (its lines 145–154, 173) for this story's "no per-id endpoint" case.
- **Renaming a category/brand to its own current name (unchanged case):** the backend's update duplicate-check excludes the row being updated (`c.Id != id && EF.Functions.ILike(...)`, backend plan lines 186, 327) — this is a backend-side guarantee, not something the frontend needs to special-case; submitting the unchanged name succeeds with a `200`.
- **List page's `GET /api/categories`/`GET /api/brands` fails or returns an empty array:** `error` (fetch failure) and `data.length === 0` (empty list, not an error) are rendered as two distinct states in task 3/6 — a bordered "Retry" notice for the former, "No categories/brands yet." for the latter, matching the three-state pattern already used across this codebase (e.g. `app/pages/index.vue`).
- **SSR vs. self-signed dev certificate:** identical to every prior story — all `useFetch`/`$fetch` calls in this story's six pages use `server: false` (or run from a client-triggered handler) to avoid the Nitro server making the request during SSR, where Node would reject the API's self-signed HTTPS certificate.
- **Double-submit on create/edit:** `BaseButton`'s `:disabled="disabled || loading"` (`app/components/base/Button.vue` line 30) combined with `loading.value = true` set before the `$fetch` call and reset only in `finally` (tasks 4–5) prevents a second click from firing a second request while the first is in flight — same pattern as Story 12's forms.

---

## Test Plan

**No test runner is configured** in this project — confirmed by Stories 05, 07–12 (`package.json` has `build`/`dev`/`generate`/`preview`/`postinstall` only, no Vitest dependency), still true as of this story. Verification is manual (see Verification Steps). If Vitest + `@nuxt/test-utils` is introduced later, these are the tests to add:

1. **Unit — `nameFormSchema`** (`app/utils/validation.ts`): a valid name parses and is trimmed; an empty or whitespace-only name and a name over 100 characters each produce the exact messages from task 2, retrievable via `fieldErrors()`.
2. **Component — `app/pages/admin/categories/index.vue`** with the list `useFetch` mocked: `pending` renders a skeleton; `error` renders the bordered notice with a working Retry; `data: []` renders "No categories yet."; a populated list renders one row per category with working Edit links and a Delete button per row.
3. **Component — delete flow**: confirming the dialog and a `204` response refreshes the list and shows a success toast; confirming and a `409` response shows the backend's exact `message` in the error toast; declining the dialog makes no network call.
4. **Component — `app/pages/admin/categories/new.vue`**: submitting with an empty name shows the `fieldErrors()` message and makes no network call; a valid submit calls `POST /api/categories` with `{ name }` JSON body and the `Authorization` header; a `400` duplicate-name response toasts that exact message; success toasts and redirects to `/admin/categories`.
5. **Component — `app/pages/admin/categories/[id]/edit.vue`**: pre-fills `form.name` from the matching entry in a mocked `GET /api/categories` response; an `id` with no match renders the not-found branch; a valid submit calls `PUT /api/categories/{id}` with `{ name }` JSON body.
6. **Component — Brand pages**: mirror tests 2–5 for `app/pages/admin/brands/*`, asserting the identical behavior against `/api/brands`.

---

## Verification Steps

1. **Frontend builds:** in `online-store-frontend/`, run `pnpm build`. Must succeed — a wrong auto-import name for `<BaseButton>`/`<BaseInput>`/`<BaseForm>`, a bad `nameFormSchema` reference, or a missing `definePageMeta` fails here.
2. **Backend endpoints live:** confirm `GET https://localhost:7225/api/categories` and `GET https://localhost:7225/api/brands` both return `200` (with or without a token) before testing any frontend page — per the hard-block in Prerequisites.
3. **Sidebar:** log in as admin, open `/admin/dashboard` — the sidebar shows "Categories" and "Brands" nav items alongside "Dashboard"/"Products"; clicking each navigates to the respective list page and stays highlighted on `/admin/categories/new`.
4. **Categories list:** `/admin/categories` shows all seeded categories in a table; the network tab shows a `GET` to `/api/categories` with an `Authorization: Bearer <token>` header.
5. **Create category, happy path:** `/admin/categories/new`, enter a unique name, submit — `POST /api/categories` fires with `{ name }` JSON body; success toast + redirect to `/admin/categories`; the new category appears in the list.
6. **Create category, duplicate name:** submit a name that already exists (any case) — a toast shows the backend's exact "A category with this name already exists" message; no redirect.
7. **Create category, empty name:** submit with an empty name field — the `fieldErrors()` message renders inline; no network request fires.
8. **Edit category:** click "Edit" on a row — the form pre-fills with the current name; change it and submit — `PUT /api/categories/{id}` fires with the new `{ name }`; success toast + redirect; the list reflects the new name.
9. **Delete category, success path:** delete a category not referenced by any product, confirm the dialog — the row disappears and a success toast appears.
10. **Delete category, blocked path:** delete a category referenced by an existing product, confirm the dialog — a toast shows the backend's exact `409` message ("Cannot delete category: it is still used by one or more products"), not a generic error; the row remains in the list.
11. **Repeat steps 4–10 for `/admin/brands`** with the equivalent brand payloads and messages.
12. **Submit-loading state:** on a throttled connection, click submit on either form and confirm the button shows its loading spinner and is unclickable until the request resolves.
13. **Backend unchanged:** `dotnet build` in `OnlineStore.API/` — expected unchanged, this story touches no C#.
14. **Responsive:** at 375 px the list tables and both forms stack without horizontal overflow; at 1280 px all pages read comfortably within the admin layout's content width.

---

## Done Criteria

- [ ] `app/pages/admin/categories/index.vue` fetches `GET /api/categories` with the admin bearer token attached; renders a table of name + Edit/Delete actions; has an "Add category" link to `/admin/categories/new`.
- [ ] `app/pages/admin/categories/new.vue` exists: a single-field (`name`) Zod-validated form, submits JSON via `POST /api/categories`, toasts + redirects on success, surfaces the backend's duplicate-name `400` message verbatim on failure.
- [ ] `app/pages/admin/categories/[id]/edit.vue` exists: the same form pre-filled by matching the route id against a fetched `GET /api/categories` list (no per-id endpoint exists), submits `PUT /api/categories/{id}`, and renders a not-found branch for an unmatched id.
- [ ] Delete on the categories list is behind a `window.confirm` dialog; a successful delete refreshes the list and toasts; a `409` (category in use) surfaces the backend's exact message.
- [ ] `app/layouts/admin.vue`'s sidebar has a "Categories" nav item.
- [ ] The same four behaviors (list, new, edit, delete) are mirrored for Brands at `app/pages/admin/brands/*` against `/api/brands`, with a "Brands" sidebar item.
- [ ] Both forms show per-field validation errors via `fieldErrors()` + `BaseInput`'s `error` prop, and a submit-loading state via `BaseButton`'s `loading` prop that blocks double-submits.
- [ ] The follow-up to upgrade Story 12's `categoryId`/`brandId` numeric inputs to `<select>` dropdowns is documented (task 7) but not implemented by this story.
- [ ] `pnpm build` succeeds; no arbitrary `bg-[#…]`/`text-[#…]` or raw `px` spacing introduced.

---

**STOP HERE. Report to the user and wait for confirmation before proceeding to any follow-up story.**
