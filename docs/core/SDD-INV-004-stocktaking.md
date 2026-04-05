# SDD-INV-004 — Stocktaking

> Status: Active
> Last updated: 2026-04-05
> Owner: TBD
> Category: Core

## 1. Context & Scope

This spec defines the Stocktaking sub-domain for the Warehouse Inventory system. It covers stocktake session management, individual count entries per product/location, and variance reporting (expected vs actual quantities).

**In scope:**
- Stocktake session lifecycle (Draft -> InProgress -> Completed -> Cancelled)
- Individual count entries per product/location
- Variance calculation (expected vs actual)
- Automatic adjustment creation from completed stocktake
- Audit columns on all entities

**Out of scope:**
- Product catalog management (see SDD-INV-001)
- Stock level management (see SDD-INV-002 -- adjustments from stocktake are created via SDD-INV-002)
- Warehouse structure (see SDD-INV-003)
- Barcode scanning and mobile device integration (future enhancement)

**Related specs:**
- `SDD-AUTH-001` — All endpoints require JWT authentication and permission-based authorization.
- `SDD-INV-001` — Products referenced by count entries.
- `SDD-INV-002` — Completed stocktakes with variances create inventory adjustments.
- `SDD-INV-003` — Warehouses and locations referenced by stocktake sessions.

---

## 2. Behavior

### 2.1 Stocktake Sessions

#### 2.1.1 Create Stocktake Session

- The system MUST support creating a stocktake session with: warehouse ID, optional zone ID (to scope the count), name, and optional notes.
- The system MUST set the initial status to `Draft`.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.
- The system MUST return the created session with its generated ID.

**Edge cases:**
- Creating a session for a non-existent warehouse MUST return a 404 Not Found error.

#### 2.1.2 Get Stocktake Session

- The system MUST support retrieving a stocktake session by ID with all count entries.

#### 2.1.3 Search Stocktake Sessions

- The system MUST support paginated listing of sessions with filtering by warehouse ID, status, and date range.

#### 2.1.4 Start Stocktake

- The system MUST support starting a draft session. This sets the status to `InProgress`.
- When starting, the system MUST snapshot the current stock levels for the scoped warehouse (and zone if specified) into expected quantities for each count entry.
- Only sessions with status `Draft` MAY be started.

#### 2.1.5 Complete Stocktake

- The system MUST support completing an in-progress session. This sets the status to `Completed`.
- Only sessions with status `InProgress` MAY be completed.
- Completing a stocktake MUST calculate variances for each count entry (actual - expected).
- The system MUST record `CompletedAtUtc` and `CompletedByUserId`.

#### 2.1.6 Cancel Stocktake

- The system MUST support cancelling a draft or in-progress session. This sets the status to `Cancelled`.
- Only sessions with status `Draft` or `InProgress` MAY be cancelled.

#### 2.1.7 Create Adjustment from Stocktake

- The system MUST support creating an inventory adjustment (SDD-INV-002) from a completed stocktake.
- The adjustment MUST include one line per count entry that has a non-zero variance.
- Only completed sessions MAY generate adjustments.
- The generated adjustment MUST reference the stocktake session ID.

### 2.2 Stocktake Counts

#### 2.2.1 Add Count Entry

- The system MUST support adding count entries to an in-progress session with: product ID, location ID (optional), and actual quantity.
- The system MUST only allow adding counts to sessions with status `InProgress`.
- The system MUST prevent duplicate count entries for the same product and location within the same session. This MUST return a 409 Conflict error.

#### 2.2.2 Update Count Entry

- The system MUST support updating the actual quantity on a count entry.
- Only count entries in `InProgress` sessions MAY be updated.

#### 2.2.3 Delete Count Entry

- The system MUST support deleting a count entry from an in-progress session.

#### 2.2.4 List Count Entries

- The system MUST support listing all count entries for a stocktake session.
- The list MUST include product details, location details, expected quantity, actual quantity, and variance.

### 2.3 Variance Reports

#### 2.3.1 Get Variance Report

- The system MUST support generating a variance report for a completed stocktake session.
- The report MUST list all count entries with: product name, product code, location, expected quantity, actual quantity, variance (actual - expected), and variance percentage.
- The report MUST include summary totals.
- Only completed sessions MAY generate variance reports.

---

## 3. Validation Rules

### 3.1 Stocktake Session

| # | Field | Rule | Error Code |
|---|---|---|---|
| V1 | WarehouseId | Required. Must reference an existing warehouse. | `INVALID_WAREHOUSE` |
| V2 | ZoneId | Optional. Must reference an existing zone when provided. | `INVALID_ZONE` |
| V3 | Name | Required. 1-200 characters. | `INVALID_SESSION_NAME` |
| V4 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |

### 3.2 Stocktake Count

| # | Field | Rule | Error Code |
|---|---|---|---|
| V5 | ProductId | Required. Must reference an existing product. | `INVALID_PRODUCT` |
| V6 | LocationId | Optional. Must reference an existing location. | `INVALID_LOCATION` |
| V7 | ActualQuantity | Required. Must be >= 0. | `INVALID_ACTUAL_QUANTITY` |
| V8 | ProductId + LocationId | Must be unique within the session. | `DUPLICATE_COUNT_ENTRY` |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Session not found | 404 | `SESSION_NOT_FOUND` | Stocktake session not found. |
| E2 | Start non-draft session | 409 | `SESSION_NOT_DRAFT` | Only draft sessions can be started. Current status: {status}. |
| E3 | Complete non-in-progress session | 409 | `SESSION_NOT_IN_PROGRESS` | Only in-progress sessions can be completed. Current status: {status}. |
| E4 | Cancel completed session | 409 | `SESSION_CANNOT_CANCEL` | Cannot cancel a session with status: {status}. |
| E5 | Add count to non-in-progress session | 409 | `SESSION_NOT_IN_PROGRESS` | Count entries can only be added to in-progress sessions. |
| E6 | Duplicate count entry | 409 | `DUPLICATE_COUNT_ENTRY` | A count entry for this product and location already exists in this session. |
| E7 | Count entry not found | 404 | `COUNT_ENTRY_NOT_FOUND` | Stocktake count entry not found. |
| E8 | Generate adjustment from non-completed | 409 | `SESSION_NOT_COMPLETED` | Adjustments can only be generated from completed sessions. |
| E9 | Variance report from non-completed | 409 | `SESSION_NOT_COMPLETED` | Variance reports can only be generated from completed sessions. |
| E10 | Validation failure (field-level) | 400 | `VALIDATION_ERROR` | One or more fields are invalid. |
| E11 | Insufficient permissions | 403 | `FORBIDDEN` | You do not have permission to perform this action. |

All error responses MUST use ProblemDetails (RFC 7807) format.

---

## 5. API Endpoints

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| **Stocktake Sessions** | | | | |
| POST | `/api/v1/stocktake-sessions` | Create session | Yes | `stocktake:create` |
| GET | `/api/v1/stocktake-sessions` | Search sessions (paginated) | Yes | `stocktake:read` |
| GET | `/api/v1/stocktake-sessions/{id}` | Get session by ID | Yes | `stocktake:read` |
| POST | `/api/v1/stocktake-sessions/{id}/start` | Start session | Yes | `stocktake:update` |
| POST | `/api/v1/stocktake-sessions/{id}/complete` | Complete session | Yes | `stocktake:finalize` |
| POST | `/api/v1/stocktake-sessions/{id}/cancel` | Cancel session | Yes | `stocktake:update` |
| POST | `/api/v1/stocktake-sessions/{id}/create-adjustment` | Create adjustment from session | Yes | `stocktake:finalize` |
| GET | `/api/v1/stocktake-sessions/{id}/variance-report` | Get variance report | Yes | `stocktake:read` |
| **Stocktake Counts** | | | | |
| POST | `/api/v1/stocktake-sessions/{sessionId}/counts` | Add count entry | Yes | `stocktake:update` |
| PUT | `/api/v1/stocktake-sessions/{sessionId}/counts/{countId}` | Update count entry | Yes | `stocktake:update` |
| DELETE | `/api/v1/stocktake-sessions/{sessionId}/counts/{countId}` | Delete count entry | Yes | `stocktake:update` |
| GET | `/api/v1/stocktake-sessions/{sessionId}/counts` | List count entries | Yes | `stocktake:read` |

---

## 6. Database Schema

**Schema name:** `inventory`

### Tables

**inventory.StocktakeSessions**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| WarehouseId | INT | NOT NULL, FK -> inventory.Warehouses(Id) |
| ZoneId | INT | NULL, FK -> inventory.Zones(Id) |
| Name | NVARCHAR(200) | NOT NULL |
| Notes | NVARCHAR(2000) | NULL |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'Draft' |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| StartedAtUtc | DATETIME2(7) | NULL |
| CompletedAtUtc | DATETIME2(7) | NULL |
| CompletedByUserId | INT | NULL |

**inventory.StocktakeCounts**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| SessionId | INT | NOT NULL, FK -> inventory.StocktakeSessions(Id) |
| ProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| LocationId | INT | NULL, FK -> inventory.StorageLocations(Id) |
| ExpectedQuantity | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| ActualQuantity | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| Variance | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| CountedAtUtc | DATETIME2(7) | NULL |
| CountedByUserId | INT | NULL |

