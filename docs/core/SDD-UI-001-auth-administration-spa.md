# SDD-UI-001 — Auth Administration SPA

> Status: Implemented
> Last updated: 2026-04-03
> Owner: TBD
> Category: Core

## 1. Context & Scope

This spec defines the Vue.js Single Page Application (SPA) that provides a web-based administrative interface for the authentication and authorization system defined in `SDD-AUTH-001`. The SPA consumes the Warehouse.Auth.API REST endpoints to manage users, roles, permissions, and audit logs.

**In scope:**
- Login page with JWT authentication flow
- Automatic token refresh and session management
- User management (list, create, edit, deactivate, password change, role assignment)
- Role management (list, create, edit, delete, permission assignment)
- Permission management (list, create)
- Audit log viewer with filtering
- Application layout with sidebar navigation
- Route guards (redirect unauthenticated users to login)
- Error handling and user feedback (snackbar notifications)
- Layout density toggle: compact mode (fit-to-viewport) and comfortable mode (scrollable)
- Internationalization (i18n): English and Bulgarian language support

**Out of scope:**
- Warehouse business feature UIs (inventory, shipments — future specs)
- User self-registration (not defined in SDD-AUTH-001)
- OAuth2 / SSO login flows (future enhancement per SDD-AUTH-001)
- Dark mode (future enhancement)
- Mobile-responsive layout (future enhancement — desktop-first)
- Languages other than English and Bulgarian

**Related specs:**
- `SDD-AUTH-001` — Backend authentication and authorization API (consumed by this SPA)

---

## 2. Behavior

### 2.1 Authentication Flow

- The SPA MUST display a login page when no valid access token is present in the client state.
- The SPA MUST authenticate users by calling `POST /api/v1/auth/login` with username and password.
- On successful login, the SPA MUST store the access token, refresh token, and expiration timestamp in the Pinia auth store.
- The SPA MUST persist auth tokens to `localStorage` so that sessions survive page refresh.
- The SPA MUST attach the access token as a `Bearer` token in the `Authorization` header of all API requests.
- The SPA MUST automatically refresh the access token by calling `POST /api/v1/auth/refresh` when receiving a 401 response, then retry the original request.
- If token refresh fails (refresh token expired or revoked), the SPA MUST clear all stored auth state and redirect to the login page.
- The SPA MUST support logout by calling `POST /api/v1/auth/logout`, clearing all stored auth state, and redirecting to the login page.
- The SPA MUST display the current user's username in the application header/sidebar.

**Edge cases:**
- If the user opens the SPA with an expired access token but valid refresh token in `localStorage`, the SPA MUST attempt a silent refresh before showing the login page.
- If multiple API calls fail with 401 simultaneously, the SPA MUST ensure only one refresh request is made (queue concurrent 401 retries).
- If `localStorage` is cleared externally (e.g., browser dev tools), the SPA SHOULD redirect to login on the next API call failure.

### 2.2 Route Guards

- All routes except `/login` MUST be protected by an authentication guard.
- The authentication guard MUST check for a valid (non-expired) access token before allowing navigation.
- If the guard detects no valid token, it MUST redirect to `/login` and preserve the intended destination as a query parameter (`?redirect=/intended-path`).
- After successful login, the SPA MUST redirect to the preserved destination or to the dashboard if no destination was stored.

### 2.3 User Management

- The Users page MUST display a paginated data table of all users with columns: Username, Email, First Name, Last Name, Active status, Created date.
- The data table MUST support sorting by column headers.
- The SPA MUST provide a "Create User" action that opens a form dialog with fields: Username, Email, Password, First Name, Last Name.
- The SPA MUST provide an "Edit User" action that opens a form dialog pre-filled with the user's current profile (Email, First Name, Last Name). Username MUST NOT be editable.
- The SPA MUST provide a "Deactivate User" action that shows a confirmation dialog before calling `DELETE /api/v1/users/{id}`.
- The SPA MUST provide a "Change Password" action that opens a dialog with Current Password and New Password fields.
- The SPA MUST provide a "Manage Roles" action that opens a dialog showing the user's current roles with the ability to assign/remove roles.
- After any successful mutation (create, edit, deactivate), the SPA MUST refresh the users table to reflect the change.
- The SPA MUST display validation errors returned by the API (ProblemDetails) inline on the relevant form fields.

