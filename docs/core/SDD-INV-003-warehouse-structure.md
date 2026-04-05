# SDD-INV-003 — Warehouse Structure

> Status: Active
> Last updated: 2026-04-05
> Owner: TBD
> Category: Core

## 1. Context & Scope

This spec defines the Warehouse Structure sub-domain for the Warehouse Inventory system. It covers warehouse definitions, hierarchical storage locations (zone, row, shelf, bin), and warehouse-to-warehouse stock transfers.

**In scope:**
- Warehouse CRUD with soft-delete
- Zone management within warehouses
- Hierarchical storage location management (zone -> row -> shelf -> bin)
- Warehouse-to-warehouse stock transfers with transfer lines
- Audit columns on all entities

**Out of scope:**
- Product catalog management (see SDD-INV-001)
- Stock levels and movements (see SDD-INV-002 -- transfers create movements)
- Stocktaking (see SDD-INV-004)
- Warehouse floor plans and visual layouts (future enhancement)

**Related specs:**
- `SDD-AUTH-001` — All endpoints require JWT authentication and permission-based authorization.
- `SDD-INV-001` — Products referenced by transfer lines.
- `SDD-INV-002` — Stock levels and movements reference warehouses and locations defined here. Transfers create stock movements.

---

## 2. Behavior

### 2.1 Warehouses

#### 2.1.1 Create Warehouse

- The system MUST support creating a warehouse with a code, name, address, and active status.
- The system MUST enforce unique warehouse codes.
- The system MUST set `IsActive = true` by default on creation.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.

**Edge cases:**
- Creating a warehouse with a duplicate code MUST return a 409 Conflict error.

#### 2.1.2 Update Warehouse

- The system MUST support updating warehouse name, address, and notes.
- The system MUST NOT allow changing the warehouse code after creation.
- The system MUST update `ModifiedAtUtc` and `ModifiedByUserId` on every update.
- Updating a soft-deleted warehouse MUST return a 404 Not Found error.

#### 2.1.3 Get Warehouse

- The system MUST support retrieving a single warehouse by ID with its zones.
- Retrieving a soft-deleted warehouse MUST return a 404 Not Found error.

#### 2.1.4 List Warehouses

- The system MUST support paginated listing of warehouses.
- The system MUST support filtering by name (contains), code, and active status.
- The system MUST exclude soft-deleted warehouses by default.

#### 2.1.5 Deactivate Warehouse (Soft Delete)

- The system MUST support soft-deleting a warehouse.
- The system SHOULD prevent deactivation if the warehouse has non-zero stock levels. This SHOULD return a 409 Conflict error.

**Edge cases:**
- Deactivating an already soft-deleted warehouse MUST return a 404 Not Found error.

### 2.2 Zones

#### 2.2.1 Create Zone

- The system MUST support creating zones within a warehouse with a code, name, and optional description.
- The system MUST enforce unique zone codes within the same warehouse.

#### 2.2.2 Update Zone

- The system MUST support updating zone name and description.

#### 2.2.3 Delete Zone

- The system MUST prevent deletion of a zone that has storage locations. This MUST return a 409 Conflict error.

#### 2.2.4 List Zones

- The system MUST support listing all zones for a specific warehouse.

### 2.3 Storage Locations

#### 2.3.1 Create Storage Location

- The system MUST support creating storage locations with: warehouse ID, zone ID, code, name, location type (row, shelf, bin, bulk), and capacity.
- The system MUST enforce unique location codes within the same warehouse.

#### 2.3.2 Update Storage Location

- The system MUST support updating location name, type, and capacity.

#### 2.3.3 Delete Storage Location

- The system MUST prevent deletion of a location that has stock. This MUST return a 409 Conflict error.

#### 2.3.4 List Storage Locations

- The system MUST support listing all locations for a warehouse, optionally filtered by zone.

#### 2.3.5 Search Storage Locations

- The system MUST support paginated search across all warehouses with filtering by warehouse, zone, and code.

### 2.4 Warehouse Transfers

#### 2.4.1 Create Transfer

