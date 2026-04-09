# SDD-INV-005 --- Batch Creation on Goods Receipt

> Status: Draft
> Last updated: 2026-04-09
> Owner: TBD
> Category: Core

---

## 1. Context & Scope

This spec defines the automated batch creation and stock intake flow triggered when the Purchasing service completes a goods receipt. The Inventory service consumes the `GoodsReceiptCompletedEvent` via MassTransit and, for each accepted line, creates or reuses batches, records immutable stock movements, and upserts stock levels. The same logic applies when a quarantined goods receipt line is later accepted (`GoodsReceiptLineAcceptedEvent`).

**ISA-95 Conformance:** Conforms to ISA-95 Part 2, Section 7 --- Material Model (Material Lot creation from receipt) and ISA-95 Part 3 --- Inventory Operations Activity Model (Material Receipt, Material Movement). Maintains the ISA-95 material traceability chain: Material Definition -> Material Lot -> Material Sublot (compliance rule 5). All stock movements are immutable event records per ISA-95 Operations Event Model (compliance rule 10).

**In scope:**

- MassTransit consumer for `GoodsReceiptCompletedEvent` in the Inventory service
- MassTransit consumer for `GoodsReceiptLineAcceptedEvent` in the Inventory service
- Batch creation or lookup per receipt line (ProductId + BatchNumber uniqueness)
- Automatic batch number generation when the product requires batch tracking but no number is provided
- Immutable `StockMovement` creation with reason `Receipt` and reference type `PurchaseOrder`
- `StockLevel` upsert (create or increment) for the product/warehouse/location/batch combination
- Idempotency on event redelivery (no duplicate movements for the same goods receipt line)
- Partial success --- individual line failures do not block processing of remaining lines

**Out of scope:**

- Goods receipt creation and completion logic (see `SDD-PURCH-001`)
- Product catalog management and `RequiresBatchTracking` flag definition (see `SDD-INV-001`)
- Manual batch CRUD endpoints (see `SDD-INV-002`)
- Stock level query endpoints (see `SDD-INV-002`)
- Warehouse and storage location validation (see `SDD-INV-003`)
- Centralized sequence generator implementation (see planned `SDD-INFRA-003`)
- Supplier return stock movements (separate consumer, future spec)

**Related specs:**

- `SDD-PURCH-001` --- Goods receipt completion publishes `GoodsReceiptCompletedEvent` and `GoodsReceiptLineAcceptedEvent` consumed by this flow.
- `SDD-INV-001` --- Product entity defines `RequiresBatchTracking` flag that governs batch creation behavior.
- `SDD-INV-002` --- Stock movements, stock levels, and batch entity definitions; validation rules for movement recording.
- `SDD-INV-003` --- Warehouse and storage location entities referenced by stock levels and movements.
- `SDD-INFRA-001` --- MassTransit message bus configuration and registration used by the consumer.
- `SDD-INFRA-003` (planned) --- Centralized sequence generator for batch number auto-generation.

---

## 2. Behavior

### 2.1 GoodsReceiptCompletedEvent Consumer

#### 2.1.1 Event Consumption (Happy Path)

- The Inventory service MUST register a MassTransit consumer `GoodsReceiptCompletedConsumer` that subscribes to `GoodsReceiptCompletedEvent`.
- The consumer MUST process each line in the event's `Lines` collection sequentially within a single database transaction per line.
- For each line, the consumer MUST perform the following steps in order:
  1. **Idempotency check** --- query `inventory.StockMovements` for an existing movement with `ReferenceType = PurchaseOrder` and `ReferenceId = GoodsReceiptLineId`. If found, skip this line.
  2. **Product lookup** --- load the `Product` entity by `ProductId` to check `RequiresBatchTracking`.
  3. **Batch resolution** --- resolve the batch according to the rules in Section 2.2.
  4. **Stock movement creation** --- create an immutable `StockMovement` record per Section 2.3.
  5. **Stock level upsert** --- create or update the `StockLevel` record per Section 2.4.