**Edge cases:**
- Deactivating the currently logged-in user SHOULD be prevented with a client-side warning.
- If the API returns a 409 Conflict (duplicate username/email), the SPA MUST display the error on the specific conflicting field.

### 2.4 Role Management

- The Roles page MUST display a data table of all roles with columns: Name, Description, System role flag.
- The SPA MUST provide a "Create Role" action that opens a form dialog with fields: Name, Description.
- The SPA MUST provide an "Edit Role" action that opens a form dialog pre-filled with the role's current values.
- The SPA MUST provide a "Delete Role" action with a confirmation dialog. System roles (`IsSystem = true`) MUST NOT show a delete option.
- The SPA MUST provide a "Manage Permissions" action that opens a dialog showing the role's current permissions with the ability to assign/remove permissions.
- The Manage Permissions dialog MUST display available permissions grouped by resource for easier selection.
- After any successful mutation, the SPA MUST refresh the roles table.

**Edge cases:**
- If deleting a role returns 409 (role in use), the SPA MUST display the error message from the API (including affected user count).
- System roles MUST visually indicate they are protected (e.g., chip/badge, disabled delete button).

### 2.5 Permission Management

- The Permissions page MUST display a data table of all permissions with columns: Resource, Action, Description.
- The data table SHOULD group or allow filtering permissions by resource.
- The SPA MUST provide a "Create Permission" action that opens a form dialog with fields: Resource, Action (select from: read, write, update, delete, all), Description.
- Permissions MUST NOT be editable or deletable via the UI (append-only, matching the backend design).
- If creating a permission returns 409 (duplicate), the SPA MUST display the error.

### 2.6 Audit Log Viewer

- The Audit page MUST display a paginated data table of audit logs with columns: Date/Time, User, Action, Resource, Details, IP Address.
- The SPA MUST provide filters for: User (dropdown or autocomplete), Action type, Date range.
- The SPA SHOULD support viewing the full JSON details of an audit entry in a dialog or expandable row.
- Audit logs MUST be read-only — no create, edit, or delete actions.

### 2.7 Application Layout

- The SPA MUST use a persistent sidebar navigation with links to: Dashboard, Users, Roles, Permissions, Audit Logs.
- The SPA MUST display the current user's username and a Logout button in the application bar.
- The active navigation item MUST be visually highlighted based on the current route.
- The sidebar SHOULD be collapsible to a rail (icons only) to maximize content space.
- The SPA MUST display a dashboard page as the default landing page after login. The dashboard MAY display summary cards (user count, role count) or a welcome message.

### 2.8 Notifications and Error Handling

- The SPA MUST display success notifications (snackbar) after successful mutations (create, update, delete).
- The SPA MUST display error notifications (snackbar) for unexpected API errors (500, network failures).
- Validation errors (400) MUST be displayed inline on the form fields, not as snackbar notifications.
- The SPA MUST display a loading indicator (progress bar or spinner) during API calls.

### 2.9 Layout Density Toggle

The SPA MUST support two layout modes, toggled via a button in the application bar:

- **Compact mode** — Content fills the viewport height without vertical scrolling. Data tables, forms, and dialogs fit within the visible window. Tables use Vuetify's `dense` density and reduce vertical padding. This mode is optimized for data entry workflows where the user stays in a single view.
- **Comfortable mode** — Content is allowed to overflow vertically (`overflow-y: auto`). Tables, forms, and cards use standard spacing and padding for improved readability. This mode is optimized for reviewing data.

- The SPA MUST persist the selected mode to `localStorage` so it survives page refresh.
- The SPA MUST default to compact mode on first visit.
- The toggle MUST be accessible from any page (placed in the application bar).
- The toggle SHOULD use a clear icon or label indicating the current mode (e.g., a viewport/expand icon).
- Switching modes MUST NOT trigger a page reload — only CSS/density classes change reactively.

### 2.10 Internationalization (i18n)

