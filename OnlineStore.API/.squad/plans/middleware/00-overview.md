# middleware — plan overview

Entry point for the **middleware** feature (cross-cutting request-pipeline concerns: authentication, authorization). Stories execute in order by their `NN` prefix.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 03 | [03-authorization-middleware.md](03-authorization-middleware.md) | Enable Authorization Middleware & Protect Endpoints | — | Story 02 (auth/login) |

## Dependency notes

- **Story 03 → Story 02:** the JWT validation pipeline and `TokenService` are established by [auth/02-login-api.md](../auth/02-login-api.md); Story 03 completes the authorization gaps (explicit `AddAuthorization`, Swagger Authorize button, a protected `GET /api/auth/me`).
- **Story 03 → future Products CRUD story:** applying `[Authorize(Roles="admin")]` to `POST/PUT/DELETE /api/products` is **blocked** until a Products CRUD story creates `ProductsController`. Story 03 defines the authorization matrix to apply then; acceptance criteria for admin 403/200 are delivered by that story.
