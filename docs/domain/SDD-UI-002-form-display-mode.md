# SDD-UI-002 — Form Display Mode (Modal vs Page)

> Status: Draft
> Last updated: 2026-04-03
> Owner: TBD
> Category: Domain

## 1. Context & Scope

This spec defines a user-configurable form display mode that controls how CRUD form interactions open in the Warehouse Management SPA. Users can choose between two modes: **modal** (forms open as dialog overlays on the current list page) and **page** (forms open as dedicated route pages with a back button). The setting is per-user, persisted to `localStorage`, and toggled from the user profile dropdown in the application bar.

The modal mode preserves the existing behavior defined in `SDD-UI-001` and implemented via organism dialog components (`UserFormDialog`, `ChangePasswordDialog`, `UserRolesDialog`, `RoleFormDialog`, `RolePermissionsDialog`, `PermissionFormDialog`). The page mode introduces new routes and wrapper page components that embed the same organism components in a full-page layout instead of a `v-dialog`.

**In scope:**
- Form display mode setting (`modal` or `page`) in the Pinia layout store
- Persistence of the selected mode to `localStorage`
- Toggle control in the user profile dropdown menu (Display section)
- New routes for all form pages (create, edit, password, role assignment, permission assignment)
- Page wrapper components that embed existing form organisms in a `v-card` layout
- Refactoring form organisms to work in both dialog and page contexts
- Back button navigation on page-mode form views
- i18n support for setting labels (English and Bulgarian)

**Out of scope:**
- Per-form-type mode selection (the setting is global, not per entity)
- Server-side persistence of the preference (localStorage only)
- Changes to form validation, error handling, or API interactions (these remain identical in both modes)
- New form fields or new CRUD operations
- Mobile-responsive layout for page-mode forms (desktop-first, per `SDD-UI-001`)

**Related specs:**
- `SDD-UI-001` — Auth Administration SPA (defines the base modal dialog behavior, layout store, user profile dropdown, routing, and Atomic Design component structure)
- `SDD-AUTH-001` — Authentication and Authorization API (backend endpoints consumed by all forms)

---

## 2. Behavior

### 2.1 Form Display Mode Setting

- The layout store MUST expose a `formDisplayMode` property with two valid values: `'modal'` and `'page'`.
- The default value MUST be `'modal'` when no persisted preference exists.
- The SPA MUST persist the selected `formDisplayMode` to `localStorage` under a dedicated key.
- The SPA MUST restore the `formDisplayMode` from `localStorage` on application initialization.
- Changing the `formDisplayMode` MUST take effect immediately without a page reload.
- The `formDisplayMode` MUST NOT affect the Audit Log viewer (audit is read-only, no forms).

**Edge cases:**
- If `localStorage` contains an invalid value for `formDisplayMode` (e.g., `'invalid'`, empty string, or a non-string type), the SPA MUST fall back to `'modal'` and overwrite the invalid value in `localStorage`.
- If `localStorage` is unavailable (e.g., private browsing with storage disabled), the SPA SHOULD default to `'modal'` and operate without persistence. The SPA MUST NOT crash or display errors.

### 2.2 User Profile Dropdown Toggle

- The user profile dropdown menu MUST include a "Form Display" subsection in the Display section, alongside the existing density toggle.
- The subsection MUST present two selectable options: "Modal" and "Page" (or their translated equivalents).
- The options MUST use a radio-style selection pattern (consistent with the Language section in the dropdown, per `CHG-ENH-001`).
- The currently active mode MUST be visually indicated (e.g., a check icon or filled radio indicator).
- The label "Form Display" and the option labels "Modal" and "Page" MUST be translated via i18n for both English and Bulgarian.

### 2.3 Modal Mode Behavior (Default)

- When `formDisplayMode` is `'modal'`, all form interactions MUST open as Vuetify `v-dialog` overlays on the current list page.
- This MUST preserve the exact behavior defined in `SDD-UI-001` Sections 2.3, 2.4, and 2.5: action chips on list rows open the corresponding dialog component.
- Modal dialogs MUST be scrollable (`scrollable` prop) with a max height of `85vh` to prevent overflow on small screens.
- The modal footer MUST have a dark background matching the sidebar color (`#334155`) with white button text, consistent with page mode.
- Closing a dialog (via cancel, save, or backdrop click) MUST return focus to the list page without navigation.
- The URL MUST NOT change when a modal dialog opens or closes.