- The SPA MUST support two languages: **English (en)** and **Bulgarian (bg)**.
- English MUST be the default language.
- The SPA MUST provide a language switcher in the application bar (e.g., dropdown or toggle).
- The SPA MUST persist the selected language to `localStorage` so it survives page refresh.
- All user-facing text MUST be externalized into locale files — no hardcoded strings in Vue templates.
- Locale files MUST be organized as `frontend/src/i18n/locales/en.ts` and `frontend/src/i18n/locales/bg.ts`.
- Client-side validation error messages MUST be translated.
- UI labels (navigation, buttons, table headers, dialog titles, form labels, placeholders) MUST be translated.
- API error messages (ProblemDetails) are returned in English from the backend and SHOULD be mapped to translated equivalents on the client side using the error code.
- Date and number formatting SHOULD respect the selected locale.

**Edge cases:**
- If a translation key is missing in the active locale, the SPA MUST fall back to the English value.
- Switching language MUST NOT trigger a page reload — the UI re-renders reactively.

---

## 3. Validation Rules

Client-side validation mirrors the backend validation defined in `SDD-AUTH-001` Section 3. The SPA performs client-side validation before submitting to the API, but the API remains the authoritative validator.

### 3.1 User Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V1 | Username | Required. 3–50 characters. Alphanumeric + underscores only. | Username must be 3–50 characters (letters, numbers, underscores). |
| V2 | Email | Required. Valid email format. Max 256 characters. | Please enter a valid email address. |
| V3 | Password | Required on create. Min 8 characters. Must contain uppercase, lowercase, and digit. | Password must be at least 8 characters with uppercase, lowercase, and a number. |
| V4 | FirstName | Required. 1–100 characters. | First name is required. |
| V5 | LastName | Required. 1–100 characters. | Last name is required. |

### 3.2 Role Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V6 | Name | Required. 2–50 characters. | Role name must be 2–50 characters. |
| V7 | Description | Optional. Max 500 characters. | Description must be under 500 characters. |

### 3.3 Permission Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V8 | Resource | Required. Lowercase alphanumeric + dots. Max 100 characters. | Resource must be lowercase letters, numbers, and dots. |
| V9 | Action | Required. Must be one of: read, write, update, delete, all. | Please select an action. |
| V10 | Description | Optional. Max 500 characters. | Description must be under 500 characters. |

### 3.4 Login Form

| # | Field | Rule | Error Message |
|---|---|---|---|
| V11 | Username | Required. | Username is required. |
| V12 | Password | Required. | Password is required. |

### 3.5 Change Password Form

| # | Field | Rule | Error Message |
|---|---|---|---|
| V13 | Current Password | Required. | Current password is required. |
| V14 | New Password | Required. Min 8 characters. Must contain uppercase, lowercase, and digit. | New password must be at least 8 characters with uppercase, lowercase, and a number. |

---

## 4. Error Rules

All error handling maps to the ProblemDetails responses defined in `SDD-AUTH-001` Section 4.

| # | API Error Code | HTTP Status | UI Behavior |
|---|---|---|---|
| E1 | `INVALID_CREDENTIALS` | 401 | Display "Invalid username or password" on the login form. |
| E2 | `TOKEN_EXPIRED` | 401 | Trigger silent token refresh. If refresh fails, redirect to login. |
| E3 | `REFRESH_TOKEN_EXPIRED` | 401 | Clear auth state, redirect to login with message "Session expired. Please log in again." |
| E4 | `REFRESH_TOKEN_REVOKED` | 401 | Clear auth state, redirect to login with message "Session expired. Please log in again." |
| E5 | `FORBIDDEN` | 403 | Display snackbar: "You do not have permission to perform this action." |
| E6 | `USER_NOT_FOUND` | 404 | Display snackbar: "User not found." Refresh the table. |
| E7 | `ROLE_NOT_FOUND` | 404 | Display snackbar: "Role not found." Refresh the table. |
| E8 | `DUPLICATE_USERNAME` | 409 | Display inline error on Username field: "This username is already taken." |
| E9 | `DUPLICATE_EMAIL` | 409 | Display inline error on Email field: "This email is already taken." |
| E10 | `DUPLICATE_ROLE_NAME` | 409 | Display inline error on Name field: "A role with this name already exists." |
| E11 | `DUPLICATE_PERMISSION` | 409 | Display inline error: "This permission already exists." |
| E12 | `ROLE_IN_USE` | 409 | Display snackbar with the API message (includes affected user count). |
| E13 | `PROTECTED_ROLE` | 400 | Display snackbar: "This role is protected and cannot be deleted." |
| E14 | `INVALID_CURRENT_PASSWORD` | 400 | Display inline error on Current Password field: "Current password is incorrect." |
| E15 | `VALIDATION_ERROR` | 400 | Map `errors` object from ProblemDetails to inline field errors. |
| E16 | Network error | — | Display snackbar: "Unable to connect to the server. Please check your connection." |
| E17 | Unhandled 500 | 500 | Display snackbar: "An unexpected error occurred. Please try again." |

