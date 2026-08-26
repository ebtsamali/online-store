# middelware — plan overview

Entry point for the **middelware** feature: role-based route protection (admin vs customer) for the Nuxt 4 frontend — auth state in Pinia, two layouts, and the `auth`/`admin`/`guest` route middleware. Stories execute in order by their `NN` prefix.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 01 | [01-story-auth-store-persistence.md](./01-story-auth-store-persistence.md) | Auth store, persistence & login/register wiring | — | None |
| 02 | [02-story-layouts.md](./02-story-layouts.md) | Customer & admin layouts | — | Story 01 |
| 03 | [03-story-route-protection-middleware.md](./03-story-route-protection-middleware.md) | Route-protection middleware & page structure | — | Stories 01, 02 |
| 04 | [04-story-auth-ui-forms-validation.md](./04-story-auth-ui-forms-validation.md) | Auth UI redesign, global form components, zod validation & toasts | — | Stories 01, 02 |
| 14 | [14-story-admin-login-page.md](./14-story-admin-login-page.md) | Admin login page separation & middleware/logout consistency | — | Stories 01, 02, 03, 04 |
| 16 | [16-story-guest-guard-and-admin-route-separation.md](./16-story-guest-guard-and-admin-route-separation.md) | Guest guard on auth pages & admin/customer route separation | — | Stories 01, 02, 03, 04, 14 |

## Dependency notes

- Execute **strictly in order** — each story stops for confirmation before the next.
- **Story 14** was added later (see `stories/middelware/admin-login-page/intake.md`) to split the shared `/auth/login` page into a dedicated `/admin/login`, and to make `middleware/admin.ts` and the store's `logout()` consistent with that split. It depends on all four earlier stories but does not change their behavior for customers.
- **Story 16 is a bug fix** (see `stories/middelware/middelware-bug/intake.md`) closing the gap Story 14's Edge Cases named as an open follow-up: no guard stopped an **already-authenticated** visitor from opening a login/register page, so an admin could press browser **Back** off `/admin/dashboard` and land on `/admin/login` again. It adds a third middleware, `guest`, to the two from Story 03, applies it to `/admin/login`, `/auth/login` and `/auth/register`, and extends `middleware/auth.ts` so an admin session is also kept out of the customer-account pages (`/cart`, `/checkout`, `/orders*`). `middleware/admin.ts` is left unchanged.
- The whole feature rests on one architectural decision made in **Story 01**: auth is persisted in an SSR-readable **cookie** (mirrored to `localStorage`) and hydrated by a **universal** plugin, so the Story 03 route guards resolve correctly during server-side rendering of a direct `/admin/*` navigation. A localStorage-only design would break that case.
- Backend contract is external and already implemented — see [`../../../../OnlineStore.API/.squad/plans/middleware/03-authorization-middleware.md`](../../../../OnlineStore.API/.squad/plans/middleware/03-authorization-middleware.md) for the `/api/auth` responses and the server-side `[Authorize]` enforcement these frontend guards complement (they do **not** replace it).
- **Cross-cutting fix (Story 01):** `nuxt.config.ts` `apiBase` (`:5000`) does not match the running API port (`:5016`); Story 01 aligns them so login works.
- **Story 04 is presentation-only** and depends on 01/02, not 03: it rewrites the two auth pages' UI, adds the global `Base*` form components, zod validation and toasts. It deliberately keeps `/auth/login` **unguarded**, since Story 03's middleware redirects to it. **Superseded in part by Story 16:** the page now carries the `guest` guard, which still lets every logged-out visitor (and therefore every Story 03 redirect) through, and only turns away an already-authenticated session.
