# SDD-INV-002 — Stock Management

> Status: Active
> Last updated: 2026-04-08
> Owner: TBD
> Category: Core

## 1. Context & Scope

This spec defines the Stock Management sub-domain for the Warehouse Inventory system. It covers on-hand stock levels, stock movements, inventory adjustments with approval workflow, and batch/lot tracking with expiry dates.

**In scope:**
- Stock level tracking per product/warehouse/location
- Stock movement recording with reason codes
- Inventory adjustments with approval workflow (Pending -> Approved/Rejected -> Applied)
- Batch/lot management with expiry dates
- Audit columns on all entities

**Out of scope:**
- Product catalog management (see SDD-INV-001)
- Warehouse structure definition (see SDD-INV-003)
- Stocktaking/physical inventory (see SDD-INV-004)
- Automated reorder points and purchase suggestions (covered by Orders service)

**Related specs:**
- `SDD-AUTH-001` — All endpoints require JWT authentication and permission-based authorization.
- `SDD-INV-001` — Products referenced by stock levels and movements.
- `SDD-INV-003` — Warehouse and location entities referenced by stock levels.
- `SDD-INV-004` — Stocktaking creates adjustments when variances are found.

**ISA-95 Conformance:** Conforms to ISA-95 Part 2, Section 7 — Material Model (Material Lot) and ISA-95 Part 3 — Inventory Operations Activity Model (Material Inventory Tracking, Material Movement, Inventory Adjustment).

---

## 2. Behavior

### 2.1 Stock Levels

#### 2.1.1 Get Stock Level

- The system MUST support retrieving the current stock level for a specific product at a specific warehouse and location.
- The system MUST return quantity on hand, reserved quantity, and available quantity (on hand minus reserved).

#### 2.1.2 Search Stock Levels

- The system MUST support paginated listing of stock levels with configurable page size and page number.
- The system MUST support filtering by: product ID, warehouse ID, location ID, and minimum quantity.
- The system MUST support sorting by product name, quantity, and warehouse name.
- The system MUST default to sorting by product name ascending.

#### 2.1.3 Get Stock Summary by Product

- The system MUST support retrieving a summary of all stock levels for a specific product across all warehouses and locations.
- The summary MUST include total quantity on hand, total reserved, and total available.

**Edge cases:**
- Querying stock for a non-existent product MUST return a 404 Not Found error.
- Querying stock for a non-existent warehouse MUST return a 404 Not Found error.

### 2.2 Stock Movements

#### 2.2.1 Record Stock Movement

- The system MUST support recording stock movements with: product ID, warehouse ID, location ID, quantity (positive for inbound, negative for outbound), reason code, and optional reference (e.g., order ID, adjustment ID).
- The system MUST update the corresponding stock level after recording a movement.
- The system MUST prevent movements that would result in a negative stock level. This MUST return a 409 Conflict error.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on every movement.
- Stock movements are immutable -- they MUST NOT be updated or deleted after creation.
- If a product has `RequiresBatchTracking = true` (see SDD-INV-001), the movement MUST include a `BatchId`. Omitting it MUST return a 400 error with code `BATCH_REQUIRED`.

**Edge cases:**
- Recording a movement for a non-existent product MUST return a 400 Validation error with code `INVALID_PRODUCT`.
- Recording a movement for a non-existent warehouse or location MUST return a 400 Validation error with code `INVALID_WAREHOUSE` or `INVALID_LOCATION`.
- Recording a movement that would make stock negative MUST return a 409 Conflict error with code `INSUFFICIENT_STOCK`.

#### 2.2.2 Search Stock Movements

- The system MUST support paginated listing of stock movements with configurable page size and page number.
- The system MUST support filtering by: product ID, warehouse ID, location ID, reason code, date range, and reference type.
- The system MUST support sorting by movement date and quantity.
- The system MUST default to sorting by movement date descending (newest first).

#### 2.2.3 Get Movement by ID

- The system MUST support retrieving a single stock movement by ID with product, warehouse, and location details.

### 2.3 Inventory Adjustments

#### 2.3.1 Create Adjustment

- The system MUST support creating inventory adjustments with a reason, notes, and one or more adjustment lines.
- Each adjustment line MUST specify a product ID, warehouse ID, location ID, expected quantity, actual quantity, and optional batch ID.
- The system MUST set the initial status to `Pending`.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.

**Edge cases:**
- Creating an adjustment with no lines MUST return a 400 Validation error.
- Creating an adjustment with invalid product/warehouse/location references MUST return a 400 Validation error.

#### 2.3.2 Get Adjustment

- The system MUST support retrieving an adjustment by ID with all lines and their details.

#### 2.3.3 Search Adjustments

- The system MUST support paginated listing of adjustments with filtering by status and date range.

#### 2.3.4 Approve Adjustment

- The system MUST support approving a pending adjustment. This sets the status to `Approved`.
- Only adjustments with status `Pending` MAY be approved. Approving a non-pending adjustment MUST return a 409 Conflict error.
- The system MUST record the approver's user ID and timestamp.

#### 2.3.5 Reject Adjustment

- The system MUST support rejecting a pending adjustment. This sets the status to `Rejected`.
- Only adjustments with status `Pending` MAY be rejected. Rejecting a non-pending adjustment MUST return a 409 Conflict error.
- The system MUST record the rejector's user ID, timestamp, and rejection reason.

#### 2.3.6 Apply Adjustment

- The system MUST support applying an approved adjustment. This sets the status to `Applied`.
- Only adjustments with status `Approved` MAY be applied.
- Applying an adjustment MUST create stock movements for each line (actual - expected = adjustment quantity).
- For manual adjustments, the stock movement reason code MUST be `Adjustment` with reference type `InventoryAdjustment`.
- For stocktake-sourced adjustments (where `SourceStocktakeSessionId` is set), the reason code MUST be `CountAdjustment` with reference type `StocktakeSession` and reference ID pointing to the session.
- The stock movement reference MUST link back to the adjustment or source session.
- Applying MUST be performed within a single database transaction.

**Edge cases:**
- Applying an adjustment that would result in negative stock on any line MUST return a 409 Conflict error and roll back all changes.

### 2.4 Batch / Lot Tracking

#### 2.4.1 Create Batch

- The system MUST support creating batches with: product ID, batch number, manufacturing date (optional), expiry date (optional), and notes.
- The system MUST enforce unique batch numbers per product.
- The system MUST set `IsActive = true` by default.

#### 2.4.2 Update Batch

- The system MUST support updating batch expiry date, manufacturing date, and notes.
- The system MUST NOT allow changing the batch number or product ID after creation.

#### 2.4.3 Get Batch

- The system MUST support retrieving a batch by ID with product details.

#### 2.4.4 Search Batches

- The system MUST support paginated listing of batches with filtering by product ID, expiry date range, and active status.

#### 2.4.5 Deactivate Batch

- The system MUST support deactivating a batch by setting `IsActive = false`.
- The system SHOULD prevent deactivation of a batch with non-zero stock. This SHOULD return a 409 Conflict error.

**Edge cases:**
- Creating a batch with a duplicate batch number for the same product MUST return a 409 Conflict error.
- Creating a batch for a non-existent product MUST return a 400 Validation error with code `INVALID_PRODUCT`.

---

## 3. Validation Rules

### 3.1 Stock Movement

| # | Field | Rule | Error Code |
|---|---|---|---|
| V1 | ProductId | Required. Must reference an existing product. | `INVALID_PRODUCT` |
| V2 | WarehouseId | Required. Must reference an existing warehouse. | `INVALID_WAREHOUSE` |
| V3 | LocationId | Optional. Must reference an existing location when provided. | `INVALID_LOCATION` |
| V4 | Quantity | Required. Must not be zero. | `INVALID_MOVEMENT_QUANTITY` |
| V5 | ReasonCode | Required. Must be a valid StockMovementReason enum value. | `INVALID_REASON_CODE` |
| V6 | ReferenceNumber | Optional. Max 100 characters. | `INVALID_REFERENCE` |
| V7 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |
| V8 | BatchId | Required when `Product.RequiresBatchTracking = true`. Must reference an existing batch. | `BATCH_REQUIRED` |

