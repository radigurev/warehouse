# Cross-Reference Map

> Last updated: 2026-04-03
>
> Note: SDD-UI-003 added 2026-04-03

This document provides a bidirectional mapping between SDD specifications and source files.

## Section 1 — Spec → Source Files

### SDD-AUTH-001 — Authentication and Authorization

| File | Type | Role |
|---|---|---|
| `src/Interfaces/Warehouse.Auth.API/Controllers/AuthController.cs` | Controller | Login, refresh, logout endpoints |
| `src/Interfaces/Warehouse.Auth.API/Controllers/UsersController.cs` | Controller | User CRUD endpoints |
| `src/Interfaces/Warehouse.Auth.API/Controllers/RolesController.cs` | Controller | Role CRUD endpoints |
| `src/Interfaces/Warehouse.Auth.API/Controllers/PermissionsController.cs` | Controller | Permission endpoints |
| `src/Interfaces/Warehouse.Auth.API/Controllers/AuditController.cs` | Controller | Audit log endpoints |
| `src/Interfaces/Warehouse.Auth.API/Authorization/PermissionAuthorizationHandler.cs` | Middleware | RBAC permission checks |
| `src/Databases/Warehouse.DBModel/Models/Auth/` | Entities | All auth database models |
| `src/Databases/Warehouse.DBModel/WarehouseDbContext.cs` | DbContext | EF Core context |

### SDD-UI-001 — Auth Administration SPA

| File | Type | Role |
|---|---|---|
| `frontend/src/main.ts` | Entry point | App bootstrap, plugin registration |
| `frontend/src/router/index.ts` | Router | Route definitions, auth guards |
| `frontend/src/stores/auth.ts` | Store | JWT token and session state |
| `frontend/src/api/client.ts` | HTTP client | Axios instance with interceptors |
| `frontend/src/views/LoginView.vue` | View | Login page |
| `frontend/src/views/users/UsersView.vue` | View | User management |
| `frontend/src/views/roles/RolesView.vue` | View | Role management |
| `frontend/src/views/permissions/PermissionsView.vue` | View | Permission management |
| `frontend/src/views/audit/AuditView.vue` | View | Audit log viewer |
| `frontend/src/layouts/DefaultLayout.vue` | Layout | Sidebar + app bar |

### SDD-UI-002 — Form Display Mode (Modal vs Page)

| File | Type | Role |
|---|---|---|
| `frontend/src/stores/layout.ts` | Store | formDisplayMode state and localStorage persistence |
| `frontend/src/router/index.ts` | Router | Page-mode form routes |
| `frontend/src/i18n/locales/en.ts` | i18n | English translations for form display labels |
| `frontend/src/i18n/locales/bg.ts` | i18n | Bulgarian translations for form display labels |
| `frontend/src/components/templates/DefaultLayout.vue` | Layout | Form display toggle in user profile dropdown |
| `frontend/src/components/organisms/UserFormDialog.vue` | Organism | Refactored with mode prop (dialog/page) |
| `frontend/src/components/organisms/ChangePasswordDialog.vue` | Organism | Refactored with mode prop (dialog/page) |
| `frontend/src/components/organisms/UserRolesDialog.vue` | Organism | Refactored with mode prop (dialog/page) |
| `frontend/src/components/organisms/RoleFormDialog.vue` | Organism | Refactored with mode prop (dialog/page) |
| `frontend/src/components/organisms/RolePermissionsDialog.vue` | Organism | Refactored with mode prop (dialog/page) |
| `frontend/src/components/organisms/PermissionFormDialog.vue` | Organism | Refactored with mode prop (dialog/page) |
| `frontend/src/views/users/UsersView.vue` | View | Mode-aware action chip routing |
| `frontend/src/views/roles/RolesView.vue` | View | Mode-aware action chip routing |
| `frontend/src/views/permissions/PermissionsView.vue` | View | Mode-aware action chip routing |
| `frontend/src/views/users/UserCreatePage.vue` | View | Page-mode wrapper for user creation |
| `frontend/src/views/users/UserEditPage.vue` | View | Page-mode wrapper for user editing |
| `frontend/src/views/users/UserPasswordPage.vue` | View | Page-mode wrapper for password change |
| `frontend/src/views/users/UserRolesPage.vue` | View | Page-mode wrapper for role assignment |
| `frontend/src/views/roles/RoleCreatePage.vue` | View | Page-mode wrapper for role creation |
| `frontend/src/views/roles/RoleEditPage.vue` | View | Page-mode wrapper for role editing |
| `frontend/src/views/roles/RolePermissionsPage.vue` | View | Page-mode wrapper for permission assignment |
| `frontend/src/views/permissions/PermissionCreatePage.vue` | View | Page-mode wrapper for permission creation |

### SDD-UI-003 — User Settings

