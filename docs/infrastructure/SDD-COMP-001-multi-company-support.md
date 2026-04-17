# SDD-COMP-001 — Multi-Company Support

> Status: Draft
> Last updated: 2026-04-17
> Owner: TBD
> Category: Infrastructure

## 1. Context & Scope

This spec defines multi-company (multi-tenant) support for the Warehouse Management System. It introduces the Company entity as the ISA-95 **Enterprise** level, completing the equipment hierarchy (Enterprise → Site → Area → Storage Unit). All domain data — customers, suppliers, products, warehouses, orders, inventory — becomes scoped to a company. Users can belong to multiple companies but operate within one active company per session.

**ISA-95 Conformance:** Conforms to ISA-95 Part 2, Section 5 — Equipment Model (Enterprise level). Extends the Personnel Model (Part 2, Section 6) with enterprise-scoped Personnel Classes and Qualifications. Material Definitions (Part 2, Section 7) are enterprise-level master data per the standard.

**In scope:**
- Company entity (ISA-95 Enterprise) — CRUD, soft-delete, reactivation
- User-to-company assignment (M:N) with active company context
- Per-company roles and permissions (Personnel Classes / Qualifications scoped to Enterprise)
- Per-company product catalog (Material Definitions scoped to Enterprise)
- Company-scoped warehouses (Site → Enterprise ownership)
- Company-scoped customers, suppliers, and categories
- Company-scoped fulfillment and purchasing operations
- JWT token enrichment with `company_id` claim
- Tenant isolation via global query filters (`CompanyId` row-level filtering)
- Company switching (change active company without re-authentication)
- Database seeding for a default company

**Out of scope:**
- Schema-per-tenant or database-per-tenant isolation (row-level isolation only)
- Company billing, subscription, or licensing
- Cross-company data sharing or transfer
- Company-level feature flags (use existing system-wide feature flags)
- Company branding or theming (future enhancement)
- Migration of existing data to a default company (separate migration spec — `CHG-*`)

**Related specs:**
- `SDD-AUTH-001` — Authentication and authorization (extended by this spec with company context)
- `SDD-CUST-001` — Customers and accounts (affected: adds CompanyId scoping)
- `SDD-INV-001` — Products and catalog (affected: products become per-company)
- `SDD-INV-002` — Stock management (affected: inherits company scope via warehouse)
- `SDD-INV-003` — Warehouse structure (affected: warehouses scoped to company)
- `SDD-INV-004` — Stocktaking (affected: inherits company scope via warehouse)
- `SDD-PURCH-001` — Procurement operations (affected: suppliers and POs scoped to company)
- `SDD-FULF-001` — Fulfillment operations (affected: sales orders scoped to company)
- `SDD-EVTLOG-001` — Centralized event logging (affected: events tagged with CompanyId)

---

## 2. Behavior

### 2.1 Company Management

#### 2.1.1 Create Company

- The system MUST support creating a company with a code, name, tax ID, and optional notes.
- The system MUST enforce unique company codes across all companies (including soft-deleted).
- The system MUST enforce unique tax IDs across active companies. Two soft-deleted companies MAY share a tax ID.
- The system MUST set `IsActive = true` by default on creation.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.
- The system MUST return the created company with its generated ID.
- Only users with `companies:write` permission MUST be able to create companies. This is a system-level permission (not company-scoped).

**Edge cases:**
- Creating a company with a duplicate code MUST return a 409 Conflict error.
- Creating a company with a duplicate tax ID (among active companies) MUST return a 409 Conflict error.

#### 2.1.2 Update Company

- The system MUST support updating company name, tax ID, and notes.
- The system MUST NOT allow changing the company code after creation.
- The system MUST update `ModifiedAtUtc` and `ModifiedByUserId` on every update.
- Updating a soft-deleted company MUST return a 404 Not Found error.

**Edge cases:**
- Updating the tax ID to one that already belongs to another active company MUST return a 409 Conflict error.

#### 2.1.3 Get Company

- The system MUST support retrieving a single company by ID.
- Retrieving a soft-deleted company MUST return a 404 Not Found error.
- Users MUST only be able to retrieve companies they are assigned to, unless they have system-level `companies:read` permission.

#### 2.1.4 List Companies

- The system MUST support listing companies the current user is assigned to.
- System administrators with `companies:read` permission MUST be able to list all companies.
- The system MUST support pagination and sorting by name or code.
- The system MUST exclude soft-deleted companies by default.