### 3.2 Inventory Adjustment

| # | Field | Rule | Error Code |
|---|---|---|---|
| V9 | Reason | Required. 1-200 characters. | `INVALID_ADJUSTMENT_REASON` |
| V10 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |
| V11 | Lines | Required. At least one line. | `ADJUSTMENT_LINES_REQUIRED` |

### 3.3 Adjustment Line

| # | Field | Rule | Error Code |
|---|---|---|---|
| V12 | ProductId | Required. Must reference an existing product. | `INVALID_PRODUCT` |
| V13 | WarehouseId | Required. Must reference an existing warehouse. | `INVALID_WAREHOUSE` |
| V14 | LocationId | Optional. Must reference an existing location when provided. | `INVALID_LOCATION` |
| V15 | ExpectedQuantity | Required. Must be >= 0. | `INVALID_EXPECTED_QUANTITY` |
| V16 | ActualQuantity | Required. Must be >= 0. | `INVALID_ACTUAL_QUANTITY` |

### 3.4 Batch

| # | Field | Rule | Error Code |
|---|---|---|---|
| V17 | ProductId | Required. Must reference an existing product. | `INVALID_PRODUCT` |
| V18 | BatchNumber | Required. 1-50 characters. | `INVALID_BATCH_NUMBER` |
| V19 | BatchNumber | Must be unique per product. | `DUPLICATE_BATCH_NUMBER` |
| V20 | ExpiryDate | Optional. Must be a valid date when provided. | `INVALID_EXPIRY_DATE` |
| V21 | ManufacturingDate | Optional. Must be a valid date when provided. | `INVALID_MANUFACTURING_DATE` |
| V22 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Stock level not found | 404 | `STOCK_LEVEL_NOT_FOUND` | Stock level not found for the specified product and location. |
| E2 | Movement would cause negative stock | 409 | `INSUFFICIENT_STOCK` | Insufficient stock. Available: {available}, requested: {quantity}. |
| E3 | Stock movement not found | 404 | `MOVEMENT_NOT_FOUND` | Stock movement not found. |
| E4 | Adjustment not found | 404 | `ADJUSTMENT_NOT_FOUND` | Inventory adjustment not found. |
| E5 | Approve non-pending adjustment | 409 | `ADJUSTMENT_NOT_PENDING` | Only pending adjustments can be approved. Current status: {status}. |
| E6 | Reject non-pending adjustment | 409 | `ADJUSTMENT_NOT_PENDING` | Only pending adjustments can be rejected. Current status: {status}. |
| E7 | Apply non-approved adjustment | 409 | `ADJUSTMENT_NOT_APPROVED` | Only approved adjustments can be applied. Current status: {status}. |
| E8 | Apply adjustment causes negative stock | 409 | `ADJUSTMENT_INSUFFICIENT_STOCK` | Applying adjustment would result in negative stock for product {code} at {location}. |
| E9 | Batch not found | 404 | `BATCH_NOT_FOUND` | Batch not found. |
| E10 | Duplicate batch number | 409 | `DUPLICATE_BATCH_NUMBER` | A batch with this number already exists for this product. |
| E11 | Batch references non-existent product | 400 | `INVALID_PRODUCT` | The specified product does not exist. |
| E12 | Deactivate batch with stock | 409 | `BATCH_HAS_STOCK` | Cannot deactivate batch -- it has non-zero stock levels. |
| E13 | Validation failure (field-level) | 400 | `VALIDATION_ERROR` | One or more fields are invalid. |
| E14 | Insufficient permissions | 403 | `FORBIDDEN` | You do not have permission to perform this action. |
| E15 | Movement references non-existent product | 400 | `INVALID_PRODUCT` | The specified product does not exist. |
| E16 | Movement references non-existent warehouse | 400 | `INVALID_WAREHOUSE` | The specified warehouse does not exist. |
| E17 | Movement references non-existent location | 400 | `INVALID_LOCATION` | The specified storage location does not exist. |
| E18 | Product requires batch tracking but no BatchId provided | 400 | `BATCH_REQUIRED` | Product requires batch tracking but no batch was specified. |

