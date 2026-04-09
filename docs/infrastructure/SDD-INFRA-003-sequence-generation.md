# SDD-INFRA-003 — Centralized Sequence Generation

> Status: Draft
> Last updated: 2026-04-09
> Owner: TBD
> Category: Infrastructure

## 1. Context & Scope

This spec defines a centralized sequence/number generation service in `Warehouse.Infrastructure` that all microservices use to generate sequential, formatted, unique identifiers. It replaces the current ad-hoc inline generation methods scattered across individual domain services (`GenerateOrderNumberAsync`, `GenerateReceiptNumberAsync`, `GenerateReturnNumberAsync`, `GenerateShipmentNumberAsync`, `GeneratePickListNumberAsync`, `GenerateParcelNumberAsync`, `GenerateCustomerCodeAsync`, `GenerateSupplierCodeAsync`).

The service uses a database table (`infrastructure.Sequences`) with row-level locking to guarantee uniqueness and sequential ordering under concurrent access. Each consuming microservice calls the shared `ISequenceGenerator` interface to obtain the next formatted identifier.

**ISA-95 Conformance:** Conforms to ISA-95 Part 2 Section 7 (Material Model -- Material Lot identification via batch numbers) and ISA-95 Part 3 (Operations Event identification -- document numbers for procurement, fulfillment, and inventory operations). Centralized sequence generation ensures globally unique, traceable identifiers across all ISA-95 operations domains.

**In scope:**

- `ISequenceGenerator` interface and `SequenceGenerator` implementation in `Warehouse.Infrastructure`
- `SequenceDefinition` configuration class for registering known sequence patterns
- Database table `infrastructure.Sequences` for persistent counter storage with row-level locking
- Reset policies: `Daily`, `Monthly`, `Never` (monotonically increasing)
- DI registration via `services.AddSequenceGenerator()` extension method
- All document-number and auto-code sequences currently implemented inline across services
- Batch number generation with per-product sequence keys

**Out of scope:**

- Migration of existing data (already-generated numbers remain valid; no renumbering)
- Removal of inline generation methods from domain services (separate refactoring task per domain)
- REST API endpoints for managing sequences (administrative tooling is deferred)
- Sequence reservation or batch allocation (allocating ranges of numbers in advance)
- Distributed sequence generation across multiple database servers (single SQL Server assumed)

**Related specs:**

- `SDD-INFRA-001` -- Shared infrastructure library (this service extends it with sequence generation)
- `SDD-PURCH-001` -- Procurement operations (PO, GR, SR number generation; supplier auto-code)
- `SDD-FULF-001` -- Fulfillment operations (SO, PL, PKG, SH, RMA number generation)
- `SDD-INV-001` -- Products and catalog (product auto-code generation)
- `SDD-INV-002` -- Stock management (batch number generation)
- `SDD-CUST-001` -- Customers and accounts (customer auto-code generation)

---

## 2. Behavior

### 2.1 Sequence Definitions

The service MUST support the following sequence definitions. Each definition specifies a prefix, format pattern, reset policy, and the counter zero-padding width.

| Sequence Key | Format Pattern | Example Output | Reset Policy | Padding | Used By |
|---|---|---|---|---|---|
| `PO` | `PO-{yyyyMMdd}-{nnnn}` | `PO-20260410-0001` | Daily | 4 | Purchasing -- Purchase Orders |
| `GR` | `GR-{yyyyMMdd}-{nnnn}` | `GR-20260410-0001` | Daily | 4 | Purchasing -- Goods Receipts |
| `SR` | `SR-{yyyyMMdd}-{nnnn}` | `SR-20260410-0001` | Daily | 4 | Purchasing -- Supplier Returns |
| `SO` | `SO-{yyyyMMdd}-{nnnn}` | `SO-20260410-0001` | Daily | 4 | Fulfillment -- Sales Orders |
| `PL` | `PL-{yyyyMMdd}-{nnnn}` | `PL-20260410-0001` | Daily | 4 | Fulfillment -- Pick Lists |
| `PKG` | `PKG-{yyyyMMdd}-{nnnn}` | `PKG-20260410-0001` | Daily | 4 | Fulfillment -- Parcels |
| `SHP` | `SHP-{yyyyMMdd}-{nnnn}` | `SHP-20260410-0001` | Daily | 4 | Fulfillment -- Shipments |
| `CR` | `CR-{yyyyMMdd}-{nnnn}` | `CR-20260410-0001` | Daily | 4 | Fulfillment -- Customer Returns |
| `SUPP` | `SUPP-{nnnnnn}` | `SUPP-000001` | Never | 6 | Purchasing -- Supplier auto-code |
| `PROD` | `PROD-{nnnnnn}` | `PROD-000001` | Never | 6 | Inventory -- Product auto-code |
| `CUST` | `CUST-{nnnnnn}` | `CUST-000001` | Never | 6 | Customers -- Customer auto-code |
| `BATCH` | `{ProductCode}-{yyyyMM}-{nnn}` | `PROD-001-202604-001` | Monthly | 3 | Inventory -- Batch numbers |

