# Cross-Reference Map

> Last updated: 2026-04-16
>
> Note: Added CHG-ENH-001 for Nomenclature integration across consumer domains

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
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StocktakeCountsController.cs` | Controller | Stocktake count entry endpoints |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/StocktakeSessionService.cs` | Service | Stocktake session workflow |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/StocktakeCountService.cs` | Service | Count entry management |
| `src/Databases/Warehouse.Inventory.DBModel/Models/StocktakeSession.cs` | Entity | Stocktake session entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/StocktakeCount.cs` | Entity | Count entry entity |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/StocktakeSessionServiceTests.cs` | Test | Stocktake service tests |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/StocktakeCountServiceTests.cs` | Test | Count entry service tests |

### SDD-INV-005 — Batch Creation on Goods Receipt

| File | Type | Role |
|---|---|---|
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Consumers/GoodsReceiptCompletedConsumer.cs` | Consumer | MassTransit consumer for GoodsReceiptCompletedEvent |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Consumers/GoodsReceiptLineAcceptedConsumer.cs` | Consumer | MassTransit consumer for GoodsReceiptLineAcceptedEvent |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/ReceiptStockIntakeService.cs` | Service | Shared logic for batch resolution, movement creation, stock level upsert |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Interfaces/IReceiptStockIntakeService.cs` | Interface | Service interface for receipt stock intake |
| `src/Warehouse.ServiceModel/Events/GoodsReceiptCompletedEvent.cs` | Event | MassTransit event contract (existing, consumed here) |
| `src/Warehouse.ServiceModel/Events/GoodsReceiptLineAcceptedEvent.cs` | Event | MassTransit event contract (existing, consumed here) |
| `src/Databases/Warehouse.Inventory.DBModel/Models/Batch.cs` | Entity | Batch entity (existing, created by this flow) |
| `src/Databases/Warehouse.Inventory.DBModel/Models/StockMovement.cs` | Entity | Stock movement entity (existing, created by this flow) |
| `src/Databases/Warehouse.Inventory.DBModel/Models/StockLevel.cs` | Entity | Stock level entity (existing, upserted by this flow) |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Consumers/GoodsReceiptCompletedConsumerTests.cs` | Test | Consumer unit tests |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Consumers/GoodsReceiptLineAcceptedConsumerTests.cs` | Test | Accepted line consumer unit tests |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/ReceiptStockIntakeServiceTests.cs` | Test | Batch resolution unit tests |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Integration/Consumers/GoodsReceiptStockIntakeTests.cs` | Test | Integration tests with MassTransit test harness |

### SDD-PURCH-001 — Procurement Operations

