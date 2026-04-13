# CHG-REFAC-010 — Builder & Bridge for Entity Construction and Configuration

> Status: Implemented
> Last updated: 2026-04-13
> Owner: TBD
> Priority: P3

## 1. Context & Scope

**Why this change is needed:**
Two related issues: (1) Complex entities with nested collections (InventoryAdjustment+Lines, WarehouseTransfer+Lines, BillOfMaterials+BomLines) are constructed inline in service methods with scattered validation and metadata assignment. `StocktakeSessionService.CreateAdjustmentFromSessionAsync` builds an adjustment with lines from variance counts in ~30 lines of inline construction. Adding a new parent-child entity requires re-implementing this construction pattern. (2) EF entity configurations for structurally similar entities (CustomerAddress/SupplierAddress, CustomerPhone/SupplierPhone) are implemented independently in different DbContexts, duplicating address and contact configuration logic. The Builder pattern encapsulates complex construction; the Bridge pattern shares configuration across DbContexts.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### Entity Builders

- The system MUST provide builder classes for entities with nested collections: `AdjustmentBuilder`, `TransferBuilder`, `BomBuilder`.
- Each builder MUST expose a fluent API: `.WithReason(...)`, `.WithLines(...)`, `.CreatedBy(userId)`, `.Build()`.
- The `Build()` method MUST validate required fields and throw `InvalidOperationException` if the entity is incomplete.
- Builders MUST set audit fields (`CreatedAtUtc`, `CreatedByUserId`) automatically.
- `AdjustmentBuilder` MUST support `FromStocktakeSession(session, varianceCounts)` to construct from stocktake variance data.
- Service methods MUST use builders instead of inline object initialization for parent-child entities.
- Adding a new parent-child entity (e.g., `ProductionOrder+Lines`) MUST follow the same builder pattern.

### Bridge for Entity Configuration

- The system MUST provide reusable configuration fragments for structurally similar entity groups.
- `AddressConfigurationFragment<TEntity>` MUST configure: `AddressLine1`, `AddressLine2`, `City`, `Region`, `PostalCode`, `Country`, `IsDefault` with common constraints.
- `ContactConfigurationFragment<TEntity>` MUST configure: `IsPrimary`, `CreatedAtUtc`, `ModifiedAtUtc` with common defaults.
- `AuditableConfigurationFragment<TEntity>` MUST configure: `CreatedAtUtc` default, `ModifiedAtUtc` nullable, `CreatedByUserId`, `ModifiedByUserId`.
- Entity configuration classes MUST compose these fragments: `builder.ApplyFragment(new AddressConfigurationFragment<SupplierAddress>(...))`.
- Adding a new address-like entity MUST require only: applying the fragment and adding entity-specific configuration.

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | Builder.Build() | All required fields MUST be set | *(InvalidOperationException — programming error, not user-facing)* |
| V2 | Builder.WithLines() | At least one line MUST be added | EMPTY_LINE_ITEMS |

## 4. Error Rules

No new user-facing error rules — builders enforce invariants at construction time.

## 5. Versioning Notes

**API version impact:** None

**Database migration required:** No

**Backwards compatibility:** Fully compatible — internal refactor only.

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] AdjustmentBuilder_Build_CreatesValidAdjustment` — verify complete construction
- [ ] `[Unit] AdjustmentBuilder_Build_ThrowsWhenNoLines` — verify validation
- [ ] `[Unit] AdjustmentBuilder_FromStocktakeSession_MapsVarianceCounts` — verify stocktake conversion
- [ ] `[Unit] AdjustmentBuilder_SetsAuditFields` — verify CreatedAtUtc and CreatedByUserId
- [ ] `[Unit] TransferBuilder_Build_CreatesValidTransfer` — verify complete construction
- [ ] `[Unit] TransferBuilder_Build_ThrowsWhenNoLines` — verify validation
- [ ] `[Unit] BomBuilder_Build_CreatesValidBom` — verify complete construction
- [ ] `[Unit] AddressConfigurationFragment_ConfiguresCommonColumns` — verify fragment
- [ ] `[Unit] AuditableConfigurationFragment_ConfiguresTimestampsAndUserIds` — verify fragment

### Integration Tests

- [ ] `[Integration] CreateAdjustment_ViaBuilder_PersistsWithLines` — end-to-end
- [ ] `[Integration] CreateTransfer_ViaBuilder_PersistsWithLines` — end-to-end

## 7. Detailed Design

### Adjustment Builder

```csharp
// src/Interfaces/Inventory/Warehouse.Inventory.API/Builders/AdjustmentBuilder.cs
public sealed class AdjustmentBuilder
{
    private string? _reason;
    private string? _notes;
    private int? _sourceStocktakeSessionId;
    private int _createdByUserId;
    private readonly List<InventoryAdjustmentLine> _lines = [];

