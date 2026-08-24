# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/admin-products-crud/admin-products-list-create-and-edit/intake.md`
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

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Admin Products List, Create and Edit
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Replace the `app/pages/admin/products/index.vue` placeholder ("Product management coming soon") with a real admin product list (paginated, search, shows inactive products too, real edit/delete wired up — currently disabled in `AdminRecentProductsTable`), and add the two missing pages: create product and edit product, both using the backend's `multipart/form-data` image-upload endpoints.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] `app/pages/admin/products/index.vue` (`layout: 'admin'`, `middleware: 'admin'`) fetches `GET /api/products?page=&pageSize=&search=` **with the admin's Authorization bearer token attached** (so inactive products are included — the existing dashboard fetch was flagged as NOT sending the token; fix that pattern here, and consider fixing it on the dashboard too as a small aligned improvement, but that's optional).
- [ ] Table (can reuse/extend `AdminRecentProductsTable.vue`, currently has disabled Edit/Delete) with working Edit (link to `/admin/products/[id]/edit`) and Delete (calls `DELETE /api/products/{id}`, confirm dialog first, toast on result). Since the backend hard-deletes and will fail with a 500 if the product is referenced by any order, catch that failure and show a clear message suggesting deactivation instead (via edit → `isActive=false`) rather than surfacing a raw 500.
- [ ] Search input + pagination controls (same UX pattern as the customer products-catalog page for consistency).
- [ ] "Add product" button linking to `/admin/products/new`.
- [ ] New page `app/pages/admin/products/new.vue`: form with name, description, price, stock, category (numeric id input is acceptable if the categories-and-brands backend/admin-categories story hasn't landed yet; a `<select>` populated from `GET /api/categories` if it has), brand (same), image file input (required, client-side validate type jpg/jpeg/png/webp and size ≤5MB before submit to match backend limits and fail fast). Submits as `multipart/form-data` via `POST /api/products`. Zod validation for text/number fields following the `app/utils/validation.ts` pattern; on success, toast + redirect to the products list.
- [ ] New page `app/pages/admin/products/[id]/edit.vue`: same form pre-filled from `GET /api/products/{id}`, image input optional (existing image kept if not replaced — show current image as a preview), plus an `isActive` toggle. Submits as `multipart/form-data` via `PUT /api/products/{id}`.
- [ ] Both forms show per-field validation errors (via `fieldErrors()` + `BaseInput`'s `error` prop) and a submit-loading state (`BaseButton`'s `loading` prop) to prevent double-submits, especially important given image upload latency.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| *(e.g. `attachments/flow.png`)* | *(e.g. UX flow)* |

*(Add rows per file. If none, write "None.")*

---

## Dependencies

- **Blocked by / related ids:** Soft dependency on backend story "Categories and Brands CRUD" for a proper category/brand `<select>` — degrade gracefully to a numeric id input if that story hasn't landed yet (see acceptance criteria).
- **Depends on code areas or other stories:** `app/pages/admin/products/index.vue` (existing stub), `app/components/admin/RecentProductsTable.vue`, `app/layouts/admin.vue`, `app/middleware/admin.ts`, `app/utils/validation.ts`, `app/composables/useProducts.ts`, `app/composables/useProductImage.ts`.

## Extra notes (optional)

- This is the first of the two remaining admin stories (products first, then categories) since categories in the admin UI are only useful once products can reference them meaningfully from a form — but this story does not strictly block on categories per the dependency note above.
- The dashboard's known gap ("fetch sends no Authorization header, so admin stats are actually the anonymous/active-only view") should NOT be silently propagated into this new list page — attach the bearer token here from the start.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.
- `POST /api/products` / `PUT /api/products/{id}` (admin only) are `multipart/form-data`, not JSON — build the request with a `FormData` object and pass it to `$fetch`/native `fetch` with the `Authorization: Bearer <token>` header (from `useAuthStore().token`) and let the browser set the multipart boundary (do not set `Content-Type` manually).
- `PUT` image is optional (old file kept if omitted, old file deleted if replaced); `isActive` is part of the update payload.
- `DELETE /api/products/{id}` hard-deletes and deletes the image file; throws a 500 (unhandled FK Restrict violation) if any `OrderItem` references the product — there is no clean 4xx for this today, so the frontend must catch the failure generically and suggest deactivation.
- Read the exact `CreateProductFormRequest`/`UpdateProductFormRequest` field names from `OnlineStore.API` source during planning to avoid multipart field-name mismatches.

## Out of scope

- Bulk actions (bulk delete/activate).
- Image cropping/editing UI — raw file upload only.
- Category/brand management UI itself (separate "Admin Categories List, Create and Edit" story).
