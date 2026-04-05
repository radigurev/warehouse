# Cross-Reference Map

> Last updated: 2026-04-05
>
> Note: Updated all paths for domain-split restructure (Auth.DBModel, Customers.DBModel, Interfaces/Auth/, Interfaces/Customers/)

This document provides a bidirectional mapping between SDD specifications and source files.

## Section 1 — Spec → Source Files

### SDD-AUTH-001 — Authentication and Authorization

| File | Type | Role |
|---|---|---|
| `src/Interfaces/Auth/Warehouse.Auth.API/Controllers/AuthController.cs` | Controller | Login, refresh, logout endpoints |
| `src/Interfaces/Auth/Warehouse.Auth.API/Controllers/UsersController.cs` | Controller | User CRUD endpoints |
| `src/Interfaces/Auth/Warehouse.Auth.API/Controllers/RolesController.cs` | Controller | Role CRUD endpoints |
| `src/Interfaces/Auth/Warehouse.Auth.API/Controllers/PermissionsController.cs` | Controller | Permission endpoints |
| `src/Interfaces/Auth/Warehouse.Auth.API/Controllers/AuditController.cs` | Controller | Audit log endpoints |
| `src/Interfaces/Auth/Warehouse.Auth.API/Authorization/PermissionAuthorizationHandler.cs` | Middleware | RBAC permission checks |
| `src/Databases/Warehouse.Auth.DBModel/Models/` | Entities | All auth database models |
| `src/Databases/Warehouse.Auth.DBModel/AuthDbContext.cs` | DbContext | Auth EF Core context |

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

### SDD-CUST-001 — Customers and Accounts

| File | Type | Role |
|---|---|---|
| `src/Interfaces/Customers/Warehouse.Customers.API/Controllers/CustomersController.cs` | Controller | Customer CRUD endpoints |
| `src/Interfaces/Customers/Warehouse.Customers.API/Controllers/CustomerAccountsController.cs` | Controller | Account management endpoints |
| `src/Interfaces/Customers/Warehouse.Customers.API/Controllers/CustomerContactsController.cs` | Controller | Address, phone, email endpoints |
| `src/Interfaces/Customers/Warehouse.Customers.API/Controllers/CustomerCategoriesController.cs` | Controller | Category CRUD endpoints |
| `src/Interfaces/Customers/Warehouse.Customers.API/Services/CustomerService.cs` | Service | Customer business logic |
| `src/Interfaces/Customers/Warehouse.Customers.API/Services/CustomerAccountService.cs` | Service | Account business logic |
| `src/Interfaces/Customers/Warehouse.Customers.API/Services/CustomerContactService.cs` | Service | Contact info business logic |
| `src/Interfaces/Customers/Warehouse.Customers.API/Services/CustomerCategoryService.cs` | Service | Category business logic |
| `src/Databases/Warehouse.Customers.DBModel/Models/` | Entities | All customer database models |
| `src/Databases/Warehouse.Customers.DBModel/CustomersDbContext.cs` | DbContext | Customers EF Core context |
| `src/Warehouse.ServiceModel/DTOs/Customers/` | DTOs | Customer domain DTOs |
| `src/Warehouse.ServiceModel/Requests/Customers/` | Requests | Customer request models |
| `src/Warehouse.Mapping/Profiles/Customers/CustomerMappingProfile.cs` | Mapping | AutoMapper profile |
| `src/Interfaces/Customers/Warehouse.Customers.API.Tests/` | Tests | Unit and integration tests |

### SDD-INV-001 — Products and Catalog

| File | Type | Role |
|---|---|---|
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ProductsController.cs` | Controller | Product CRUD endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ProductCategoriesController.cs` | Controller | Product category CRUD endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/UnitsOfMeasureController.cs` | Controller | Unit of measure CRUD endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/BomController.cs` | Controller | Bill of materials endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ProductAccessoriesController.cs` | Controller | Product accessory endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ProductSubstitutesController.cs` | Controller | Product substitute endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/ProductService.cs` | Service | Product business logic |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/ProductCategoryService.cs` | Service | Category business logic |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/UnitOfMeasureService.cs` | Service | UoM business logic |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/BomService.cs` | Service | BOM business logic |
| `src/Databases/Warehouse.Inventory.DBModel/Models/Product.cs` | Entity | Product entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/ProductCategory.cs` | Entity | Category entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/UnitOfMeasure.cs` | Entity | UoM entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/BillOfMaterials.cs` | Entity | BOM header entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/BomLine.cs` | Entity | BOM line entity |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/ProductServiceTests.cs` | Test | Product service tests |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/BomServiceTests.cs` | Test | BOM service tests |

### SDD-INV-002 — Stock Management