| File | Type | Role |
|---|---|---|
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/SuppliersController.cs` | Controller | Supplier CRUD endpoints |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/SupplierCategoriesController.cs` | Controller | Supplier category CRUD endpoints |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/SupplierContactsController.cs` | Controller | Address, phone, email endpoints |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/PurchaseOrdersController.cs` | Controller | PO lifecycle endpoints |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/PurchaseOrderLinesController.cs` | Controller | PO line management endpoints |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/GoodsReceiptsController.cs` | Controller | Goods receiving endpoints |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/SupplierReturnsController.cs` | Controller | Supplier return endpoints |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/PurchaseEventsController.cs` | Controller | Purchase event history endpoints |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/SupplierService.cs` | Service | Supplier business logic |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/SupplierCategoryService.cs` | Service | Category business logic |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/SupplierContactService.cs` | Service | Contact info business logic |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/PurchaseOrderService.cs` | Service | PO lifecycle and status machine |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/GoodsReceiptService.cs` | Service | Goods receiving business logic |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/ReceivingInspectionService.cs` | Service | Inspection business logic |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/SupplierReturnService.cs` | Service | Supplier return business logic |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/PurchaseEventService.cs` | Service | Event history business logic |
| `src/Databases/Warehouse.Purchasing.DBModel/Models/` | Entities | All purchasing database models |
| `src/Databases/Warehouse.Purchasing.DBModel/PurchasingDbContext.cs` | DbContext | Purchasing EF Core context |
| `src/Warehouse.ServiceModel/DTOs/Purchasing/` | DTOs | Purchasing domain DTOs |
| `src/Warehouse.ServiceModel/Requests/Purchasing/` | Requests | Purchasing request models |
| `src/Warehouse.ServiceModel/Events/GoodsReceiptCompletedEvent.cs` | Event | MassTransit event contract |
| `src/Warehouse.ServiceModel/Events/SupplierReturnCompletedEvent.cs` | Event | MassTransit event contract |
| `src/Warehouse.ServiceModel/Events/GoodsReceiptLineAcceptedEvent.cs` | Event | MassTransit event contract |
| `src/Warehouse.Mapping/Profiles/Purchasing/PurchasingMappingProfile.cs` | Mapping | AutoMapper profile |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API.Tests/` | Tests | Unit and integration tests |

### SDD-FULF-001 — Fulfillment Operations

| File | Type | Role |
|---|---|---|
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Controllers/SalesOrdersController.cs` | Controller | SO lifecycle endpoints |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Controllers/SalesOrderLinesController.cs` | Controller | SO line management endpoints |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Controllers/PickListsController.cs` | Controller | Pick list generation and execution endpoints |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Controllers/PackingController.cs` | Controller | Parcel and packing endpoints |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Controllers/ShipmentsController.cs` | Controller | Shipment dispatch and tracking endpoints |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Controllers/CarriersController.cs` | Controller | Carrier CRUD endpoints |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Controllers/CarrierServiceLevelsController.cs` | Controller | Carrier service level endpoints |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Controllers/CustomerReturnsController.cs` | Controller | Customer return endpoints |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Controllers/FulfillmentEventsController.cs` | Controller | Fulfillment event history endpoints |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/SalesOrderService.cs` | Service | SO lifecycle and status machine |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/PickListService.cs` | Service | Pick list generation and execution |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/PackingService.cs` | Service | Packing business logic |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/ShipmentService.cs` | Service | Dispatch and tracking business logic |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/CarrierService.cs` | Service | Carrier business logic |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/CustomerReturnService.cs` | Service | Customer return business logic |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/FulfillmentEventService.cs` | Service | Event history business logic |
| `src/Databases/Warehouse.Fulfillment.DBModel/Models/` | Entities | All fulfillment database models |
| `src/Databases/Warehouse.Fulfillment.DBModel/FulfillmentDbContext.cs` | DbContext | Fulfillment EF Core context |
| `src/Warehouse.ServiceModel/DTOs/Fulfillment/` | DTOs | Fulfillment domain DTOs |
| `src/Warehouse.ServiceModel/Requests/Fulfillment/` | Requests | Fulfillment request models |
| `src/Warehouse.ServiceModel/Events/ShipmentDispatchedEvent.cs` | Event | MassTransit event contract |
| `src/Warehouse.ServiceModel/Events/CustomerReturnReceivedEvent.cs` | Event | MassTransit event contract |
| `src/Warehouse.ServiceModel/Events/StockReservationRequestedEvent.cs` | Event | MassTransit event contract |
| `src/Warehouse.ServiceModel/Events/StockReservationReleasedEvent.cs` | Event | MassTransit event contract |
| `src/Warehouse.Mapping/Profiles/Fulfillment/FulfillmentMappingProfile.cs` | Mapping | AutoMapper profile |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API.Tests/` | Tests | Unit and integration tests |

### SDD-UI-004 — Purchasing Operations SPA

| File | Type | Role |
|---|---|---|
| `frontend/src/features/purchasing/api/purchase-orders.ts` | API | PO CRUD and status action API calls |
| `frontend/src/features/purchasing/api/suppliers.ts` | API | Supplier CRUD API calls |
| `frontend/src/features/purchasing/api/supplier-categories.ts` | API | Supplier category CRUD API calls |
| `frontend/src/features/purchasing/api/supplier-contacts.ts` | API | Supplier address, phone, email API calls |
| `frontend/src/features/purchasing/api/goods-receipts.ts` | API | Goods receipt and inspection API calls |
| `frontend/src/features/purchasing/api/supplier-returns.ts` | API | Supplier return API calls |
| `frontend/src/features/purchasing/api/purchase-events.ts` | API | Purchase event history API calls |
| `frontend/src/features/purchasing/types/purchasing.ts` | Types | TypeScript types for purchasing domain |
| `frontend/src/features/purchasing/composables/*.ts` | Composables | View logic for purchasing views |
| `frontend/src/features/purchasing/views/**/*.vue` | Views | All purchasing view and page components |
| `frontend/src/features/purchasing/components/organisms/*.vue` | Organisms | Form dialogs and inspection dialogs |
| `frontend/src/app/router/purchasing.routes.ts` | Router | Purchasing feature routes |
| `frontend/src/shared/i18n/locales/en.ts` | i18n | English translations (purchasing keys) |
| `frontend/src/shared/i18n/locales/bg.ts` | i18n | Bulgarian translations (purchasing keys) |
| `frontend/src/shared/components/templates/DefaultLayout.vue` | Layout | Purchasing nav group in sidebar |

### SDD-UI-005 — Fulfillment Operations SPA