#### 2.1.5 Deactivate Company (Soft Delete)

- The system MUST support soft-deleting a company by setting `IsDeleted = true` and `DeletedAtUtc = current UTC time`.
- Soft-deleted companies MUST NOT appear in standard queries.
- The system MUST set `IsActive = false` when a company is soft-deleted.
- The system MUST prevent deactivation if the company has active warehouses with non-zero stock levels. This MUST return a 409 Conflict error.
- Only users with `companies:delete` permission MUST be able to deactivate companies. This is a system-level permission.

**Edge cases:**
- Deactivating an already soft-deleted company MUST return a 404 Not Found error.
- Deactivating a company with active stock MUST return a 409 Conflict error.

#### 2.1.6 Reactivate Company

- The system MUST support reactivating a soft-deleted company by clearing `IsDeleted`, `DeletedAtUtc`, and setting `IsActive = true`.
- Reactivating a company whose code or tax ID conflicts with another active company MUST return a 409 Conflict error.

### 2.2 User-Company Assignment

#### 2.2.1 Assign User to Company

- The system MUST support assigning a user to one or more companies.
- The system MUST record the assignment timestamp (`AssignedAtUtc`).
- When a user is assigned to their first company, the system MUST set that company as the user's default company.
- The system MUST prevent duplicate assignments (same user + same company).

**Edge cases:**
- Assigning a user to a non-existent or soft-deleted company MUST return a 404 Not Found error.
- Assigning a user who is already assigned to the company MUST return a 409 Conflict error.

#### 2.2.2 Remove User from Company

- The system MUST support removing a user from a company.
- The system MUST prevent removing a user from their last assigned company. This MUST return a 409 Conflict error.
- If the removed company was the user's default, the system MUST promote the next most recently assigned company to default.
- The system MUST revoke any company-scoped roles the user held in the removed company.

**Edge cases:**
- Removing a user from a company they are not assigned to MUST return a 404 Not Found error.

#### 2.2.3 List User Companies

- The system MUST support listing all companies a user is assigned to.
- The response MUST indicate which company is the user's default and which is currently active in the session.

#### 2.2.4 Set Default Company

- The system MUST support changing a user's default company.
- The default company is used when the user logs in (initial active company context).
- The target company MUST be one the user is already assigned to.

### 2.3 Company Context & Switching

#### 2.3.1 Active Company in JWT

- The system MUST include a `company_id` claim in the JWT access token representing the user's active company.
- The system MUST include a `company_code` claim in the JWT access token for display purposes.
- All API requests MUST be executed within the context of the active company.
- The active company MUST be set to the user's default company on login.

#### 2.3.2 Switch Active Company

- The system MUST support switching the active company without full re-authentication.
- Switching MUST issue a new JWT access token + refresh token pair with the new `company_id` claim.
- The target company MUST be one the user is assigned to.
- The old refresh token MUST be revoked on switch (same as token rotation).
- The system MUST validate that the user still has valid assignment to the target company.

**Edge cases:**
- Switching to a company the user is not assigned to MUST return a 403 Forbidden error.
- Switching to a soft-deleted company MUST return a 404 Not Found error.

### 2.4 Per-Company Roles and Permissions

#### 2.4.1 Company-Scoped Roles

- Roles MUST be scoped to a company. Each company defines its own set of roles.
- The `auth.Roles` table MUST include a `CompanyId` column.
- System roles (`IsSystem = true`) MUST have `CompanyId = NULL` and apply across all companies.
- The system MUST seed an `Admin` role per company when a company is created.
- Role names MUST be unique within a company (not globally).

#### 2.4.2 Company-Scoped Permissions

- Permissions MUST remain system-wide (global). Permission definitions (resource:action pairs) are shared across all companies.
- Role-permission assignments are company-scoped through the role's CompanyId.
- This means: the same permission set exists globally, but which permissions a user has depends on their roles in the active company.

#### 2.4.3 System-Level Permissions

- The system MUST support system-level permissions that are not company-scoped.
- System-level permissions control operations that span companies: `companies:read`, `companies:write`, `companies:update`, `companies:delete`, `companies:assign-users`.
- System-level permissions MUST be assigned through system roles (`CompanyId = NULL`).

#### 2.4.4 Permission Resolution