**Note on fulfillment prefix changes:** The existing inline implementations use `SH-` for shipments and `RMA-` for customer returns. This spec defines the canonical prefixes as `SHP-` and `CR-` respectively. When domain services are refactored to use `ISequenceGenerator`, the new prefixes MUST be applied going forward. Existing records with `SH-` and `RMA-` prefixes are grandfathered and MUST NOT be renumbered.

**Note on entity auto-code padding:** Existing inline implementations for `CUST-` and `SUPP-` use 6-digit padding (e.g., `CUST-000001`). This spec preserves that convention. The `PROD-` auto-code, which is new, MUST also use 6-digit padding for consistency.

### 2.2 NextAsync -- Primary Sequence Generation

`NextAsync(string sequenceKey, CancellationToken cancellationToken)` MUST:

1. Determine the `SequenceDefinition` for the given `sequenceKey` from the registered definitions.
2. Compute the composite key based on the definition's reset policy:
   - `Daily`: composite key = `{sequenceKey}:{yyyyMMdd}` (e.g., `PO:20260410`)
   - `Monthly`: composite key = `{sequenceKey}:{yyyyMM}` (e.g., `BATCH:202604`)
   - `Never`: composite key = `{sequenceKey}` (e.g., `CUST`)
3. Open a database transaction with `IsolationLevel.Serializable`.
4. Execute a SQL statement that atomically reads and increments the counter for the composite key, using `UPDLOCK, HOLDLOCK` row hints to prevent concurrent reads from obtaining the same value.
5. If no row exists for the composite key, insert a new row with `CurrentValue = 1` (upsert behavior).
6. If a row exists, increment `CurrentValue` by 1 and update `ModifiedAtUtc`.
7. Commit the transaction.
8. Format the output string using the definition's format pattern and the new counter value.
9. Return the formatted string.

The method MUST return a unique, sequential value even when multiple concurrent callers request the same sequence key simultaneously.

The method MUST NOT use any caching layer -- it MUST always read from and write to the database to guarantee correctness.

The method MUST use `DateTime.UtcNow` for all date computations (date segment in format patterns, reset policy evaluation, and `ModifiedAtUtc`).

**Edge cases:**

- First call for a new date segment (daily reset): the counter MUST start at 1 for the new date, regardless of the previous day's counter value. A new row MUST be inserted for the new composite key.
- First call for a new month segment (monthly reset): the counter MUST start at 1 for the new month.
- Concurrent calls: two simultaneous calls for the same sequence key MUST produce different, sequential values. The second caller MUST wait for the first caller's transaction to complete before obtaining its value.
- Unknown sequence key (not registered in definitions): the method MUST throw `ArgumentException` with a message identifying the unrecognized key.

### 2.3 NextBatchNumberAsync -- Batch Number Generation

`NextBatchNumberAsync(string productCode, CancellationToken cancellationToken)` MUST:

1. Use the `BATCH` sequence definition internally.
2. Compute the composite key as `BATCH:{productCode}:{yyyyMM}` (e.g., `BATCH:PROD-001:202604`).
3. Follow the same atomic increment logic as `NextAsync` (Section 2.2, steps 3--7).
4. Format the output as `{productCode}-{yyyyMM}-{nnn}` where `{nnn}` is the counter zero-padded to 3 digits.
5. Return the formatted string.

The composite key includes the product code, ensuring that batch number counters are independent per product per month.

**Edge cases:**

