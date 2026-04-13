# CHG-REFAC-004 — Template Method for Service Base Classes

> Status: Implemented
> Last updated: 2026-04-13
> Owner: TBD
> Priority: P1

## 1. Context & Scope

**Why this change is needed:**
Across 12+ Inventory services, 6+ Customer services, and 6 DbContext files, the same structural patterns repeat: search/paginate (~30 lines per service), CRUD lifecycle (find-validate-map-save), and EF entity configuration (table/schema, PK, audit columns). Each new entity added to the system requires re-implementing this boilerplate, increasing the chance of inconsistency. For an extensible core, new entities should inherit standard behavior and override only what is unique.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### Searchable Service Base

- The system MUST provide a `SearchableServiceBase<TEntity, TDto, TSearchRequest>` abstract class that implements the search/paginate algorithm as a template method.
- The template method MUST execute in this order: `BuildSearchQuery()` → `CountAsync()` → `ApplySorting()` → `Skip/Take` → `MapToDto()` → `WrapInPaginatedResponse()`.
- Subclasses MUST override `BuildSearchQuery(TSearchRequest request)` to provide entity-specific filtering.
- Subclasses SHOULD override `ApplySorting(IQueryable<TEntity> query, string? sortBy, bool sortDescending)` to provide entity-specific sorting. The base MUST provide a default sort by primary key descending.
- Subclasses MUST NOT re-implement pagination logic — it lives exclusively in the base class.
- The base class MUST accept `IMapper` and `DbContext` via constructor injection.

### Customer Contact CRUD Base

- The system MUST provide a `BaseCrudService<TEntity, TDto, TCreateRequest, TUpdateRequest>` abstract class for simple entity CRUD.
- The base MUST implement: `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` as template methods.
- Subclasses MUST override: `MapFromCreateRequest(TCreateRequest)`, `ApplyUpdate(TEntity, TUpdateRequest)`, `ValidateCreateAsync(TCreateRequest)`, `ValidateUpdateAsync(int id, TUpdateRequest)`.
- The existing `BaseCustomerEntityService` MUST be extended (not replaced) with these template methods.
- Primary flag management (unset others, promote next) MUST remain in `PrimaryFlagHelper` — not duplicated in the base.

### EF Entity Configuration Base

- The system MUST provide a `BaseEntityConfiguration<TEntity>` abstract class implementing `IEntityTypeConfiguration<TEntity>`.
- The base MUST configure: PK with `UseIdentityColumn()`, `CreatedAtUtc` with `SYSUTCDATETIME()` default, `ModifiedAtUtc` as nullable.
- Subclasses MUST call `base.Configure(builder)` and then add entity-specific configuration (relationships, indexes, column constraints).
- The base MUST accept schema name and table name as constructor parameters.
- The base MUST NOT configure relationships or indexes — those are entity-specific.

### Extensibility

- Adding a new searchable entity MUST require only: creating a service class that inherits `SearchableServiceBase`, overriding `BuildSearchQuery`, and optionally overriding `ApplySorting`.
- Adding a new simple CRUD entity MUST require only: creating a service that inherits `BaseCrudService` and implementing the 4 abstract methods.
- Adding a new EF entity MUST require only: creating a configuration class that inherits `BaseEntityConfiguration` and adding entity-specific mappings.

## 3. Validation Rules

No new validation rules — this is a structural refactor.

## 4. Error Rules

No new error rules — this is a structural refactor.

## 5. Versioning Notes

**API version impact:** None — external API contracts remain identical.

**Database migration required:** No

