# CHG-REFAC-013 — Fix SOLID Principle Violations Across Microservices

> Status: Draft
> Last updated: 2026-04-15
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**
A cross-project review identified five SOLID principle violations across the Auth, Purchasing, Fulfillment, and Inventory microservices. These include a Dependency Inversion Principle (DIP) violation where an interface depends on a concrete EF Core entity, three cases of Primitive Obsession where methods accept 7-8 primitive parameters instead of parameter objects (violating the project's "parameter objects for 3+ primitives" rule from `csharp-persona.md`), and a one-class-per-file convention violation. All fixes are internal refactors with zero behavior change and zero public API surface impact.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

**Explicitly excluded:**
- No new features or endpoints
- No database schema or migration changes
- No public REST API contract changes (all changes are internal interface/service level)
- No changes to MassTransit event contracts in `Warehouse.ServiceModel/Events/`
- No changes to DTOs, request models, or response models

## 2. Behavior (RFC 2119)

This is a pure structural refactor. All rules below describe the refactored code; the externally observable behavior MUST remain identical.

### Group A — Dependency Inversion: IJwtTokenService (HIGH)

**Before:** `IJwtTokenService.GenerateAccessToken(User user)` accepts an EF Core entity `User` from `Warehouse.Auth.DBModel.Models`. The interface project (`Warehouse.Auth.API`) has a direct dependency on the database model for this single method signature.

**After:**

- The `IJwtTokenService` interface MUST NOT reference `Warehouse.Auth.DBModel.Models.User`.
- The `GenerateAccessToken` method MUST accept primitives or a parameter object containing only the data it needs: user ID (`int`) and username (`string`).
- A `JwtTokenClaims` record MUST be created in the `Warehouse.Auth.API` project (e.g., `Models/JwtTokenClaims.cs`) with properties `int UserId` and `string Username`.
- The method signature MUST become `GenerateAccessToken(JwtTokenClaims claims)`.
- `JwtTokenService` MUST be updated to use the new parameter type. The `BuildClaims` private method MUST accept `JwtTokenClaims` instead of `User`.
- `AuthService.GenerateTokenPairAsync` (the sole caller) MUST construct `JwtTokenClaims` from the `User` entity before calling `GenerateAccessToken`.
- The `User` using directive MUST be removed from `IJwtTokenService.cs`.
- The `User` using directive in `JwtTokenService.cs` MUST be removed if no other usage remains.

**Affected files:**
| File | Change |
|---|---|
| `src/Interfaces/Auth/Warehouse.Auth.API/Models/JwtTokenClaims.cs` | New file |
| `src/Interfaces/Auth/Warehouse.Auth.API/Interfaces/IJwtTokenService.cs` | Signature change |
| `src/Interfaces/Auth/Warehouse.Auth.API/Services/JwtTokenService.cs` | Implementation update |
| `src/Interfaces/Auth/Warehouse.Auth.API/Services/AuthService.cs` | Caller update |

### Group B — Primitive Obsession: Event Recording Methods (MEDIUM)

Three services share the same anti-pattern: a `RecordEventAsync` method with 8 parameters (6 required primitives + 2 optional strings). Each MUST be refactored to accept a single parameter object.

#### B1 — IPurchaseEventService.RecordEventAsync

**Before:** `RecordEventAsync(string eventType, string entityType, int entityId, int userId, string? payload, CancellationToken cancellationToken, string? supplierName = null, string? documentNumber = null)`

**After:**

- A `RecordPurchaseEventRequest` class MUST be created in the Purchasing API project (e.g., `Models/RecordPurchaseEventRequest.cs`).
- The class MUST contain properties: `string EventType`, `string EntityType`, `int EntityId`, `int UserId`, `string? Payload`, `string? SupplierName`, `string? DocumentNumber`.
- `EventType`, `EntityType`, `EntityId`, and `UserId` MUST be marked `required`.
- The method signature MUST become `RecordEventAsync(RecordPurchaseEventRequest request, CancellationToken cancellationToken)`.
- `CancellationToken` MUST remain a separate parameter (not part of the request object).
- All callers MUST be updated to construct the request object instead of passing individual arguments.

**Affected files:**
| File | Change |
|---|---|
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Models/RecordPurchaseEventRequest.cs` | New file |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Interfaces/IPurchaseEventService.cs` | Signature change |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/PurchaseEventService.cs` | Implementation update |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/PurchaseOrderService.cs` | Caller update |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/GoodsReceiptService.cs` | Caller update |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/ReceivingInspectionService.cs` | Caller update |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/SupplierReturnService.cs` | Caller update |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/SupplierService.cs` | Caller update |

#### B2 — IFulfillmentEventService.RecordEventAsync

**Before:** `RecordEventAsync(string eventType, string entityType, int entityId, int userId, string? payload, CancellationToken cancellationToken, string? customerName = null, string? documentNumber = null)`

**After:**

- A `RecordFulfillmentEventRequest` class MUST be created in the Fulfillment API project (e.g., `Models/RecordFulfillmentEventRequest.cs`).
- The class MUST contain properties: `string EventType`, `string EntityType`, `int EntityId`, `int UserId`, `string? Payload`, `string? CustomerName`, `string? DocumentNumber`.
- `EventType`, `EntityType`, `EntityId`, and `UserId` MUST be marked `required`.
- The method signature MUST become `RecordEventAsync(RecordFulfillmentEventRequest request, CancellationToken cancellationToken)`.
- All callers MUST be updated to construct the request object.

**Affected files:**
| File | Change |
|---|---|
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Models/RecordFulfillmentEventRequest.cs` | New file |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Interfaces/IFulfillmentEventService.cs` | Signature change |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/FulfillmentEventService.cs` | Implementation update |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/SalesOrderService.cs` | Caller update |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/PickListService.cs` | Caller update |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/PackingService.cs` | Caller update |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/ShipmentService.cs` | Caller update |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/CustomerReturnService.cs` | Caller update |

#### B3 — IAuditService.LogAsync

**Before:** `LogAsync(int? userId, string action, string resource, string? details, string? ipAddress, CancellationToken cancellationToken, string? username = null)`

**After:**

- An `AuditLogRequest` class MUST be created in the Auth API project (e.g., `Models/AuditLogRequest.cs`).
- The class MUST contain properties: `int? UserId`, `string Action`, `string Resource`, `string? Details`, `string? IpAddress`, `string? Username`.
- `Action` and `Resource` MUST be marked `required`.
- The method signature MUST become `LogAsync(AuditLogRequest request, CancellationToken cancellationToken)`.
- All callers MUST be updated to construct the request object.

**Affected files:**
| File | Change |
|---|---|
| `src/Interfaces/Auth/Warehouse.Auth.API/Models/AuditLogRequest.cs` | New file |
| `src/Interfaces/Auth/Warehouse.Auth.API/Interfaces/IAuditService.cs` | Signature change |
| `src/Interfaces/Auth/Warehouse.Auth.API/Services/AuditService.cs` | Implementation update |
| `src/Interfaces/Auth/Warehouse.Auth.API/Services/UserService.cs` | Caller update |
| `src/Interfaces/Auth/Warehouse.Auth.API/Services/RoleService.cs` | Caller update |
| `src/Interfaces/Auth/Warehouse.Auth.API/Services/PermissionService.cs` | Caller update |

### Group C — Convention: Two Types in One File (LOW)

**Before:** `IReceiptStockIntakeService.cs` contains both the `IReceiptStockIntakeService` interface and the `ReceiptLineContext` class (86 lines total).

**After:**

- `ReceiptLineContext` MUST be moved to its own file at `src/Interfaces/Inventory/Warehouse.Inventory.API/Models/ReceiptLineContext.cs`.
- The `IReceiptStockIntakeService.cs` file MUST contain only the `IReceiptStockIntakeService` interface definition.
- `IReceiptStockIntakeService.cs` MUST add a `using` for the `ReceiptLineContext` namespace if the models are in a different namespace.
- The `ReceiptLineContext` class MUST retain its exact same public API (all properties, XML docs, `required`/`init` modifiers).

**Affected files:**
| File | Change |
|---|---|
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Models/ReceiptLineContext.cs` | New file (moved from interface file) |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/Interfaces/IReceiptStockIntakeService.cs` | Remove class definition |

### Cross-Cutting Rules

- All new parameter object classes MUST follow the project naming conventions and include XML `<summary>` documentation on the class and all public properties.
- All new parameter object classes MUST be `sealed` (no inheritance intended).
- All new files MUST contain exactly one type per file.
- The parameter objects MUST NOT be placed in `Warehouse.ServiceModel` since they are internal to their respective API projects (not shared across services).
- The `CancellationToken` parameter MUST always remain a separate method parameter, never embedded in a request object.
- All existing unit and integration tests MUST continue to pass after the refactor with only mock setup changes (no behavior assertions should change).

## 3. Validation Rules

No new validation rules. This is a structural refactor with no new inputs or business rules.

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| — | — | No changes | — |

## 4. Error Rules

No new error rules. Existing error behavior MUST remain unchanged.

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| — | — | — | — | No changes |

## 5. Versioning Notes

**API version impact:** None -- all changes are internal to service interfaces. No REST endpoint signatures, request/response DTOs, or URL routes change.

**Database migration required:** No

**Backwards compatibility:** Fully compatible at the public API level. Breaking at the internal C# interface level (callers within the same project must be updated simultaneously).

## 6. Test Plan

This is a regression-only test plan. No new behavior is introduced, so tests verify that existing behavior survives the refactor. All test names below follow the convention `MethodName_Scenario_ExpectedResult`.

### Unit Tests

**Group A -- IJwtTokenService DIP fix:**
- [ ] `[Unit] GenerateAccessToken_WithJwtTokenClaims_ReturnsValidToken` -- verify the refactored method produces a valid JWT containing the correct sub and username claims
- [ ] `[Unit] GenerateAccessToken_ClaimsContainUserIdAndUsername_MatchesInput` -- verify claim values match the JwtTokenClaims properties
- [ ] `[Unit] AuthService_GenerateTokenPair_ConstructsJwtTokenClaimsFromUser` -- verify AuthService correctly maps User entity to JwtTokenClaims before calling GenerateAccessToken

**Group B1 -- IPurchaseEventService parameter object:**
- [ ] `[Unit] RecordEventAsync_WithRecordPurchaseEventRequest_PersistsEvent` -- verify the refactored method creates and saves a PurchaseEvent entity with correct field mapping
- [ ] `[Unit] RecordEventAsync_WithRecordPurchaseEventRequest_PublishesEvent` -- verify MassTransit event is published with correct data from the request object
- [ ] `[Unit] PurchaseOrderService_AllCallers_ConstructRequestObject` -- verify all PurchaseOrderService calls to RecordEventAsync compile and pass correct data through the request object

**Group B2 -- IFulfillmentEventService parameter object:**
- [ ] `[Unit] RecordEventAsync_WithRecordFulfillmentEventRequest_PersistsEvent` -- verify the refactored method creates and saves a FulfillmentEvent entity with correct field mapping
- [ ] `[Unit] RecordEventAsync_WithRecordFulfillmentEventRequest_PublishesEvent` -- verify MassTransit event is published with correct data from the request object
- [ ] `[Unit] SalesOrderService_AllCallers_ConstructRequestObject` -- verify all SalesOrderService calls to RecordEventAsync compile and pass correct data through the request object

**Group B3 -- IAuditService parameter object:**
- [ ] `[Unit] LogAsync_WithAuditLogRequest_PersistsEntry` -- verify the refactored method creates and saves a UserActionLog entity with correct field mapping
- [ ] `[Unit] LogAsync_WithAuditLogRequest_PublishesAuthAuditLoggedEvent` -- verify MassTransit event is published with correct data from the request object
- [ ] `[Unit] UserService_AllCallers_ConstructAuditLogRequest` -- verify all UserService calls to LogAsync compile and pass correct data through the request object

**Group C -- File split (compile-time only):**
- [ ] `[Unit] ReceiptStockIntakeService_ProcessLine_StillAcceptsReceiptLineContext` -- verify the moved class works identically from its new file location

**Regression (all groups):**
- [ ] `[Unit] AllExistingAuthServiceTests_PassUnchanged` -- full Auth.API.Tests suite green
- [ ] `[Unit] AllExistingPurchasingServiceTests_PassUnchanged` -- full Purchasing.API.Tests suite green
- [ ] `[Unit] AllExistingFulfillmentServiceTests_PassUnchanged` -- full Fulfillment.API.Tests suite green
- [ ] `[Unit] AllExistingInventoryServiceTests_PassUnchanged` -- full Inventory.API.Tests suite green

### Integration Tests

No new integration tests. Existing integration tests MUST pass without modification to their behavioral assertions (mock setups may need updating for new parameter types).

## 7. Detailed Design

### API Changes

None. No REST endpoints, request DTOs, response DTOs, or URL routes are modified.

### Data Model Changes

None. No database entities, columns, or migrations are affected.

### Service Layer Changes

#### New Files (4 parameter objects + 1 file split)

| # | File | Type | Contents |
|---|---|---|---|
| 1 | `src/Interfaces/Auth/Warehouse.Auth.API/Models/JwtTokenClaims.cs` | Record | `sealed record JwtTokenClaims(int UserId, string Username)` |
| 2 | `src/Interfaces/Auth/Warehouse.Auth.API/Models/AuditLogRequest.cs` | Class | `sealed class` with 6 properties (2 required, 4 optional) |
| 3 | `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Models/RecordPurchaseEventRequest.cs` | Class | `sealed class` with 7 properties (4 required, 3 optional) |
| 4 | `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Models/RecordFulfillmentEventRequest.cs` | Class | `sealed class` with 7 properties (4 required, 3 optional) |
| 5 | `src/Interfaces/Inventory/Warehouse.Inventory.API/Models/ReceiptLineContext.cs` | Class | Moved from `IReceiptStockIntakeService.cs` (no content change) |

#### Modified Interfaces (3)

| # | Interface | Before | After |
|---|---|---|---|
| 1 | `IJwtTokenService.GenerateAccessToken` | `(User user)` | `(JwtTokenClaims claims)` |
| 2 | `IPurchaseEventService.RecordEventAsync` | 8 params | `(RecordPurchaseEventRequest request, CancellationToken ct)` |
| 3 | `IFulfillmentEventService.RecordEventAsync` | 8 params | `(RecordFulfillmentEventRequest request, CancellationToken ct)` |
| 4 | `IAuditService.LogAsync` | 7 params | `(AuditLogRequest request, CancellationToken ct)` |

#### Modified Implementations (4)

| # | Service | Change |
|---|---|---|
| 1 | `JwtTokenService.GenerateAccessToken` | Accept `JwtTokenClaims`, update `BuildClaims` private method |
| 2 | `PurchaseEventService.RecordEventAsync` | Accept `RecordPurchaseEventRequest`, extract fields from request |
| 3 | `FulfillmentEventService.RecordEventAsync` | Accept `RecordFulfillmentEventRequest`, extract fields from request |
| 4 | `AuditService.LogAsync` | Accept `AuditLogRequest`, extract fields from request |

#### Modified Callers (14)

| # | Caller | Calls | Change |
|---|---|---|---|
| 1 | `AuthService.GenerateTokenPairAsync` | `IJwtTokenService.GenerateAccessToken` | Construct `JwtTokenClaims` from `User` entity |
| 2 | `PurchaseOrderService` | `IPurchaseEventService.RecordEventAsync` | Construct `RecordPurchaseEventRequest` |
| 3 | `GoodsReceiptService` | `IPurchaseEventService.RecordEventAsync` | Construct `RecordPurchaseEventRequest` |
| 4 | `ReceivingInspectionService` | `IPurchaseEventService.RecordEventAsync` | Construct `RecordPurchaseEventRequest` |
| 5 | `SupplierReturnService` | `IPurchaseEventService.RecordEventAsync` | Construct `RecordPurchaseEventRequest` |
| 6 | `SupplierService` | `IPurchaseEventService.RecordEventAsync` | Construct `RecordPurchaseEventRequest` |
| 7 | `SalesOrderService` | `IFulfillmentEventService.RecordEventAsync` | Construct `RecordFulfillmentEventRequest` |
| 8 | `PickListService` | `IFulfillmentEventService.RecordEventAsync` | Construct `RecordFulfillmentEventRequest` |
| 9 | `PackingService` | `IFulfillmentEventService.RecordEventAsync` | Construct `RecordFulfillmentEventRequest` |
| 10 | `ShipmentService` | `IFulfillmentEventService.RecordEventAsync` | Construct `RecordFulfillmentEventRequest` |
| 11 | `CustomerReturnService` | `IFulfillmentEventService.RecordEventAsync` | Construct `RecordFulfillmentEventRequest` |
| 12 | `UserService` | `IAuditService.LogAsync` | Construct `AuditLogRequest` |
| 13 | `RoleService` | `IAuditService.LogAsync` | Construct `AuditLogRequest` |
| 14 | `PermissionService` | `IAuditService.LogAsync` | Construct `AuditLogRequest` |

#### Modified Interface File (1 -- file split)

| # | File | Change |
|---|---|---|
| 1 | `IReceiptStockIntakeService.cs` | Remove `ReceiptLineContext` class definition; add `using` for new namespace |

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-AUTH-001 | No behavior change -- `IJwtTokenService` and `IAuditService` internal signatures updated |
| SDD-PURCH-001 | No behavior change -- `IPurchaseEventService` internal signature updated |
| SDD-FULF-001 | No behavior change -- `IFulfillmentEventService` internal signature updated |
| SDD-INV-005 | No behavior change -- `ReceiptLineContext` moved to own file, same namespace available |

## Migration Plan

1. **Pre-deployment:** Implement all 5 groups in a single commit. Run full test suite across Auth, Purchasing, Fulfillment, and Inventory projects. All existing tests must pass (with mock setup adjustments only).
2. **Deployment:** Deploy updated assemblies -- no schema changes, no configuration changes.
3. **Post-deployment:** Verify all health checks pass. Smoke test login (JWT generation), purchase order creation (event recording), sales order creation (event recording), and goods receipt (stock intake).
4. **Rollback:** Revert to previous commit -- no data migration needed.

## Implementation Order

The five groups are independent and can be implemented in any order. Recommended sequence for minimal risk:

1. **Group C** (file split) -- lowest risk, compile-time only
2. **Group A** (DIP fix) -- highest priority violation, isolated to Auth
3. **Group B3** (AuditService) -- Auth domain, 3 callers
4. **Group B1** (PurchaseEventService) -- Purchasing domain, 5 callers
5. **Group B2** (FulfillmentEventService) -- Fulfillment domain, 5 callers

## Open Questions

- [ ] Should `JwtTokenClaims` be a `sealed record` (immutable, value equality) or a `sealed class` with `required init` properties? Record is preferred for its conciseness and immutability guarantees.
- [ ] Should the `Models/` subfolder be used for parameter objects, or should a different folder name (e.g., `Contracts/`, `Internal/`) be used to distinguish them from ServiceModel request/response models?
