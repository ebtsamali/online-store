# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/auth/sign-up/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `auth`

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
sign-up
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Task: Implement Sign Up (User Registration) API

Description:
Implement the backend endpoint that allows a new user to register an account.
The system must validate the input, hash the password, and store the user in 
the database. The endpoint should only confirm that the account was created 
successfully — it should NOT return a JWT token, since the user is expected 
to log in separately afterwards.

Entity: User
- Id (int, PK)
- Name (string, required)
- Email (string, required, unique)
- PasswordHash (string) — hashed password, never store plain text
- Role (string, default = "customer")
- CreatedAt (DateTime, default = current UTC time)

DTOs:
- RegisterRequest(string Name, string Email, string Password)
- RegisterResponse(string Message, string Name, string Email)

Endpoint:
POST /api/auth/register

Request body example:
{
  "name": "Sara Ahmed",
  "email": "sara@test.com",
  "password": "Test@123"
}

Success response (201 Created):
{
  "message": "Account created successfully",
  "name": "Sara Ahmed",
  "email": "sara@test.com"
}

Business logic:
1. Check if the email already exists in the database.
   - If it exists, return 409 Conflict with message "Email already exists".
2. Hash the password using BCrypt before saving it.
3. Save the new user with default Role = "customer".
4. Return a success confirmation only — do NOT generate or return a JWT token.

Validation rules:
- Name: required, 3–50 characters
- Email: required, valid email format, must be unique
- Password: required, minimum 8 characters, must include at least one 
  uppercase letter and one number

Error handling:
- 409 Conflict → "Email already exists"
- 400 Bad Request → validation errors per field
- 500 Internal Server Error → "Something went wrong"
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] User can register with valid data and receives a success confirmation.
- [ ] Registration is rejected if the email is already used.
- [ ] Password is stored hashed in the database, never in plain text.
- [ ] No JWT token is generated or returned from this endpoint.
- [ ] CORS is enabled to allow requests from the frontend (localhost:).
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

- **Blocked by / related ids:** (tracker ids only; optional short note)
- **Depends on code areas or other stories:**

## Extra notes (optional)

- 

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `c#`.

## Out of scope

- What this story explicitly does **not** cover:
