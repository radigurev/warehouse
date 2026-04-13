# CHG-REFAC-005 — Strategy Pattern for Movement, Sorting & Event Mapping

> Status: Implemented
> Last updated: 2026-04-13
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**
Three distinct areas use hardcoded conditional logic that must be modified when new types are introduced: (1) Stock movement recording — three services independently decide reason codes, reference types, and metadata using inline conditionals. (2) Sorting — 12+ services contain `switch` expressions mapping `sortBy` strings to `OrderBy` expressions. (3) Event type mapping in EventQueryService — a `switch` expression maps 5 event subtypes to DTOs. Each of these violates the Open/Closed Principle: adding a new movement type, sortable field, or event domain requires modifying existing code. The Strategy pattern makes each of these extensible via registration rather than modification.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### Stock Movement Strategy

- The system MUST define an `IStockMovementStrategy` interface with a method `CreateMovement(StockMovementContext context): StockMovement`.
- The system MUST provide implementations: `DirectMovementStrategy`, `AdjustmentMovementStrategy`, `TransferMovementStrategy`, `CountAdjustmentMovementStrategy`.
- Each strategy MUST encapsulate: reason code selection, reference type and ID setup, notes/metadata generation, and any strategy-specific validation.
- The system MUST provide an `IStockMovementStrategyFactory` that resolves the correct strategy by `StockMovementReason` or operation context.
- `StockMovementService`, `InventoryAdjustmentService`, and `WarehouseTransferService` MUST delegate movement creation to the appropriate strategy via the factory.
- Adding a new movement type (e.g., `QualityHold`, `Assembly`) MUST require only: creating a new strategy class and registering it in DI.

### Sorting Strategy

- The system MUST define an `ISortStrategy<TEntity>` interface with a method `Apply(IQueryable<TEntity> query, bool descending): IOrderedQueryable<TEntity>`.
- The system MUST provide a `SortStrategyRegistry<TEntity>` that maps sort field names (strings) to `ISortStrategy<TEntity>` implementations.
- Each searchable service MUST register its sortable fields in DI or via a static registry.
- The `ApplySorting` method in services MUST delegate to the registry instead of using `switch` expressions.
- The registry MUST return a default sort strategy when the requested field is not found.
- Adding a new sortable field MUST require only: registering a new strategy in the registry.

### Event Type Mapping Strategy

- The system MUST define an `IEventMappingStrategy` interface with methods: `bool CanMap(OperationsEvent entity)` and `OperationsEventDto Map(OperationsEvent entity, IMapper mapper, bool includePayload)`.
- The system MUST provide implementations for each event subtype: `AuthEventMappingStrategy`, `CustomerEventMappingStrategy`, `InventoryEventMappingStrategy`, `PurchaseEventMappingStrategy`, `FulfillmentEventMappingStrategy`.
- `EventQueryService.MapSingleToDto` MUST iterate registered strategies and delegate to the first that returns `true` from `CanMap`.
- Adding a new event domain MUST require only: creating a new strategy and registering it in DI.

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | Movement reason code | Strategy factory MUST resolve a strategy for the given reason | UNSUPPORTED_MOVEMENT_TYPE |
| V2 | Sort field name | Registry MUST fall back to default sort when field not found | *(no error — graceful fallback)* |

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | No strategy registered for movement reason | 400 | UNSUPPORTED_MOVEMENT_TYPE | Movement type '{reason}' is not supported. |

## 5. Versioning Notes

**API version impact:** None — external API contracts remain identical.

**Database migration required:** No

**Backwards compatibility:** Fully compatible — internal refactor only.

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] DirectMovementStrategy_SetsCorrectReasonAndReference` — verify direct movement fields
- [ ] `[Unit] AdjustmentMovementStrategy_SetsAdjustmentReferenceType` — verify adjustment-specific fields
- [ ] `[Unit] TransferMovementStrategy_SetsTransferReferenceType` — verify transfer-specific fields
- [ ] `[Unit] CountAdjustmentStrategy_SetsStocktakeReference` — verify stocktake-sourced adjustments
- [ ] `[Unit] StrategyFactory_ResolvesCorrectStrategy_ByReason` — verify factory resolution
- [ ] `[Unit] StrategyFactory_ThrowsForUnregisteredReason` — verify error handling
- [ ] `[Unit] SortStrategyRegistry_ResolvesRegisteredField` — verify sort resolution
- [ ] `[Unit] SortStrategyRegistry_FallsBackToDefault` — verify graceful fallback
- [ ] `[Unit] AuthEventMappingStrategy_CanMap_ReturnsTrue_ForAuthEvent` — verify type matching
- [ ] `[Unit] EventMappingStrategies_MapCorrectDtoType` — verify each strategy maps to correct DTO

### Integration Tests

- [ ] `[Integration] StockMovement_ViaStrategy_RecordsCorrectly` — verify end-to-end movement
- [ ] `[Integration] SearchProducts_SortByName_UsesStrategy` — verify sort via registry

## 7. Detailed Design

### Stock Movement Strategy

```csharp
// src/Warehouse.Common/Strategies/IStockMovementStrategy.cs
public interface IStockMovementStrategy
{
    StockMovement CreateMovement(StockMovementContext context);
}

// src/Warehouse.Common/Strategies/StockMovementContext.cs
public sealed class StockMovementContext
{
    public required int ProductId { get; init; }
    public required int WarehouseId { get; init; }
    public int? LocationId { get; init; }
    public int? BatchId { get; init; }
    public required decimal Quantity { get; init; }
    public required StockMovementReason ReasonCode { get; init; }
    public int? ReferenceId { get; init; }
    public StockMovementReferenceType? ReferenceType { get; init; }
    public string? Notes { get; init; }
    public required int CreatedByUserId { get; init; }
}

// src/Warehouse.Common/Strategies/IStockMovementStrategyFactory.cs
public interface IStockMovementStrategyFactory
{
    IStockMovementStrategy GetStrategy(StockMovementReason reason);
}
```

### Sorting Strategy

```csharp
// src/Warehouse.Infrastructure/Sorting/ISortStrategy.cs
public interface ISortStrategy<TEntity>
{
    IOrderedQueryable<TEntity> Apply(IQueryable<TEntity> query, bool descending);
}

// src/Warehouse.Infrastructure/Sorting/SortStrategyRegistry.cs
public sealed class SortStrategyRegistry<TEntity>
{
    private readonly Dictionary<string, ISortStrategy<TEntity>> _strategies;
    private readonly ISortStrategy<TEntity> _defaultStrategy;

    public IOrderedQueryable<TEntity> Apply(IQueryable<TEntity> query, string? field, bool descending)
    {
        ISortStrategy<TEntity> strategy = field is not null
            && _strategies.TryGetValue(field.ToLowerInvariant(), out ISortStrategy<TEntity>? s)
            ? s : _defaultStrategy;
        return strategy.Apply(query, descending);
    }
}
```

### Services Affected

| Service | Change |
|---|---|
| `StockMovementService.RecordAsync` | Delegates to `IStockMovementStrategyFactory.GetStrategy(reason).CreateMovement(context)` |
| `InventoryAdjustmentService.ApplyAdjustmentLinesToStockAsync` | Uses `AdjustmentMovementStrategy` via factory |
| `WarehouseTransferService.AdjustStockForTransferLineAsync` | Uses `TransferMovementStrategy` via factory |
| All 12+ searchable services | Replace `ApplySorting` switch with `SortStrategyRegistry<T>.Apply(...)` |
| `EventQueryService.MapSingleToDto` | Iterate `IEnumerable<IEventMappingStrategy>` from DI |

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-INV-002 | Movement recording behavior unchanged — strategy encapsulates existing logic |
| SDD-INV-003 | Transfer stock adjustment behavior unchanged — delegated to strategy |
| SDD-INV-004 | Stocktake count adjustment behavior unchanged — delegated to strategy |

## Migration Plan

1. **Pre-deployment:** Implement strategy interfaces and factory. Create strategy classes that replicate existing inline logic. Swap one service (StockMovementService) as proof of concept. Run all tests. Migrate remaining services.
2. **Deployment:** Deploy updated assemblies — no schema changes.
3. **Post-deployment:** Verify stock movements, adjustments, and transfers record correctly via API.
4. **Rollback:** Revert to previous commit — no data migration needed.

## Open Questions

- [ ] Should sort strategies be registered per-service (compile-time) or globally in DI (runtime)?
- [ ] Should `StockMovementContext` carry the full entity reference or just the ID + type?