- Two different products generating batch numbers in the same month MUST have independent counters (e.g., `PROD-001-202604-001` and `PROD-002-202604-001` are both valid).
- A product code containing hyphens (e.g., `PROD-001`) MUST be handled correctly -- the composite key uses `BATCH:{productCode}:{yyyyMM}` so the product code is treated as an opaque string.
- If `productCode` is null or empty, the method MUST throw `ArgumentException`.

### 2.4 Database Table Schema

The `infrastructure.Sequences` table MUST have the following schema:

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `INT` | `IDENTITY(1,1)`, `PRIMARY KEY` | Surrogate primary key |
| `CompositeKey` | `NVARCHAR(200)` | `NOT NULL`, `UNIQUE` | Composite key (e.g., `PO:20260410`, `CUST`, `BATCH:PROD-001:202604`) |
| `SequenceKey` | `NVARCHAR(50)` | `NOT NULL` | Base sequence key (e.g., `PO`, `CUST`, `BATCH`) |
| `CurrentValue` | `INT` | `NOT NULL`, `DEFAULT 0` | Current counter value (last issued number) |
| `ResetPolicy` | `NVARCHAR(20)` | `NOT NULL` | Reset policy: `Daily`, `Monthly`, `Never` |
| `CreatedAtUtc` | `DATETIME2(7)` | `NOT NULL`, `DEFAULT SYSUTCDATETIME()` | Row creation timestamp |
| `ModifiedAtUtc` | `DATETIME2(7)` | `NOT NULL`, `DEFAULT SYSUTCDATETIME()` | Last modification timestamp |

**Naming conventions:**

- Table: `infrastructure.Sequences`
- PK: `PK_Sequences`
- Unique index on `CompositeKey`: `IX_Sequences_CompositeKey` (UNIQUE)
- Index on `SequenceKey`: `IX_Sequences_SequenceKey` (non-unique, for administrative queries)

The table MUST be created via a SQL migration script, not via EF Core migrations, because it is a shared infrastructure table that lives in the `infrastructure` schema and is accessed via raw SQL from any DbContext.

### 2.5 SQL Execution Strategy

The `SequenceGenerator` MUST execute the atomic increment using a raw SQL command against the calling service's database connection. The SQL MUST:

1. Use `MERGE` or an equivalent atomic upsert pattern to handle both insert (new composite key) and update (existing composite key) in a single statement.
2. Apply `WITH (UPDLOCK, HOLDLOCK)` hints on the target row to serialize concurrent access.
3. Return the new `CurrentValue` via an `OUTPUT` clause.
4. Execute within an explicit `DbTransaction` obtained from the calling service's `DbContext.Database`.

The service MUST accept a `DbContext` parameter (or obtain it via DI) to execute SQL against the correct database connection. Since each microservice has its own DbContext and potentially its own database, the `infrastructure.Sequences` table MUST exist in every database that uses sequence generation.

**Edge cases:**

- If the database connection fails during the transaction, the method MUST NOT return a partial result. The transaction MUST be rolled back and the exception propagated.
- If the `infrastructure` schema does not exist, the migration script MUST create it.

### 2.6 DI Registration

`AddSequenceGenerator()` extension method on `IServiceCollection` MUST:

1. Register `ISequenceGenerator` with `SequenceGenerator` as a scoped service (scoped to match DbContext lifetime).
2. Register all built-in `SequenceDefinition` instances (the table in Section 2.1).

The extension method MUST be called in each service's `Program.cs` that needs sequence generation.