- The consumer MUST acknowledge the message after processing all lines, regardless of individual line outcomes.
- The consumer MUST NOT throw exceptions --- all errors MUST be caught, logged, and the message acknowledged to prevent poison queue buildup.

#### 2.1.2 GoodsReceiptLineAcceptedEvent Consumer

- The Inventory service MUST register a MassTransit consumer `GoodsReceiptLineAcceptedConsumer` that subscribes to `GoodsReceiptLineAcceptedEvent`.
- This consumer MUST apply the same logic as Section 2.1.1 for a single line, using the event's `GoodsReceiptLineId`, `ProductId`, `WarehouseId`, `LocationId`, `Quantity`, `BatchNumber`, `ManufacturingDate`, `ExpiryDate`, and `AcceptedByUserId` fields.
- The idempotency check MUST use the same `ReferenceType = PurchaseOrder` and `ReferenceId = GoodsReceiptLineId` combination.
- The `CreatedByUserId` on movement and batch records MUST be set to `AcceptedByUserId` from the event.

### 2.2 Batch Resolution

#### 2.2.1 Batch Number Provided

- When a receipt line has a non-null, non-empty `BatchNumber`:
  - The consumer MUST query `inventory.Batches` for an existing batch matching `ProductId` AND `BatchNumber`.
  - If a matching batch exists, the consumer MUST reuse it (use its `Id` for the stock movement). The consumer MUST NOT create a duplicate batch.
  - If no matching batch exists, the consumer MUST create a new `Batch` record with:
    - `ProductId` from the receipt line
    - `BatchNumber` from the receipt line
    - `ManufacturingDate` from the receipt line (may be null)
    - `ExpiryDate` from the receipt line (may be null)
    - `IsActive = true`
    - `CreatedAtUtc = DateTime.UtcNow`
    - `CreatedByUserId` from the event's `ReceivedByUserId` (or `AcceptedByUserId` for accepted line events)

#### 2.2.2 Batch Number Not Provided --- Product Requires Batch Tracking

- When a receipt line has a null or empty `BatchNumber` AND the product's `RequiresBatchTracking` is `true`:
  - The consumer MUST auto-generate a batch number using `ISequenceGenerator.NextBatchNumberAsync(productCode)`.
  - The generated batch number format MUST be `{ProductCode}-{YYYYMMDD}-{SequentialNumber padded to 4 digits}` (e.g., `PROD-001-20260409-0001`).
  - If `ISequenceGenerator` is not yet available (planned `SDD-INFRA-003`), the consumer MUST use a fallback format: `{ProductCode}-{YYYYMMDD}-{GoodsReceiptLineId}`.
  - The consumer MUST create a new `Batch` record with the generated batch number.

#### 2.2.3 Batch Number Not Provided --- Product Does Not Require Batch Tracking

- When a receipt line has a null or empty `BatchNumber` AND the product's `RequiresBatchTracking` is `false`:
  - The consumer MUST NOT create a batch.
  - The `BatchId` on the resulting `StockMovement` and `StockLevel` MUST be `null`.

### 2.3 Stock Movement Creation

- For each successfully processed receipt line, the consumer MUST create a `StockMovement` record with:
  - `ProductId` from the receipt line
  - `WarehouseId` from the event header
  - `LocationId` from the event header (may be null)
  - `BatchId` from the resolved batch (may be null per Section 2.2.3)
  - `Quantity` from the receipt line (MUST be positive; receipt lines represent inbound stock)
  - `ReasonCode = Receipt` (ISA-95 base type: Receipt)
  - `ReferenceType = PurchaseOrder`
  - `ReferenceId = GoodsReceiptLineId` (used for idempotency)
  - `ReferenceNumber` from the event's `PurchaseOrderNumber`
  - `Notes` = `"Auto-created from goods receipt {GoodsReceiptNumber}"` (or equivalent structured value)
  - `CreatedAtUtc` = event's `ReceivedAtUtc` (or `AcceptedAtUtc` for accepted line events)
  - `CreatedByUserId` from the event's `ReceivedByUserId` (or `AcceptedByUserId`)
