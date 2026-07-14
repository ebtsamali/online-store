# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/auth/login/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `auth`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `login` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
login
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Task: Implement Login (User Authentication) API

Description:
Implement the backend endpoint that allows an existing user to log in using 
their email and password. On successful authentication, the system should 
return a JWT token that the frontend will use to authenticate future requests.

DTOs:
- LoginRequest(string Email, string Password)
- AuthResponse(string Token, string Name, string Email, string Role)

Endpoint:
POST /api/auth/login

Request body example:
{
  "email": "sara@test.com",
  "password": "Test@123"
}

Success response (200 OK):
{
  "token": "eyJhbGciOi...",
  "name": "Sara Ahmed",
  "email": "sara@test.com",
  "role": "customer"
}

Business logic:
1. Look up the user by email in the database.
   - If no user is found, return 401 Unauthorized with message 
     "Invalid email or password".
2. Verify the provided password against the stored PasswordHash using BCrypt.
   - If the password doesn't match, return 401 Unauthorized with the same 
     generic message "Invalid email or password" (do not reveal which 
     field is wrong, for security reasons).
3. Generate a JWT token containing: userId, email, role.
4. Return the token along with basic user info.

Validation rules:
- Email: required, valid email format
- Password: required, not empty

Error handling:
- 401 Unauthorized → "Invalid email or password" 
  (used for both "email not found" and "wrong password" cases)
- 400 Bad Request → validation errors per field (e.g. missing email/password)
- 500 Internal Server Error → "Something went wrong"

Security notes:
- Never reveal whether the email exists or the password was wrong 
  specifically — always return the same generic error message.
- JWT token expiry: 7 days.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] User can log in successfully with correct email and password.
- [ ] Login is rejected with a generic error if email doesn't exist.
- [ ] Login is rejected with the same generic error if password is wrong.
- [ ] A valid JWT token is returned on successful login.
- [ ] CORS is enabled to allow requests from the frontend (localhost)
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

- Anything not captured above (e.g. chat context) — keep short.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `c#`.

## Out of scope

- What this story explicitly does **not** cover:
