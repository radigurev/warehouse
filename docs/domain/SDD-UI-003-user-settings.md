# SDD-UI-003 — User Settings

> Status: Implemented
> Last updated: 2026-04-03
> Owner: TBD
> Category: Domain

## 1. Context & Scope

This spec defines the User Settings feature in the Warehouse Management SPA. It provides a self-service page where any authenticated user can view and edit their own profile (email, first name, last name) and change their own password. This is distinct from the admin User Management feature defined in `SDD-UI-001`, which allows administrators to manage all users. The User Settings feature is available to every authenticated user regardless of role or permissions.

The settings page consumes the same backend endpoints defined in `SDD-AUTH-001`: `GET /api/v1/users/{id}` to load the user's profile, `PUT /api/v1/users/{id}` to update it, and `PUT /api/v1/users/{id}/password` to change the password. The user ID is decoded from the JWT access token's `sub` claim.

Profile editing and password change forms respect the `formDisplayMode` setting defined in `SDD-UI-002`: when set to `'modal'`, forms open as dialog overlays on the settings page; when set to `'page'`, forms navigate to dedicated page routes.

**In scope:**
- Settings page (`/settings`) displaying the current user's profile information
- Self-service profile editing (email, first name, last name) via existing `PUT /api/v1/users/{id}` endpoint
- Self-service password change via existing `PUT /api/v1/users/{id}/password` endpoint (requires current password + new password)
- Integration with `formDisplayMode` (modal or page) for edit and password forms
- New `userId` property in the auth store, decoded from the JWT `sub` claim
- Navigation entry in the user profile dropdown menu (app bar)
- New routes: `/settings`, `/settings/edit-profile`, `/settings/change-password`
- i18n support for English and Bulgarian
- Reuse of existing `ChangePasswordDialog` organism for the password form
- New `ProfileEditDialog` organism for the profile edit form

**Out of scope:**
- Admin user management (covered by `SDD-UI-001`)
- Admin password reset (auto-generated password, covered by `SDD-UI-001`)
- Profile avatar or profile picture upload
- Account deletion or self-deactivation
- Email verification after email change
- Two-factor authentication settings
- Notification preferences
- Theme or display preferences (density and form display mode are in the profile dropdown, not the settings page)

**Related specs:**
- `SDD-AUTH-001` — Backend authentication and authorization API (user profile and password endpoints)
- `SDD-UI-001` — Auth Administration SPA (admin user management, layout, auth store, profile dropdown)
- `SDD-UI-002` — Form Display Mode (modal vs page rendering, FormWrapper molecule)

---

## 2. Behavior

### 2.1 Auth Store: User ID

- The auth store MUST expose a `userId` property containing the numeric user ID.
- The `userId` MUST be decoded from the JWT access token's `sub` claim on login and on token refresh.
- The `userId` MUST be persisted to `localStorage` alongside other auth state properties (access token, refresh token, username, roles).
- The `userId` MUST be restored from `localStorage` on application initialization (same pattern as `username` and `roles`).
- On logout or state clear, the `userId` MUST be removed from `localStorage` and reset to `null`.

**Edge cases:**
- If the JWT `sub` claim is missing or cannot be parsed as a number, the `userId` MUST be set to `null`. The settings page MUST display an error state and not attempt API calls with an invalid user ID.
- If `localStorage` contains a non-numeric `userId` value, the auth store MUST fall back to `null` and attempt to re-decode from the current access token.

### 2.2 Settings Page (`/settings`)

- The SPA MUST register a `/settings` route that renders a `SettingsView` component.
- The `/settings` route MUST be protected by the authentication guard (same as all other authenticated routes per `SDD-UI-001` Section 2.2).
- The `/settings` route MUST NOT require admin role or any specific permission. Any authenticated user MUST be able to access it.
- The `SettingsView` MUST load the current user's full profile by calling `GET /api/v1/users/{id}` using the `userId` from the auth store.
- While loading, the page MUST display a loading indicator (skeleton loader or progress circular).
- On successful load, the page MUST display the user's profile information in a card layout with the following fields:
  - **Username** (read-only, displayed as text, not an input field)
  - **Email** (read-only display, editable via the edit form)
  - **First Name** (read-only display, editable via the edit form)
  - **Last Name** (read-only display, editable via the edit form)