- When resolving permissions for an API request, the system MUST:
  1. Load the user's roles for the active company (`CompanyId` from JWT `company_id` claim).
  2. Load system roles (`CompanyId = NULL`) assigned to the user.
  3. Union all permissions from both company-scoped and system roles.
  4. Check the required permission against the union set.

### 2.5 Tenant Isolation

#### 2.5.1 Row-Level Isolation

- All company-scoped entities MUST include a `CompanyId` column (INT, NOT NULL, FK → `auth.Companies`).
- All DbContexts MUST apply global query filters on `CompanyId` to ensure automatic tenant isolation.
- The `CompanyId` for query filters MUST be resolved from the current user's JWT `company_id` claim via `ICurrentCompanyProvider`.
- Direct SQL queries or raw EF queries MUST NOT bypass the global query filter unless explicitly justified and documented.

#### 2.5.2 Company-Scoped Entities

The following entities MUST be scoped to a company via `CompanyId`:

**Auth domain:**
- `Role` (except system roles where `CompanyId = NULL`)
- `UserCompany` (new join table — user-to-company assignment)

**Customers domain:**
- `Customer`
- `CustomerCategory`

**Inventory domain:**
- `WarehouseEntity` (Site → Enterprise)
- `Product`
- `ProductCategory`
- `UnitOfMeasure`
- `BillOfMaterials`

**Purchasing domain:**
- `Supplier`
- `SupplierCategory`
- `PurchaseOrder`

**Fulfillment domain:**
- `SalesOrder`
- `Carrier`
- `CustomerReturn`

#### 2.5.3 Entities That Inherit Company Scope

The following entities inherit company scope through their parent and do NOT need a direct `CompanyId` column:

- `Zone` — inherits via `WarehouseEntity.CompanyId`
- `StorageLocation` — inherits via `WarehouseEntity.CompanyId`
- `StockLevel` — inherits via `WarehouseEntity.CompanyId`
- `StockMovement` — inherits via `WarehouseEntity.CompanyId`
- `Batch` — inherits via `Product.CompanyId`
- `CustomerAccount` — inherits via `Customer.CompanyId`
- `CustomerAddress` — inherits via `Customer.CompanyId`
- `CustomerPhone` — inherits via `Customer.CompanyId`
- `CustomerEmail` — inherits via `Customer.CompanyId`
- `SalesOrderLine` — inherits via `SalesOrder.CompanyId`
- `PickList` — inherits via `SalesOrder.CompanyId`
- `Parcel` — inherits via `SalesOrder.CompanyId`
- `Shipment` — inherits via `SalesOrder.CompanyId`
- `PurchaseOrderLine` — inherits via `PurchaseOrder.CompanyId`
- `GoodsReceipt` — inherits via `PurchaseOrder.CompanyId`
- `BomLine` — inherits via `BillOfMaterials.CompanyId`
- `ProductAccessory` — inherits via `Product.CompanyId`
- `ProductSubstitute` — inherits via `Product.CompanyId`
- `CarrierServiceLevel` — inherits via `Carrier.CompanyId`
- `CustomerReturnLine` — inherits via `CustomerReturn.CompanyId`
- `InventoryAdjustment` — inherits via `WarehouseEntity.CompanyId`
- `WarehouseTransfer` — inherits via source/destination `WarehouseEntity.CompanyId`
- `StocktakeSession` — inherits via `WarehouseEntity.CompanyId`

#### 2.5.4 Entities That Remain Global

- `User` — users exist at system level; scoped to companies via `UserCompany` join
- `Permission` — permission definitions are system-wide
- `RefreshToken` — authentication is system-level
- `UserActionLog` — audit records reference CompanyId for context but are not filtered by it

#### 2.5.5 Cross-Company Validation

- The system MUST validate that referenced entities belong to the same company as the active company context. For example:
  - Creating a `SalesOrder` with a `CustomerId` from a different company MUST return a 400 Validation error.
  - Creating a `PurchaseOrder` with a `SupplierId` from a different company MUST return a 400 Validation error.
  - Assigning a `Product` to a `WarehouseEntity` from a different company MUST return a 400 Validation error.

### 2.6 Database Seeding

- On startup, the system MUST create a default company (code: `DEFAULT`, name: `Default Company`) if no companies exist.
- The system MUST assign the seed admin user to the default company.
- The system MUST create an `Admin` role for the default company and assign all permissions to it.
- The system MUST assign the admin user to the company's Admin role.
- Existing system-level Admin role (`IsSystem = true`, `CompanyId = NULL`) MUST be preserved for system-wide operations.