### 2.4 Page Mode Behavior

- When `formDisplayMode` is `'page'`, all form interactions MUST navigate to a dedicated route page instead of opening a dialog.
- Each page-mode form MUST display inside a flat, full-width `v-card` with no border-radius that fills the available viewport (not centered with a max-width like the modal).
- The page form card title MUST be hidden — the app bar already shows the page title dynamically via route meta.
- The `v-container` padding MUST be removed for form page routes so the form stretches edge-to-edge.
- The form page MUST use flexbox layout to fill `v-main` without causing vertical overflow or inactive scrollbars.
- Each page-mode form MUST include a back button bar at the top of the form (above the form fields, below the app bar) with an arrow-left icon and translated "Back" label.
- The back button MUST navigate to the parent list route (e.g., `/users`, `/roles`, `/permissions`).
- The form footer (card-actions) MUST be pinned to the bottom of the viewport using `margin-top: auto` within the flex layout.
- The form footer MUST have a dark background matching the sidebar color (`#334155`) with white button text.
- The form footer MUST NOT have rounded corners in page mode.
- The form content area (`v-card-text`) MUST have top padding (`24px`) to prevent the first input's outline from being clipped by the back bar.
- On successful form submission (create or edit), the SPA MUST navigate back to the originating list page.
- On cancel, the SPA MUST navigate back to the originating list page without submitting changes.
- The app bar page title (per `CHG-ENH-001` Section 2.2) MUST update to reflect the form action (e.g., "Create User", "Edit Role", "Change Password").

**Edge cases:**
- If a user navigates directly to a page-mode edit route (e.g., `/users/5/edit` via URL bar or bookmark) and the entity ID does not exist, the SPA MUST display a "Not Found" message and provide a link back to the list page. The SPA MUST NOT crash.
- If a user navigates directly to a page-mode edit route while `formDisplayMode` is set to `'modal'`, the route MUST still render the page form correctly. The mode setting controls how forms are opened from list views, not whether the page routes are accessible.

### 2.5 Routes for Page Mode Forms

The following routes MUST be registered for page-mode form views:

| Route | Page Component | Form Organism Embedded | Auth Required |
|---|---|---|---|
| `/users/create` | `UserCreatePage` | `UserFormDialog` (create mode) | Yes |
| `/users/:id/edit` | `UserEditPage` | `UserFormDialog` (edit mode) | Yes |
| `/users/:id/password` | `UserPasswordPage` | `ChangePasswordDialog` | Yes |
| `/users/:id/roles` | `UserRolesPage` | `UserRolesDialog` | Yes |
| `/roles/create` | `RoleCreatePage` | `RoleFormDialog` (create mode) | Yes |
| `/roles/:id/edit` | `RoleEditPage` | `RoleFormDialog` (edit mode) | Yes |
| `/roles/:id/permissions` | `RolePermissionsPage` | `RolePermissionsDialog` | Yes |
| `/permissions/create` | `PermissionCreatePage` | `PermissionFormDialog` (create mode) | Yes |

- All page-mode routes MUST be protected by the same authentication guard defined in `SDD-UI-001` Section 2.2.
- All page-mode route components MUST be lazy-loaded (dynamic `import()`) to avoid increasing the initial bundle size.
- Routes with `:id` parameter MUST validate that `id` is a positive integer. Non-integer values MUST redirect to the parent list page.

### 2.6 Form Organism Refactoring

- Each form organism (`UserFormDialog`, `ChangePasswordDialog`, `UserRolesDialog`, `RoleFormDialog`, `RolePermissionsDialog`, `PermissionFormDialog`) MUST support rendering in both dialog and page contexts.
- Each organism MUST accept a `mode` prop with values `'dialog'` or `'page'` that controls the outer wrapper.
- When `mode` is `'dialog'`, the organism MUST render its content inside a `v-dialog` with a `v-card` (current behavior).
- When `mode` is `'page'`, the organism MUST render its content inside a standalone `v-card` without any `v-dialog` wrapper.
- The form content (fields, validation, submit/cancel actions, API calls) MUST be identical in both modes.
- The organism MUST emit a `saved` event on successful submission and a `cancelled` event on cancel, regardless of mode.
- The page wrapper component is responsible for handling `saved` and `cancelled` events by navigating back to the list page.
- The list view is responsible for handling `saved` and `cancelled` events by closing the dialog and refreshing the table.

### 2.7 List View Integration

- In list views (`UsersView`, `RolesView`, `PermissionsView`), action chips MUST check the `formDisplayMode` from the layout store.
- When `formDisplayMode` is `'modal'`, clicking an action chip MUST open the corresponding dialog (current behavior).
- When `formDisplayMode` is `'page'`, clicking an action chip MUST navigate to the corresponding page route using `router.push()`.
- The "Create" button on each list view MUST also respect `formDisplayMode`: open a create dialog in modal mode, or navigate to the create route in page mode.
- Confirm dialogs (e.g., deactivate user confirmation, delete role confirmation) MUST always open as modals regardless of `formDisplayMode`. Only CRUD form dialogs are affected by the mode setting.

### 2.8 i18n Labels

- The following translation keys MUST be defined in both `en.ts` and `bg.ts` locale files:

| Key Path | English | Bulgarian |
|---|---|---|
| `settings.formDisplay` | Form Display | Показване на формуляри |
| `settings.formDisplayModal` | Modal | Модален прозорец |
| `settings.formDisplayPage` | Page | Цяла страница |
| `pageTitle.createUser` | Create User | Създаване на потребител |
| `pageTitle.editUser` | Edit User | Редактиране на потребител |
| `pageTitle.changePassword` | Change Password | Промяна на парола |
| `pageTitle.manageUserRoles` | Manage User Roles | Управление на роли |
| `pageTitle.createRole` | Create Role | Създаване на роля |
| `pageTitle.editRole` | Edit Role | Редактиране на роля |
| `pageTitle.manageRolePermissions` | Manage Permissions | Управление на права |
| `pageTitle.createPermission` | Create Permission | Създаване на право |

- All new UI labels MUST use `$t()` or `t()` for rendering. Hardcoded strings MUST NOT be used.

---

## 3. Validation Rules

### 3.1 Form Display Mode Value

| # | Field | Rule | Error Behavior |
|---|---|---|---|
| V1 | `formDisplayMode` | MUST be `'modal'` or `'page'`. | Invalid values silently reset to `'modal'`. |

### 3.2 Route Parameter: Entity ID

| # | Field | Rule | Error Behavior |
|---|---|---|---|
| V2 | `:id` in page-mode routes | MUST be a positive integer (`> 0`). | Non-integer or non-positive values redirect to the parent list route. |

### 3.3 No New Form Validation Rules

All form field validation rules remain identical to those defined in `SDD-UI-001` Section 3. The form display mode changes the presentation context only, not the validation behavior.

---

## 4. Error Rules

### 4.1 Page-Mode Specific Errors

| # | Trigger | Type | UI Behavior |
|---|---|---|---|
| E1 | User navigates to `/users/:id/edit` (or any `:id` edit/action route) and the entity does not exist (API returns 404). | Not Found | Display a "Not Found" message in the page card with a link back to the parent list page. Do not display a snackbar. |
| E2 | User navigates to a page-mode route with an invalid `:id` parameter (non-integer, zero, negative). | Validation | Redirect to the parent list page immediately via route guard. No error message displayed. |
| E3 | User navigates to a page-mode route while unauthenticated. | Auth | Redirect to `/login` with `?redirect=` preserving the intended page-mode route (existing guard behavior per `SDD-UI-001` Section 2.2). |

### 4.2 Existing Error Rules Unchanged

All error rules from `SDD-UI-001` Section 4 (E1 through E17) remain in effect and apply identically in both modal and page modes. The form organisms handle API errors the same way regardless of their rendering context.

---

## 5. Versioning Notes