All error responses MUST use ProblemDetails (RFC 7807) format.

---

## 5. API Endpoints

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| **Stock Levels** | | | | |
| GET | `/api/v1/stock-levels` | Search stock levels (paginated) | Yes | `stock:read` |
| GET | `/api/v1/stock-levels/product/{productId}` | Get stock summary for product | Yes | `stock:read` |
| **Stock Movements** | | | | |
| POST | `/api/v1/stock-movements` | Record stock movement | Yes | `stock:adjust` |
| GET | `/api/v1/stock-movements` | Search stock movements (paginated) | Yes | `stock-movements:read` |
| GET | `/api/v1/stock-movements/{id}` | Get movement by ID | Yes | `stock-movements:read` |
| **Inventory Adjustments** | | | | |
| POST | `/api/v1/inventory-adjustments` | Create adjustment | Yes | `stock:adjust` |
| GET | `/api/v1/inventory-adjustments` | Search adjustments (paginated) | Yes | `stock:read` |
| GET | `/api/v1/inventory-adjustments/{id}` | Get adjustment by ID | Yes | `stock:read` |
| POST | `/api/v1/inventory-adjustments/{id}/approve` | Approve adjustment | Yes | `stock:adjust` |
| POST | `/api/v1/inventory-adjustments/{id}/reject` | Reject adjustment | Yes | `stock:adjust` |
| POST | `/api/v1/inventory-adjustments/{id}/apply` | Apply adjustment | Yes | `stock:adjust` |
| **Batches** | | | | |
| POST | `/api/v1/batches` | Create batch | Yes | `batches:create` |
| GET | `/api/v1/batches` | Search batches (paginated) | Yes | `batches:read` |
| GET | `/api/v1/batches/{id}` | Get batch by ID | Yes | `batches:read` |
| PUT | `/api/v1/batches/{id}` | Update batch | Yes | `batches:update` |
| DELETE | `/api/v1/batches/{id}` | Deactivate batch | Yes | `batches:update` |

---

## 6. Database Schema

**Schema name:** `inventory`

### Tables

**inventory.StockLevels**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| WarehouseId | INT | NOT NULL, FK -> inventory.Warehouses(Id) |
| LocationId | INT | NULL, FK -> inventory.StorageLocations(Id) |
| BatchId | INT | NULL, FK -> inventory.Batches(Id) |
| QuantityOnHand | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| QuantityReserved | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**inventory.StockMovements**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| WarehouseId | INT | NOT NULL, FK -> inventory.Warehouses(Id) |
| LocationId | INT | NULL, FK -> inventory.StorageLocations(Id) |
| BatchId | INT | NULL, FK -> inventory.Batches(Id) |
| Quantity | DECIMAL(18,4) | NOT NULL |
| ReasonCode | NVARCHAR(50) | NOT NULL |
| ReferenceType | NVARCHAR(50) | NULL |
| ReferenceId | INT | NULL |
| ReferenceNumber | NVARCHAR(100) | NULL |
| Notes | NVARCHAR(2000) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |

**inventory.InventoryAdjustments**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Reason | NVARCHAR(200) | NOT NULL |
| Notes | NVARCHAR(2000) | NULL |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'Pending' |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| ApprovedAtUtc | DATETIME2(7) | NULL |
| ApprovedByUserId | INT | NULL |
| RejectedAtUtc | DATETIME2(7) | NULL |
| RejectedByUserId | INT | NULL |
| RejectionReason | NVARCHAR(500) | NULL |
| AppliedAtUtc | DATETIME2(7) | NULL |
| AppliedByUserId | INT | NULL |
| SourceStocktakeSessionId | INT | NULL, FK -> inventory.StocktakeSessions(Id) |

**inventory.InventoryAdjustmentLines**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| AdjustmentId | INT | NOT NULL, FK -> inventory.InventoryAdjustments(Id) |
| ProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| WarehouseId | INT | NOT NULL, FK -> inventory.Warehouses(Id) |
| LocationId | INT | NULL, FK -> inventory.StorageLocations(Id) |
| BatchId | INT | NULL, FK -> inventory.Batches(Id) |
| ExpectedQuantity | DECIMAL(18,4) | NOT NULL |
| ActualQuantity | DECIMAL(18,4) | NOT NULL |