### 2.7 Event Publishing

- The system MUST publish a `CompanyCreatedEvent` when a company is created.
- The system MUST publish a `CompanyUpdatedEvent` when a company is updated.
- All existing domain events MUST include the `CompanyId` of the company context in which the event occurred.

### 2.8 Redis Cache Isolation

- All cache keys MUST be prefixed with the company ID: `{companyId}:{service}:{entity}:all`.
- Cache invalidation MUST only affect the active company's cache entries.
- Example: `42:inventory:product-categories:all` for company ID 42.

---

## 3. Validation Rules

### 3.1 Company

| # | Field | Rule | Error Code |
|---|---|---|---|
| V1 | Code | Required. 2–20 characters. Alphanumeric + hyphens. | `INVALID_COMPANY_CODE` |
| V2 | Name | Required. 1–200 characters. | `INVALID_COMPANY_NAME` |
| V3 | TaxId | Optional. 1–50 characters when provided. | `INVALID_COMPANY_TAX_ID` |
| V4 | Notes | Optional. Max 2000 characters. | `INVALID_COMPANY_NOTES` |
| V5 | Code | Must be unique across all companies (including soft-deleted). | `DUPLICATE_COMPANY_CODE` |
| V6 | TaxId | Must be unique across active companies when provided. | `DUPLICATE_COMPANY_TAX_ID` |

### 3.2 User-Company Assignment

| # | Field | Rule | Error Code |
|---|---|---|---|
| V7 | UserId | Required. Must reference an existing active user. | `INVALID_USER` |
| V8 | CompanyId | Required. Must reference an existing active company. | `INVALID_COMPANY` |
| V9 | UserId + CompanyId | Must not be a duplicate assignment. | `DUPLICATE_COMPANY_ASSIGNMENT` |

### 3.3 Company Switch

| # | Field | Rule | Error Code |
|---|---|---|---|
| V10 | CompanyId | Required. Must reference an existing active company. | `INVALID_COMPANY` |
| V11 | CompanyId | User must be assigned to the target company. | `COMPANY_ACCESS_DENIED` |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Company not found (or soft-deleted) | 404 | `COMPANY_NOT_FOUND` | Company not found. |
| E2 | Duplicate company code | 409 | `DUPLICATE_COMPANY_CODE` | A company with this code already exists. |
| E3 | Duplicate company tax ID | 409 | `DUPLICATE_COMPANY_TAX_ID` | An active company with this tax ID already exists. |
| E4 | Deactivate company with active stock | 409 | `COMPANY_HAS_ACTIVE_STOCK` | Cannot deactivate company — warehouses contain active stock. |
| E5 | Reactivate company with conflicting code | 409 | `DUPLICATE_COMPANY_CODE` | A company with this code already exists. |
| E6 | Reactivate company with conflicting tax ID | 409 | `DUPLICATE_COMPANY_TAX_ID` | An active company with this tax ID already exists. |
| E7 | User not assigned to target company | 403 | `COMPANY_ACCESS_DENIED` | You do not have access to this company. |
| E8 | Duplicate user-company assignment | 409 | `DUPLICATE_COMPANY_ASSIGNMENT` | User is already assigned to this company. |
| E9 | Remove user from last company | 409 | `LAST_COMPANY_ASSIGNMENT` | Cannot remove user from their last assigned company. |
| E10 | Cross-company entity reference | 400 | `CROSS_COMPANY_REFERENCE` | Referenced entity belongs to a different company. |
| E11 | Validation failure | 400 | `VALIDATION_ERROR` | One or more fields are invalid. (ProblemDetails with field errors) |
| E12 | Insufficient permissions | 403 | `FORBIDDEN` | You do not have permission to perform this action. |
| E13 | Switch to non-assigned company | 403 | `COMPANY_ACCESS_DENIED` | You do not have access to this company. |
| E14 | No active company context | 401 | `NO_COMPANY_CONTEXT` | No active company context. Please log in again. |
| E15 | User not found | 404 | `USER_NOT_FOUND` | User not found. |

All error responses MUST use ProblemDetails (RFC 7807) format:

```json
{
  "type": "https://warehouse.local/errors/{error-code}",
  "title": "Short error title",
  "status": 400,
  "detail": "Human-readable description.",
  "instance": "/api/v1/companies/{id}",
  "errors": {}
}
```

---

## 5. API Endpoints

