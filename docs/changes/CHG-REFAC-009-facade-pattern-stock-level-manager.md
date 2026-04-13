# CHG-REFAC-009 — Facade Pattern for Stock Level Manager

> Status: Implemented
> Last updated: 2026-04-13
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**
Three services independently implement stock level find-or-create, availability validation, and quantity adjustment logic: `InventoryAdjustmentService.ApplyAdjustmentLinesToStockAsync`, `WarehouseTransferService.AdjustStockForTransferLineAsync`, and `StocktakeCountService` (implicit through count updates). Each service directly queries and mutates `StockLevel` entities with its own implementation of the same algorithm. There is no single source of truth for stock manipulation rules. As new operations are added (Fulfillment picking, Production consumption, Quality holds), each would need to re-implement this logic. A `StockLevelManager` facade centralizes all stock operations behind a single interface.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### StockLevelManager Interface

- The system MUST provide an `IStockLevelManager` interface as the single entry point for all stock level mutations.
- The interface MUST expose:
  - `GetOrCreateAsync(int productId, int warehouseId, int? locationId, int? batchId, CancellationToken ct)` — find existing or create new StockLevel record.
  - `AdjustQuantityAsync(StockAdjustmentRequest request, CancellationToken ct)` — validate availability and adjust quantity.
  - `HasSufficientStockAsync(int productId, int warehouseId, int? locationId, decimal quantity, CancellationToken ct)` — check availability without mutating.
  - `GetAvailableQuantityAsync(int productId, int warehouseId, int? locationId, CancellationToken ct)` — return current available quantity.

### Stock Adjustment Rules

- `AdjustQuantityAsync` MUST find-or-create the StockLevel record before adjusting.
- For negative adjustments (outbound), the manager MUST validate that `QuantityOnHand + adjustment >= 0` and return a failure `Result` if insufficient.
- For positive adjustments (inbound), no availability check is required.
- The manager MUST NOT call `SaveChangesAsync` — the calling service owns the unit of work and transaction boundary.
- The manager MUST NOT publish events — event publishing belongs to the calling service or the `IResilientPublisher` (see CHG-REFAC-007).

### Service Integration

- `InventoryAdjustmentService.ApplyAdjustmentLinesToStockAsync` MUST delegate stock adjustments to `IStockLevelManager.AdjustQuantityAsync`.
- `WarehouseTransferService.AdjustStockForTransferLineAsync` MUST delegate debit and credit operations to `IStockLevelManager.AdjustQuantityAsync`.
- `StockMovementService.RecordAsync` SHOULD delegate stock level updates to `IStockLevelManager.AdjustQuantityAsync`.
- Future services (Fulfillment picking, Production consumption) MUST use `IStockLevelManager` — direct `StockLevel` entity manipulation MUST be prohibited outside the manager.

### Extensibility

- Adding a new stock operation (e.g., reservation, hold, allocation) MUST require only: adding a new method to `IStockLevelManager` — not modifying calling services.
- The manager SHOULD be designed to support future extensions: soft reservations, batch-level tracking, location-level constraints.

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | Negative adjustment | QuantityOnHand + adjustment MUST be >= 0 | INSUFFICIENT_STOCK |
| V2 | Product/Warehouse | MUST exist in the database | PRODUCT_NOT_FOUND / WAREHOUSE_NOT_FOUND |

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Insufficient stock for outbound adjustment | 409 | INSUFFICIENT_STOCK | Insufficient stock for product {id}. Available: {qty}, requested: {qty}. |

*Note: This error already exists — the facade produces identical errors.*

## 5. Versioning Notes

**API version impact:** None — external API contracts remain identical.

**Database migration required:** No

**Backwards compatibility:** Fully compatible — internal refactor only.

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] StockLevelManager_GetOrCreate_ReturnsExisting_WhenFound` — verify lookup
- [ ] `[Unit] StockLevelManager_GetOrCreate_CreatesNew_WhenNotFound` — verify creation
- [ ] `[Unit] StockLevelManager_AdjustQuantity_AddsToExisting` — verify positive adjustment
- [ ] `[Unit] StockLevelManager_AdjustQuantity_SubtractsFromExisting` — verify negative adjustment
- [ ] `[Unit] StockLevelManager_AdjustQuantity_RejectsInsufficientStock` — verify validation
- [ ] `[Unit] StockLevelManager_HasSufficientStock_ReturnsTrue_WhenAvailable` — verify check
- [ ] `[Unit] StockLevelManager_HasSufficientStock_ReturnsFalse_WhenInsufficient` — verify check
- [ ] `[Unit] StockLevelManager_DoesNotCallSaveChanges` — verify unit of work boundary

### Integration Tests

- [ ] `[Integration] AdjustmentApply_UsesStockLevelManager_AdjustsCorrectly` — end-to-end
- [ ] `[Integration] TransferComplete_UsesStockLevelManager_DebitsAndCredits` — end-to-end
- [ ] `[Integration] StockMovement_UsesStockLevelManager_UpdatesLevel` — end-to-end

## 7. Detailed Design

### Interface

```csharp
// src/Interfaces/Inventory/Warehouse.Inventory.API/Services/Stock/IStockLevelManager.cs
public interface IStockLevelManager
{
    Task<StockLevel> GetOrCreateAsync(
        int productId, int warehouseId, int? locationId, int? batchId,
        CancellationToken cancellationToken);

    Task<Result> AdjustQuantityAsync(
        StockAdjustmentRequest request, CancellationToken cancellationToken);

    Task<bool> HasSufficientStockAsync(
        int productId, int warehouseId, int? locationId, decimal quantity,
        CancellationToken cancellationToken);

    Task<decimal> GetAvailableQuantityAsync(
        int productId, int warehouseId, int? locationId,
        CancellationToken cancellationToken);
}

// src/Interfaces/Inventory/Warehouse.Inventory.API/Services/Stock/StockAdjustmentRequest.cs
public sealed class StockAdjustmentRequest
{
    public required int ProductId { get; init; }
    public required int WarehouseId { get; init; }
    public int? LocationId { get; init; }
    public int? BatchId { get; init; }
    public required decimal Quantity { get; init; }
}
```

### Implementation

```csharp
// src/Interfaces/Inventory/Warehouse.Inventory.API/Services/Stock/StockLevelManager.cs
public sealed class StockLevelManager : IStockLevelManager
{
    private readonly InventoryDbContext _context;

    public async Task<StockLevel> GetOrCreateAsync(
        int productId, int warehouseId, int? locationId, int? batchId,
        CancellationToken cancellationToken)
    {
        StockLevel? existing = await _context.StockLevels
            .FirstOrDefaultAsync(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId &&
                s.LocationId == locationId &&
                s.BatchId == batchId, cancellationToken);

        if (existing is not null) return existing;

        StockLevel created = new()
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            BatchId = batchId,
            QuantityOnHand = 0
        };
        _context.StockLevels.Add(created);
        return created;
    }

    public async Task<Result> AdjustQuantityAsync(
        StockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        StockLevel stockLevel = await GetOrCreateAsync(
            request.ProductId, request.WarehouseId, request.LocationId, request.BatchId,
            cancellationToken);

        if (request.Quantity < 0 && stockLevel.QuantityOnHand + request.Quantity < 0)
        {
            return Result.Failure("INSUFFICIENT_STOCK",
                $"Insufficient stock. Available: {stockLevel.QuantityOnHand}, requested: {Math.Abs(request.Quantity)}.",
                409);
        }

        stockLevel.QuantityOnHand += request.Quantity;
        stockLevel.ModifiedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }
}
```

### Services Affected

| Service | Current Method | New Call |
|---|---|---|
| `InventoryAdjustmentService` | `ApplyAdjustmentLinesToStockAsync` (inline stock manipulation) | `_stockLevelManager.AdjustQuantityAsync(...)` |
| `WarehouseTransferService` | `AdjustStockForTransferLineAsync` (inline debit/credit) | `_stockLevelManager.AdjustQuantityAsync(...)` x2 (debit + credit) |
| `StockMovementService` | `FindOrCreateStockLevelAsync` + inline update | `_stockLevelManager.AdjustQuantityAsync(...)` |

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-INV-002 | Stock adjustment and movement logic now centralized — behavior identical |
| SDD-INV-003 | Transfer stock debit/credit now centralized — behavior identical |
| SDD-INV-004 | Stocktake-triggered adjustments use same centralized logic |

## Migration Plan

1. **Pre-deployment:** Implement `StockLevelManager`. Migrate `StockMovementService` first as proof of concept. Run all tests. Migrate remaining services.
2. **Deployment:** Deploy updated assemblies — no schema changes.
3. **Post-deployment:** Verify stock levels adjust correctly for movements, adjustments, and transfers.
4. **Rollback:** Revert to previous commit — no data migration needed.

## Open Questions

- [ ] Should the manager support batch-level stock tracking from the start, or add it incrementally?
- [ ] Should the manager expose a `ReserveAsync` method now (for Fulfillment picking), or defer until Fulfillment is implemented?
- [ ] Should the manager validate product/warehouse existence, or should that remain in the validation chain (CHG-REFAC-006)?