- **v2 -- Page mode layout refinements (2026-04-03)** (non-breaking)
  - Page mode: full-width flat card, no border-radius, hidden card title
  - Back button moved to top bar above form fields (not in footer)
  - Footer pinned to bottom with dark sidebar-colored background
  - Container padding removed for form page routes
  - Modal mode: scrollable with 85vh max, dark footer
  - Form content top padding to prevent input clipping
  - FormWrapper molecule handles all dialog/page rendering logic
- **v1 -- Initial specification (2026-04-03)**
  - Defines form display mode feature: modal vs page rendering for all CRUD forms
  - Adds `formDisplayMode` to Pinia layout store with `localStorage` persistence
  - Introduces 8 new page-mode routes for user, role, and permission forms
  - Defines organism refactoring strategy (`mode` prop: `'dialog'` | `'page'`)
  - Adds i18n labels for EN and BG
  - Non-breaking: modal remains the default, page mode is opt-in

---

## 6. Test Plan

### Unit Tests -- Layout Store

- `formDisplayMode_DefaultsToModal` [Unit]
- `formDisplayMode_SetToPage_UpdatesState` [Unit]
- `formDisplayMode_PersistsToLocalStorage` [Unit]
- `formDisplayMode_RestoresFromLocalStorage` [Unit]
- `formDisplayMode_InvalidLocalStorageValue_FallsBackToModal` [Unit]
- `formDisplayMode_LocalStorageUnavailable_DefaultsToModal` [Unit]

### Unit Tests -- Form Organism Mode Prop

- `UserFormDialog_ModeDialog_RendersInsideVDialog` [Unit]
- `UserFormDialog_ModePage_RendersWithoutVDialog` [Unit]
- `UserFormDialog_ModeDialog_EmitsSavedOnSuccess` [Unit]
- `UserFormDialog_ModePage_EmitsSavedOnSuccess` [Unit]
- `RoleFormDialog_ModeDialog_RendersInsideVDialog` [Unit]
- `RoleFormDialog_ModePage_RendersWithoutVDialog` [Unit]
- `PermissionFormDialog_ModeDialog_RendersInsideVDialog` [Unit]
- `PermissionFormDialog_ModePage_RendersWithoutVDialog` [Unit]
- `ChangePasswordDialog_ModePage_RendersWithoutVDialog` [Unit]
- `UserRolesDialog_ModePage_RendersWithoutVDialog` [Unit]
- `RolePermissionsDialog_ModePage_RendersWithoutVDialog` [Unit]

### Unit Tests -- List View Mode Branching

- `UsersView_ModalMode_EditChipOpensDialog` [Unit]
- `UsersView_PageMode_EditChipNavigatesToRoute` [Unit]
- `UsersView_ModalMode_CreateButtonOpensDialog` [Unit]
- `UsersView_PageMode_CreateButtonNavigatesToRoute` [Unit]
- `RolesView_PageMode_EditChipNavigatesToRoute` [Unit]
- `PermissionsView_PageMode_CreateButtonNavigatesToRoute` [Unit]
- `UsersView_ConfirmDeactivate_AlwaysOpensModal` [Unit]
- `RolesView_ConfirmDelete_AlwaysOpensModal` [Unit]

### Unit Tests -- Page Wrapper Components

- `UserCreatePage_RendersFormOrganismInPageMode` [Unit]
- `UserEditPage_RendersFormOrganismWithEntityData` [Unit]
- `UserEditPage_BackButton_NavigatesToUsersList` [Unit]
- `RoleCreatePage_RendersFormOrganismInPageMode` [Unit]
- `RoleEditPage_BackButton_NavigatesToRolesList` [Unit]
- `PermissionCreatePage_RendersFormOrganismInPageMode` [Unit]
- `UserPasswordPage_RendersChangePasswordInPageMode` [Unit]
- `UserRolesPage_RendersUserRolesDialogInPageMode` [Unit]
- `RolePermissionsPage_RendersRolePermissionsInPageMode` [Unit]

### Unit Tests -- Route Validation