### Company Management

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| POST | `/api/v1/companies` | Create company | Yes | `companies:write` (system) |
| GET | `/api/v1/companies` | List companies (user's assigned or all for system admin) | Yes | `companies:read` (system) or authenticated |
| GET | `/api/v1/companies/{id}` | Get company by ID | Yes | `companies:read` (system) or assigned |
| PUT | `/api/v1/companies/{id}` | Update company | Yes | `companies:update` (system) |
| DELETE | `/api/v1/companies/{id}` | Soft-delete company | Yes | `companies:delete` (system) |
| POST | `/api/v1/companies/{id}/reactivate` | Reactivate soft-deleted company | Yes | `companies:update` (system) |

### User-Company Assignment

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| GET | `/api/v1/companies/{companyId}/users` | List users assigned to company | Yes | `companies:read` (system) |
| POST | `/api/v1/companies/{companyId}/users` | Assign user to company | Yes | `companies:assign-users` (system) |
| DELETE | `/api/v1/companies/{companyId}/users/{userId}` | Remove user from company | Yes | `companies:assign-users` (system) |
| GET | `/api/v1/users/{userId}/companies` | List companies for a user | Yes | `users:read` or own user |
| PUT | `/api/v1/users/{userId}/default-company` | Set user's default company | Yes | Own user or `users:update` |

### Company Switching

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| POST | `/api/v1/auth/switch-company` | Switch active company (issues new tokens) | Yes | Authenticated + assigned to target |

---

## 6. Database Schema

**Schema name:** `auth` (Company entity lives in the auth schema as it is cross-cutting)

### New Tables

**auth.Companies**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Code | NVARCHAR(20) | NOT NULL, UNIQUE |
| Name | NVARCHAR(200) | NOT NULL |
| TaxId | NVARCHAR(50) | NULL |
| Notes | NVARCHAR(2000) | NULL |
| IsActive | BIT | NOT NULL, DEFAULT 1 |
| IsDeleted | BIT | NOT NULL, DEFAULT 0 |
| DeletedAtUtc | DATETIME2(7) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NULL (FK → auth.Users — nullable for seed data) |
| ModifiedAtUtc | DATETIME2(7) | NULL |
| ModifiedByUserId | INT | NULL (FK → auth.Users) |

**auth.UserCompanies**

| Column | Type | Constraints |
|---|---|---|
| UserId | INT | PK, FK → auth.Users(Id) |
| CompanyId | INT | PK, FK → auth.Companies(Id) |
| IsDefault | BIT | NOT NULL, DEFAULT 0 |
| AssignedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |

### Modified Tables

**auth.Roles** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NULL, FK → auth.Companies(Id). NULL = system role. |

- Unique constraint on `(CompanyId, Name)` replaces the existing unique constraint on `Name` alone.

**auth.UserActionLogs** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NULL, FK → auth.Companies(Id). Records which company context the action occurred in. |

### Schema Changes to Other Domains

**customers.Customers** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

- Unique constraint on `Code` changes to `(CompanyId, Code)`.
- Filtered unique index on `TaxId` changes to `(CompanyId, TaxId)` where `IsDeleted = 0 AND TaxId IS NOT NULL`.

**customers.CustomerCategories** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

- Unique constraint on `Name` changes to `(CompanyId, Name)`.

**inventory.Warehouses** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

- Unique constraint on `Code` changes to `(CompanyId, Code)`.

**inventory.Products** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

- Unique constraint on `Code` changes to `(CompanyId, Code)`.

**inventory.ProductCategories** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

- Unique constraint on `Name` changes to `(CompanyId, Name)`.

**inventory.UnitsOfMeasure** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

- Unique constraint on `Code` changes to `(CompanyId, Code)`.

**inventory.BillOfMaterials** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

**fulfillment.SalesOrders** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

**fulfillment.Carriers** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

**fulfillment.CustomerReturns** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

**purchasing.Suppliers** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

**purchasing.SupplierCategories** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

**purchasing.PurchaseOrders** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NOT NULL, FK → auth.Companies(Id) |

**eventlog.OperationsEvents** — add column:

| Column | Type | Constraints |
|---|---|---|
| CompanyId | INT | NULL (events may reference pre-company data) |

### Indexes

| Name | Table | Columns | Type |
|---|---|---|---|
| IX_Companies_Code | auth.Companies | Code | Unique |
| IX_Companies_TaxId | auth.Companies | TaxId | Non-unique (filtered: IsDeleted = 0 AND TaxId IS NOT NULL) |
| IX_Companies_IsDeleted | auth.Companies | IsDeleted | Non-unique (filtered: IsDeleted = 0) |
| IX_UserCompanies_UserId | auth.UserCompanies | UserId | Non-unique |
| IX_UserCompanies_CompanyId | auth.UserCompanies | CompanyId | Non-unique |
| IX_Roles_CompanyId_Name | auth.Roles | CompanyId, Name | Unique |
| IX_Customers_CompanyId_Code | customers.Customers | CompanyId, Code | Unique |
| IX_Customers_CompanyId_TaxId | customers.Customers | CompanyId, TaxId | Non-unique (filtered: IsDeleted = 0 AND TaxId IS NOT NULL) |
| IX_Warehouses_CompanyId_Code | inventory.Warehouses | CompanyId, Code | Unique |
| IX_Products_CompanyId_Code | inventory.Products | CompanyId, Code | Unique |
| IX_ProductCategories_CompanyId_Name | inventory.ProductCategories | CompanyId, Name | Unique |
| IX_UoM_CompanyId_Code | inventory.UnitsOfMeasure | CompanyId, Code | Unique |
| IX_Suppliers_CompanyId_Code | purchasing.Suppliers | CompanyId, Code | Unique |
| IX_SupplierCategories_CompanyId_Name | purchasing.SupplierCategories | CompanyId, Name | Unique |
| IX_CustomerCategories_CompanyId_Name | customers.CustomerCategories | CompanyId, Name | Unique |

---

## 7. Versioning Notes

- **v1 — Initial specification (2026-04-17)**
  - Company entity (ISA-95 Enterprise level)
  - User-company assignment (M:N)
  - Per-company roles, company-scoped permissions resolution
  - Row-level tenant isolation via global query filters
  - JWT enrichment with `company_id` claim
  - Company switching endpoint
  - Schema changes across all domains (CompanyId column additions)
  - Database seeding for default company
  - Redis cache key isolation per company
  - Domain event enrichment with CompanyId

---

## 8. Test Plan

### Unit Tests — CompanyServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedCompany` [Unit]
- `CreateAsync_DuplicateCode_ReturnsConflictError` [Unit]
- `CreateAsync_DuplicateTaxId_ReturnsConflictError` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedCompany` [Unit]
- `UpdateAsync_SoftDeletedCompany_ReturnsNotFound` [Unit]
- `UpdateAsync_DuplicateTaxId_ReturnsConflictError` [Unit]
- `UpdateAsync_ChangeCode_ReturnsBadRequest` [Unit]
- `GetByIdAsync_ExistingCompany_ReturnsCompany` [Unit]
- `GetByIdAsync_SoftDeletedCompany_ReturnsNotFound` [Unit]
- `GetByIdAsync_UserNotAssigned_ReturnsForbidden` [Unit]
- `ListAsync_ReturnsOnlyAssignedCompanies` [Unit]
- `ListAsync_SystemAdmin_ReturnsAllCompanies` [Unit]
- `DeactivateAsync_ActiveCompany_SetsIsDeletedAndDeletedAt` [Unit]
- `DeactivateAsync_CompanyWithActiveStock_ReturnsConflict` [Unit]
- `DeactivateAsync_AlreadyDeleted_ReturnsNotFound` [Unit]
- `ReactivateAsync_SoftDeletedCompany_ClearsDeletedFlags` [Unit]
- `ReactivateAsync_ConflictingCode_ReturnsConflictError` [Unit]
- `ReactivateAsync_ConflictingTaxId_ReturnsConflictError` [Unit]

### Unit Tests — UserCompanyServiceTests

- `AssignAsync_ValidRequest_CreatesAssignment` [Unit]
- `AssignAsync_FirstCompany_SetsIsDefaultTrue` [Unit]
- `AssignAsync_DuplicateAssignment_ReturnsConflictError` [Unit]
- `AssignAsync_NonExistentCompany_ReturnsNotFound` [Unit]
- `AssignAsync_NonExistentUser_ReturnsNotFound` [Unit]
- `RemoveAsync_ValidRequest_RemovesAssignment` [Unit]
- `RemoveAsync_LastCompany_ReturnsConflictError` [Unit]
- `RemoveAsync_DefaultCompany_PromotesNext` [Unit]
- `RemoveAsync_RevokesCompanyRoles` [Unit]
- `ListCompaniesForUserAsync_ReturnsAssignedCompanies` [Unit]
- `SetDefaultCompanyAsync_ValidCompany_SetsDefault` [Unit]
- `SetDefaultCompanyAsync_NotAssigned_ReturnsForbidden` [Unit]

### Unit Tests — CompanySwitchServiceTests

- `SwitchAsync_ValidCompany_ReturnsNewTokens` [Unit]
- `SwitchAsync_NotAssigned_ReturnsForbidden` [Unit]
- `SwitchAsync_SoftDeletedCompany_ReturnsNotFound` [Unit]
- `SwitchAsync_RevokesOldRefreshToken` [Unit]
- `SwitchAsync_NewTokenContainsCompanyIdClaim` [Unit]

### Unit Tests — PermissionResolutionTests

- `ResolvePermissions_CompanyRolesOnly_ReturnsCompanyPermissions` [Unit]
- `ResolvePermissions_SystemRolesOnly_ReturnsSystemPermissions` [Unit]
- `ResolvePermissions_BothRoleTypes_ReturnsUnion` [Unit]
- `ResolvePermissions_NoRolesInCompany_ReturnsOnlySystemPermissions` [Unit]
- `ResolvePermissions_UserNotInCompany_ReturnsEmpty` [Unit]

### Unit Tests — TenantIsolationTests

- `GlobalQueryFilter_FiltersEntitiesByCompanyId` [Unit]
- `GlobalQueryFilter_DoesNotAffectGlobalEntities` [Unit]
- `CreateEntity_SetsCompanyIdFromCurrentContext` [Unit]
- `CrossCompanyReference_ReturnsValidationError` [Unit]

### Unit Tests — CreateCompanyRequestValidatorTests

- `Validate_ValidRequest_NoErrors` [Unit]
- `Validate_EmptyCode_ReturnsError` [Unit]
- `Validate_CodeTooLong_ReturnsError` [Unit]
- `Validate_CodeWithSpecialChars_ReturnsError` [Unit]
- `Validate_EmptyName_ReturnsError` [Unit]
- `Validate_NameTooLong_ReturnsError` [Unit]
- `Validate_TaxIdTooLong_ReturnsError` [Unit]
- `Validate_NotesTooLong_ReturnsError` [Unit]

### Integration Tests — CompaniesControllerTests

- `CreateCompany_ValidPayload_Returns201` [Integration]
- `CreateCompany_DuplicateCode_Returns409` [Integration]
- `CreateCompany_DuplicateTaxId_Returns409` [Integration]
- `GetCompany_ExistingCompany_Returns200` [Integration]
- `GetCompany_SoftDeleted_Returns404` [Integration]
- `ListCompanies_ReturnsAssignedCompanies` [Integration]
- `UpdateCompany_ValidPayload_Returns200` [Integration]
- `DeactivateCompany_Returns204` [Integration]
- `DeactivateCompany_WithActiveStock_Returns409` [Integration]
- `ReactivateCompany_Returns200` [Integration]

### Integration Tests — CompanySwitchTests

- `SwitchCompany_ValidTarget_Returns200WithNewTokens` [Integration]
- `SwitchCompany_NotAssigned_Returns403` [Integration]
- `SwitchCompany_SoftDeleted_Returns404` [Integration]
- `SwitchCompany_NewTokenHasCompanyIdClaim` [Integration]
- `SwitchCompany_OldRefreshTokenRevoked` [Integration]

### Integration Tests — TenantIsolationTests

- `GetCustomers_ReturnsOnlyActiveCompanyCustomers` [Integration]
- `GetProducts_ReturnsOnlyActiveCompanyProducts` [Integration]
- `CreateSalesOrder_WithOtherCompanyCustomer_Returns400` [Integration]
- `CreatePurchaseOrder_WithOtherCompanySupplier_Returns400` [Integration]
- `SwitchCompany_DataIsolationMaintained` [Integration]

### Integration Tests — UserCompanyTests

- `AssignUser_ValidPayload_Returns201` [Integration]
- `AssignUser_Duplicate_Returns409` [Integration]
- `RemoveUser_ValidRequest_Returns204` [Integration]
- `RemoveUser_LastCompany_Returns409` [Integration]
- `ListUserCompanies_Returns200` [Integration]
- `SetDefaultCompany_Returns200` [Integration]

---

## 9. Acceptance Criteria

- [ ] Company CRUD with soft delete and reactivation works
- [ ] Users can be assigned to multiple companies
- [ ] Login sets active company to user's default company
- [ ] JWT tokens contain `company_id` and `company_code` claims
- [ ] Company switch issues new tokens without re-authentication
- [ ] Roles are scoped per company; system roles remain global
- [ ] Permission resolution unions company-scoped and system role permissions
- [ ] All domain entities return only data belonging to the active company
- [ ] Cross-company entity references are rejected with validation error
- [ ] Unique constraints respect company scope (e.g., customer code unique per company)
- [ ] Cache keys include company ID for proper isolation
- [ ] Domain events include CompanyId
- [ ] Default company and admin role seeded on startup
- [ ] All error responses use ProblemDetails format
- [ ] All existing domain API tests pass after migration
- [ ] ISA-95 equipment hierarchy complete: Enterprise (Company) → Site (Warehouse) → Area (Zone) → Storage Unit (StorageLocation)

---

## 10. ISA-95 Compliance Summary

| ISA-95 Requirement | How This Spec Satisfies It |
|---|---|
| Equipment Hierarchy (Part 2, §5) — Enterprise level | `Company` entity completes Enterprise → Site → Area → Storage Unit chain |
| Personnel Model (Part 2, §6) — enterprise-scoped qualifications | Roles scoped per company; permissions resolved per active company |
| Material Model (Part 2, §7) — enterprise-level definitions | Products, categories, UoM scoped per company (Material Definitions are enterprise assets) |
| Rule 1 — Entity Classification | Company classified as ISA-95 Enterprise (Equipment Model) |
| Rule 4 — Equipment Hierarchy | Enterprise (Company) → Site (Warehouse) → Area (Zone) → Storage Unit (StorageLocation) |
| Rule 7 — Terminology Preference | "Company" used instead of "Enterprise" for business familiarity; ISA-95 mapping documented |
| Rule 8 — Domain Boundaries | Company lives in Auth domain (Personnel & Authorization) as cross-cutting entity |
| Rule 10 — Immutable Events | UserActionLog, FulfillmentEvent, StockMovement enriched with CompanyId |

---

## Key Files (Planned)

- `src/Databases/Warehouse.Auth.DBModel/Models/Company.cs`
- `src/Databases/Warehouse.Auth.DBModel/Models/UserCompany.cs`
- `src/Databases/Warehouse.Auth.DBModel/AuthDbContext.cs` (modified — add Company, UserCompany DbSets)
- `src/Interfaces/Auth/Warehouse.Auth.API/Controllers/CompaniesController.cs`
- `src/Interfaces/Auth/Warehouse.Auth.API/Services/CompanyService.cs`
- `src/Interfaces/Auth/Warehouse.Auth.API/Services/UserCompanyService.cs`
- `src/Interfaces/Auth/Warehouse.Auth.API/Services/CompanySwitchService.cs`
- `src/Interfaces/Auth/Warehouse.Auth.API/Services/ICurrentCompanyProvider.cs`
- `src/Interfaces/Auth/Warehouse.Auth.API/Validators/Companies/CreateCompanyRequestValidator.cs`
- `src/Interfaces/Auth/Warehouse.Auth.API/Validators/Companies/UpdateCompanyRequestValidator.cs`
- `src/Warehouse.ServiceModel/DTOs/CompanyDto.cs`
- `src/Warehouse.ServiceModel/Requests/CreateCompanyRequest.cs`
- `src/Warehouse.ServiceModel/Requests/UpdateCompanyRequest.cs`
- `src/Warehouse.ServiceModel/Requests/SwitchCompanyRequest.cs`
- `src/Warehouse.ServiceModel/Events/CompanyCreatedEvent.cs`
- `src/Warehouse.ServiceModel/Events/CompanyUpdatedEvent.cs`
- `src/Warehouse.Mapping/Profiles/CompanyMappingProfile.cs`
- `src/Interfaces/Auth/Warehouse.Auth.API.Tests/Unit/Services/CompanyServiceTests.cs`
- `src/Interfaces/Auth/Warehouse.Auth.API.Tests/Unit/Services/UserCompanyServiceTests.cs`
- `src/Interfaces/Auth/Warehouse.Auth.API.Tests/Unit/Services/CompanySwitchServiceTests.cs`
- All domain DbContexts (modified — global query filters for CompanyId)