---

## 5. Routes

| Route | View | Auth Required | Description |
|---|---|---|---|
| `/login` | LoginView | No | Login form |
| `/` | DashboardView | Yes | Dashboard landing page |
| `/users` | UsersView | Yes | User management table |
| `/roles` | RolesView | Yes | Role management table |
| `/permissions` | PermissionsView | Yes | Permission management table |
| `/audit` | AuditView | Yes | Audit log viewer |

---

## 6. Technology Stack

| Concern | Technology | Purpose |
|---|---|---|
| Framework | Vue 3 (Composition API) | SPA framework |
| Language | TypeScript | Type safety |
| Build tool | Vite | Dev server and production bundling |
| UI library | Vuetify 3 | Material Design component library |
| Routing | Vue Router 4 | Client-side SPA routing |
| State management | Pinia | Centralized auth and notification state |
| HTTP client | Axios | API communication with interceptors |
| Internationalization | vue-i18n 9 | English and Bulgarian locale support |
| Package manager | npm | Dependency management |
| Icons | @mdi/font | Material Design Icons (used by Vuetify) |

---

## 7. Versioning Notes

- **v1 — Initial specification (2026-04-03)**
  - Login, user management, role management, permission management, audit viewer
  - JWT authentication flow with silent refresh
  - Client-side validation mirroring SDD-AUTH-001 rules
  - Vuetify-based Material Design UI
  - Layout density toggle (compact / comfortable)
  - Internationalization: English and Bulgarian

---

## 8. Test Plan

### Unit Tests — Auth Store

- `login_ValidCredentials_StoresTokensAndUser` [Unit]
- `login_InvalidCredentials_ThrowsAndClearsState` [Unit]
- `logout_ClearsTokensAndRedirects` [Unit]
- `refreshToken_ValidRefreshToken_UpdatesTokens` [Unit]
- `refreshToken_ExpiredRefreshToken_ClearsStateAndRedirects` [Unit]
- `isAuthenticated_WithValidToken_ReturnsTrue` [Unit]
- `isAuthenticated_WithExpiredToken_ReturnsFalse` [Unit]
- `initializeFromStorage_RestoresPersistedState` [Unit]

### Unit Tests — API Client Interceptors

- `requestInterceptor_AttachesAuthorizationHeader` [Unit]
- `responseInterceptor_On401_TriggersTokenRefresh` [Unit]
- `responseInterceptor_On401_RetriesOriginalRequest` [Unit]
- `responseInterceptor_ConcurrentRefresh_QueuesRequests` [Unit]
- `responseInterceptor_RefreshFails_RedirectsToLogin` [Unit]

### Unit Tests — Validation

- `userForm_EmptyUsername_ShowsError` [Unit]
- `userForm_ShortPassword_ShowsError` [Unit]
- `userForm_InvalidEmail_ShowsError` [Unit]
- `roleForm_EmptyName_ShowsError` [Unit]
- `permissionForm_InvalidResource_ShowsError` [Unit]
- `loginForm_EmptyFields_ShowsErrors` [Unit]

### Unit Tests — Layout Density

- `densityToggle_DefaultsToCompact` [Unit]
- `densityToggle_SwitchToComfortable_AppliesScrollableLayout` [Unit]
- `densityToggle_SwitchToCompact_AppliesFitToViewportLayout` [Unit]
- `densityToggle_PersistsToLocalStorage` [Unit]
- `densityToggle_RestoresFromLocalStorage` [Unit]

### Unit Tests — i18n