| File | Type | Role |
|---|---|---|
| `frontend/src/features/fulfillment/api/sales-orders.ts` | API | SO CRUD and status action API calls |
| `frontend/src/features/fulfillment/api/pick-lists.ts` | API | Pick list generation and execution API calls |
| `frontend/src/features/fulfillment/api/shipments.ts` | API | Shipment dispatch and tracking API calls |
| `frontend/src/features/fulfillment/api/carriers.ts` | API | Carrier and service level CRUD API calls |
| `frontend/src/features/fulfillment/api/customer-returns.ts` | API | Customer return CRUD and status API calls |
| `frontend/src/features/fulfillment/api/fulfillment-events.ts` | API | Fulfillment event history API calls |
| `frontend/src/features/fulfillment/types/fulfillment.ts` | Types | TypeScript types for fulfillment domain |
| `frontend/src/features/fulfillment/composables/*.ts` | Composables | View logic for fulfillment views |
| `frontend/src/features/fulfillment/views/**/*.vue` | Views | All fulfillment view and page components |
| `frontend/src/features/fulfillment/components/organisms/*.vue` | Organisms | Form, pick, packing, dispatch, and tracking dialogs |
| `frontend/src/app/router/fulfillment.routes.ts` | Router | Fulfillment feature routes |
| `frontend/src/shared/i18n/locales/en.ts` | i18n | English translations (fulfillment keys) |
| `frontend/src/shared/i18n/locales/bg.ts` | i18n | Bulgarian translations (fulfillment keys) |
| `frontend/src/shared/components/templates/DefaultLayout.vue` | Layout | Fulfillment nav group in sidebar |

### SDD-EVTLOG-001 — Centralized Event Logging Service

| File | Type | Role |
|---|---|---|
| `src/Infrastructure/EventLog/Warehouse.EventLog.API/Program.cs` | Entry point | DI root, MassTransit consumer registration, health checks |
| `src/Infrastructure/EventLog/Warehouse.EventLog.API/Controllers/EventsController.cs` | Controller | Read-only event query endpoints |
| `src/Infrastructure/EventLog/Warehouse.EventLog.API/Services/EventQueryService.cs` | Service | Search, get by ID, correlation timeline |
| `src/Infrastructure/EventLog/Warehouse.EventLog.API/Consumers/AuthAuditLoggedEventConsumer.cs` | Consumer | MassTransit consumer for auth audit events |
| `src/Infrastructure/EventLog/Warehouse.EventLog.API/Consumers/PurchaseEventOccurredEventConsumer.cs` | Consumer | MassTransit consumer for purchase events |
| `src/Infrastructure/EventLog/Warehouse.EventLog.API/Consumers/FulfillmentEventOccurredEventConsumer.cs` | Consumer | MassTransit consumer for fulfillment events |
| `src/Infrastructure/EventLog/Warehouse.EventLog.API/Consumers/InventoryEventOccurredEventConsumer.cs` | Consumer | MassTransit consumer for inventory events |
| `src/Infrastructure/EventLog/Warehouse.EventLog.API/Consumers/CustomerEventOccurredEventConsumer.cs` | Consumer | MassTransit consumer for customer events |
| `src/Databases/Warehouse.EventLog.DBModel/EventLogDbContext.cs` | DbContext | EF Core TPT configuration |
| `src/Databases/Warehouse.EventLog.DBModel/Models/OperationsEvent.cs` | Entity | Base TPT entity |
| `src/Databases/Warehouse.EventLog.DBModel/Models/AuthEvent.cs` | Entity | Auth derived entity |
| `src/Databases/Warehouse.EventLog.DBModel/Models/PurchaseEvent.cs` | Entity | Purchase derived entity |
| `src/Databases/Warehouse.EventLog.DBModel/Models/FulfillmentEvent.cs` | Entity | Fulfillment derived entity |
| `src/Databases/Warehouse.EventLog.DBModel/Models/InventoryEvent.cs` | Entity | Inventory derived entity |
| `src/Databases/Warehouse.EventLog.DBModel/Models/CustomerEvent.cs` | Entity | Customer derived entity |
| `src/Warehouse.ServiceModel/Events/AuthAuditLoggedEvent.cs` | Event | MassTransit contract |
| `src/Warehouse.ServiceModel/Events/PurchaseEventOccurredEvent.cs` | Event | MassTransit contract |
| `src/Warehouse.ServiceModel/Events/FulfillmentEventOccurredEvent.cs` | Event | MassTransit contract |
| `src/Warehouse.ServiceModel/Events/InventoryEventOccurredEvent.cs` | Event | MassTransit contract |
| `src/Warehouse.ServiceModel/Events/CustomerEventOccurredEvent.cs` | Event | MassTransit contract |
| `src/Warehouse.ServiceModel/DTOs/EventLog/` | DTOs | Event log domain DTOs |
| `src/Warehouse.ServiceModel/Requests/EventLog/SearchEventsRequest.cs` | Request | Unified event search request |
| `src/Warehouse.Mapping/Profiles/EventLog/EventLogMappingProfile.cs` | Mapping | AutoMapper profile |
| `src/Infrastructure/EventLog/Warehouse.EventLog.API.Tests/` | Tests | Unit and integration tests |

