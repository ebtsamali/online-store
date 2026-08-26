# Story 15 — Admin Add/Edit Product: Category and Brand as Select Inputs

Replaces the plain numeric "Category ID" / "Brand ID" `<BaseInput type="number">` fields on the admin add-product and edit-product forms with `<select>` dropdowns populated from the now-existing `GET /api/categories` and `GET /api/brands` endpoints, closing out the follow-up explicitly deferred by Story 12.

---

## Prerequisites

- [Story 12 — Admin Products List, Create and Edit](./12-story-admin-products-list-create-and-edit.md) completed: `app/pages/admin/products/new.vue` and `app/pages/admin/products/[id]/edit.vue` exist with the numeric category/brand inputs this story replaces.
- The backend dependency Story 12 flagged as not-yet-landed ("Categories and Brands CRUD") **has since landed**: `OnlineStore.API/Controllers/CategoriesController.cs` and `OnlineStore.API/Controllers/BrandsController.cs` both exist, each with an `[AllowAnonymous] [HttpGet]` `List()` action (lines 22–39 in both files) returning `CategoryDto`/`BrandDto` — `public record CategoryDto(int Id, string Name);` (`OnlineStore.API/Dtos/CategoryDto.cs` line 3) and `public record BrandDto(int Id, string Name);` (`OnlineStore.API/Dtos/BrandDto.cs` line 3), serialized camelCase (`{ id, name }`). **No backend change is required or permitted in this story.**
- [Story 13 — Admin Categories List, Create and Edit](../admin-categories-crud/13-story-admin-categories-list-create-and-edit.md) already ships `app/pages/admin/categories/*` and `app/pages/admin/brands/*`, each with its own inline `GET /api/categories` / `GET /api/brands` fetch — read as precedent for the response shape, **not** modified or reused directly by this story (see Out of Scope).
- The API must be running (`https://localhost:7225/api`, CORS allows only `http://localhost:3000`) — same constraint as every prior story touching these pages.

---

## Story Goal

1. `app/pages/admin/products/new.vue`: the "Category ID" numeric input becomes a `<select>` populated from `GET /api/categories`, labelled "Category", showing each category's `name` with its `id` as the underlying value. Same treatment for "Brand ID" → "Brand" from `GET /api/brands`.
2. `app/pages/admin/products/[id]/edit.vue`: the same two fields get the same treatment, and the select correctly pre-selects the product's existing category/brand once both the product fetch and the categories/brands fetch resolve.
3. A new shared `BaseSelect` component (`app/components/base/Select.vue`) provides the `label`/`error` contract already used by `BaseInput`, so both forms render the new fields with the same visual language (focus ring, error state) as the surrounding `BaseInput` fields in the same form — not the older gray-bordered style used by the plain `<textarea>` already in these files.
4. Two new composables, `useCategories()` and `useBrands()` (`app/composables/useCategories.ts`, `app/composables/useBrands.ts`), fetch the option lists once per page and are shared between `new.vue` and `edit.vue`, avoiding a third copy of the inline-fetch pattern already duplicated across `app/pages/admin/categories/index.vue` and `app/pages/admin/brands/index.vue`.
5. **No change to submit behavior**: `productFormSchema` (`app/utils/validation.ts`), the `FormData` field names (`CategoryId`, `BrandId`), and the numeric-id-as-string values sent to the backend are all unchanged — only the input control changes.

**Not in scope:** any change to `app/pages/admin/categories/*` or `app/pages/admin/brands/*`; any backend change; an inline "create new category/brand" affordance on the product form; multi-select, search, or autocomplete UX.

---

## Context — Read These Files First

