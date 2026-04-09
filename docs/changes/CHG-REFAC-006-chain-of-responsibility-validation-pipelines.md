# CHG-REFAC-006 — Chain of Responsibility for Validation Pipelines

> Status: Draft
> Last updated: 2026-04-09
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**
Service methods across the codebase follow a repeated pattern: sequential `Result? validation = await Validate*Async(...); if (validation is not null) return failure;` chains. In `StockMovementService.RecordAsync` there are 4 sequential validators; in `CustomerService.CreateAsync` and `UpdateAsync` there are 3 each (code, taxId, category); in `AuthService.RefreshAsync` there are 4 sequential auth checks. These validators are private methods, not reusable across services — yet `ValidateProductExistsAsync` and `ValidateWarehouseExistsAsync` are needed by multiple services. Adding a new validation rule requires modifying the service method. The Chain of Responsibility pattern makes validators composable, reusable, and independently testable.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### Validation Chain Infrastructure

- The system MUST define an `IValidator<TRequest>` interface with method `Task<Result?> ValidateAsync(TRequest request, CancellationToken cancellationToken)`.
- The system MUST provide a `ValidationChain<TRequest>` that executes validators in registration order and returns the first failure (fail-fast mode).
- The system SHOULD support a `ValidateAll` mode that accumulates all failures into a single `Result` with multiple error details.
- Validators MUST be registered in DI as `IEnumerable<IValidator<TRequest>>` so the chain resolves automatically.
- Each validator MUST have a single responsibility — one check per class.
- Adding a new validation rule MUST require only: creating a new `IValidator<TRequest>` implementation and registering it in DI.

### Reusable Validators

- The system MUST extract the following validators as reusable, injectable classes:
  - `ProductExistsValidator` — verifies product exists and is not deleted
  - `WarehouseExistsValidator` — verifies warehouse exists and is active
  - `LocationExistsValidator` — verifies storage location exists and belongs to warehouse
  - `BatchTrackingValidator` — verifies batch tracking rules for a product
  - `SufficientStockValidator` — verifies available stock for outbound movements
  - `UniquenessValidator<TEntity>` — generic uniqueness check (code, taxId, email, etc.)
  - `CategoryExistsValidator` — verifies category exists (customer or product)
- These validators MUST be shared across services that need them — no more private method duplication.

### Service Integration

- `StockMovementService.RecordAsync` MUST use `ValidationChain<RecordStockMovementRequest>` with: ProductExists, BatchTracking, WarehouseExists, SufficientStock.
- `CustomerService.CreateAsync` MUST use `ValidationChain<CreateCustomerRequest>` with: UniqueCode, UniqueTaxId, CategoryExists.
- `CustomerService.UpdateAsync` MUST use `ValidationChain<UpdateCustomerRequest>` with: UniqueCode (excluding self), UniqueTaxId (excluding self), CategoryExists.
- `AuthService.RefreshAsync` SHOULD use a validation chain for token checks: TokenExists, TokenNotRevoked, TokenNotExpired, UserIsActive.
- Services MUST NOT contain inline validation logic that duplicates chain validators.

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | Chain execution | First failure stops the chain (fail-fast mode) | *(varies by validator)* |
| V2 | Chain execution | ValidateAll mode accumulates all failures | VALIDATION_FAILED |

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Product not found or deleted | 404 | PRODUCT_NOT_FOUND | Product with ID {id} was not found. |
| E2 | Warehouse not found | 404 | WAREHOUSE_NOT_FOUND | Warehouse with ID {id} was not found. |
| E3 | Insufficient stock | 409 | INSUFFICIENT_STOCK | Insufficient stock for product {id} at location {id}. |
| E4 | Duplicate code/taxId/email | 409 | DUPLICATE_{FIELD} | A record with this {field} already exists. |
| E5 | Category not found | 404 | CATEGORY_NOT_FOUND | Category with ID {id} was not found. |

*Note: These error codes already exist — the chain produces identical errors to current inline logic.*

## 5. Versioning Notes

**API version impact:** None — external API contracts and error responses remain identical.

**Database migration required:** No