### Indexes

| Name | Table | Columns | Type |
|---|---|---|---|
| IX_StocktakeSessions_WarehouseId | StocktakeSessions | WarehouseId | Non-unique |
| IX_StocktakeSessions_Status | StocktakeSessions | Status | Non-unique |
| IX_StocktakeSessions_CreatedAtUtc | StocktakeSessions | CreatedAtUtc | Non-unique |
| IX_StocktakeCounts_SessionId | StocktakeCounts | SessionId | Non-unique |
| IX_StocktakeCounts_ProductId | StocktakeCounts | ProductId | Non-unique |
| IX_StocktakeCounts_SessionId_ProductId_LocationId | StocktakeCounts | SessionId, ProductId, LocationId | Unique |

---

## 7. Versioning Notes

- **v1 -- Initial specification (2026-04-05)**
  - Stocktake session lifecycle (Draft -> InProgress -> Completed/Cancelled)
  - Count entry management with expected/actual quantities
  - Variance calculation and reporting
  - Automatic adjustment creation from completed stocktake
  - ProblemDetails error responses
  - Database schema on `inventory` schema

- **v2 -- Status activation (2026-04-05)** (non-breaking)
  - Confirmed endpoint uses `/complete` (not `/finalize`)
  - Confirmed permission prefix uses `stocktake:` (not `stocktake-sessions:`)
  - Confirmed separate StocktakeCountsController for count entry endpoints
  - Status changed from Draft to Active

---

## 8. Test Plan

### Unit Tests -- StocktakeSessionServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedSession` [Unit]
- `CreateAsync_NonExistentWarehouse_ReturnsNotFound` [Unit]
- `StartAsync_DraftSession_SetsInProgressAndSnapshots` [Unit]
- `StartAsync_NonDraftSession_ReturnsConflict` [Unit]
- `CompleteAsync_InProgressSession_SetsCompleted` [Unit]
- `CompleteAsync_NonInProgressSession_ReturnsConflict` [Unit]
- `CancelAsync_DraftSession_SetsCancelled` [Unit]
- `CancelAsync_CompletedSession_ReturnsConflict` [Unit]
- `CreateAdjustmentAsync_CompletedSession_CreatesAdjustment` [Unit]
- `CreateAdjustmentAsync_NonCompletedSession_ReturnsConflict` [Unit]
- `GetVarianceReportAsync_CompletedSession_ReturnsReport` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]

### Unit Tests -- StocktakeCountServiceTests

- `AddAsync_ValidRequest_ReturnsCreatedCount` [Unit]
- `AddAsync_NonInProgressSession_ReturnsConflict` [Unit]
- `AddAsync_DuplicateProductLocation_ReturnsConflict` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedCount` [Unit]
- `DeleteAsync_ExistingCount_RemovesSuccessfully` [Unit]
- `ListAsync_ReturnsCountsForSession` [Unit]

### Unit Tests -- Validation

- `CreateStocktakeSessionRequestValidator_MissingWarehouseId_Fails` [Unit]
- `CreateStocktakeSessionRequestValidator_MissingName_Fails` [Unit]
- `AddStocktakeCountRequestValidator_MissingProductId_Fails` [Unit]
- `AddStocktakeCountRequestValidator_NegativeQuantity_Fails` [Unit]

### Integration Tests -- StocktakeSessionsControllerTests

- `CreateSession_ValidPayload_Returns201` [Integration]
- `StartSession_DraftStatus_Returns200` [Integration]
- `CompleteSession_InProgressStatus_Returns200` [Integration]
- `CancelSession_DraftStatus_Returns200` [Integration]
- `SearchSessions_Returns200` [Integration]
- `GetVarianceReport_CompletedSession_Returns200` [Integration]

### Integration Tests -- StocktakeCountsControllerTests

- `AddCount_ValidPayload_Returns201` [Integration]
- `AddCount_DuplicateProductLocation_Returns409` [Integration]
- `UpdateCount_ValidPayload_Returns200` [Integration]
- `DeleteCount_ExistingCount_Returns204` [Integration]
- `ListCounts_ReturnsCountsForSession` [Integration]

---

## Key Files

- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StocktakeSessionsController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/StocktakeCountsController.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/StocktakeSession.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/StocktakeCount.cs`
- `src/Warehouse.ServiceModel/DTOs/Inventory/`
- `src/Warehouse.ServiceModel/Requests/Inventory/`
- `src/Warehouse.Mapping/Profiles/Inventory/`