The `SequenceGenerator` constructor MUST accept `DbContext` (the calling service's context) to access the database connection.

**Edge cases:**

- If `AddSequenceGenerator()` is called but the `infrastructure.Sequences` table does not exist in the database, the first call to `NextAsync` MUST fail with a `SqlException`. The migration script is a prerequisite.

### 2.7 SequenceDefinition Configuration

Each `SequenceDefinition` MUST contain:

| Property | Type | Description |
|---|---|---|
| `SequenceKey` | `string` | The base key (e.g., `PO`, `CUST`, `BATCH`) |
| `Prefix` | `string` | The prefix in the formatted output (e.g., `PO-`, `CUST-`) |
| `ResetPolicy` | `SequenceResetPolicy` | `Daily`, `Monthly`, or `Never` |
| `Padding` | `int` | Zero-padding width for the counter (e.g., 4 for `0001`) |
| `IncludesDateSegment` | `bool` | Whether the format includes a date portion |
| `DateFormat` | `string?` | Date format string (e.g., `yyyyMMdd`, `yyyyMM`). Null when `IncludesDateSegment` is false. |

`SequenceResetPolicy` MUST be an enum in `Warehouse.Common/Enums/` with values: `Daily`, `Monthly`, `Never`.

The built-in definitions MUST be registered as a `IReadOnlyDictionary<string, SequenceDefinition>` keyed by `SequenceKey`, injected into `SequenceGenerator` via DI.

**Edge cases:**

- Duplicate `SequenceKey` registration MUST throw `InvalidOperationException` at startup.
- A definition with `ResetPolicy = Daily` but `IncludesDateSegment = false` is a configuration error that SHOULD be caught at registration time via validation.

### 2.8 Thread Safety and Concurrency

The service MUST guarantee that no two callers ever receive the same formatted sequence value for the same sequence key, even under high concurrency. This guarantee is provided by:

1. Database-level row locking (`UPDLOCK, HOLDLOCK`) within a serializable transaction.
2. The unique constraint on `CompositeKey` as a safety net against race conditions.

The service MUST NOT use in-memory locking (e.g., `SemaphoreSlim`, `lock`) as the primary concurrency mechanism, because multiple application instances may run behind a load balancer. Database-level locking is the only correct approach for a multi-instance deployment.

**Edge cases:**

- Under sustained high concurrency (e.g., 50+ simultaneous PO creations), callers will experience serialized waits. This is acceptable because sequence generation is a brief operation (single row lock, single increment, immediate commit).
- If a transaction is rolled back by the caller after obtaining a sequence number (e.g., the domain operation fails after getting the number), the number is "consumed" and MUST NOT be reused. Gaps in the sequence are acceptable and expected.

---

## 3. Validation Rules

### 3.1 Input Validation

| # | Field | Rule | Error |
|---|---|---|---|
| V1 | `sequenceKey` (NextAsync) | MUST NOT be null, empty, or whitespace. | `ArgumentException` |
| V2 | `sequenceKey` (NextAsync) | MUST match a registered `SequenceDefinition`. | `ArgumentException` |
| V3 | `productCode` (NextBatchNumberAsync) | MUST NOT be null, empty, or whitespace. | `ArgumentException` |
| V4 | `cancellationToken` | MUST be respected by all async operations. | `OperationCanceledException` |

### 3.2 Configuration Validation

| # | Rule | Consequence |
|---|---|---|
| V5 | Every `SequenceDefinition` MUST have a non-empty `SequenceKey`. | `InvalidOperationException` at registration |
| V6 | `SequenceKey` values MUST be unique across all registered definitions. | `InvalidOperationException` at registration |
| V7 | If `ResetPolicy` is `Daily` or `Monthly`, `IncludesDateSegment` MUST be `true` and `DateFormat` MUST NOT be null. | `InvalidOperationException` at registration |
| V8 | If `ResetPolicy` is `Never`, `IncludesDateSegment` MUST be `false`. | `InvalidOperationException` at registration |
| V9 | `Padding` MUST be greater than 0. | `InvalidOperationException` at registration |

### 3.3 Database Constraints

| # | Rule | Enforcement |
|---|---|---|
| V10 | `CompositeKey` MUST be unique across all rows. | UNIQUE index `IX_Sequences_CompositeKey` |
| V11 | `CurrentValue` MUST be >= 0. | Application-level (counter starts at 0, incremented to 1 on first use) |
| V12 | `ResetPolicy` MUST be one of `Daily`, `Monthly`, `Never`. | Application-level (stored as string for readability) |

---

## 4. Error Rules

| # | Condition | Error Type | Domain Mapping | Notes |
|---|---|---|---|---|
| E1 | `sequenceKey` is null, empty, or whitespace | `ArgumentException` | Thrown immediately, no DB call | Caller bug -- fail fast |
| E2 | `sequenceKey` not found in registered definitions | `ArgumentException` | Thrown immediately, no DB call | Caller bug -- unregistered key |
| E3 | `productCode` is null, empty, or whitespace (batch) | `ArgumentException` | Thrown immediately, no DB call | Caller bug -- fail fast |
| E4 | Database connection failure during transaction | `SqlException` | Propagated to caller | Caller decides retry/error response |
| E5 | `infrastructure.Sequences` table does not exist | `SqlException` | Propagated to caller | Migration not applied -- deployment error |
| E6 | Transaction deadlock | `SqlException` (deadlock) | Propagated to caller | Rare under `UPDLOCK, HOLDLOCK`; caller MAY retry |
| E7 | Duplicate `SequenceKey` registration | `InvalidOperationException` | Thrown at startup | Configuration error |
| E8 | Invalid `SequenceDefinition` (V7, V8, V9 violations) | `InvalidOperationException` | Thrown at startup | Configuration error |
| E9 | `CancellationToken` cancelled | `OperationCanceledException` | Propagated to caller | Standard async cancellation |

The `SequenceGenerator` MUST NOT catch or suppress database exceptions. All `SqlException` instances MUST propagate to the calling service, which is responsible for mapping them to `Result<T>` failures or ProblemDetails responses as appropriate.

---

## 5. Database Schema

### 5.1 Migration Script

The migration script MUST be named `v1.0.0_AddInfrastructureSequencesTable.sql` and placed in each database's migration folder (or executed as a shared script). It MUST:

1. Create the `infrastructure` schema if it does not exist.
2. Create the `infrastructure.Sequences` table with `IF NOT EXISTS` guards.
3. Create the unique index on `CompositeKey` with `IF NOT EXISTS` guards.
4. Create the non-unique index on `SequenceKey` with `IF NOT EXISTS` guards.

```sql
-- Schema
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'infrastructure')
    EXEC('CREATE SCHEMA [infrastructure]');

-- Table
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('infrastructure.Sequences') AND type = 'U')
CREATE TABLE [infrastructure].[Sequences]
(
    [Id]            INT             IDENTITY(1,1)   NOT NULL,
    [CompositeKey]  NVARCHAR(200)   NOT NULL,
    [SequenceKey]   NVARCHAR(50)    NOT NULL,
    [CurrentValue]  INT             NOT NULL    CONSTRAINT DF_Sequences_CurrentValue DEFAULT (0),
    [ResetPolicy]   NVARCHAR(20)    NOT NULL,
    [CreatedAtUtc]  DATETIME2(7)    NOT NULL    CONSTRAINT DF_Sequences_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    [ModifiedAtUtc] DATETIME2(7)    NOT NULL    CONSTRAINT DF_Sequences_ModifiedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Sequences] PRIMARY KEY CLUSTERED ([Id])
);

-- Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sequences_CompositeKey' AND object_id = OBJECT_ID('infrastructure.Sequences'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Sequences_CompositeKey] ON [infrastructure].[Sequences] ([CompositeKey]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sequences_SequenceKey' AND object_id = OBJECT_ID('infrastructure.Sequences'))
    CREATE NONCLUSTERED INDEX [IX_Sequences_SequenceKey] ON [infrastructure].[Sequences] ([SequenceKey]);
```

### 5.2 Atomic Increment SQL

The core SQL for obtaining the next value MUST follow this pattern:

```sql
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;

MERGE [infrastructure].[Sequences] WITH (HOLDLOCK) AS target
USING (SELECT @CompositeKey AS CompositeKey) AS source
ON target.CompositeKey = source.CompositeKey
WHEN MATCHED THEN
    UPDATE SET CurrentValue = CurrentValue + 1, ModifiedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (CompositeKey, SequenceKey, CurrentValue, ResetPolicy, CreatedAtUtc, ModifiedAtUtc)
    VALUES (@CompositeKey, @SequenceKey, 1, @ResetPolicy, SYSUTCDATETIME(), SYSUTCDATETIME())
OUTPUT inserted.CurrentValue;

COMMIT TRANSACTION;
```

---

## 6. Versioning Notes

- **v1 -- Initial specification (2026-04-09)**
  - Defines the centralized `ISequenceGenerator` / `SequenceGenerator` service
  - Covers all 12 sequence definitions (PO, GR, SR, SO, PL, PKG, SHP, CR, SUPP, PROD, CUST, BATCH)
  - Defines database schema, atomic increment strategy, DI registration, and concurrency guarantees
  - Introduces `SHP-` prefix for shipments (replacing inline `SH-`) and `CR-` prefix for customer returns (replacing inline `RMA-`)
  - Status: Draft (not yet implemented)

---

## 7. Test Plan

### Unit Tests -- SequenceGeneratorTests

- `NextAsync_ValidSequenceKey_ReturnsFormattedString` [Unit]
- `NextAsync_DailyReset_IncludesDateSegment` [Unit]
- `NextAsync_NeverReset_OmitsDateSegment` [Unit]
- `NextAsync_FirstCallForKey_ReturnsCounterOne` [Unit]
- `NextAsync_SecondCallForSameKey_ReturnsCounterTwo` [Unit]
- `NextAsync_DailyReset_NewDateResetsCounter` [Unit]
- `NextAsync_MonthlyReset_NewMonthResetsCounter` [Unit]
- `NextAsync_UnknownSequenceKey_ThrowsArgumentException` [Unit]
- `NextAsync_NullSequenceKey_ThrowsArgumentException` [Unit]
- `NextAsync_EmptySequenceKey_ThrowsArgumentException` [Unit]
- `NextAsync_OutputMatchesExpectedFormat_PO` [Unit]
- `NextAsync_OutputMatchesExpectedFormat_CUST` [Unit]
- `NextAsync_OutputMatchesExpectedFormat_SUPP` [Unit]
- `NextAsync_PaddingAppliedCorrectly` [Unit]

### Unit Tests -- NextBatchNumberAsyncTests

- `NextBatchNumberAsync_ValidProductCode_ReturnsFormattedBatchNumber` [Unit]
- `NextBatchNumberAsync_DifferentProducts_IndependentCounters` [Unit]
- `NextBatchNumberAsync_NewMonth_ResetsCounter` [Unit]
- `NextBatchNumberAsync_NullProductCode_ThrowsArgumentException` [Unit]
- `NextBatchNumberAsync_EmptyProductCode_ThrowsArgumentException` [Unit]
- `NextBatchNumberAsync_ProductCodeWithHyphens_HandledCorrectly` [Unit]

### Unit Tests -- SequenceDefinitionTests

- `SequenceDefinition_DuplicateKey_ThrowsInvalidOperationException` [Unit]
- `SequenceDefinition_DailyResetWithoutDateSegment_ThrowsInvalidOperationException` [Unit]
- `SequenceDefinition_NeverResetWithDateSegment_ThrowsInvalidOperationException` [Unit]
- `SequenceDefinition_ZeroPadding_ThrowsInvalidOperationException` [Unit]
- `SequenceDefinition_AllBuiltInDefinitions_AreValid` [Unit]

### Integration Tests -- SequenceGeneratorConcurrencyTests

- `NextAsync_ConcurrentCalls_ProducesUniqueValues` [Integration]
- `NextAsync_ConcurrentCallsSameKey_ValuesAreSequential` [Integration]
- `NextAsync_TransactionRollback_NumberIsConsumed` [Integration]
- `NextBatchNumberAsync_ConcurrentCallsSameProduct_ProducesUniqueValues` [Integration]

### Integration Tests -- SequenceGeneratorDatabaseTests

- `NextAsync_SequenceTableNotExists_ThrowsSqlException` [Integration]
- `NextAsync_NewCompositeKey_InsertsRow` [Integration]
- `NextAsync_ExistingCompositeKey_IncrementsValue` [Integration]
- `NextAsync_DailyReset_CreatesNewRowForNewDate` [Integration]

---

## Key Files (New)

| File | Purpose |
|---|---|
| `src/Warehouse.Infrastructure/Sequences/ISequenceGenerator.cs` | Service interface with `NextAsync` and `NextBatchNumberAsync` |
| `src/Warehouse.Infrastructure/Sequences/SequenceGenerator.cs` | Implementation with raw SQL atomic increment |
| `src/Warehouse.Infrastructure/Sequences/SequenceDefinition.cs` | Configuration record for a sequence pattern |
| `src/Warehouse.Common/Enums/SequenceResetPolicy.cs` | Enum: `Daily`, `Monthly`, `Never` |
| `src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Modified -- add `AddSequenceGenerator()` |