**Backwards compatibility:** Fully compatible — internal refactor only.

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] ProductExistsValidator_ReturnsNull_WhenProductExists` — verify happy path
- [ ] `[Unit] ProductExistsValidator_ReturnsFailure_WhenProductNotFound` — verify error
- [ ] `[Unit] SufficientStockValidator_ReturnsNull_WhenStockAvailable` — verify happy path
- [ ] `[Unit] SufficientStockValidator_ReturnsFailure_WhenInsufficient` — verify error
- [ ] `[Unit] UniquenessValidator_ReturnsNull_WhenUnique` — verify happy path
- [ ] `[Unit] UniquenessValidator_ReturnsFailure_WhenDuplicate` — verify error
- [ ] `[Unit] UniquenessValidator_ExcludesSelf_OnUpdate` — verify exclude logic
- [ ] `[Unit] ValidationChain_FailFast_StopsAtFirstFailure` — verify chain behavior
- [ ] `[Unit] ValidationChain_ValidateAll_AccumulatesFailures` — verify accumulation
- [ ] `[Unit] ValidationChain_AllPass_ReturnsNull` — verify success path
- [ ] `[Unit] BatchTrackingValidator_RequiresBatch_ForTrackedProduct` — verify batch rule

### Integration Tests

- [ ] `[Integration] RecordStockMovement_ChainValidation_RejectsInvalidProduct` — end-to-end
- [ ] `[Integration] CreateCustomer_ChainValidation_RejectsDuplicateCode` — end-to-end

## 7. Detailed Design

### Interfaces

```csharp
// src/Warehouse.Common/Validation/IValidator.cs
public interface IValidator<in TRequest>
{
    int Order { get; }
    Task<Result?> ValidateAsync(TRequest request, CancellationToken cancellationToken);
}

// src/Warehouse.Common/Validation/ValidationChain.cs
public sealed class ValidationChain<TRequest>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationChain(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators.OrderBy(v => v.Order);
    }

    public async Task<Result?> ExecuteAsync(TRequest request, CancellationToken cancellationToken)
    {
        foreach (IValidator<TRequest> validator in _validators)
        {
            Result? result = await validator.ValidateAsync(request, cancellationToken);
            if (result is not null)
                return result;
        }
        return null;
    }
}
```

### Validator Example

```csharp
// src/Interfaces/Inventory/Warehouse.Inventory.API/Validators/Chain/ProductExistsValidator.cs
public sealed class ProductExistsValidator : IValidator<RecordStockMovementRequest>
{
    private readonly InventoryDbContext _context;
    public int Order => 10;

    public async Task<Result?> ValidateAsync(
        RecordStockMovementRequest request, CancellationToken cancellationToken)
    {
        bool exists = await _context.Products
            .AnyAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);
        return exists ? null : Result.Failure("PRODUCT_NOT_FOUND", $"Product with ID {request.ProductId} was not found.", 404);
    }
}
```

### DI Registration

```csharp
// In Inventory.API Program.cs
services.AddScoped<IValidator<RecordStockMovementRequest>, ProductExistsValidator>();
services.AddScoped<IValidator<RecordStockMovementRequest>, BatchTrackingValidator>();
services.AddScoped<IValidator<RecordStockMovementRequest>, WarehouseExistsValidator>();
services.AddScoped<IValidator<RecordStockMovementRequest>, SufficientStockValidator>();
services.AddScoped<ValidationChain<RecordStockMovementRequest>>();
```

### Services Affected

| Service | Current Validators | Chain Replaces |
|---|---|---|
| `StockMovementService.RecordAsync` | 4 private methods | `ValidationChain<RecordStockMovementRequest>` |
| `CustomerService.CreateAsync` | 3 private methods | `ValidationChain<CreateCustomerRequest>` |
| `CustomerService.UpdateAsync` | 3 private methods | `ValidationChain<UpdateCustomerRequest>` |
| `AuthService.RefreshAsync` | 4 inline if-checks | `ValidationChain<RefreshTokenRequest>` |
| `InventoryAdjustmentService.CreateAsync` | 2 private methods | `ValidationChain<CreateAdjustmentRequest>` |
| `WarehouseTransferService.CreateAsync` | 2 private methods | `ValidationChain<CreateTransferRequest>` |

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-INV-002 | Validation behavior unchanged — validators produce identical errors |
| SDD-INV-003 | Validation behavior unchanged |
| SDD-INV-004 | Validation behavior unchanged |
| SDD-CUST-001 | Validation behavior unchanged |
| SDD-AUTH-001 | Token validation behavior unchanged |

## Migration Plan

1. **Pre-deployment:** Implement chain infrastructure in Warehouse.Common. Extract validators from StockMovementService as proof of concept. Run all tests. Migrate remaining services.
2. **Deployment:** Deploy updated assemblies — no schema changes.
3. **Post-deployment:** Verify validation errors return identical HTTP status codes and error codes.
4. **Rollback:** Revert to previous commit — no data migration needed.

## Open Questions

- [ ] Should validators that need DbContext access live in the API project or in a shared validation project?
- [ ] Should the chain support async validator ordering (e.g., cheap checks first, DB queries last)?
- [ ] Should FluentValidation (request-level) and chain validators (business-level) be unified or kept separate?