| File | Type | Role |
|---|---|---|
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StockLevelsController.cs` | Controller | Stock level query endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StockMovementsController.cs` | Controller | Stock movement endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/InventoryAdjustmentsController.cs` | Controller | Adjustment lifecycle endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/BatchesController.cs` | Controller | Batch/lot management endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/StockLevelService.cs` | Service | Stock level queries |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/StockMovementService.cs` | Service | Movement recording |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/InventoryAdjustmentService.cs` | Service | Adjustment workflow |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/BatchService.cs` | Service | Batch management |
| `src/Databases/Warehouse.Inventory.DBModel/Models/StockLevel.cs` | Entity | Stock level entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/StockMovement.cs` | Entity | Movement entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/InventoryAdjustment.cs` | Entity | Adjustment entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/Batch.cs` | Entity | Batch entity |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/StockMovementServiceTests.cs` | Test | Movement service tests |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/InventoryAdjustmentServiceTests.cs` | Test | Adjustment service tests |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/BatchServiceTests.cs` | Test | Batch service tests |

### SDD-INV-003 — Warehouse Structure

| File | Type | Role |
|---|---|---|
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/WarehousesController.cs` | Controller | Warehouse CRUD endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ZonesController.cs` | Controller | Zone CRUD endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StorageLocationsController.cs` | Controller | Storage location CRUD endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/WarehouseTransfersController.cs` | Controller | Transfer lifecycle endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/WarehouseService.cs` | Service | Warehouse business logic |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/ZoneService.cs` | Service | Zone business logic |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/StorageLocationService.cs` | Service | Location business logic |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/WarehouseTransferService.cs` | Service | Transfer workflow |
| `src/Databases/Warehouse.Inventory.DBModel/Models/WarehouseEntity.cs` | Entity | Warehouse entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/Zone.cs` | Entity | Zone entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/StorageLocation.cs` | Entity | Location entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/WarehouseTransfer.cs` | Entity | Transfer entity |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/WarehouseServiceTests.cs` | Test | Warehouse service tests |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/ZoneServiceTests.cs` | Test | Zone service tests |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/WarehouseTransferServiceTests.cs` | Test | Transfer service tests |

### SDD-INV-004 — Stocktaking

| File | Type | Role |
|---|---|---|
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StocktakeSessionsController.cs` | Controller | Stocktake session lifecycle endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/StocktakeSessionService.cs` | Service | Stocktake session workflow |
| `src/Databases/Warehouse.Inventory.DBModel/Models/StocktakeSession.cs` | Entity | Stocktake session entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/StocktakeCount.cs` | Entity | Count entry entity |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/StocktakeSessionServiceTests.cs` | Test | Stocktake service tests |

## Section 2 — Source File → Specs

| Source File | Spec ID(s) |
|---|---|
| `src/Interfaces/Auth/Warehouse.Auth.API/Controllers/AuthController.cs` | SDD-AUTH-001 |
| `src/Interfaces/Auth/Warehouse.Auth.API/Controllers/UsersController.cs` | SDD-AUTH-001 |
| `src/Interfaces/Auth/Warehouse.Auth.API/Controllers/RolesController.cs` | SDD-AUTH-001 |
| `src/Interfaces/Auth/Warehouse.Auth.API/Controllers/PermissionsController.cs` | SDD-AUTH-001 |
| `src/Interfaces/Auth/Warehouse.Auth.API/Controllers/AuditController.cs` | SDD-AUTH-001 |
| `src/Interfaces/Auth/Warehouse.Auth.API/Authorization/PermissionAuthorizationHandler.cs` | SDD-AUTH-001 |
| `src/Databases/Warehouse.Auth.DBModel/Models/*` | SDD-AUTH-001 |
| `src/Databases/Warehouse.Customers.DBModel/Models/*` | SDD-CUST-001 |
| `src/Interfaces/Customers/Warehouse.Customers.API/Controllers/*` | SDD-CUST-001 |
| `src/Interfaces/Customers/Warehouse.Customers.API/Services/*` | SDD-CUST-001 |
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
| `src/Databases/Warehouse.Inventory.DBModel/Models/*` | SDD-INV-001, SDD-INV-002, SDD-INV-003, SDD-INV-004 |
| `src/Databases/Warehouse.Inventory.DBModel/InventoryDbContext.cs` | SDD-INV-001, SDD-INV-002, SDD-INV-003, SDD-INV-004 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ProductsController.cs` | SDD-INV-001 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ProductCategoriesController.cs` | SDD-INV-001 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/UnitsOfMeasureController.cs` | SDD-INV-001 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/BomController.cs` | SDD-INV-001 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StockLevelsController.cs` | SDD-INV-002 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StockMovementsController.cs` | SDD-INV-002 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/InventoryAdjustmentsController.cs` | SDD-INV-002 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/BatchesController.cs` | SDD-INV-002 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/WarehousesController.cs` | SDD-INV-003 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ZonesController.cs` | SDD-INV-003 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StorageLocationsController.cs` | SDD-INV-003 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/WarehouseTransfersController.cs` | SDD-INV-003 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StocktakeSessionsController.cs` | SDD-INV-004 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/*` | SDD-INV-001, SDD-INV-002, SDD-INV-003, SDD-INV-004 |
| `src/Warehouse.Mapping/Profiles/Inventory/InventoryMappingProfile.cs` | SDD-INV-001, SDD-INV-002, SDD-INV-003, SDD-INV-004 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/*` | SDD-INV-001, SDD-INV-002, SDD-INV-003, SDD-INV-004 |