| File | Type | Role |
|---|---|---|
| `frontend/src/stores/auth.ts` | Store | Extended with userId decoded from JWT sub claim |
| `frontend/src/router/index.ts` | Router | Settings routes (/settings, /settings/edit-profile, /settings/change-password) |
| `frontend/src/views/settings/SettingsView.vue` | View | User settings page with profile display |
| `frontend/src/views/settings/SettingsEditProfilePage.vue` | View | Page-mode wrapper for profile editing |
| `frontend/src/views/settings/SettingsChangePasswordPage.vue` | View | Page-mode wrapper for password change |
| `frontend/src/components/organisms/ProfileEditDialog.vue` | Organism | Profile edit form with FormWrapper |
| `frontend/src/components/organisms/ChangePasswordDialog.vue` | Organism | Reused for self-service password change |
| `frontend/src/components/templates/DefaultLayout.vue` | Layout | Settings menu item in user profile dropdown |
| `frontend/src/i18n/locales/en.ts` | i18n | English translations for settings labels |
| `frontend/src/i18n/locales/bg.ts` | i18n | Bulgarian translations for settings labels |

## Section 2 — Source File → Specs

| Source File | Spec ID(s) |
|---|---|
| `src/Interfaces/Warehouse.Auth.API/Controllers/AuthController.cs` | SDD-AUTH-001 |
| `src/Interfaces/Warehouse.Auth.API/Controllers/UsersController.cs` | SDD-AUTH-001 |
| `src/Interfaces/Warehouse.Auth.API/Controllers/RolesController.cs` | SDD-AUTH-001 |
| `src/Interfaces/Warehouse.Auth.API/Controllers/PermissionsController.cs` | SDD-AUTH-001 |
| `src/Interfaces/Warehouse.Auth.API/Controllers/AuditController.cs` | SDD-AUTH-001 |
| `src/Interfaces/Warehouse.Auth.API/Authorization/PermissionAuthorizationHandler.cs` | SDD-AUTH-001 |
| `src/Databases/Warehouse.DBModel/Models/Auth/*` | SDD-AUTH-001 |
| `frontend/src/router/index.ts` | SDD-UI-001, SDD-UI-002, SDD-UI-003 |
| `frontend/src/stores/auth.ts` | SDD-UI-001, SDD-UI-003 |
| `frontend/src/stores/layout.ts` | SDD-UI-001, SDD-UI-002 |
| `frontend/src/api/client.ts` | SDD-UI-001 |
| `frontend/src/views/LoginView.vue` | SDD-UI-001 |
| `frontend/src/views/users/UsersView.vue` | SDD-UI-001, SDD-UI-002 |
| `frontend/src/views/roles/RolesView.vue` | SDD-UI-001, SDD-UI-002 |
| `frontend/src/views/permissions/PermissionsView.vue` | SDD-UI-001, SDD-UI-002 |
| `frontend/src/views/audit/AuditView.vue` | SDD-UI-001 |
| `frontend/src/layouts/DefaultLayout.vue` | SDD-UI-001 |
| `frontend/src/components/templates/DefaultLayout.vue` | SDD-UI-001, SDD-UI-002, SDD-UI-003 |
| `frontend/src/components/organisms/UserFormDialog.vue` | SDD-UI-001, SDD-UI-002 |
| `frontend/src/components/organisms/ChangePasswordDialog.vue` | SDD-UI-001, SDD-UI-002, SDD-UI-003 |
| `frontend/src/components/organisms/UserRolesDialog.vue` | SDD-UI-001, SDD-UI-002 |
| `frontend/src/components/organisms/RoleFormDialog.vue` | SDD-UI-001, SDD-UI-002 |
| `frontend/src/components/organisms/RolePermissionsDialog.vue` | SDD-UI-001, SDD-UI-002 |
| `frontend/src/components/organisms/PermissionFormDialog.vue` | SDD-UI-001, SDD-UI-002 |
| `frontend/src/i18n/locales/en.ts` | SDD-UI-001, SDD-UI-002, SDD-UI-003 |
| `frontend/src/i18n/locales/bg.ts` | SDD-UI-001, SDD-UI-002, SDD-UI-003 |
| `frontend/src/views/users/UserCreatePage.vue` | SDD-UI-002 |
| `frontend/src/views/users/UserEditPage.vue` | SDD-UI-002 |
| `frontend/src/views/users/UserPasswordPage.vue` | SDD-UI-002 |
| `frontend/src/views/users/UserRolesPage.vue` | SDD-UI-002 |
| `frontend/src/views/roles/RoleCreatePage.vue` | SDD-UI-002 |
| `frontend/src/views/roles/RoleEditPage.vue` | SDD-UI-002 |
| `frontend/src/views/roles/RolePermissionsPage.vue` | SDD-UI-002 |
| `frontend/src/views/permissions/PermissionCreatePage.vue` | SDD-UI-002 |
| `frontend/src/views/settings/SettingsView.vue` | SDD-UI-003 |
| `frontend/src/views/settings/SettingsEditProfilePage.vue` | SDD-UI-003 |
| `frontend/src/views/settings/SettingsChangePasswordPage.vue` | SDD-UI-003 |
| `frontend/src/components/organisms/ProfileEditDialog.vue` | SDD-UI-003 |