**inventory.Batches**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| BatchNumber | NVARCHAR(50) | NOT NULL |
| ManufacturingDate | DATE | NULL |
| ExpiryDate | DATE | NULL |
| Notes | NVARCHAR(2000) | NULL |
| IsActive | BIT | NOT NULL, DEFAULT 1 |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| ModifiedAtUtc | DATETIME2(7) | NULL |
| ModifiedByUserId | INT | NULL |

### Indexes

| Name | Table | Columns | Type |
|---|---|---|---|
| IX_StockLevels_ProductId | StockLevels | ProductId | Non-unique |
| IX_StockLevels_WarehouseId | StockLevels | WarehouseId | Non-unique |
| IX_StockLevels_LocationId | StockLevels | LocationId | Non-unique |
| IX_StockLevels_Product_Warehouse_Location | StockLevels | ProductId, WarehouseId, LocationId | Unique (filtered: LocationId IS NOT NULL) |
| IX_StockMovements_ProductId | StockMovements | ProductId | Non-unique |
| IX_StockMovements_WarehouseId | StockMovements | WarehouseId | Non-unique |
| IX_StockMovements_CreatedAtUtc | StockMovements | CreatedAtUtc | Non-unique |
| IX_StockMovements_ReasonCode | StockMovements | ReasonCode | Non-unique |
| IX_InventoryAdjustments_Status | InventoryAdjustments | Status | Non-unique |
| IX_InventoryAdjustments_CreatedAtUtc | InventoryAdjustments | CreatedAtUtc | Non-unique |
| IX_InventoryAdjustmentLines_AdjustmentId | InventoryAdjustmentLines | AdjustmentId | Non-unique |
| IX_Batches_ProductId | Batches | ProductId | Non-unique |
| IX_Batches_ProductId_BatchNumber | Batches | ProductId, BatchNumber | Unique |
| IX_Batches_ExpiryDate | Batches | ExpiryDate | Non-unique (filtered: ExpiryDate IS NOT NULL) |

---

## 7. Versioning Notes

- **v1 -- Initial specification (2026-04-05)**
  - Stock level tracking with on-hand, reserved, and available quantities
  - Immutable stock movements with reason codes
  - Inventory adjustment workflow (Pending -> Approved/Rejected -> Applied)
  - Batch/lot tracking with expiry dates
  - ProblemDetails error responses
  - Database schema on `inventory` schema

- **v2 -- Error alignment (2026-04-05)** (non-breaking)
  - Non-existent product/warehouse/location references on stock movements return 400 (validation error) instead of 404
  - Non-existent product on batch creation returns 400 `INVALID_PRODUCT` instead of 404 `PRODUCT_NOT_FOUND`
  - Added explicit error rules E15-E17 for movement reference validation
  - Status changed from Draft to Active

- **v3 — Batch tracking and CountAdjustment documentation (2026-04-08)** (non-breaking)
  - Documented batch tracking enforcement on stock movements (`BATCH_REQUIRED` error E18)
  - Added V8 validation rule for BatchId when product requires batch tracking
  - Documented `CountAdjustment` reason code for stocktake-sourced adjustments vs `Adjustment` for manual
  - Added `SourceStocktakeSessionId` column to InventoryAdjustments schema
  - Renumbered validation rules V8-V21 → V9-V22

---

## 8. Test Plan

### Unit Tests -- StockLevelServiceTests

- `GetByProductAndLocationAsync_ExistingLevel_ReturnsStockLevel` [Unit]
- `GetByProductAndLocationAsync_NonExistent_ReturnsNotFound` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]
- `GetProductSummaryAsync_ExistingProduct_ReturnsSummary` [Unit]
- `GetProductSummaryAsync_NonExistentProduct_ReturnsNotFound` [Unit]

### Unit Tests -- StockMovementServiceTests