- The profile card MUST include an "Edit Profile" action button.
- The page MUST include a separate "Change Password" section or card with a "Change Password" action button.
- The app bar page title MUST display the translated settings title (e.g., "Settings" in English, the Bulgarian equivalent in BG).

**Edge cases:**
- If the `GET /api/v1/users/{id}` call fails with 404 (user not found -- e.g., user was deactivated by an admin while the session is still active), the page MUST display an error message: "Unable to load your profile. Your account may have been deactivated." The page MUST NOT crash.
- If the API call fails with a network error or 500, the page MUST display a generic error message with a "Retry" button that re-attempts the profile load.

### 2.3 Profile Editing

- When the user clicks "Edit Profile", the behavior MUST depend on the `formDisplayMode` from the layout store:
  - **Modal mode (`'modal'`):** The SPA MUST open a `ProfileEditDialog` organism as a dialog overlay on the settings page.
  - **Page mode (`'page'`):** The SPA MUST navigate to `/settings/edit-profile`, which renders a page wrapper embedding the `ProfileEditDialog` organism in page mode.
- The `ProfileEditDialog` organism MUST accept a `mode` prop (`'dialog'` | `'page'`) and use the `FormWrapper` molecule for rendering (consistent with `SDD-UI-002` Section 2.6).
- The profile edit form MUST display editable fields for: Email, First Name, Last Name.
- The form MUST NOT display or allow editing of the Username field.
- The form MUST be pre-populated with the user's current profile data.
- On successful submission, the form MUST call `PUT /api/v1/users/{id}` with the updated profile data using the `userId` from the auth store.
- On successful save, the SPA MUST:
  - Display a success notification (snackbar).
  - Refresh the profile data displayed on the settings page.
  - Close the dialog (modal mode) or navigate back to `/settings` (page mode).
- On cancel, the SPA MUST close the dialog (modal mode) or navigate back to `/settings` (page mode) without saving.
- The `ProfileEditDialog` MUST emit `saved` and `cancelled` events (consistent with the pattern in `SDD-UI-002` Section 2.6).

**Edge cases:**
- If the update API returns 409 `DUPLICATE_EMAIL`, the form MUST display an inline error on the Email field: "This email is already taken."
- If the update API returns 404 `USER_NOT_FOUND` (account deactivated during edit), the form MUST display a snackbar error and navigate back to `/settings`.

### 2.4 Password Change

- When the user clicks "Change Password", the behavior MUST depend on the `formDisplayMode` from the layout store:
  - **Modal mode (`'modal'`):** The SPA MUST open the existing `ChangePasswordDialog` organism as a dialog overlay on the settings page.
  - **Page mode (`'page'`):** The SPA MUST navigate to `/settings/change-password`, which renders a page wrapper embedding the `ChangePasswordDialog` organism in page mode.
- The `ChangePasswordDialog` MUST be reused as-is from `SDD-UI-001`. It already accepts a `userId` prop and a `mode` prop and uses the `FormWrapper` molecule.
- The `userId` passed to `ChangePasswordDialog` MUST be the current user's ID from the auth store (not a route parameter).
- On successful password change, the SPA MUST display a success notification and return to the settings page (close dialog in modal mode, navigate back in page mode).

**Edge cases:**
- If the API returns 400 `INVALID_CURRENT_PASSWORD`, the form MUST display an inline error on the Current Password field (this is already handled by the existing `ChangePasswordDialog` component).
- If the user's session expires during the password change flow, the token refresh interceptor MUST handle it transparently (per `SDD-UI-001` Section 2.1).

### 2.5 Navigation Entry

- The user profile dropdown in the app bar MUST include a "Settings" menu item.
- The "Settings" item MUST be placed after the "Form Display" section and before the "Logout" item, separated by dividers.
- The "Settings" item MUST use a gear icon (`mdi-cog`).
- Clicking the "Settings" item MUST navigate to `/settings`.
- The "Settings" label MUST be translated via i18n for both English and Bulgarian.

### 2.6 Routes

The following routes MUST be registered:

| Route | Name | Page Component | Auth Required | Description |
|---|---|---|---|---|
| `/settings` | `settings` | `SettingsView` | Yes | User settings page |
| `/settings/edit-profile` | `settings-edit-profile` | `SettingsEditProfilePage` | Yes | Page-mode profile editing |
| `/settings/change-password` | `settings-change-password` | `SettingsChangePasswordPage` | Yes | Page-mode password change |

- All settings routes MUST be children of the authenticated layout route (same parent as dashboard, users, etc.).
- Page-mode routes (`/settings/edit-profile`, `/settings/change-password`) MUST be lazy-loaded (dynamic `import()`).
- The `/settings` route MUST NOT appear in the sidebar navigation (it is accessed via the profile dropdown only).
- The `/settings/edit-profile` and `/settings/change-password` routes MUST be recognized as form pages by the layout (added to the `formPageRoutes` array in `DefaultLayout.vue`) so that the `v-container` wrapper is removed and the form fills the viewport.
- The app bar page title MUST update dynamically via route `meta.titleKey` for each settings route.

**Edge cases:**
- If a user navigates directly to `/settings/edit-profile` via URL (e.g., bookmark), the page MUST load the user's profile from the API and render the edit form correctly, regardless of `formDisplayMode`.
- If a user navigates directly to `/settings/change-password` via URL, the page MUST render the password change form correctly using the `userId` from the auth store.

### 2.7 i18n Labels

The following translation keys MUST be defined in both `en.ts` and `bg.ts` locale files:

| Key Path | English | Bulgarian |
|---|---|---|
| `settings.title` | Settings | Настройки |
| `settings.profile` | Profile | Профил |
| `settings.editProfile` | Edit Profile | Редактиране на профил |
| `settings.changePassword` | Change Password | Промяна на парола |
| `settings.profileUpdated` | Profile updated successfully. | Профилът е обновен успешно. |
| `settings.profileLoadError` | Unable to load your profile. Your account may have been deactivated. | Профилът не може да бъде зареден. Акаунтът ви може да е деактивиран. |
| `settings.retryLoad` | Retry | Опитай отново |
| `settings.username` | Username | Потребителско име |
| `settings.email` | Email | Имейл |
| `settings.firstName` | First Name | Име |
| `settings.lastName` | Last Name | Фамилия |
| `nav.settings` | Settings | Настройки |
| `pageTitle.settings` | Settings | Настройки |
| `pageTitle.editProfile` | Edit Profile | Редактиране на профил |
| `pageTitle.settingsChangePassword` | Change Password | Промяна на парола |

- All new UI labels MUST use `$t()` or `t()` for rendering. Hardcoded strings MUST NOT be used.

---

## 3. Validation Rules

### 3.1 Profile Edit Form

Client-side validation mirrors the backend rules from `SDD-AUTH-001` Section 3.1 (same as `SDD-UI-001` Section 3.1 for the admin user edit form).

| # | Field | Rule | Error Message |
|---|---|---|---|
| V1 | Email | Required. Valid email format. Max 256 characters. | Please enter a valid email address. |
| V2 | FirstName | Required. 1-100 characters. | First name is required. / First name must be under 100 characters. |
| V3 | LastName | Required. 1-100 characters. | Last name is required. / Last name must be under 100 characters. |

### 3.2 Password Change Form

Validation is handled by the existing `ChangePasswordDialog` component per `SDD-UI-001` Section 3.5.

| # | Field | Rule | Error Message |
|---|---|---|---|
| V4 | Current Password | Required. | Current password is required. |
| V5 | New Password | Required. Min 8 characters. Must contain uppercase, lowercase, and digit. | New password must be at least 8 characters with uppercase, lowercase, and a number. |

### 3.3 Auth Store: User ID

| # | Field | Rule | Error Behavior |
|---|---|---|---|
| V6 | `userId` | MUST be a positive integer decoded from JWT `sub` claim. | If missing or non-numeric, set to `null`. Settings page displays error state. |

---

## 4. Error Rules

### 4.1 Settings Page Errors

