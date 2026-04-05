# CHG-REFAC-001 — Extract shared infrastructure and split contact services

> Status: Implemented
> Last updated: 2026-04-05
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**
Auth.API and Customers.API duplicate 6 identical infrastructure files (~240 lines) plus ~80 lines of Program.cs setup code. `CustomerContactService` is a 531-line fat service handling 3 unrelated entities (addresses, phones, emails). There is no shared base class for common service patterns (find-or-404, map, save), causing boilerplate duplication across all services.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

This is a pure refactoring — no behavior changes.

- The system MUST maintain identical API behavior (routes, responses, status codes) after refactoring.
- The system MUST pass all existing unit and integration tests without modification to test assertions.
- The system MUST NOT introduce new NuGet dependencies to existing API projects.
- The system SHOULD reduce code duplication by extracting shared infrastructure to a new `Warehouse.Infrastructure` project.
- The system SHOULD split `CustomerContactService` into `CustomerAddressService`, `CustomerPhoneService`, and `CustomerEmailService`.

## 3. Validation Rules

No new validation rules — refactoring only.

## 4. Error Rules

No new error rules — refactoring only.

## 5. Versioning Notes

**API version impact:** None

**Database migration required:** No

**Backwards compatibility:** Fully compatible

## 6. Test Plan

### Unit Tests

- [x] `[Unit] All existing CustomerServiceTests pass` — verify no regression
- [x] `[Unit] All existing CustomerAccountServiceTests pass` — verify no regression
- [x] `[Unit] All existing CustomerCategoryServiceTests pass` — verify no regression
- [ ] `[Unit] CustomerAddressServiceTests pass` — split from CustomerContactServiceTests
- [ ] `[Unit] CustomerPhoneServiceTests pass` — split from CustomerContactServiceTests
- [ ] `[Unit] CustomerEmailServiceTests pass` — split from CustomerContactServiceTests

### Integration Tests

- [x] `[Integration] All existing Auth integration tests pass` — verify Infrastructure extraction
- [x] `[Integration] All existing Customer integration tests pass` — verify service split

## 7. Detailed Design

### New Project: `Warehouse.Infrastructure`

Shared API infrastructure referenced by all microservice projects:
- `Authorization/` — PermissionRequirement, RequirePermissionAttribute, PermissionPolicyProvider, PermissionAuthorizationHandler
- `Middleware/` — GlobalExceptionHandlerMiddleware
- `Configuration/` — JwtSettings
- `Controllers/` — BaseApiController (replaces BaseAuthController and BaseCustomersController)
- `Extensions/` — ServiceCollectionExtensions (AddWarehouseAuthentication, AddWarehouseApiVersioning, AddWarehouseSwagger, AddWarehouseHealthChecks), WebApplicationExtensions (UseWarehousePipeline)
- `Services/` — BaseEntityService<TContext>, PrimaryFlagHelper

### New Interfaces in `Warehouse.Common`

- `IEntity` — marker for entities with `int Id`
- `ICustomerOwnedEntity` — extends IEntity with `int CustomerId`

### Service Layer Changes

**Deleted:**
- `CustomerContactService` (531 lines) → split into 3 focused services
- `ICustomerContactService` → split into 3 interfaces

**Created:**
- `CustomerAddressService : BaseCustomerEntityService` (~130 lines)
- `CustomerPhoneService : BaseCustomerEntityService` (~120 lines)
- `CustomerEmailService : BaseCustomerEntityService` (~140 lines)
- `BaseCustomerEntityService : BaseEntityService<CustomersDbContext>` — shared customer validation

**Refactored:**
- `CustomerCategoryService` → extends `BaseCustomerEntityService`
- `CustomerAccountService` → extends `BaseCustomerEntityService`, uses `PrimaryFlagHelper`
- `CustomerService` → extends `BaseCustomerEntityService`

**Deleted from both APIs (14 files):**
- `Authorization/` (4 files each) — replaced by Infrastructure
- `Middleware/GlobalExceptionHandlerMiddleware.cs` — replaced by Infrastructure
- `Configuration/JwtSettings.cs` — replaced by Infrastructure
- `Controllers/BaseAuthController.cs` / `BaseCustomersController.cs` — replaced by `BaseApiController`

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-AUTH-001 | No behavior change — infrastructure extraction only |
| SDD-CUST-001 | No behavior change — service split and base class extraction |

## Migration Plan

1. **Pre-deployment:** No preparation needed
2. **Deployment:** Deploy updated assemblies (no schema changes)
3. **Post-deployment:** Verify health endpoints and run smoke tests
4. **Rollback:** Revert to previous commit — no data migration needed