**Backwards compatibility:** Fully compatible — internal refactor only.

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] SearchableServiceBase_AppliesPagination_ReturnsCorrectPage` — verify Skip/Take logic
- [ ] `[Unit] SearchableServiceBase_CountsBeforePaging` — verify TotalCount is unpaginated
- [ ] `[Unit] SearchableServiceBase_UsesSubclassFilter` — verify BuildSearchQuery is called
- [ ] `[Unit] SearchableServiceBase_DefaultSort_ByPkDescending` — verify fallback sorting
- [ ] `[Unit] BaseCrudService_CreateAsync_CallsValidateAndMap` — verify template order
- [ ] `[Unit] BaseCrudService_UpdateAsync_CallsValidateAndApply` — verify template order
- [ ] `[Unit] BaseEntityConfiguration_ConfiguresPkAndAuditColumns` — verify base config
- [ ] `[Unit] BaseEntityConfiguration_SubclassCanAddIndexes` — verify extensibility
- [ ] `[Unit] AllExistingServiceTests_PassUnchanged` — verify no regression

### Integration Tests

- [ ] `[Integration] ProductService_Search_ReturnsPaginatedResults` — verify refactored service
- [ ] `[Integration] CustomerEmailService_CRUD_Lifecycle` — verify refactored contact service

## 7. Detailed Design

### SearchableServiceBase

```csharp
// src/Warehouse.Infrastructure/Services/SearchableServiceBase.cs
public abstract class SearchableServiceBase<TEntity, TDto, TSearchRequest> : BaseEntityService<DbContext>
    where TEntity : class
    where TSearchRequest : PaginationParams
{
    protected SearchableServiceBase(DbContext context, IMapper mapper) : base(context, mapper) { }

    public async Task<Result<PaginatedResponse<TDto>>> SearchAsync(
        TSearchRequest request, CancellationToken cancellationToken)
    {
        IQueryable<TEntity> query = BuildSearchQuery(request);
        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        query = ApplySorting(query, request.SortBy, request.SortDescending);
        List<TEntity> items = await query
            .Skip(request.Skip).Take(request.EffectivePageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<TDto> dtos = Mapper.Map<IReadOnlyList<TDto>>(items);
        return Result<PaginatedResponse<TDto>>.Success(new PaginatedResponse<TDto>
        {
            Items = dtos, Page = request.Page,
            PageSize = request.EffectivePageSize, TotalCount = totalCount
        });
    }

    protected abstract IQueryable<TEntity> BuildSearchQuery(TSearchRequest request);
    protected virtual IQueryable<TEntity> ApplySorting(
        IQueryable<TEntity> query, string? sortBy, bool sortDescending)
        => sortDescending ? query.OrderByDescending(e => EF.Property<int>(e, "Id"))
                          : query.OrderBy(e => EF.Property<int>(e, "Id"));
}
```

### BaseEntityConfiguration

```csharp
// src/Warehouse.Infrastructure/EntityConfiguration/BaseEntityConfiguration.cs
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class
{
    private readonly string _tableName;
    private readonly string _schema;

    protected BaseEntityConfiguration(string tableName, string schema) { ... }

    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(e => EF.Property<int>(e, "Id"));
        builder.Property(e => EF.Property<int>(e, "Id")).UseIdentityColumn();
        builder.Property(e => EF.Property<DateTime>(e, "CreatedAtUtc"))
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
```

### Services Affected

| Service | Base Class | Override |
|---|---|---|
| `ProductService` | `SearchableServiceBase<Product, ProductDto, SearchProductsRequest>` | `BuildSearchQuery`, `ApplySorting` |
| `WarehouseService` | `SearchableServiceBase<WarehouseEntity, WarehouseDto, SearchWarehousesRequest>` | `BuildSearchQuery`, `ApplySorting` |
| `StockLevelService` | `SearchableServiceBase<StockLevel, StockLevelDto, SearchStockLevelsRequest>` | `BuildSearchQuery`, `ApplySorting` |
| `BatchService` | `SearchableServiceBase<Batch, BatchDto, SearchBatchesRequest>` | `BuildSearchQuery`, `ApplySorting` |
| `StorageLocationService` | `SearchableServiceBase<StorageLocation, StorageLocationDto, ...>` | `BuildSearchQuery`, `ApplySorting` |
| `ZoneService` | `SearchableServiceBase<Zone, ZoneDto, SearchZonesRequest>` | `BuildSearchQuery`, `ApplySorting` |
| `StocktakeSessionService` | `SearchableServiceBase<StocktakeSession, ..., ...>` | `BuildSearchQuery`, `ApplySorting` |
| `WarehouseTransferService` | `SearchableServiceBase<WarehouseTransfer, ..., ...>` | `BuildSearchQuery`, `ApplySorting` |
| `InventoryAdjustmentService` | `SearchableServiceBase<InventoryAdjustment, ..., ...>` | `BuildSearchQuery`, `ApplySorting` |
| `StockMovementService` | `SearchableServiceBase<StockMovement, ..., ...>` | `BuildSearchQuery`, `ApplySorting` |
| `CustomerEmailService` | `BaseCrudService<CustomerEmail, ..., ..., ...>` | CRUD overrides |
| `CustomerPhoneService` | `BaseCrudService<CustomerPhone, ..., ..., ...>` | CRUD overrides |
| `CustomerAddressService` | `BaseCrudService<CustomerAddress, ..., ..., ...>` | CRUD overrides |

### DbContext Configurations Affected

All 6 DbContext files: AuthDbContext, CustomersDbContext, InventoryDbContext, PurchasingDbContext, FulfillmentDbContext, EventLogDbContext — each entity configuration method migrates to a separate `*Configuration` class inheriting `BaseEntityConfiguration`.

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-INV-001 | No behavior change — services refactored to use base class |
| SDD-INV-002 | No behavior change — services refactored to use base class |
| SDD-INV-003 | No behavior change — services refactored to use base class |
| SDD-INV-004 | No behavior change — services refactored to use base class |
| SDD-CUST-001 | No behavior change — contact services refactored to use base class |

## Migration Plan

1. **Pre-deployment:** Implement base classes in Warehouse.Infrastructure. Migrate one service (e.g., ProductService) as proof of concept. Run all tests. Then migrate remaining services.
2. **Deployment:** Deploy updated assemblies — no schema changes.
3. **Post-deployment:** Verify all search endpoints return correct paginated results.
4. **Rollback:** Revert to previous commit — no data migration needed.

## Open Questions

- [ ] Should `SearchableServiceBase` live in `Warehouse.Infrastructure` or in each API project?
- [ ] Should EF configuration classes be extracted to separate files per entity, or remain as private methods in DbContext?
