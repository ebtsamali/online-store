# middelware — plan overview

Entry point for the **middelware** feature: role-based route protection (admin vs customer) for the Nuxt 4 frontend — auth state in Pinia, two layouts, and `auth`/`admin` route middleware. Stories execute in order by their `NN` prefix.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 01 | [01-story-auth-store-persistence.md](./01-story-auth-store-persistence.md) | Auth store, persistence & login/register wiring | — | None |
| 02 | [02-story-layouts.md](./02-story-layouts.md) | Customer & admin layouts | — | Story 01 |
| 03 | [03-story-route-protection-middleware.md](./03-story-route-protection-middleware.md) | Route-protection middleware & page structure | — | Stories 01, 02 |
| 04 | [04-story-auth-ui-forms-validation.md](./04-story-auth-ui-forms-validation.md) | Auth UI redesign, global form components, zod validation & toasts | — | Stories 01, 02 |

## Dependency notes

- Execute **strictly in order** — each story stops for confirmation before the next.
- The whole feature rests on one architectural decision made in **Story 01**: auth is persisted in an SSR-readable **cookie** (mirrored to `localStorage`) and hydrated by a **universal** plugin, so the Story 03 route guards resolve correctly during server-side rendering of a direct `/admin/*` navigation. A localStorage-only design would break that case.
- Backend contract is external and already implemented — see [`../../../../OnlineStore.API/.squad/plans/middleware/03-authorization-middleware.md`](../../../../OnlineStore.API/.squad/plans/middleware/03-authorization-middleware.md) for the `/api/auth` responses and the server-side `[Authorize]` enforcement these frontend guards complement (they do **not** replace it).
- **Cross-cutting fix (Story 01):** `nuxt.config.ts` `apiBase` (`:5000`) does not match the running API port (`:5016`); Story 01 aligns them so login works.
- **Story 04 is presentation-only** and depends on 01/02, not 03: it rewrites the two auth pages' UI, adds the global `Base*` form components, zod validation and toasts. It deliberately keeps `/auth/login` **unguarded**, since Story 03's middleware redirects to it.
