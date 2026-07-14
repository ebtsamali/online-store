# auth — plan overview

Entry point for the **auth** feature. Stories execute in order by their `NN` prefix.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 01 | [01-user-registration-api.md](01-user-registration-api.md) | Sign Up (User Registration) API | — | None |
| 02 | [02-login-api.md](02-login-api.md) | Login (User Authentication) API | — | Story 01 |

## Dependency notes

- **Story 02 → Story 01:** Login reuses the `User` entity, `Users` `DbSet` + unique `Email` index, the `AuthController`, and the `"Frontend"` CORS policy created in Story 01. It adds JWT issuance/validation (no new migration).
