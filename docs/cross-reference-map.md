# Cross-Reference Map

> Last updated: 2026-04-03

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
| `frontend/src/router/index.ts` | SDD-UI-001 |
| `frontend/src/stores/auth.ts` | SDD-UI-001 |
| `frontend/src/api/client.ts` | SDD-UI-001 |
| `frontend/src/views/LoginView.vue` | SDD-UI-001 |
| `frontend/src/views/users/UsersView.vue` | SDD-UI-001 |
| `frontend/src/views/roles/RolesView.vue` | SDD-UI-001 |
| `frontend/src/views/permissions/PermissionsView.vue` | SDD-UI-001 |
| `frontend/src/views/audit/AuditView.vue` | SDD-UI-001 |
| `frontend/src/layouts/DefaultLayout.vue` | SDD-UI-001 |