### SDD-INFRA-001 — Shared Infrastructure Library

| File | Type | Role |
|---|---|---|
| `src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Extension | All `Add*` DI registration extension methods |
| `src/Warehouse.Infrastructure/Extensions/WebApplicationExtensions.cs` | Extension | `UseWarehousePipeline` middleware pipeline |
| `src/Warehouse.Infrastructure/Middleware/CorrelationIdMiddleware.cs` | Middleware | Inbound correlation ID handling and NLog context |
| `src/Warehouse.Infrastructure/Middleware/GlobalExceptionHandlerMiddleware.cs` | Middleware | Global exception-to-ProblemDetails handler |
| `src/Warehouse.Infrastructure/Http/CorrelationIdDelegatingHandler.cs` | Handler | Outbound correlation ID propagation |
| `src/Warehouse.Infrastructure/Authorization/RequirePermissionAttribute.cs` | Attribute | Permission attribute for controllers |
| `src/Warehouse.Infrastructure/Authorization/PermissionRequirement.cs` | Requirement | Authorization requirement value object |
| `src/Warehouse.Infrastructure/Authorization/PermissionPolicyProvider.cs` | Provider | Dynamic policy provider for permissions |
| `src/Warehouse.Infrastructure/Authorization/PermissionAuthorizationHandler.cs` | Handler | JWT permission claim evaluator |
| `src/Warehouse.Infrastructure/Controllers/BaseApiController.cs` | Controller | Abstract base with Result-to-ActionResult helpers |
| `src/Warehouse.Infrastructure/Services/BaseEntityService.cs` | Service | Abstract base with EF Core helpers |
| `src/Warehouse.Infrastructure/Services/PrimaryFlagHelper.cs` | Utility | Static primary/default flag management |
| `src/Warehouse.Infrastructure/Configuration/FeatureFlags.cs` | Configuration | Feature flag name constants |
| `src/Warehouse.Infrastructure/Configuration/JwtSettings.cs` | Configuration | Strongly-typed JWT configuration model |
| `src/Warehouse.Common/Models/Result.cs` | Model | Non-generic Result outcome type |
| `src/Warehouse.Common/Models/ResultT.cs` | Model | Generic Result<T> outcome type |
| `src/Warehouse.Common/Interfaces/IEntity.cs` | Interface | Entity marker interface with int Id |

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
| `src/Databases/Warehouse.Inventory.DBModel/Models/*` | SDD-INV-001, SDD-INV-002, SDD-INV-003, SDD-INV-004, SDD-INV-005 |
| `src/Databases/Warehouse.Inventory.DBModel/InventoryDbContext.cs` | SDD-INV-001, SDD-INV-002, SDD-INV-003, SDD-INV-004, SDD-INV-005 |
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
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StocktakeCountsController.cs` | SDD-INV-004 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/*` | SDD-INV-001, SDD-INV-002, SDD-INV-003, SDD-INV-004, SDD-INV-005 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Consumers/GoodsReceiptCompletedConsumer.cs` | SDD-INV-005 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Consumers/GoodsReceiptLineAcceptedConsumer.cs` | SDD-INV-005 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Interfaces/IReceiptStockIntakeService.cs` | SDD-INV-005 |
| `src/Warehouse.Mapping/Profiles/Inventory/InventoryMappingProfile.cs` | SDD-INV-001, SDD-INV-002, SDD-INV-003, SDD-INV-004 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/*` | SDD-INV-001, SDD-INV-002, SDD-INV-003, SDD-INV-004 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Consumers/GoodsReceiptCompletedConsumerTests.cs` | SDD-INV-005 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Consumers/GoodsReceiptLineAcceptedConsumerTests.cs` | SDD-INV-005 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/ReceiptStockIntakeServiceTests.cs` | SDD-INV-005 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Integration/Consumers/GoodsReceiptStockIntakeTests.cs` | SDD-INV-005 |
| `src/Databases/Warehouse.Purchasing.DBModel/Models/*` | SDD-PURCH-001 |
| `src/Databases/Warehouse.Purchasing.DBModel/PurchasingDbContext.cs` | SDD-PURCH-001 |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/*` | SDD-PURCH-001 |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/*` | SDD-PURCH-001 |
| `src/Warehouse.ServiceModel/DTOs/Purchasing/*` | SDD-PURCH-001 |
| `src/Warehouse.ServiceModel/Requests/Purchasing/*` | SDD-PURCH-001 |
| `src/Warehouse.ServiceModel/Events/GoodsReceiptCompletedEvent.cs` | SDD-PURCH-001, SDD-INV-002, SDD-INV-005 |
| `src/Warehouse.ServiceModel/Events/SupplierReturnCompletedEvent.cs` | SDD-PURCH-001, SDD-INV-002 |
| `src/Warehouse.ServiceModel/Events/GoodsReceiptLineAcceptedEvent.cs` | SDD-PURCH-001, SDD-INV-002, SDD-INV-005 |
| `src/Warehouse.Mapping/Profiles/Purchasing/PurchasingMappingProfile.cs` | SDD-PURCH-001 |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API.Tests/Unit/Services/*` | SDD-PURCH-001 |
| `src/Databases/Warehouse.Fulfillment.DBModel/Models/*` | SDD-FULF-001 |
| `src/Databases/Warehouse.Fulfillment.DBModel/FulfillmentDbContext.cs` | SDD-FULF-001 |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Controllers/*` | SDD-FULF-001 |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/*` | SDD-FULF-001 |
| `src/Warehouse.ServiceModel/DTOs/Fulfillment/*` | SDD-FULF-001 |
| `src/Warehouse.ServiceModel/Requests/Fulfillment/*` | SDD-FULF-001 |
| `src/Warehouse.ServiceModel/Events/ShipmentDispatchedEvent.cs` | SDD-FULF-001, SDD-INV-002 |
| `src/Warehouse.ServiceModel/Events/CustomerReturnReceivedEvent.cs` | SDD-FULF-001, SDD-INV-002 |
| `src/Warehouse.ServiceModel/Events/StockReservationRequestedEvent.cs` | SDD-FULF-001, SDD-INV-002 |
| `src/Warehouse.ServiceModel/Events/StockReservationReleasedEvent.cs` | SDD-FULF-001, SDD-INV-002 |
| `src/Warehouse.Mapping/Profiles/Fulfillment/FulfillmentMappingProfile.cs` | SDD-FULF-001 |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API.Tests/Unit/Services/*` | SDD-FULF-001 |
| `src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | SDD-INFRA-001, SDD-OBS-001, SDD-INFRA-002, SDD-INFRA-003 |
| `src/Warehouse.Infrastructure/Extensions/WebApplicationExtensions.cs` | SDD-INFRA-001 |
| `src/Warehouse.Infrastructure/Middleware/CorrelationIdMiddleware.cs` | SDD-INFRA-001, SDD-OBS-001, SDD-INFRA-002 |
| `src/Warehouse.Infrastructure/Middleware/GlobalExceptionHandlerMiddleware.cs` | SDD-INFRA-001 |
| `src/Warehouse.Infrastructure/Http/CorrelationIdDelegatingHandler.cs` | SDD-INFRA-001, SDD-OBS-001 |
| `src/Warehouse.Infrastructure/Authorization/RequirePermissionAttribute.cs` | SDD-INFRA-001, SDD-AUTH-001 |
| `src/Warehouse.Infrastructure/Authorization/PermissionRequirement.cs` | SDD-INFRA-001, SDD-AUTH-001 |
| `src/Warehouse.Infrastructure/Authorization/PermissionPolicyProvider.cs` | SDD-INFRA-001, SDD-AUTH-001 |
| `src/Warehouse.Infrastructure/Authorization/PermissionAuthorizationHandler.cs` | SDD-INFRA-001, SDD-AUTH-001 |
| `src/Warehouse.Infrastructure/Controllers/BaseApiController.cs` | SDD-INFRA-001 |
| `src/Warehouse.Infrastructure/Services/BaseEntityService.cs` | SDD-INFRA-001 |
| `src/Warehouse.Infrastructure/Services/PrimaryFlagHelper.cs` | SDD-INFRA-001 |
| `src/Warehouse.Infrastructure/Configuration/FeatureFlags.cs` | SDD-INFRA-001 |
| `src/Warehouse.Infrastructure/Configuration/JwtSettings.cs` | SDD-INFRA-001 |
| `src/Warehouse.Common/Models/Result.cs` | SDD-INFRA-001 |
| `src/Warehouse.Common/Models/ResultT.cs` | SDD-INFRA-001 |
| `src/Warehouse.Common/Interfaces/IEntity.cs` | SDD-INFRA-001 |
| `src/Interfaces/Auth/Warehouse.Auth.API/nlog.config` | SDD-OBS-001 |
| `src/Interfaces/Customers/Warehouse.Customers.API/nlog.config` | SDD-OBS-001 |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/nlog.config` | SDD-OBS-001 |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/nlog.config` | SDD-OBS-001 |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/nlog.config` | SDD-OBS-001 |
| `src/Gateway/Warehouse.Gateway/nlog.config` | SDD-OBS-001, SDD-INFRA-002 |
| `docker-compose.infrastructure.yml` | SDD-OBS-001, SDD-INFRA-002 |
| `docker/loki/loki-config.yml` | SDD-OBS-001 |
| `docker/grafana/provisioning/datasources/loki.yml` | SDD-OBS-001 |
| `src/Gateway/Warehouse.Gateway/Program.cs` | SDD-INFRA-002 |
| `src/Gateway/Warehouse.Gateway/appsettings.json` | SDD-INFRA-002 |
| `src/Gateway/Warehouse.Gateway/appsettings.json.template` | SDD-INFRA-002 |
| `src/Gateway/Warehouse.Gateway/nlog.config` | SDD-INFRA-002 |
| `src/Gateway/Warehouse.Gateway/Warehouse.Gateway.csproj` | SDD-INFRA-002 |
| `src/Warehouse.Infrastructure/Middleware/CorrelationIdMiddleware.cs` | SDD-INFRA-002 |

### SDD-OBS-001 — Observability (Centralized Logging & Distributed Tracing)

| File | Type | Role |
|---|---|---|
| `src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Extension | `AddWarehouseTracing()` extension method for OpenTelemetry registration |
| `src/Warehouse.Infrastructure/Middleware/CorrelationIdMiddleware.cs` | Middleware | Sets CorrelationId in NLog scope context for log correlation |
| `src/Warehouse.Infrastructure/Http/CorrelationIdDelegatingHandler.cs` | Handler | Propagates correlation ID to outbound HTTP calls |
| `src/Interfaces/Auth/Warehouse.Auth.API/nlog.config` | Configuration | Auth service NLog targets (console, file, Loki) |
| `src/Interfaces/Customers/Warehouse.Customers.API/nlog.config` | Configuration | Customers service NLog targets |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/nlog.config` | Configuration | Inventory service NLog targets |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/nlog.config` | Configuration | Purchasing service NLog targets |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/nlog.config` | Configuration | Fulfillment service NLog targets |
| `src/Gateway/Warehouse.Gateway/nlog.config` | Configuration | Gateway NLog targets (no EF Core rule, adds Yarp rule) |
| `docker-compose.infrastructure.yml` | Docker | Loki, Grafana, Jaeger container definitions |
| `docker/loki/loki-config.yml` | Configuration | Loki server configuration (retention, schema, storage) |
| `docker/grafana/provisioning/datasources/loki.yml` | Configuration | Grafana Loki data source provisioning |

### SDD-INFRA-002 — API Gateway

| File | Type | Role |
|---|---|---|
| `src/Gateway/Warehouse.Gateway/Program.cs` | Entry point | YARP proxy setup, rate limiting, health aggregation, middleware pipeline |
| `src/Gateway/Warehouse.Gateway/appsettings.json` | Configuration | Route definitions, cluster destinations |
| `src/Gateway/Warehouse.Gateway/appsettings.json.template` | Template | Configuration template with placeholders |
| `src/Gateway/Warehouse.Gateway/nlog.config` | Configuration | NLog targets (console, file, Loki) for gateway logging |
| `src/Gateway/Warehouse.Gateway/Warehouse.Gateway.csproj` | Project | Package references (YARP, NLog, HealthChecks.Uris) |
| `src/Warehouse.Infrastructure/Middleware/CorrelationIdMiddleware.cs` | Middleware | Correlation ID generation and propagation |
| `src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Extension | Provides `AddWarehouseTracing()` used by gateway |

### SDD-INFRA-003 — Centralized Sequence Generation

| File | Type | Role |
|---|---|---|
| `src/Warehouse.Infrastructure/Sequences/ISequenceGenerator.cs` | Interface | Service interface with `NextAsync` and `NextBatchNumberAsync` |
| `src/Warehouse.Infrastructure/Sequences/SequenceGenerator.cs` | Service | Implementation with raw SQL atomic increment |
| `src/Warehouse.Infrastructure/Sequences/SequenceDefinition.cs` | Configuration | Sequence pattern definition record |
| `src/Warehouse.Infrastructure/Sequences/SequenceDefinitions.cs` | Configuration | Built-in definitions for all 12 sequence keys |
| `src/Warehouse.Common/Enums/SequenceResetPolicy.cs` | Enum | Reset policy: `Daily`, `Monthly`, `Never` |
| `src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Extension | `AddSequenceGenerator<TContext>()` DI registration |
| `src/Databases/Scripts/v1.0.0_AddInfrastructureSequencesTable.sql` | Migration | Shared SQL script for `infrastructure.Sequences` table |
| `src/Warehouse.Infrastructure.Tests/Sequences/SequenceGeneratorTests.cs` | Test | Unit and integration tests for sequence generation |
| `src/Warehouse.Infrastructure.Tests/Sequences/SequenceDefinitionTests.cs` | Test | Validation tests for sequence definitions |

### Section 2 Addendum — New UI Specs

| Source File | Spec ID(s) |
|---|---|
| `frontend/src/features/purchasing/api/*` | SDD-UI-004 |
| `frontend/src/features/purchasing/types/*` | SDD-UI-004 |
| `frontend/src/features/purchasing/composables/*` | SDD-UI-004 |
| `frontend/src/features/purchasing/views/**/*` | SDD-UI-004 |
| `frontend/src/features/purchasing/components/organisms/*` | SDD-UI-004 |
| `frontend/src/app/router/purchasing.routes.ts` | SDD-UI-004 |
| `frontend/src/features/fulfillment/api/*` | SDD-UI-005 |
| `frontend/src/features/fulfillment/types/*` | SDD-UI-005 |
| `frontend/src/features/fulfillment/composables/*` | SDD-UI-005 |
| `frontend/src/features/fulfillment/views/**/*` | SDD-UI-005 |
| `frontend/src/features/fulfillment/components/organisms/*` | SDD-UI-005 |
| `frontend/src/app/router/fulfillment.routes.ts` | SDD-UI-005 |
| `frontend/src/shared/i18n/locales/en.ts` | SDD-UI-001, SDD-UI-002, SDD-UI-003, SDD-UI-004, SDD-UI-005 |
| `frontend/src/shared/i18n/locales/bg.ts` | SDD-UI-001, SDD-UI-002, SDD-UI-003, SDD-UI-004, SDD-UI-005 |
| `frontend/src/shared/components/templates/DefaultLayout.vue` | SDD-UI-001, SDD-UI-002, SDD-UI-003, SDD-UI-004, SDD-UI-005 |
| `src/Infrastructure/EventLog/Warehouse.EventLog.API/**/*` | SDD-EVTLOG-001 |
| `src/Databases/Warehouse.EventLog.DBModel/**/*` | SDD-EVTLOG-001 |
| `src/Warehouse.ServiceModel/Events/AuthAuditLoggedEvent.cs` | SDD-EVTLOG-001 |
| `src/Warehouse.ServiceModel/Events/PurchaseEventOccurredEvent.cs` | SDD-EVTLOG-001 |
| `src/Warehouse.ServiceModel/Events/FulfillmentEventOccurredEvent.cs` | SDD-EVTLOG-001 |
| `src/Warehouse.ServiceModel/Events/InventoryEventOccurredEvent.cs` | SDD-EVTLOG-001 |
| `src/Warehouse.ServiceModel/Events/CustomerEventOccurredEvent.cs` | SDD-EVTLOG-001 |
| `src/Warehouse.ServiceModel/DTOs/EventLog/*` | SDD-EVTLOG-001 |
| `src/Warehouse.ServiceModel/Requests/EventLog/*` | SDD-EVTLOG-001 |
| `src/Warehouse.Infrastructure/Sequences/ISequenceGenerator.cs` | SDD-INFRA-003 |
| `src/Warehouse.Infrastructure/Sequences/SequenceGenerator.cs` | SDD-INFRA-003 |
| `src/Warehouse.Infrastructure/Sequences/SequenceDefinition.cs` | SDD-INFRA-003 |
| `src/Warehouse.Infrastructure/Sequences/SequenceDefinitions.cs` | SDD-INFRA-003 |
| `src/Warehouse.Common/Enums/SequenceResetPolicy.cs` | SDD-INFRA-003 |
| `src/Databases/Scripts/v1.0.0_AddInfrastructureSequencesTable.sql` | SDD-INFRA-003 |
| `src/Warehouse.Infrastructure.Tests/Sequences/SequenceGeneratorTests.cs` | SDD-INFRA-003 |
| `src/Warehouse.Infrastructure.Tests/Sequences/SequenceDefinitionTests.cs` | SDD-INFRA-003 |
| `src/Interfaces/Auth/Warehouse.Auth.API/Services/AuditService.cs` | SDD-AUTH-001, SDD-EVTLOG-001 |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/PurchaseEventService.cs` | SDD-PURCH-001, SDD-EVTLOG-001 |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/FulfillmentEventService.cs` | SDD-FULF-001, SDD-EVTLOG-001 |

### SDD-NOM-001 — Nomenclature Reference Data (Draft — No Implementation Yet)

| File | Type | Role |
|---|---|---|
| `src/Databases/Warehouse.Nomenclature.DBModel/NomenclatureDbContext.cs` | DbContext | Nomenclature EF Core context (planned) |
| `src/Databases/Warehouse.Nomenclature.DBModel/Models/Country.cs` | Entity | Country entity (planned) |
| `src/Databases/Warehouse.Nomenclature.DBModel/Models/StateProvince.cs` | Entity | State/province entity (planned) |
| `src/Databases/Warehouse.Nomenclature.DBModel/Models/City.cs` | Entity | City entity (planned) |
| `src/Databases/Warehouse.Nomenclature.DBModel/Models/Currency.cs` | Entity | Currency entity (planned) |
| `src/Interfaces/Nomenclature/Warehouse.Nomenclature.API/Controllers/CountriesController.cs` | Controller | Country CRUD endpoints (planned) |
| `src/Interfaces/Nomenclature/Warehouse.Nomenclature.API/Controllers/StateProvincesController.cs` | Controller | State/province endpoints (planned) |
| `src/Interfaces/Nomenclature/Warehouse.Nomenclature.API/Controllers/CitiesController.cs` | Controller | City endpoints (planned) |
| `src/Interfaces/Nomenclature/Warehouse.Nomenclature.API/Controllers/CurrenciesController.cs` | Controller | Currency CRUD endpoints (planned) |
| `src/Interfaces/Nomenclature/Warehouse.Nomenclature.API/Services/CountryService.cs` | Service | Country business logic (planned) |
| `src/Interfaces/Nomenclature/Warehouse.Nomenclature.API/Services/StateProvinceService.cs` | Service | State/province business logic (planned) |
| `src/Interfaces/Nomenclature/Warehouse.Nomenclature.API/Services/CityService.cs` | Service | City business logic (planned) |
| `src/Interfaces/Nomenclature/Warehouse.Nomenclature.API/Services/CurrencyService.cs` | Service | Currency business logic (planned) |
| `src/Warehouse.ServiceModel/DTOs/Nomenclature/` | DTOs | Nomenclature domain DTOs (planned) |
| `src/Warehouse.ServiceModel/Requests/Nomenclature/` | Requests | Nomenclature request models (planned) |
| `src/Warehouse.Mapping/Profiles/Nomenclature/NomenclatureMappingProfile.cs` | Mapping | AutoMapper profile (planned) |
| `src/Interfaces/Nomenclature/Warehouse.Nomenclature.API.Tests/` | Tests | Unit and integration tests (planned) |

### CHG-ENH-001 — Nomenclature Integration Across Consumer Domains (Draft)

| File | Type | Role |
|---|---|---|
| `src/Warehouse.Infrastructure/Services/NomenclatureResolver.cs` | Service | Read-only resolver for cached Nomenclature data (planned) |
| `src/Warehouse.Infrastructure/Interfaces/INomenclatureResolver.cs` | Interface | Nomenclature resolver contract (planned) |
| `frontend/src/shared/api/nomenclature.ts` | API | Shared Nomenclature API calls (planned) |
| `frontend/src/shared/composables/useNomenclature.ts` | Composable | Nomenclature data loading and caching (planned) |
| `frontend/src/shared/components/molecules/NomenclatureAddressFields.vue` | Component | Cascading Country > State > City dropdowns (planned) |
| `src/Warehouse.ServiceModel/DTOs/Customers/CustomerAddressDto.cs` | DTO | Add `CountryName` field (planned) |
| `src/Warehouse.ServiceModel/DTOs/Customers/CustomerAccountDto.cs` | DTO | Add `CurrencyName` field (planned) |
| `src/Warehouse.ServiceModel/DTOs/Purchasing/SupplierAddressDto.cs` | DTO | Add `CountryName` field (planned) |
| `src/Warehouse.ServiceModel/DTOs/Fulfillment/SalesOrderDetailDto.cs` | DTO | Add `ShippingCountryName` field (planned) |
| `src/Warehouse.ServiceModel/DTOs/Fulfillment/ShipmentDetailDto.cs` | DTO | Add `ShippingCountryName` field (planned) |