- The system MUST support creating a warehouse transfer with: source warehouse ID, destination warehouse ID, optional notes, and one or more transfer lines.
- Each transfer line MUST specify a product ID, quantity, and optional source/destination locations.
- The system MUST set the initial status to `Draft`.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId`.

**Edge cases:**
- Creating a transfer between the same warehouse (same source and destination) MUST return a 400 Validation error.
- Creating a transfer with no lines MUST return a 400 Validation error.

#### 2.4.2 Get Transfer

- The system MUST support retrieving a transfer by ID with all lines and their details.

#### 2.4.3 Search Transfers

- The system MUST support paginated listing of transfers with filtering by status, source warehouse, destination warehouse, and date range.

#### 2.4.4 Complete Transfer

- The system MUST support completing a draft transfer. This sets the status to `Completed`.
- Completing a transfer MUST create stock movements: outbound at source, inbound at destination for each line.
- The stock movement reason code MUST be `Transfer`.
- Completing MUST be performed within a single database transaction.
- The system MUST prevent completion if any line would result in negative stock at the source. This MUST return a 409 Conflict error.

#### 2.4.5 Cancel Transfer

- The system MUST support cancelling a draft transfer. This sets the status to `Cancelled`.
- Only transfers with status `Draft` MAY be cancelled.

---

## 3. Validation Rules

### 3.1 Warehouse

| # | Field | Rule | Error Code |
|---|---|---|---|
| V1 | Code | Required. 1-20 characters. Alphanumeric, hyphens. | `INVALID_WAREHOUSE_CODE` |
| V2 | Name | Required. 1-200 characters. | `INVALID_WAREHOUSE_NAME` |
| V3 | Address | Optional. Max 500 characters. | `INVALID_WAREHOUSE_ADDRESS` |
| V4 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |
| V5 | Code | Must be unique across all warehouses. | `DUPLICATE_WAREHOUSE_CODE` |

### 3.2 Zone

| # | Field | Rule | Error Code |
|---|---|---|---|
| V6 | Code | Required. 1-20 characters. | `INVALID_ZONE_CODE` |
| V7 | Name | Required. 1-100 characters. | `INVALID_ZONE_NAME` |
| V8 | Description | Optional. Max 500 characters. | `INVALID_ZONE_DESCRIPTION` |
| V9 | Code | Must be unique within the warehouse. | `DUPLICATE_ZONE_CODE` |

### 3.3 Storage Location

| # | Field | Rule | Error Code |
|---|---|---|---|
| V10 | Code | Required. 1-30 characters. | `INVALID_LOCATION_CODE` |
| V11 | Name | Required. 1-100 characters. | `INVALID_LOCATION_NAME` |
| V12 | LocationType | Required. One of: Row, Shelf, Bin, Bulk. | `INVALID_LOCATION_TYPE` |
| V13 | Capacity | Optional. Must be >= 0 when provided. | `INVALID_CAPACITY` |
| V14 | Code | Must be unique within the warehouse. | `DUPLICATE_LOCATION_CODE` |

### 3.4 Warehouse Transfer

| # | Field | Rule | Error Code |
|---|---|---|---|
| V15 | SourceWarehouseId | Required. Must reference an existing warehouse. | `INVALID_SOURCE_WAREHOUSE` |
| V16 | DestinationWarehouseId | Required. Must reference an existing warehouse. Must not equal source. | `INVALID_DESTINATION_WAREHOUSE` |
| V17 | Lines | Required. At least one line. | `TRANSFER_LINES_REQUIRED` |
| V18 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |

### 3.5 Transfer Line

| # | Field | Rule | Error Code |
|---|---|---|---|
| V19 | ProductId | Required. Must reference an existing product. | `INVALID_PRODUCT` |
| V20 | Quantity | Required. Must be > 0. | `INVALID_TRANSFER_QUANTITY` |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Warehouse not found (or soft-deleted) | 404 | `WAREHOUSE_NOT_FOUND` | Warehouse not found. |
| E2 | Duplicate warehouse code | 409 | `DUPLICATE_WAREHOUSE_CODE` | A warehouse with this code already exists. |
| E3 | Deactivate warehouse with stock | 409 | `WAREHOUSE_HAS_STOCK` | Cannot deactivate warehouse -- it has non-zero stock levels. |
| E4 | Zone not found | 404 | `ZONE_NOT_FOUND` | Zone not found. |
| E5 | Duplicate zone code in warehouse | 409 | `DUPLICATE_ZONE_CODE` | A zone with this code already exists in this warehouse. |
| E6 | Delete zone with locations | 409 | `ZONE_HAS_LOCATIONS` | Cannot delete zone -- it has {count} storage location(s). |
| E7 | Location not found | 404 | `LOCATION_NOT_FOUND` | Storage location not found. |
| E8 | Duplicate location code in warehouse | 409 | `DUPLICATE_LOCATION_CODE` | A location with this code already exists in this warehouse. |
| E9 | Delete location with stock | 409 | `LOCATION_HAS_STOCK` | Cannot delete location -- it has non-zero stock levels. |
| E10 | Transfer not found | 404 | `TRANSFER_NOT_FOUND` | Warehouse transfer not found. |
| E11 | Same source and destination | 400 | `TRANSFER_SAME_WAREHOUSE` | Source and destination warehouses must be different. |
| E12 | Complete transfer insufficient stock | 409 | `INSUFFICIENT_STOCK` | Insufficient stock at source for product {code}. Available: {available}, requested: {quantity}. |
| E13 | Cancel non-draft transfer | 409 | `TRANSFER_NOT_DRAFT` | Only draft transfers can be cancelled. Current status: {status}. |
| E14 | Complete non-draft transfer | 409 | `TRANSFER_NOT_DRAFT` | Only draft transfers can be completed. Current status: {status}. |
| E15 | Validation failure (field-level) | 400 | `VALIDATION_ERROR` | One or more fields are invalid. |
| E16 | Insufficient permissions | 403 | `FORBIDDEN` | You do not have permission to perform this action. |

All error responses MUST use ProblemDetails (RFC 7807) format.

---

## 5. API Endpoints

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| **Warehouses** | | | | |
| POST | `/api/v1/warehouses` | Create warehouse | Yes | `warehouses:create` |
| GET | `/api/v1/warehouses` | List warehouses (paginated) | Yes | `warehouses:read` |
| GET | `/api/v1/warehouses/{id}` | Get warehouse by ID | Yes | `warehouses:read` |
| PUT | `/api/v1/warehouses/{id}` | Update warehouse | Yes | `warehouses:update` |
| DELETE | `/api/v1/warehouses/{id}` | Soft-delete warehouse | Yes | `warehouses:delete` |
| **Zones** | | | | |
| POST | `/api/v1/warehouses/{warehouseId}/zones` | Create zone | Yes | `zones:create` |
| GET | `/api/v1/warehouses/{warehouseId}/zones` | List zones for warehouse | Yes | `zones:read` |
| PUT | `/api/v1/warehouses/{warehouseId}/zones/{zoneId}` | Update zone | Yes | `zones:update` |
| DELETE | `/api/v1/warehouses/{warehouseId}/zones/{zoneId}` | Delete zone | Yes | `zones:delete` |
| **Storage Locations** | | | | |
| POST | `/api/v1/warehouses/{warehouseId}/locations` | Create location | Yes | `storage-locations:create` |
| GET | `/api/v1/warehouses/{warehouseId}/locations` | List locations for warehouse | Yes | `storage-locations:read` |
| GET | `/api/v1/storage-locations` | Search locations across warehouses | Yes | `storage-locations:read` |
| PUT | `/api/v1/warehouses/{warehouseId}/locations/{locationId}` | Update location | Yes | `storage-locations:update` |
| DELETE | `/api/v1/warehouses/{warehouseId}/locations/{locationId}` | Delete location | Yes | `storage-locations:delete` |
| **Warehouse Transfers** | | | | |
| POST | `/api/v1/warehouse-transfers` | Create transfer | Yes | `stock:transfer` |
| GET | `/api/v1/warehouse-transfers` | Search transfers (paginated) | Yes | `stock:transfer` |
| GET | `/api/v1/warehouse-transfers/{id}` | Get transfer by ID | Yes | `stock:transfer` |
| POST | `/api/v1/warehouse-transfers/{id}/complete` | Complete transfer | Yes | `stock:transfer` |
| POST | `/api/v1/warehouse-transfers/{id}/cancel` | Cancel transfer | Yes | `stock:transfer` |

---

## 6. Database Schema

**Schema name:** `inventory`

### Tables

**inventory.Warehouses**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Code | NVARCHAR(20) | NOT NULL, UNIQUE |
| Name | NVARCHAR(200) | NOT NULL |
| Address | NVARCHAR(500) | NULL |
| Notes | NVARCHAR(2000) | NULL |
| IsActive | BIT | NOT NULL, DEFAULT 1 |
| IsDeleted | BIT | NOT NULL, DEFAULT 0 |
| DeletedAtUtc | DATETIME2(7) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| ModifiedAtUtc | DATETIME2(7) | NULL |
| ModifiedByUserId | INT | NULL |

**inventory.Zones**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| WarehouseId | INT | NOT NULL, FK -> inventory.Warehouses(Id) |
| Code | NVARCHAR(20) | NOT NULL |
| Name | NVARCHAR(100) | NOT NULL |
| Description | NVARCHAR(500) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**inventory.StorageLocations**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| WarehouseId | INT | NOT NULL, FK -> inventory.Warehouses(Id) |
| ZoneId | INT | NULL, FK -> inventory.Zones(Id) |
| Code | NVARCHAR(30) | NOT NULL |
| Name | NVARCHAR(100) | NOT NULL |
| LocationType | NVARCHAR(20) | NOT NULL |
| Capacity | DECIMAL(18,4) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**inventory.WarehouseTransfers**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| SourceWarehouseId | INT | NOT NULL, FK -> inventory.Warehouses(Id) |
| DestinationWarehouseId | INT | NOT NULL, FK -> inventory.Warehouses(Id) |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'Draft' |
| Notes | NVARCHAR(2000) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| CompletedAtUtc | DATETIME2(7) | NULL |
| CompletedByUserId | INT | NULL |

**inventory.WarehouseTransferLines**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| TransferId | INT | NOT NULL, FK -> inventory.WarehouseTransfers(Id) |
| ProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| Quantity | DECIMAL(18,4) | NOT NULL |
| SourceLocationId | INT | NULL, FK -> inventory.StorageLocations(Id) |
| DestinationLocationId | INT | NULL, FK -> inventory.StorageLocations(Id) |

### Indexes

| Name | Table | Columns | Type |
|---|---|---|---|
| IX_Warehouses_Code | Warehouses | Code | Unique |
| IX_Warehouses_IsDeleted | Warehouses | IsDeleted | Non-unique (filtered: IsDeleted = 0) |
| IX_Zones_WarehouseId | Zones | WarehouseId | Non-unique |
| IX_Zones_WarehouseId_Code | Zones | WarehouseId, Code | Unique |
| IX_StorageLocations_WarehouseId | StorageLocations | WarehouseId | Non-unique |
| IX_StorageLocations_ZoneId | StorageLocations | ZoneId | Non-unique |
| IX_StorageLocations_WarehouseId_Code | StorageLocations | WarehouseId, Code | Unique |
| IX_WarehouseTransfers_SourceWarehouseId | WarehouseTransfers | SourceWarehouseId | Non-unique |
| IX_WarehouseTransfers_DestinationWarehouseId | WarehouseTransfers | DestinationWarehouseId | Non-unique |
| IX_WarehouseTransfers_Status | WarehouseTransfers | Status | Non-unique |
| IX_WarehouseTransferLines_TransferId | WarehouseTransferLines | TransferId | Non-unique |

---

## 7. Versioning Notes

- **v1 -- Initial specification (2026-04-05)**
  - Warehouse CRUD with soft-delete
  - Zone management within warehouses
  - Storage location management with types
  - Warehouse transfer workflow (Draft -> Completed/Cancelled)
  - ProblemDetails error responses
  - Database schema on `inventory` schema

- **v2 -- Error alignment (2026-04-05)** (non-breaking)
  - Transfer insufficient stock error code changed from `TRANSFER_INSUFFICIENT_STOCK` to `INSUFFICIENT_STOCK` for consistency with SDD-INV-002
  - Status changed from Draft to Active

---

## 8. Test Plan

### Unit Tests -- WarehouseServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedWarehouse` [Unit]
- `CreateAsync_DuplicateCode_ReturnsConflictError` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedWarehouse` [Unit]
- `UpdateAsync_SoftDeletedWarehouse_ReturnsNotFound` [Unit]
- `GetByIdAsync_ExistingWarehouse_ReturnsWarehouse` [Unit]
- `GetByIdAsync_SoftDeletedWarehouse_ReturnsNotFound` [Unit]
- `DeactivateAsync_ActiveWarehouse_SetsIsDeletedAndDeletedAt` [Unit]
- `DeactivateAsync_AlreadyDeleted_ReturnsNotFound` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]

### Unit Tests -- ZoneServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedZone` [Unit]
- `CreateAsync_DuplicateCodeInWarehouse_ReturnsConflictError` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedZone` [Unit]
- `DeleteAsync_ZoneWithLocations_ReturnsConflict` [Unit]
- `DeleteAsync_EmptyZone_DeletesSuccessfully` [Unit]
- `ListAsync_ReturnsZonesForWarehouse` [Unit]

### Unit Tests -- StorageLocationServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedLocation` [Unit]
- `CreateAsync_DuplicateCodeInWarehouse_ReturnsConflictError` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedLocation` [Unit]
- `DeleteAsync_LocationWithStock_ReturnsConflict` [Unit]
- `DeleteAsync_EmptyLocation_DeletesSuccessfully` [Unit]
- `ListAsync_ReturnsLocationsForWarehouse` [Unit]

### Unit Tests -- WarehouseTransferServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedTransfer` [Unit]
- `CreateAsync_SameWarehouse_ReturnsValidationError` [Unit]
- `CreateAsync_NoLines_ReturnsValidationError` [Unit]
- `CompleteAsync_DraftTransfer_SetsCompletedAndCreatesMovements` [Unit]
- `CompleteAsync_NonDraftTransfer_ReturnsConflict` [Unit]
- `CompleteAsync_InsufficientStock_ReturnsConflict` [Unit]
- `CancelAsync_DraftTransfer_SetsCancelled` [Unit]
- `CancelAsync_NonDraftTransfer_ReturnsConflict` [Unit]

