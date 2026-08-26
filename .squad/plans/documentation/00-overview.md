# documentation — plan overview

Entry point for the **documentation** feature: the repository's written documentation set — a root `README.md`, a real README for each of the two apps, and four deep-dive documents under `docs/` covering setup, the API surface, the data model, and authentication. Stories execute in order by their `NN` prefix.

## Stories

| NN | File | Title | Tracker id | Depends on |
|----|------|-------|------------|------------|
| 01 | [01-story-repo-frontend-and-backend-documentation.md](./01-story-repo-frontend-and-backend-documentation.md) | Repository README, frontend README & backend documentation | — | None |

## Dependency notes

- **Story 01 has no code prerequisites.** Everything it documents already exists and is verified against the sources cited in its `## Context — Read These Files First` section. It modifies **no** application source, configuration, or `.gitignore`.
- **Seven files, one story.** The documents cross-link heavily (root README → `docs/*` → both app READMEs), so splitting them across stories would risk broken links and contradictory statements. They are delivered as one coherent pass.
- **`NN` numbering across three workspaces.** This repository contains three independent squad-kit workspaces, each with its own `config.yaml`, `plans/00-index.md` and `NN` sequence:
  - `.squad/` (this one, repo root, `projectRoots: ["."]`) — starts at **01**;
  - `online-store-frontend/.squad/` — currently **01–16**;
  - `OnlineStore.API/.squad/` — currently **01–08**.
  "Story 01" is therefore ambiguous without naming the workspace. Story 01 documents this convention in the root README so future readers are not caught by it.
- **Cross-workspace source of truth.** The route-protection content must stay consistent with [`../../../online-store-frontend/.squad/plans/middelware/16-story-guest-guard-and-admin-route-separation.md`](../../../online-store-frontend/.squad/plans/middelware/16-story-guest-guard-and-admin-route-separation.md), which is the current authority on frontend guards (four middleware: `role-area.global.ts`, `admin.ts`, `auth.ts`, `guest.ts`). The API reference must cover every area listed in [`../../../OnlineStore.API/.squad/plans/00-index.md`](../../../OnlineStore.API/.squad/plans/00-index.md).
- **Secrets constraint.** `OnlineStore.API/appsettings.json` has a committed database password and JWT signing key, and the root `.gitignore` does not exclude it. Story 01 must document configuration with **placeholders only** and add a security note recommending `dotnet user-secrets` or environment variables. Rotating those values and adjusting `.gitignore` is deliberately **out of scope** and needs its own story.
- **Three verified inconsistencies are documented, not fixed** by Story 01: the register password policy is weaker on the client (6 chars) than on the server (8 chars + uppercase + digit); deleting a product referenced by an order returns `500` rather than the `409` its category equivalent returns; and the `"admin"` role string is compared case-sensitively on both sides. Each is a candidate follow-up story.
- **No test runner and no CI exist** in either app (no `test` script, no test project, no files in `.github/`). Story 01 states this plainly rather than implying coverage; its own verification is a manual command-and-link pass plus clean `dotnet build` and `pnpm build`.