1. `app/pages/admin/products/new.vue` — all 138 lines. The Category ID / Brand ID `<BaseInput type="number">` block this story replaces is lines 110–121. The reactive `form` object (lines 10–17) keeps `categoryId`/`brandId` as **strings**, matching every other numeric-looking form field in this codebase (coerced only by Zod at submit, per the existing `type="number"` fields on the same form). `onSubmit` (lines 47–83) builds a `FormData` and appends `String(parsed.data.categoryId)` / `String(parsed.data.brandId)` (lines 65–66) — unchanged by this story.
2. `app/pages/admin/products/[id]/edit.vue` — all 192 lines. The matching numeric-input block is lines 157–168. The `watch(product, ...)` block (lines 40–53) sets `form.categoryId = String(p.categoryId)` / `form.brandId = String(p.brandId)` (lines 48–49) from the fetched `ProductDetail` — this story does not touch that watcher; a `<select v-model="form.categoryId">` bound to the same reactive string field pre-selects correctly as long as the rendered `<option>` values are also strings, since native `<select>` matching is string-based.
3. `app/components/base/Input.vue` — all 108 lines. The exact prop contract and styling this story's new `BaseSelect` must mirror: `modelValue`/`label`/`error`/`required` props (lines 2–13), `defineEmits<{ "update:modelValue": [value: string] }>()` (line 15), SSR-stable `useId()` for the `<label for>` association (line 18), and the two-state class list at lines 46–53 — `bg-[#e8edf9] focus:border-[#1b3a6b] focus:ring-[#1b3a6b]/40` normally, `bg-red-50 ring-1 ring-red-400 focus:ring-red-500` when `error` is set — plus the `<p :id="`${id}-error`" class="text-xs text-red-600">` error line (lines 104–106). **Do not** match the older `border-gray-300 focus:border-primary` styling used by the plain `<textarea>` in the same product forms (`new.vue` lines 93–100, `edit.vue` lines 140–147) — that is a pre-existing, unrelated visual inconsistency in these files; the new selects sit directly beside `BaseInput` fields (Name, Price, Stock) and must match those, not the textarea.
4. `app/composables/useProducts.ts` — all 57 lines. The composable pattern this story's two new composables follow: a typed interface (`ProductListItem`, lines 1–8), a `useFetch<T>` call with `server: false` and a `default: () => [...]` fallback (`useProducts`, lines 32–47) — the `server: false` / self-signed-cert comment (lines 42–44) applies identically to the new composables.
5. `OnlineStore.API/Controllers/CategoriesController.cs` — all 130 lines, specifically `List()` (lines 22–39): `[AllowAnonymous]` (line 22, overriding the controller's class-level `[Authorize(Roles = "admin")]` at line 12) — **no bearer token is required** to call this endpoint. `.OrderBy(c => c.Name)` (line 29) means the response already arrives alphabetically sorted; the frontend does not need to re-sort. Generic `catch { return StatusCode(500, ...) }` (lines 35–38) — same shape as every other list endpoint in this codebase, surfaced via `useFetch`'s `error` ref.
6. `OnlineStore.API/Controllers/BrandsController.cs` — all 129 lines. Identical shape to `CategoriesController.cs`: `List()` at lines 22–39, `[AllowAnonymous]` at line 22, `.OrderBy(b => b.Name)` at line 29.
7. `OnlineStore.API/Dtos/CategoryDto.cs` (3 lines) and `OnlineStore.API/Dtos/BrandDto.cs` (3 lines) — `record CategoryDto(int Id, string Name)` / `record BrandDto(int Id, string Name)`, camelCase over the wire: `{ id: number, name: string }`.
8. `app/utils/validation.ts` — all 54 lines. `productFormSchema` (lines 20–27) already validates `categoryId`/`brandId` via `z.coerce.number(...).int().min(1, "Category id is required.")` / `"Brand id is required."` — **unchanged by this story**. An unselected `<select>` submits an empty string `""`, and `z.coerce.number()` coerces `""` to `0` (same as `Number("")`), which already fails the existing `.min(1, ...)` check with the existing message — no schema change needed to preserve the "required" error behavior acceptance criteria call for.
9. `app/pages/admin/categories/index.vue` — all 102 lines, specifically lines 6–22: the inline `CategoryDto` interface and `useFetch<CategoryDto[]>` call this story's `useCategories()` composable formalizes into a reusable composable, without modifying this file (Out of Scope).
10. `app/composables/useApi.ts` — confirm the exported `useApi()` signature (already used unchanged as `${apiBase}/...` in every other composable/page in this codebase); the two new composables call it the same way.

---

## Frontend Tasks

### 1 — Add the shared `BaseSelect` component

**Create file: `app/components/base/Select.vue`**

```vue
<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    modelValue?: string;
    label?: string;
    error?: string;
    options: { value: string; label: string }[];
    placeholder?: string;
    required?: boolean;
  }>(),
  { modelValue: "", required: false, placeholder: "Select..." },
);

defineEmits<{ "update:modelValue": [value: string] }>();

// useId() is SSR-stable, so the <label for> association survives hydration.
const id = useId();
</script>

<template>
  <div class="space-y-1.5">
    <label v-if="label" :for="id" class="block text-sm font-medium text-[#1b3a6b]">
      {{ label }}
    </label>

    <select
      :id="id"
      :value="modelValue"
      :required="required"
      :aria-invalid="!!error"
      :aria-describedby="error ? `${id}-error` : undefined"
      :class="[
        'w-full rounded-lg px-4 py-3 text-sm text-gray-900',
        'border border-transparent transition focus:outline-none focus:ring-2',
        error
          ? 'bg-red-50 ring-1 ring-red-400 focus:ring-red-500'
          : 'bg-[#e8edf9] focus:border-[#1b3a6b] focus:ring-[#1b3a6b]/40',
      ]"
      @change="$emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
    >
      <option value="" disabled>{{ placeholder }}</option>
      <option v-for="opt in options" :key="opt.value" :value="opt.value">
        {{ opt.label }}
      </option>
    </select>

    <p v-if="error" :id="`${id}-error`" class="text-xs text-red-600">
      {{ error }}
    </p>
  </div>
</template>
```

This lives under `app/components/base/`, so Nuxt's auto-import exposes it globally as `<BaseSelect>`, matching `<BaseInput>`/`<BaseButton>`/`<BaseForm>` — no explicit import needed in either page (Context item 3 confirms this convention).

### 2 — Add `useCategories()` and `useBrands()` composables

**Create file: `app/composables/useCategories.ts`**

```ts
export interface CategoryOption {
  id: number;
  name: string;
}

// Client-only: the .NET dev server's self-signed HTTPS cert is rejected by
// Nitro's SSR fetch (same reason as useProducts.ts).
export const useCategories = () => {
  const apiBase = useApi();
  return useFetch<CategoryOption[]>(`${apiBase}/categories`, {
    key: "categories-list",
    server: false,
    default: (): CategoryOption[] => [],
  });
};
```

**Create file: `app/composables/useBrands.ts`**

```ts
export interface BrandOption {
  id: number;
  name: string;
}

export const useBrands = () => {
  const apiBase = useApi();
  return useFetch<BrandOption[]>(`${apiBase}/brands`, {
    key: "brands-list",
    server: false,
    default: (): BrandOption[] => [],
  });
};
```

No `Authorization` header on either call — both endpoints are `[AllowAnonymous]` (Context items 5–6), so the header is not required for populating the dropdowns.

### 3 — Wire the selects into the "new product" page

**File: `app/pages/admin/products/new.vue`**

Add, alongside the existing `form`/`errors`/`loading` refs (lines 10–21):

```ts
const { data: categories, error: categoriesError } = useCategories();
const { data: brands, error: brandsError } = useBrands();

const categoryOptions = computed(
  () => categories.value?.map((c) => ({ value: String(c.id), label: c.name })) ?? [],
);
const brandOptions = computed(
  () => brands.value?.map((b) => ({ value: String(b.id), label: b.name })) ?? [],
);
```

Replace the `<BaseInput v-model="form.categoryId" ...>` / `<BaseInput v-model="form.brandId" ...>` block (lines 110–121) with:

```vue
<BaseSelect
  v-model="form.categoryId"
  label="Category"
  placeholder="Select a category"
  :options="categoryOptions"
  :error="errors.categoryId"
/>
<p v-if="categoriesError" class="text-xs text-red-600">Could not load categories.</p>

<BaseSelect
  v-model="form.brandId"
  label="Brand"
  placeholder="Select a brand"
  :options="brandOptions"
  :error="errors.brandId"
/>
<p v-if="brandsError" class="text-xs text-red-600">Could not load brands.</p>
```

The field-level Zod error (`errors.categoryId`/`errors.brandId`, unchanged from today) and the fetch-level error (`categoriesError`/`brandsError`, new) are rendered separately — a validation error means "you must pick one," a fetch error means "the list itself failed to load," and conflating them into one string would lose that distinction.

### 4 — Wire the selects into the "edit product" page

**File: `app/pages/admin/products/[id]/edit.vue`**

Same two additions as task 3 (`useCategories()`/`useBrands()` calls, `categoryOptions`/`brandOptions` computed refs) added alongside the existing `form`/`isActive`/`errors` refs (lines 26–38).

Replace the `<BaseInput v-model="form.categoryId" ...>` / `<BaseInput v-model="form.brandId" ...>` block (lines 157–168) with the same `<BaseSelect>` + fetch-error-`<p>` pairs as task 3. No change to the `watch(product, ...)` block (lines 40–53) — it already assigns `form.categoryId = String(p.categoryId)` (line 48) and `form.brandId = String(p.brandId)` (line 49), which the new `<select v-model="form.categoryId">` matches against its string `<option value>`s regardless of whether `categoryOptions`/`brandOptions` have finished loading yet (Vue re-renders the `<option>` list reactively once the fetch resolves; the bound value itself does not need to change).

---

## Edge Cases & Failure Modes

- **Categories or brands fetch fails** (network error, 500): `categoriesError`/`brandsError` (tasks 3–4) is truthy, `categoryOptions`/`brandOptions` stays `[]` (the `default: () => []` in the composable, task 2), so the `<select>` renders only its disabled placeholder option. The form still renders and every other field remains fully usable; submitting fails client-side with the existing `"Category id is required."` message (Context item 8) since the coerced value is `0` — the user sees the inline "Could not load categories." message explaining why nothing can be selected, rather than a silently broken control.
- **Zero categories or brands exist** (nothing seeded yet): same visible result as a fetch failure (placeholder-only select) but with no `categoriesError`/`brandsError` set — this is expected, not a bug this story fixes; creating categories/brands from the product form is explicitly out of scope.
- **Editing a product whose `categoryId`/`brandId` no longer matches any fetched option** (e.g. the category was deleted after this product was assigned): the `<select>` shows no option selected (falls back to the disabled placeholder) rather than crashing — this should not normally occur, since `CategoriesController.Delete`/`BrandsController.Delete` both pre-check for referencing products and return `409 Conflict` instead of deleting (`CategoriesController.cs` lines 113–118, `BrandsController.cs` lines 113–116), but the select must not error out if it does.
- **`edit.vue`'s product fetch and the categories/brands fetch resolve in either order**: both are independent `useFetch` calls with no shared `await` between them (Context item 2, Frontend Task 4) — whichever resolves first simply updates its own reactive ref; Vue's reactivity re-renders the `<select>`'s options and re-evaluates the bound value's match on every relevant change, so there is no race that leaves the form permanently blank or mismatched.
- **An unselected `<select>` submits `""`, not `undefined` or `null`**: confirmed compatible with the existing `productFormSchema` (Context item 8) without any schema change — `z.coerce.number()` on `""` produces `0`, which the existing `.min(1, ...)` check already rejects with the existing message.

---

## Test Plan

**No test runner is configured** in this project — confirmed unchanged since Story 12 (`package.json` has `build`/`dev`/`generate`/`preview`/`postinstall` only). Verification is manual (see Verification Steps). If Vitest + `@nuxt/test-utils` is introduced later, these are the tests to add:

1. **Unit — `BaseSelect`** (`app/components/base/Select.vue`): renders one `<option>` per entry in `options` plus the disabled placeholder; emits `update:modelValue` with the selected option's `value` on change; applies the error-state classes and renders the `<p>` error text when `error` is set.
2. **Unit — `useCategories()` / `useBrands()`**: with the underlying `useFetch` mocked, a successful response populates `data`; a failed response leaves `data` as `[]` (the composable's `default`) and sets `error`.
3. **Component — `app/pages/admin/products/new.vue`**: with `useCategories`/`useBrands` mocked to a fixed list, the Category/Brand selects render those names as options; selecting one and submitting builds a `FormData` whose `CategoryId`/`BrandId` match the selected option's numeric id (same assertion Story 12's own test plan already specifies for this page, unchanged in shape); submitting with no category selected shows `"Category id is required."` under the field and makes no network call; mocking `useCategories` to an error state renders "Could not load categories." and leaves the rest of the form interactive.
4. **Component — `app/pages/admin/products/[id]/edit.vue`**: given a mocked product with `categoryId: 3` and a mocked categories list containing `{ id: 3, name: "Electronics" }`, the Category select pre-selects "Electronics"; changing the selection and submitting sends the newly selected id in the `PUT` request's `FormData`.

---

## Verification Steps

1. **Frontend builds:** in `online-store-frontend/`, run `pnpm build`. Must succeed — a wrong auto-import name for `<BaseSelect>`, a bad `useCategories`/`useBrands` reference, or a template/type mismatch fails here.
2. **Backend unchanged:** `dotnet build` in `OnlineStore.API/` — expected unchanged, this story touches no C#.
3. **Create, selects populated:** with at least one category and one brand seeded (via `/admin/categories/new` and `/admin/brands/new`, or existing seed data), open `/admin/products/new` — Category and Brand render as `<select>` elements listing real names, not numeric ids; the network tab shows unauthenticated `GET` requests to `/api/categories` and `/api/brands`.
4. **Create, happy path:** select a category and a brand, fill the rest of the form, attach a valid image, submit — the network tab's `multipart/form-data` `POST /api/products` request carries `CategoryId`/`BrandId` matching the selected options' numeric ids; success toast + redirect, as before.
5. **Create, required validation:** submit with no category (or no brand) selected — `"Category id is required."` (or the brand equivalent) renders under the field, no network request fires.
6. **Edit, pre-selection:** open `/admin/products/{id}/edit` for a product with a known category/brand — the selects show that product's current category/brand name pre-selected, not the placeholder.
7. **Edit, happy path:** change the selected category/brand and submit — the `PUT` request's `FormData` reflects the newly selected ids; the product's category/brand shown on the products list updates accordingly (if the list surfaces names — otherwise confirm via `GET /api/products/{id}`).
8. **Fetch-failure fallback:** temporarily stop the API (or block the `/api/categories` request via devtools) and reload `/admin/products/new` — "Could not load categories." renders near the Category field, the rest of the form (Name, Price, Stock, Brand if unaffected, Image) remains usable, and submitting without a category still shows the existing required-field message rather than crashing.
9. **Responsive:** at 375 px and 1280 px, the new select fields stack and size the same way the existing `BaseInput` fields on the same forms do, with no horizontal overflow.

---

## Done Criteria

- [ ] `app/components/base/Select.vue` exists, matching `BaseInput`'s `label`/`error` prop contract and visual style (Context item 3).
- [ ] `app/composables/useCategories.ts` and `app/composables/useBrands.ts` exist, following the `useProducts.ts` fetch pattern (`server: false`, empty-array `default`).
- [ ] `app/pages/admin/products/new.vue`'s Category/Brand fields are `<BaseSelect>` populated from `useCategories()`/`useBrands()`, showing names as labels and ids as values.
- [ ] `app/pages/admin/products/[id]/edit.vue`'s Category/Brand fields are the same, and correctly pre-select the product's existing category/brand.
- [ ] `productFormSchema` is unchanged; an unselected category/brand still shows the existing "required" field error.
- [ ] A categories/brands fetch failure shows an inline message near the affected select without breaking the rest of the form.
- [ ] The submitted `FormData`'s `CategoryId`/`BrandId` values are unchanged in shape (numeric id as a string) from before this story.
- [ ] `app/pages/admin/categories/*` and `app/pages/admin/brands/*` are untouched by this story.
- [ ] `pnpm build` succeeds; no arbitrary `bg-[#…]`/`text-[#…]` beyond those already used by `BaseInput` (matched exactly, not introduced anew) or raw `px` spacing.