| # | Trigger | Type | UI Behavior |
|---|---|---|---|
| E1 | `GET /api/v1/users/{id}` returns 404 (user deactivated). | Not Found | Display translated error message in the settings page card: "Unable to load your profile. Your account may have been deactivated." No profile data or action buttons shown. |
| E2 | `GET /api/v1/users/{id}` fails with network error or 500. | Server Error | Display generic error message with a "Retry" button. On retry, re-attempt the API call. |
| E3 | `userId` is `null` (JWT missing `sub` claim). | Client Error | Display error state on settings page. Do not attempt API calls. Show message guiding the user to log out and log in again. |

### 4.2 Profile Edit Errors

| # | Trigger | Type | UI Behavior |
|---|---|---|---|
| E4 | `PUT /api/v1/users/{id}` returns 409 `DUPLICATE_EMAIL`. | Conflict | Display inline error on Email field: "This email is already taken." |
| E5 | `PUT /api/v1/users/{id}` returns 404 `USER_NOT_FOUND`. | Not Found | Display snackbar error. Navigate back to `/settings`. |
| E6 | `PUT /api/v1/users/{id}` returns 400 `VALIDATION_ERROR`. | Validation | Map `errors` object from ProblemDetails to inline field errors (same pattern as admin user edit). |
| E7 | `PUT /api/v1/users/{id}` fails with network error or 500. | Server Error | Display snackbar: "An unexpected error occurred. Please try again." |

### 4.3 Password Change Errors

All password change errors are handled by the existing `ChangePasswordDialog` component as defined in `SDD-UI-001` Section 4 (E14 for `INVALID_CURRENT_PASSWORD`, E15 for `VALIDATION_ERROR`, E17 for unexpected errors). No new error handling is needed.

---

## 5. Versioning Notes

- **v1 -- Initial specification (2026-04-03)**
  - Self-service user settings page at `/settings`
  - Profile viewing and editing (email, first name, last name)
  - Self-service password change reusing `ChangePasswordDialog`
  - New `ProfileEditDialog` organism with `FormWrapper` integration
  - Auth store extended with `userId` from JWT `sub` claim
  - Form display mode integration (modal / page)
  - i18n support for English and Bulgarian
  - Navigation entry in user profile dropdown

---

## 6. Test Plan

### Unit Tests -- Auth Store: User ID

- `userId_DecodedFromJwtSubClaimOnLogin` [Unit]
- `userId_PersistedToLocalStorage` [Unit]
- `userId_RestoredFromLocalStorageOnInit` [Unit]
- `userId_ClearedOnLogout` [Unit]
- `userId_MissingSubClaim_ReturnsNull` [Unit]
- `userId_NonNumericSubClaim_ReturnsNull` [Unit]
- `userId_InvalidLocalStorageValue_FallsBackToNull` [Unit]

### Unit Tests -- SettingsView

- `SettingsView_LoadsCurrentUserProfile` [Unit]
- `SettingsView_DisplaysUsernameReadOnly` [Unit]
- `SettingsView_DisplaysEmailFirstNameLastName` [Unit]
- `SettingsView_ShowsLoadingIndicatorWhileFetching` [Unit]
- `SettingsView_ProfileLoadError404_DisplaysDeactivatedMessage` [Unit]
- `SettingsView_ProfileLoadNetworkError_DisplaysRetryButton` [Unit]
- `SettingsView_RetryButton_ReAttemptsProfileLoad` [Unit]
- `SettingsView_NullUserId_DisplaysErrorState` [Unit]

### Unit Tests -- ProfileEditDialog

- `ProfileEditDialog_ModeDialog_RendersInsideFormWrapper` [Unit]
- `ProfileEditDialog_ModePage_RendersInsideFormWrapper` [Unit]
- `ProfileEditDialog_PrePopulatesWithCurrentData` [Unit]
- `ProfileEditDialog_UsernameFieldNotPresent` [Unit]
- `ProfileEditDialog_EmptyEmail_ShowsValidationError` [Unit]
- `ProfileEditDialog_InvalidEmail_ShowsValidationError` [Unit]
- `ProfileEditDialog_EmptyFirstName_ShowsValidationError` [Unit]
- `ProfileEditDialog_EmptyLastName_ShowsValidationError` [Unit]
- `ProfileEditDialog_ValidSubmit_CallsUpdateUserApi` [Unit]
- `ProfileEditDialog_DuplicateEmail_ShowsInlineError` [Unit]
- `ProfileEditDialog_UserNotFound_ShowsSnackbarError` [Unit]
- `ProfileEditDialog_EmitsSavedOnSuccess` [Unit]
- `ProfileEditDialog_EmitsCancelledOnCancel` [Unit]