- `RecordAsync_ValidInboundMovement_CreatesMovementAndUpdatesStock` [Unit]
- `RecordAsync_ValidOutboundMovement_CreatesMovementAndUpdatesStock` [Unit]
- `RecordAsync_InsufficientStock_ReturnsConflictError` [Unit]
- `RecordAsync_NonExistentProduct_ReturnsValidationError` [Unit]
- `RecordAsync_BatchTrackingRequired_NoBatchId_ReturnsBatchRequired` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]
- `GetByIdAsync_ExistingMovement_ReturnsMovement` [Unit]

### Unit Tests -- InventoryAdjustmentServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedAdjustment` [Unit]
- `CreateAsync_NoLines_ReturnsValidationError` [Unit]
- `ApproveAsync_PendingAdjustment_SetsApproved` [Unit]
- `ApproveAsync_NonPendingAdjustment_ReturnsConflict` [Unit]
- `RejectAsync_PendingAdjustment_SetsRejected` [Unit]
- `RejectAsync_NonPendingAdjustment_ReturnsConflict` [Unit]
- `ApplyAsync_ApprovedAdjustment_CreatesMovementsAndUpdatesStock` [Unit]
- `ApplyAsync_NonApprovedAdjustment_ReturnsConflict` [Unit]
- `ApplyAsync_InsufficientStock_ReturnsConflictAndRollsBack` [Unit]

### Unit Tests -- BatchServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedBatch` [Unit]
- `CreateAsync_DuplicateBatchNumber_ReturnsConflict` [Unit]
- `CreateAsync_NonExistentProduct_ReturnsValidationError` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedBatch` [Unit]
- `DeactivateAsync_ActiveBatch_SetsInactive` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]

### Unit Tests -- Validation

- `RecordStockMovementRequestValidator_MissingProductId_Fails` [Unit]
- `RecordStockMovementRequestValidator_ZeroQuantity_Fails` [Unit]
- `RecordStockMovementRequestValidator_MissingReasonCode_Fails` [Unit]
- `CreateAdjustmentRequestValidator_MissingReason_Fails` [Unit]
- `CreateAdjustmentRequestValidator_EmptyLines_Fails` [Unit]
- `CreateBatchRequestValidator_MissingBatchNumber_Fails` [Unit]
- `CreateBatchRequestValidator_MissingProductId_Fails` [Unit]

### Integration Tests -- StockLevelsControllerTests

- `SearchStockLevels_Returns200` [Integration]
- `GetProductSummary_ExistingProduct_Returns200` [Integration]
- `GetProductSummary_NonExistentProduct_Returns404` [Integration]

### Integration Tests -- StockMovementsControllerTests

- `RecordMovement_ValidPayload_Returns201` [Integration]
- `RecordMovement_InsufficientStock_Returns409` [Integration]
- `SearchMovements_Returns200` [Integration]
- `GetMovement_ExistingId_Returns200` [Integration]

### Integration Tests -- InventoryAdjustmentsControllerTests

- `CreateAdjustment_ValidPayload_Returns201` [Integration]
- `ApproveAdjustment_PendingStatus_Returns200` [Integration]
- `RejectAdjustment_PendingStatus_Returns200` [Integration]
- `ApplyAdjustment_ApprovedStatus_Returns200` [Integration]
- `ApplyAdjustment_NonApprovedStatus_Returns409` [Integration]

### Integration Tests -- BatchesControllerTests

- `CreateBatch_ValidPayload_Returns201` [Integration]
- `CreateBatch_DuplicateNumber_Returns409` [Integration]
- `SearchBatches_Returns200` [Integration]
- `UpdateBatch_ValidPayload_Returns200` [Integration]
- `DeactivateBatch_Returns204` [Integration]

---

## Key Files

- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StockLevelsController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StockMovementsController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/InventoryAdjustmentsController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/BatchesController.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/StockLevel.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/StockMovement.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/InventoryAdjustment.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/InventoryAdjustmentLine.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/Batch.cs`
- `src/Warehouse.ServiceModel/DTOs/Inventory/`
- `src/Warehouse.ServiceModel/Requests/Inventory/`
- `src/Warehouse.Mapping/Profiles/Inventory/`