- `i18n_DefaultsToEnglish` [Unit]
- `i18n_SwitchToBulgarian_TranslatesLabels` [Unit]
- `i18n_PersistsLanguageToLocalStorage` [Unit]
- `i18n_RestoresLanguageFromLocalStorage` [Unit]
- `i18n_MissingKey_FallsBackToEnglish` [Unit]
- `i18n_ValidationMessages_TranslatedInBulgarian` [Unit]
- `i18n_ApiErrorCodes_TranslatedInBulgarian` [Unit]

### Integration Tests — Page Flows

- `loginPage_ValidLogin_RedirectsToDashboard` [Integration]
- `loginPage_InvalidLogin_ShowsError` [Integration]
- `usersPage_LoadsUserTable` [Integration]
- `usersPage_CreateUser_AppearsInTable` [Integration]
- `usersPage_EditUser_UpdatesTable` [Integration]
- `usersPage_DeactivateUser_ShowsConfirmation` [Integration]
- `usersPage_ChangePassword_SubmitsSuccessfully` [Integration]
- `usersPage_AssignRole_UpdatesUserRoles` [Integration]
- `rolesPage_LoadsRoleTable` [Integration]
- `rolesPage_CreateRole_AppearsInTable` [Integration]
- `rolesPage_DeleteSystemRole_ShowsProtectedError` [Integration]
- `rolesPage_AssignPermissions_UpdatesRolePermissions` [Integration]
- `permissionsPage_LoadsPermissionTable` [Integration]
- `permissionsPage_CreatePermission_AppearsInTable` [Integration]
- `permissionsPage_DuplicatePermission_ShowsConflictError` [Integration]
- `auditPage_LoadsAuditTable` [Integration]
- `auditPage_FilterByUser_FiltersResults` [Integration]
- `routeGuard_UnauthenticatedAccess_RedirectsToLogin` [Integration]
- `routeGuard_AfterLogin_RedirectsToOriginalDestination` [Integration]

---

## 9. Acceptance Criteria

- [ ] Users can log in with username/password and see the dashboard
- [ ] Access tokens refresh silently — users are not logged out while actively using the app
- [ ] Session persists across page refresh (tokens in localStorage)
- [ ] Expired sessions redirect to login with a clear message
- [ ] Users page shows paginated user list with create, edit, deactivate, password change, and role management
- [ ] Roles page shows role list with create, edit, delete, and permission management
- [ ] Permissions page shows permission list with create
- [ ] Audit page shows paginated logs with user, action, and date filtering
- [ ] Client-side validation matches SDD-AUTH-001 rules
- [ ] API errors (ProblemDetails) display as inline field errors or snackbar notifications
- [ ] Navigation highlights active route, sidebar is collapsible
- [ ] Layout density toggle switches between compact (fit-to-viewport) and comfortable (scrollable) modes
- [ ] Density preference persists across page refresh
- [ ] Language switcher toggles between English and Bulgarian
- [ ] All UI text renders correctly in Bulgarian
- [ ] Language preference persists across page refresh
- [ ] All protected routes redirect to login when unauthenticated

---

## Key Files

- `frontend/src/main.ts`
- `frontend/src/App.vue`
- `frontend/src/router/index.ts`
- `frontend/src/stores/auth.ts`
- `frontend/src/stores/notification.ts`
- `frontend/src/stores/layout.ts`
- `frontend/src/i18n/index.ts`
- `frontend/src/i18n/locales/en.ts`
- `frontend/src/i18n/locales/bg.ts`
- `frontend/src/api/client.ts`
- `frontend/src/api/auth.ts`
- `frontend/src/api/users.ts`
- `frontend/src/api/roles.ts`
- `frontend/src/api/permissions.ts`
- `frontend/src/api/audit.ts`
- `frontend/src/types/auth.ts`
- `frontend/src/types/user.ts`
- `frontend/src/types/role.ts`
- `frontend/src/types/permission.ts`
- `frontend/src/types/audit.ts`
- `frontend/src/views/LoginView.vue`
- `frontend/src/views/DashboardView.vue`
- `frontend/src/views/users/UsersView.vue`
- `frontend/src/views/roles/RolesView.vue`
- `frontend/src/views/permissions/PermissionsView.vue`
- `frontend/src/views/audit/AuditView.vue`
- `frontend/src/layouts/DefaultLayout.vue`