- The stock movement MUST be immutable after creation (per ISA-95 compliance rule 10). The consumer MUST NOT update or delete previously created movements.

### 2.4 Stock Level Upsert

- After creating a stock movement, the consumer MUST upsert the `StockLevel` for the combination of `ProductId`, `WarehouseId`, `LocationId`, and `BatchId`.
- **If a matching `StockLevel` exists:** the consumer MUST increment `QuantityOnHand` by the receipt line's `Quantity` and set `ModifiedAtUtc` to `DateTime.UtcNow`.
- **If no matching `StockLevel` exists:** the consumer MUST create a new `StockLevel` with `QuantityOnHand` equal to the receipt line's `Quantity` and `QuantityReserved = 0`.
- The upsert MUST use database-level atomicity to prevent race conditions. The implementation SHOULD use a single SQL `MERGE` statement or an EF Core `ExecuteUpdateAsync` / `ExecuteInsertAsync` pattern that avoids read-then-write race conditions.

### 2.5 Edge Cases

#### 2.5.1 Duplicate Event Delivery

- The consumer MUST be idempotent. If a `GoodsReceiptCompletedEvent` is delivered more than once, the consumer MUST NOT create duplicate stock movements or batches.
- The idempotency check is defined in Section 2.1.1 step 1: a movement with `ReferenceType = PurchaseOrder` and `ReferenceId = GoodsReceiptLineId` already exists.
- When a duplicate is detected, the consumer MUST log an informational message and skip the line without error.

#### 2.5.2 Partial Line Failure

- If processing one receipt line fails (e.g., product not found in Inventory database, database constraint violation), the consumer MUST:
  - Log the error at `Error` level with the `GoodsReceiptId`, `GoodsReceiptLineId`, `ProductId`, and exception details.
  - Continue processing the remaining lines.
  - NOT throw an exception that would cause the message to be redelivered.
- The consumer SHOULD track which lines succeeded and which failed, and log a summary after processing all lines.

#### 2.5.3 Product Not Found in Inventory Database

- If a `ProductId` from the receipt line does not exist in `inventory.Products`, the consumer MUST log an error and skip the line.
- This scenario can occur if the Purchasing service references a product that has been hard-deleted or does not exist in the Inventory domain (data inconsistency). The consumer MUST NOT create a stock movement without a valid product reference.

#### 2.5.4 Event with Empty Lines Collection

- If the `GoodsReceiptCompletedEvent` contains an empty `Lines` collection, the consumer MUST log a warning and acknowledge the message without further processing.

#### 2.5.5 Zero or Negative Quantity

- If a receipt line has a `Quantity` of zero or negative, the consumer MUST log an error and skip the line. Receipt lines MUST always have positive quantities.

#### 2.5.6 Batch Number Conflict on Concurrent Receipts

- If two concurrent receipts attempt to create a batch with the same `ProductId` and `BatchNumber`, the database unique constraint (`UQ_Batches_ProductId_BatchNumber`) will prevent duplicate creation.
- The consumer MUST catch the unique constraint violation, re-query the existing batch, and reuse it. This is a retry-on-conflict pattern, not an error.

### 2.6 Consumer Registration

- The `GoodsReceiptCompletedConsumer` and `GoodsReceiptLineAcceptedConsumer` MUST be registered in the Inventory service's MassTransit bus configuration in `Program.cs`.
- The consumer SHOULD use the default MassTransit retry policy (no custom retry configuration). Idempotency ensures safe redelivery.
- The consumer MUST NOT configure a dead-letter queue for failed messages --- errors are logged and the message is acknowledged.

---

## 3. Validation Rules

### 3.1 Event-Level Validation

| # | Field | Rule | Error Handling |
|---|---|---|---|
| V1 | `GoodsReceiptId` | Required. Must be > 0. | Log error, acknowledge message, skip all lines. |
| V2 | `WarehouseId` | Required. Must be > 0. | Log error, acknowledge message, skip all lines. |
| V3 | `ReceivedByUserId` | Required. Must be > 0. | Log error, acknowledge message, skip all lines. |
| V4 | `Lines` | Required. Must not be null. | Log error, acknowledge message. |
| V5 | `Lines` (empty) | If empty, log warning and acknowledge. | Log warning, acknowledge message. |

### 3.2 Line-Level Validation

| # | Field | Rule | Error Handling |
|---|---|---|---|
| V6 | `GoodsReceiptLineId` | Required. Must be > 0. | Log error, skip line. |
| V7 | `ProductId` | Required. Must be > 0. Must reference an existing product in `inventory.Products`. | Log error, skip line. |
| V8 | `Quantity` | Required. Must be > 0. | Log error, skip line. |
| V9 | `BatchNumber` | Optional. Max 50 characters when provided. | Log error, skip line if > 50 chars. |
| V10 | `ExpiryDate` | Optional. When provided alongside `ManufacturingDate`, `ExpiryDate` MUST be after `ManufacturingDate`. | Log error, skip line. |

### 3.3 Cross-Field Validation

| # | Rule | Error Handling |
|---|---|---|
| V11 | When `Product.RequiresBatchTracking = true` and `BatchNumber` is null/empty, batch MUST be auto-generated. | Auto-generate batch number (not an error). |
| V12 | When `Product.RequiresBatchTracking = false` and `BatchNumber` is null/empty, no batch is created. | Skip batch creation (not an error). |
| V13 | When `Product.RequiresBatchTracking = false` and `BatchNumber` is provided, a batch SHOULD still be created. The explicit batch number takes precedence over the tracking flag. | Create batch normally. |

### 3.4 State-Based Validation

| # | Rule | Error Handling |
|---|---|---|
| V14 | Idempotency: If a `StockMovement` with `ReferenceType = PurchaseOrder` and `ReferenceId = GoodsReceiptLineId` already exists, the line is a duplicate. | Log info, skip line. |
| V15 | Batch uniqueness conflict: If creating a batch fails due to unique constraint (`ProductId` + `BatchNumber`), re-query and reuse the existing batch. | Catch constraint violation, retry with existing batch. |

---

## 4. Error Rules

Since the consumer operates as an asynchronous event handler (no HTTP boundary), errors are handled internally rather than returned as HTTP responses.

| # | Trigger | Type | Handling | Log Level |
|---|---|---|---|---|
| E1 | Event `GoodsReceiptId` <= 0 or `WarehouseId` <= 0 | Validation | Acknowledge message, skip all processing. | Error |
| E2 | `Lines` collection is null | Validation | Acknowledge message, skip all processing. | Error |
| E3 | `Lines` collection is empty | Informational | Acknowledge message, no processing needed. | Warning |
| E4 | `ProductId` does not exist in `inventory.Products` | Not Found | Skip line, continue remaining lines. | Error |
| E5 | `Quantity` <= 0 on a receipt line | Validation | Skip line, continue remaining lines. | Error |
| E6 | `BatchNumber` exceeds 50 characters | Validation | Skip line, continue remaining lines. | Error |
| E7 | `ExpiryDate` is before `ManufacturingDate` | Validation | Skip line, continue remaining lines. | Error |
| E8 | Duplicate movement detected (idempotency) | Duplicate | Skip line, continue remaining lines. | Information |
| E9 | Batch unique constraint violation (concurrent insert) | Conflict | Re-query existing batch, retry line processing. | Warning |
| E10 | Database exception during line processing | Infrastructure | Log full exception, skip line, continue remaining lines. | Error |
| E11 | Auto-generation of batch number fails (`ISequenceGenerator` unavailable) | Infrastructure | Use fallback format `{ProductCode}-{YYYYMMDD}-{GoodsReceiptLineId}`. | Warning |
| E12 | Unhandled exception in consumer | Infrastructure | Catch at top level, log, acknowledge message. | Error |

---

## 5. Versioning Notes

| Version | Date | Type | Description |
|---|---|---|---|
| v1 | 2026-04-09 | Initial | Initial specification for batch creation on goods receipt flow. Covers `GoodsReceiptCompletedEvent` and `GoodsReceiptLineAcceptedEvent` consumers, batch resolution, stock movement creation, stock level upsert, idempotency, and partial failure handling. |

---

## 6. Test Plan

### Unit Tests --- GoodsReceiptCompletedConsumerTests

- `Consume_ValidEventWithBatchNumber_CreatesBatchAndMovementAndStockLevel` [Unit]
- `Consume_ValidEventWithExistingBatch_ReusesBatchDoesNotCreateDuplicate` [Unit]
- `Consume_ProductRequiresBatchTracking_NoBatchNumber_AutoGeneratesBatchNumber` [Unit]
- `Consume_ProductDoesNotRequireBatchTracking_NoBatchNumber_SkipsBatchCreation` [Unit]
- `Consume_ProductDoesNotRequireBatchTracking_BatchNumberProvided_CreatesBatch` [Unit]
- `Consume_DuplicateEventDelivery_SkipsAlreadyProcessedLines` [Unit]
- `Consume_MultipleLines_ProcessesEachLineIndependently` [Unit]
- `Consume_OneLineFailsOtherSucceeds_PartialSuccessLogsError` [Unit]
- `Consume_ProductNotFound_SkipsLineLogsError` [Unit]
- `Consume_ZeroQuantity_SkipsLineLogsError` [Unit]
- `Consume_NegativeQuantity_SkipsLineLogsError` [Unit]
- `Consume_EmptyLines_LogsWarningAndAcknowledges` [Unit]
- `Consume_NullLines_LogsErrorAndAcknowledges` [Unit]
- `Consume_InvalidGoodsReceiptId_LogsErrorAndAcknowledges` [Unit]
- `Consume_InvalidWarehouseId_LogsErrorAndAcknowledges` [Unit]
- `Consume_BatchNumberExceeds50Chars_SkipsLineLogsError` [Unit]
- `Consume_ExpiryBeforeManufacturingDate_SkipsLineLogsError` [Unit]
- `Consume_StockMovementCreatedWithReasonReceipt` [Unit]
- `Consume_StockMovementReferencesTypeIsPurchaseOrder` [Unit]
- `Consume_StockMovementReferenceIdIsGoodsReceiptLineId` [Unit]
- `Consume_StockMovementReferenceNumberIsPONumber` [Unit]
- `Consume_StockMovementCreatedAtUtcMatchesEventReceivedAtUtc` [Unit]
- `Consume_StockMovementCreatedByUserIdMatchesEventReceivedByUserId` [Unit]
- `Consume_StockLevelCreatedWhenNotExists` [Unit]
- `Consume_StockLevelIncrementedWhenExists` [Unit]
- `Consume_StockLevelModifiedAtUtcUpdatedOnIncrement` [Unit]
- `Consume_BatchCreatedWithManufacturingAndExpiryDates` [Unit]
- `Consume_BatchCreatedWithIsActiveTrue` [Unit]
- `Consume_BatchCreatedByUserIdMatchesEventReceivedByUserId` [Unit]
- `Consume_DoesNotThrowOnAnyError` [Unit]

### Unit Tests --- GoodsReceiptLineAcceptedConsumerTests

- `Consume_ValidAcceptedLineEvent_CreatesBatchAndMovementAndStockLevel` [Unit]
- `Consume_DuplicateAcceptedLineEvent_SkipsProcessing` [Unit]
- `Consume_AcceptedLineProductRequiresBatchTracking_AutoGeneratesBatchNumber` [Unit]
- `Consume_AcceptedLineUsesAcceptedByUserIdForCreatedBy` [Unit]
- `Consume_AcceptedLineUsesAcceptedAtUtcForCreatedAtUtc` [Unit]