### Unit Tests -- Validation

- `CreateWarehouseRequestValidator_MissingCode_Fails` [Unit]
- `CreateWarehouseRequestValidator_MissingName_Fails` [Unit]
- `CreateZoneRequestValidator_MissingCode_Fails` [Unit]
- `CreateZoneRequestValidator_MissingName_Fails` [Unit]
- `CreateStorageLocationRequestValidator_MissingCode_Fails` [Unit]
- `CreateStorageLocationRequestValidator_InvalidType_Fails` [Unit]
- `CreateTransferRequestValidator_MissingSourceWarehouse_Fails` [Unit]
- `CreateTransferRequestValidator_EmptyLines_Fails` [Unit]

### Integration Tests -- WarehousesControllerTests

- `CreateWarehouse_ValidPayload_Returns201` [Integration]
- `CreateWarehouse_DuplicateCode_Returns409` [Integration]
- `ListWarehouses_Returns200` [Integration]
- `GetWarehouse_ExistingId_Returns200` [Integration]
- `UpdateWarehouse_ValidPayload_Returns200` [Integration]
- `DeleteWarehouse_ActiveWarehouse_Returns204` [Integration]

### Integration Tests -- ZonesControllerTests

- `CreateZone_ValidPayload_Returns201` [Integration]
- `CreateZone_DuplicateCode_Returns409` [Integration]
- `ListZones_ReturnsZonesForWarehouse` [Integration]
- `DeleteZone_WithLocations_Returns409` [Integration]

### Integration Tests -- StorageLocationsControllerTests

- `CreateLocation_ValidPayload_Returns201` [Integration]
- `ListLocations_ReturnsLocationsForWarehouse` [Integration]
- `DeleteLocation_Empty_Returns204` [Integration]

### Integration Tests -- WarehouseTransfersControllerTests

- `CreateTransfer_ValidPayload_Returns201` [Integration]
- `CreateTransfer_SameWarehouse_Returns400` [Integration]
- `CompleteTransfer_DraftStatus_Returns200` [Integration]
- `CancelTransfer_DraftStatus_Returns200` [Integration]

---

## Key Files

- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/WarehousesController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ZonesController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StorageLocationsController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/WarehouseTransfersController.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/WarehouseEntity.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/Zone.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/StorageLocation.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/WarehouseTransfer.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/WarehouseTransferLine.cs`
- `src/Warehouse.ServiceModel/DTOs/Inventory/`
- `src/Warehouse.ServiceModel/Requests/Inventory/`
- `src/Warehouse.Mapping/Profiles/Inventory/`