- `pageRoute_InvalidId_RedirectsToList` [Unit]
- `pageRoute_NonIntegerId_RedirectsToList` [Unit]
- `pageRoute_ZeroId_RedirectsToList` [Unit]
- `pageRoute_NegativeId_RedirectsToList` [Unit]

### Unit Tests -- i18n

- `i18n_FormDisplayLabels_TranslatedInEnglish` [Unit]
- `i18n_FormDisplayLabels_TranslatedInBulgarian` [Unit]
- `i18n_PageTitles_TranslatedInEnglish` [Unit]
- `i18n_PageTitles_TranslatedInBulgarian` [Unit]

### Unit Tests -- User Profile Dropdown

- `userMenu_FormDisplaySection_ShowsModalAndPageOptions` [Unit]
- `userMenu_FormDisplaySection_HighlightsActiveMode` [Unit]
- `userMenu_FormDisplayToggle_ChangesStoreValue` [Unit]

### Integration Tests -- Page Mode Navigation

- `usersPage_PageMode_CreateChipNavigatesAndRendersForm` [Integration]
- `usersPage_PageMode_EditChipNavigatesAndLoadsEntity` [Integration]
- `usersPage_PageMode_FormSubmit_NavigatesBackToList` [Integration]
- `usersPage_PageMode_FormCancel_NavigatesBackToList` [Integration]
- `usersPage_PageMode_ChangePassword_NavigatesAndRendersForm` [Integration]
- `usersPage_PageMode_ManageRoles_NavigatesAndRendersForm` [Integration]
- `rolesPage_PageMode_EditChipNavigatesAndLoadsEntity` [Integration]
- `rolesPage_PageMode_ManagePermissions_NavigatesAndRendersForm` [Integration]
- `permissionsPage_PageMode_CreateNavigatesAndRendersForm` [Integration]
- `pageMode_DirectUrlToNonExistentEntity_ShowsNotFound` [Integration]
- `pageMode_DirectUrlAccess_RendersFormRegardlessOfModeSetting` [Integration]
- `pageMode_UnauthenticatedAccess_RedirectsToLoginWithRedirect` [Integration]

---

## Key Files

- `frontend/src/stores/layout.ts` (modified -- add `formDisplayMode` property)
- `frontend/src/router/index.ts` (modified -- add page-mode routes)
- `frontend/src/i18n/locales/en.ts` (modified -- add form display and page title keys)
- `frontend/src/i18n/locales/bg.ts` (modified -- add form display and page title keys)
- `frontend/src/components/templates/DefaultLayout.vue` (modified -- add form display toggle to user menu)
- `frontend/src/components/organisms/UserFormDialog.vue` (modified -- accept `mode` prop)
- `frontend/src/components/organisms/ChangePasswordDialog.vue` (modified -- accept `mode` prop)
- `frontend/src/components/organisms/UserRolesDialog.vue` (modified -- accept `mode` prop)
- `frontend/src/components/organisms/RoleFormDialog.vue` (modified -- accept `mode` prop)
- `frontend/src/components/organisms/RolePermissionsDialog.vue` (modified -- accept `mode` prop)
- `frontend/src/components/organisms/PermissionFormDialog.vue` (modified -- accept `mode` prop)
- `frontend/src/views/users/UsersView.vue` (modified -- mode-aware action routing)
- `frontend/src/views/roles/RolesView.vue` (modified -- mode-aware action routing)
- `frontend/src/views/permissions/PermissionsView.vue` (modified -- mode-aware action routing)
- `frontend/src/views/users/UserCreatePage.vue` (new -- page wrapper)
- `frontend/src/views/users/UserEditPage.vue` (new -- page wrapper)
- `frontend/src/views/users/UserPasswordPage.vue` (new -- page wrapper)
- `frontend/src/views/users/UserRolesPage.vue` (new -- page wrapper)
- `frontend/src/views/roles/RoleCreatePage.vue` (new -- page wrapper)
- `frontend/src/views/roles/RoleEditPage.vue` (new -- page wrapper)
- `frontend/src/views/roles/RolePermissionsPage.vue` (new -- page wrapper)
- `frontend/src/views/permissions/PermissionCreatePage.vue` (new -- page wrapper)