### Unit Tests --- BatchResolutionTests

- `ResolveBatch_ExistingBatchNumberForProduct_ReturnsExistingBatchId` [Unit]
- `ResolveBatch_NewBatchNumberForProduct_CreatesNewBatch` [Unit]
- `ResolveBatch_ConcurrentInsertConflict_RetriesAndReusesExistingBatch` [Unit]
- `ResolveBatch_AutoGenerate_UsesProductCodeDateSequenceFormat` [Unit]
- `ResolveBatch_AutoGenerate_FallbackWhenSequenceGeneratorUnavailable` [Unit]

### Integration Tests --- GoodsReceiptStockIntakeTests

- `GoodsReceiptCompletedEvent_Published_ConsumerCreatesStockRecords` [Integration]
- `GoodsReceiptCompletedEvent_MultipleLines_AllLinesProcessed` [Integration]
- `GoodsReceiptCompletedEvent_DuplicateDelivery_NoduplicateRecords` [Integration]
- `GoodsReceiptCompletedEvent_BatchTrackingProduct_BatchAutoGenerated` [Integration]
- `GoodsReceiptCompletedEvent_StockLevelUpsertAtomicity_ConcurrentReceipts` [Integration]
- `GoodsReceiptLineAcceptedEvent_Published_ConsumerCreatesStockRecords` [Integration]

---

## 7. Key Files

### New Files

| File | Type | Role |
|---|---|---|
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Consumers/GoodsReceiptCompletedConsumer.cs` | Consumer | MassTransit consumer for `GoodsReceiptCompletedEvent` |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Consumers/GoodsReceiptLineAcceptedConsumer.cs` | Consumer | MassTransit consumer for `GoodsReceiptLineAcceptedEvent` |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/ReceiptStockIntakeService.cs` | Service | Shared logic for batch resolution, movement creation, stock level upsert |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Interfaces/IReceiptStockIntakeService.cs` | Interface | Service interface for receipt stock intake |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Consumers/GoodsReceiptCompletedConsumerTests.cs` | Test | Unit tests for the consumer |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Consumers/GoodsReceiptLineAcceptedConsumerTests.cs` | Test | Unit tests for the accepted line consumer |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Unit/Services/ReceiptStockIntakeServiceTests.cs` | Test | Unit tests for batch resolution logic |
| `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/Integration/Consumers/GoodsReceiptStockIntakeTests.cs` | Test | Integration tests with MassTransit test harness |

### Modified Files

| File | Type | Change |
|---|---|---|
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Program.cs` | DI root | Register consumers and `IReceiptStockIntakeService` |

### Existing Files Referenced

| File | Type | Role |
|---|---|---|
| `src/Warehouse.ServiceModel/Events/GoodsReceiptCompletedEvent.cs` | Event | Event contract (already exists) |
| `src/Warehouse.ServiceModel/Events/GoodsReceiptLineAcceptedEvent.cs` | Event | Event contract (already exists) |
| `src/Databases/Warehouse.Inventory.DBModel/Models/Batch.cs` | Entity | Batch entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/StockMovement.cs` | Entity | Stock movement entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/StockLevel.cs` | Entity | Stock level entity |
| `src/Databases/Warehouse.Inventory.DBModel/Models/Product.cs` | Entity | Product entity (`RequiresBatchTracking` flag) |
| `src/Databases/Warehouse.Inventory.DBModel/InventoryDbContext.cs` | DbContext | Inventory EF Core context |
| `src/Warehouse.Common/Enums/StockMovementReason.cs` | Enum | `Receipt` reason code |
| `src/Warehouse.Common/Enums/StockMovementReferenceType.cs` | Enum | `PurchaseOrder` reference type |