    public AdjustmentBuilder WithReason(string reason) { _reason = reason; return this; }
    public AdjustmentBuilder WithNotes(string notes) { _notes = notes; return this; }
    public AdjustmentBuilder CreatedBy(int userId) { _createdByUserId = userId; return this; }

    public AdjustmentBuilder FromStocktakeSession(
        StocktakeSession session, IReadOnlyList<StocktakeCount> varianceCounts)
    {
        _reason = "Stocktake variance";
        _notes = $"Auto-generated from stocktake session #{session.Id}: {session.Name}";
        _sourceStocktakeSessionId = session.Id;
        foreach (StocktakeCount count in varianceCounts)
        {
            _lines.Add(new InventoryAdjustmentLine
            {
                ProductId = count.ProductId,
                WarehouseId = session.WarehouseId,
                LocationId = count.LocationId,
                ExpectedQuantity = count.ExpectedQuantity,
                ActualQuantity = count.ActualQuantity
            });
        }
        return this;
    }

    public AdjustmentBuilder AddLine(Action<InventoryAdjustmentLine> configure)
    {
        InventoryAdjustmentLine line = new();
        configure(line);
        _lines.Add(line);
        return this;
    }

    public InventoryAdjustment Build()
    {
        if (string.IsNullOrWhiteSpace(_reason))
            throw new InvalidOperationException("Reason is required.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("At least one line is required.");

        return new InventoryAdjustment
        {
            Reason = _reason,
            Notes = _notes,
            Status = "Pending",
            SourceStocktakeSessionId = _sourceStocktakeSessionId,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _createdByUserId,
            Lines = _lines
        };
    }
}
```

### Configuration Fragment

```csharp
// src/Warehouse.Infrastructure/EntityConfiguration/AuditableConfigurationFragment.cs
public sealed class AuditableConfigurationFragment<TEntity> where TEntity : class
{
    public void Apply(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property("CreatedAtUtc").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property("ModifiedAtUtc").IsRequired(false);
    }
}

// src/Warehouse.Infrastructure/EntityConfiguration/AddressConfigurationFragment.cs
public sealed class AddressConfigurationFragment<TEntity> where TEntity : class
{
    public void Apply(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property("AddressLine1").HasMaxLength(200).IsRequired();
        builder.Property("AddressLine2").HasMaxLength(200).IsRequired(false);
        builder.Property("City").HasMaxLength(100).IsRequired();
        builder.Property("Region").HasMaxLength(100).IsRequired(false);
        builder.Property("PostalCode").HasMaxLength(20).IsRequired(false);
        builder.Property("Country").HasMaxLength(100).IsRequired();
        builder.Property("IsDefault").HasDefaultValue(false);
    }
}
```

### Services Affected

| Service | Current Code | New Code |
|---|---|---|
| `StocktakeSessionService.CreateAdjustmentFromSessionAsync` | ~30 lines inline construction | `new AdjustmentBuilder().FromStocktakeSession(session, counts).CreatedBy(userId).Build()` |
| `InventoryAdjustmentService.CreateAsync` | Inline adjustment + lines construction | `new AdjustmentBuilder().WithReason(...).AddLine(...).CreatedBy(userId).Build()` |
| `WarehouseTransferService.CreateAsync` | Inline transfer + lines construction | `new TransferBuilder().Between(...).AddLine(...).CreatedBy(userId).Build()` |
| `BomService.CreateAsync` | Inline BOM + lines construction | `new BomBuilder().ForProduct(...).AddComponent(...).Build()` |

### DbContext Configurations Affected

| Configuration | Fragment Applied |
|---|---|
| `CustomerAddress` in CustomersDbContext | `AddressConfigurationFragment<CustomerAddress>` |
| `SupplierAddress` in PurchasingDbContext | `AddressConfigurationFragment<SupplierAddress>` |
| All entities with audit columns | `AuditableConfigurationFragment<T>` |

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-INV-002 | Adjustment creation logic unchanged — encapsulated in builder |
| SDD-INV-003 | Transfer creation logic unchanged — encapsulated in builder |
| SDD-INV-004 | Stocktake-to-adjustment conversion unchanged — encapsulated in builder |
| SDD-INV-001 | BOM creation logic unchanged — encapsulated in builder |

## Migration Plan

1. **Pre-deployment:** Implement builders and configuration fragments. Migrate one service (StocktakeSessionService) as proof of concept. Run all tests. Migrate remaining services and configurations.
2. **Deployment:** Deploy updated assemblies — no schema changes.
3. **Post-deployment:** Verify entity creation via API produces correct parent-child records.
4. **Rollback:** Revert to previous commit — no data migration needed.

## Open Questions

- [ ] Should builders be stateless (new instance per build) or resettable?
- [ ] Should configuration fragments use extension methods on `EntityTypeBuilder<T>` or separate fragment classes?