### Unit Tests -- Settings Page Mode Branching

- `SettingsView_ModalMode_EditProfileOpensDialog` [Unit]
- `SettingsView_PageMode_EditProfileNavigatesToRoute` [Unit]
- `SettingsView_ModalMode_ChangePasswordOpensDialog` [Unit]
- `SettingsView_PageMode_ChangePasswordNavigatesToRoute` [Unit]

### Unit Tests -- Page Wrapper Components

- `SettingsEditProfilePage_RendersProfileEditDialogInPageMode` [Unit]
- `SettingsEditProfilePage_LoadsUserProfileFromApi` [Unit]
- `SettingsEditProfilePage_SavedEvent_NavigatesBackToSettings` [Unit]
- `SettingsEditProfilePage_CancelledEvent_NavigatesBackToSettings` [Unit]
- `SettingsChangePasswordPage_RendersChangePasswordDialogInPageMode` [Unit]
- `SettingsChangePasswordPage_PassesUserIdFromAuthStore` [Unit]
- `SettingsChangePasswordPage_SavedEvent_NavigatesBackToSettings` [Unit]

### Unit Tests -- Navigation Entry

- `UserMenu_SettingsItem_IsPresent` [Unit]
- `UserMenu_SettingsItem_NavigatesToSettings` [Unit]
- `UserMenu_SettingsItem_HasGearIcon` [Unit]

### Unit Tests -- i18n

- `i18n_SettingsLabels_TranslatedInEnglish` [Unit]
- `i18n_SettingsLabels_TranslatedInBulgarian` [Unit]
- `i18n_PageTitles_SettingsTranslatedInEnglish` [Unit]
- `i18n_PageTitles_SettingsTranslatedInBulgarian` [Unit]

### Integration Tests -- Settings Flow

- `settingsPage_LoadsCurrentUserProfile` [Integration]
- `settingsPage_EditProfile_ModalMode_OpensDialogAndSaves` [Integration]
- `settingsPage_EditProfile_PageMode_NavigatesAndSaves` [Integration]
- `settingsPage_ChangePassword_ModalMode_OpensDialogAndSaves` [Integration]
- `settingsPage_ChangePassword_PageMode_NavigatesAndSaves` [Integration]
- `settingsPage_EditProfile_DuplicateEmail_ShowsInlineError` [Integration]
- `settingsPage_ChangePassword_WrongCurrentPassword_ShowsInlineError` [Integration]
- `settingsPage_UnauthenticatedAccess_RedirectsToLogin` [Integration]
- `settingsPage_DirectUrlToEditProfile_RendersFormRegardlessOfMode` [Integration]

---

## Key Files

- `frontend/src/stores/auth.ts` (modified -- add `userId` property decoded from JWT)
- `frontend/src/router/index.ts` (modified -- add `/settings`, `/settings/edit-profile`, `/settings/change-password` routes)
- `frontend/src/views/settings/SettingsView.vue` (new -- settings page)
- `frontend/src/views/settings/SettingsEditProfilePage.vue` (new -- page-mode profile edit wrapper)
- `frontend/src/views/settings/SettingsChangePasswordPage.vue` (new -- page-mode password change wrapper)
- `frontend/src/components/organisms/ProfileEditDialog.vue` (new -- profile edit form organism)
- `frontend/src/components/organisms/ChangePasswordDialog.vue` (existing -- reused for password change)
- `frontend/src/components/molecules/FormWrapper.vue` (existing -- used by ProfileEditDialog)
- `frontend/src/components/templates/DefaultLayout.vue` (modified -- add Settings menu item to profile dropdown, add form page route names)
- `frontend/src/i18n/locales/en.ts` (modified -- add settings translation keys)
- `frontend/src/i18n/locales/bg.ts` (modified -- add settings translation keys)
- `frontend/src/api/users.ts` (existing -- `getUserById`, `updateUser`, `changePassword` already exist)
- `frontend/src/types/user.ts` (existing -- `UserDetailDto`, `UpdateUserRequest`, `ChangePasswordRequest` already exist)
