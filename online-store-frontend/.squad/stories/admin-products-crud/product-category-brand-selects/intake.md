# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/admin-products-crud/product-category-brand-selects/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Admin Products List, Create and Edit
- **Feature slug (folder under `plans/`):** `admin-products-crud`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

```
Admin Add/Edit Product: Category and Brand as Select Inputs
```

---

## Description

```
Story 12 (`.squad/plans/admin-products-crud/12-story-admin-products-list-create-and-edit.md`) shipped the admin "Add product" (`app/pages/admin/products/new.vue`) and "Edit product" (`app/pages/admin/products/[id]/edit.vue`) pages with plain numeric `<BaseInput type="number">` fields for Category ID and Brand ID, because at the time the backend had no categories/brands endpoints to populate a dropdown from. That story explicitly documented this as a follow-up (its "Optional Follow-up — Category/Brand `<select>` Upgrade" section): once a categories/brands backend landed, replace the numeric id inputs with `<select>` elements populated from the API.

That backend has since landed — `OnlineStore.API/Controllers/CategoriesController.cs` and `OnlineStore.API/Controllers/BrandsController.cs` both exist, each exposing an `[AllowAnonymous] GET` list endpoint (`GET /api/categories`, `GET /api/brands`) returning `{ id, name }[]` (`CategoryDto`/`BrandDto`, both `record(int Id, string Name)` — camelCase over the wire). The admin-only categories/brands management UI is also already built (`app/pages/admin/categories/*`, `app/pages/admin/brands/*`), each fetching its own list inline with no shared composable.

This story replaces the Category ID / Brand ID numeric inputs on the add-product page (primary ask) — and, since the edit-product page shares the exact same form shape and was named in the original follow-up note, its two matching numeric inputs too — with `<select>` dropdowns populated from `GET /api/categories` and `GET /api/brands`, showing category/brand names instead of asking the admin to know a database id.
```

---

## Acceptance criteria

```
- [ ] `app/pages/admin/products/new.vue`: the "Category ID" `<BaseInput type="number">` is replaced with a `<select>` populated from `GET /api/categories`, showing each category's `name` as the option label and its `id` as the value; same treatment for "Brand ID" from `GET /api/brands`. Labels change to "Category" / "Brand" (drop "ID" now that the id is no longer typed by hand).
- [ ] `app/pages/admin/products/[id]/edit.vue`: the same two fields are replaced the same way, and the select's initial value is correctly pre-selected from the fetched product's existing `categoryId`/`brandId` once both the product and the categories/brands lists have loaded.
- [ ] Both selects keep working with the existing `productFormSchema` (`app/utils/validation.ts`) unchanged — `categoryId`/`brandId` still validate as a positive integer via `z.coerce.number()...min(1, ...)` — and keep showing that schema's field error via the same `error`/`fieldErrors()` plumbing `BaseInput` uses today, so a submit with no category/brand selected (empty value) still surfaces "Category id is required." / "Brand id is required." under the field.
- [ ] A category/brand fetch failure does not block the rest of the form from rendering or submitting; show a clear inline message (e.g. "Could not load categories.") near the affected select instead of a blank/broken control, with no fabricated fallback list of options.
- [ ] No behavior change to what gets submitted: the multipart `FormData` still sends `CategoryId`/`BrandId` as the numeric id string, exactly as today — only the input control changes, not the submit payload shape.
- [ ] `pnpm build` succeeds; no arbitrary `bg-[#…]`/`text-[#…]` or raw `px` spacing introduced (matching the existing Done Criteria bar for this feature).
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| *(none)* | |

None.

---

## Dependencies

- **Blocked by / related ids:** None — the previously-blocking backend dependency ("Categories and Brands CRUD") has already landed: `OnlineStore.API/Controllers/CategoriesController.cs`, `OnlineStore.API/Controllers/BrandsController.cs`, `OnlineStore.API/Dtos/CategoryDto.cs`, `OnlineStore.API/Dtos/BrandDto.cs` all exist and are unchanged by this story.
- **Depends on code areas or other stories:** `app/pages/admin/products/new.vue`, `app/pages/admin/products/[id]/edit.vue`, `app/utils/validation.ts` (`productFormSchema`, unchanged), `app/components/base/Input.vue` (styling reference for a new select — there is currently no `BaseSelect`/`<select>` component anywhere in `app/components/base/`), `app/pages/admin/categories/index.vue` and `app/pages/admin/brands/index.vue` (existing inline `GET /api/categories` / `GET /api/brands` fetch pattern to mirror — both currently duplicate an inline `CategoryDto`/`BrandDto` interface and a `useFetch` call rather than sharing a composable).

## Extra notes (optional)

- This is a small, targeted follow-up to Story 12, not a new feature — scope is intentionally limited to swapping the two input controls on the two existing product forms. It does not touch the categories/brands admin management pages themselves.
- Both `GET /api/categories` and `GET /api/brands` are `[AllowAnonymous]`, so the add-product page's dropdown fetch does not need the admin bearer token the way the product list/submit calls do — plan should decide whether to send it anyway for consistency with the rest of the page's fetches, or omit it since it's not required.
- Worth deciding during planning whether to extract a small shared `useCategories()`/`useBrands()` composable (used by both `new.vue` and `edit.vue`, and optionally by the existing `admin/categories`/`admin/brands` list pages too) versus keeping the fetch inline per page like the rest of this codebase currently does — either is consistent with existing precedent (the codebase has both patterns today).

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.
- `GET /api/categories` → `CategoryDto[]` = `{ id: number; name: string }[]`, sorted by name server-side (`CategoriesController.cs` `List`, `.OrderBy(c => c.Name)`). `GET /api/brands` → same shape from `BrandsController.cs`.
- Current numeric-input markup to replace, `app/pages/admin/products/new.vue` lines 110–121 and the matching block in `app/pages/admin/products/[id]/edit.vue` lines 157–168.
- `app/pages/admin/products/[id]/edit.vue`'s form is populated by a `watch(product, ...)` (lines 40–53) that sets `form.categoryId = String(p.categoryId)` — a `<select v-model="form.categoryId">` bound to that same reactive string field should pre-select correctly as long as the option `value`s are also strings (or the comparison is coerced), since native `<select>` value matching is string-based.
- No `BaseSelect` component exists yet (`app/components/base/` only has `Button.vue`, `Form.vue`, `Input.vue`) — plan should decide whether to add one (matching `BaseInput`'s `label`/`error` prop contract, e.g. its `bg-[#e8edf9]`/error-ring styling from `Input.vue` lines 46–53) or use a bare styled `<select>` per form, matching how the description `<textarea>` on the same pages is already a bare styled element rather than a `Base*` component.

## Out of scope

- Any change to `app/pages/admin/categories/*` or `app/pages/admin/brands/*` (the categories/brands management UI itself).
- Any backend change — `CategoriesController`/`BrandsController`/`CategoryDto`/`BrandDto` are already correct and unchanged.
- Adding "create new category/brand from the product form" (an inline-create affordance) — out of scope, a plain select of existing categories/brands only.
- Multi-select, search/autocomplete, or any select UX beyond a standard native `<select>` with one option per category/brand.
