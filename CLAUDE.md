# Warehouse — Project Instructions

> Last updated: 2026-04-06

---

## 0. Active Personas & Workflow

### Execution Pipeline

```
spec-writer → implementator → tester → spec-validator
```

| Phase | Persona Category | Base Persona | Purpose |
|---|---|---|---|
| **1. Spec Writer** | `~/.claude/personas/spec-writer/` | `doc-governance.md` | Write or update the SDD specification before any code is written |
| **2. Implementator** | `~/.claude/personas/implementator/` | `csharp-persona.md` | Write production-ready code that fulfills the specification |
| **3. Tester** | `~/.claude/personas/tester/` | `csharp-persona.md` | Write tests that verify the implementation matches the specification |
| **4. Spec Validator** | `~/.claude/personas/validator/` | `doc-governance.md` | Validate that the spec, code, and tests are all in sync |

### Implementator Personas

| Concern | Persona File |
|---|---|
| Base code standards | `~/.claude/personas/csharp-persona.md` |
| .NET 8 REST Microservices | `~/.claude/personas/implementator/persona-dotnet8-microservices.md` |
| Database (EF Core, migrations) | `~/.claude/personas/implementator/persona-database.md` |
| MongoDB (if added later) | `~/.claude/personas/implementator/persona-mongodb.md` |

### Tester Personas

| Concern | Persona File |
|---|---|
| Base (always-on) | `~/.claude/personas/tester/tester-base.md` |
| EF Core / ORM tests | `~/.claude/personas/tester/tester-ef-orm.md` |
| API integration tests | `~/.claude/personas/tester/tester-integration.md` |

### Precedence

```
CLAUDE.md > persona-database > persona-dotnet8-microservices > csharp-persona > doc-governance
```

---

## 0.1 Project Policies (override persona defaults)

- **Target framework:** .NET 8.0
- **Error responses:** ProblemDetails (RFC 7807)
- **Polly:** Required on all outbound HTTP clients (to be added when services are implemented)
- **Mapping:** AutoMapper only
- **Logging:** NLog with structured message templates
- **ORM:** EF Core with separate DbContext per domain (AuthDbContext, CustomersDbContext, InventoryDbContext)
- **Validation:** FluentValidation
- **PK strategy:** INT IDENTITY (or GUID with `NEWSEQUENTIALID()` if GUID is chosen)
- **API versioning:** URL-based (`/api/v1/`)
- **Authentication:** JWT-based with RBAC (implemented in Warehouse.Auth.API)
- **Frontend:** Vue.js 3 SPA (Single Page Application) with Composition API, TypeScript, Vue Router, Pinia state management

### Configuration File Policy

-   **Only `.template` files are tracked in git.** Real `appsettings.json`, `appsettings.*.json`, `app.config`, and `web.config` files are gitignored.
-   Template files use `<PLACEHOLDER>` values for all sensitive data (connection strings, API keys, secrets).
-   When adding a new config key, update the corresponding `.template` file with a placeholder.
-   Never commit real connection strings, passwords, API keys, or tokens.
-   Developers copy `appsettings.json.template` → `appsettings.json` locally and fill in real values.

### Current Package Versions

| Package | Version | Projects |
|---|---|---|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.12 | Auth.DBModel, Customers.DBModel, Inventory.DBModel |
| Microsoft.EntityFrameworkCore.Tools | 8.0.12 | Auth.DBModel, Customers.DBModel, Inventory.DBModel |
| Microsoft.EntityFrameworkCore.Design | 8.0.12 | Auth.API, Customers.API, Inventory.API |
| AutoMapper.Extensions.Microsoft.DependencyInjection | 12.0.1 | Auth.API, Customers.API, Inventory.API, Mapping, Infrastructure |
| FluentValidation.AspNetCore | 11.3.0 | Auth.API, Customers.API, Inventory.API |
| Asp.Versioning.Mvc | 8.1.0 | Auth.API, Customers.API, Inventory.API, Infrastructure |
| Asp.Versioning.Mvc.ApiExplorer | 8.1.0 | Auth.API, Customers.API, Inventory.API, Infrastructure |
| Swashbuckle.AspNetCore | 6.6.2 | Auth.API, Customers.API, Inventory.API, Infrastructure |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.12 | Auth.API, Customers.API, Inventory.API, Infrastructure |
| NLog.Web.AspNetCore | 5.4.0 | Auth.API, Customers.API, Inventory.API |
| AspNetCore.HealthChecks.SqlServer | 8.0.2 | Auth.API, Customers.API, Inventory.API, Infrastructure |
| BCrypt.Net-Next | 4.0.3 | Auth.API |
| Polly | 8.5.2 | Customers.API |
| Microsoft.Extensions.Http.Polly | 8.0.12 | Customers.API |
| NUnit | 3.13.3 | Auth.API.Tests, Customers.API.Tests, Inventory.API.Tests |
| FluentAssertions | 6.12.2 | Auth.API.Tests, Customers.API.Tests, Inventory.API.Tests |
| Moq | 4.20.72 | Customers.API.Tests, Inventory.API.Tests |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.12 | Auth.API.Tests, Customers.API.Tests, Inventory.API.Tests |
| Microsoft.NET.Test.Sdk | 17.6.0 | Auth.API.Tests, Customers.API.Tests, Inventory.API.Tests |
| NUnit3TestAdapter | 4.2.1 | Auth.API.Tests, Customers.API.Tests, Inventory.API.Tests |
| NUnit.Analyzers | 3.6.1 | Auth.API.Tests, Customers.API.Tests, Inventory.API.Tests |
| coverlet.collector | 6.0.0 | Auth.API.Tests, Customers.API.Tests, Inventory.API.Tests |

---

## 0.2 Specification Alignment Gate

Before implementing any task, check alignment with existing specifications:

- `docs/core/SDD-*.md` — Core business pipeline
- `docs/domain/SDD-*.md` — Domain capabilities
- `docs/integration/SDD-*.md` — External integrations
- `docs/infrastructure/SDD-*.md` — Cross-cutting concerns

If a request is outside scope, contradicts a spec, or is ambiguous — **STOP and clarify**.

---

## 1. Project Purpose

Warehouse Management System (WMS) — a monorepo containing .NET 8 backend microservices and a Vue.js Single Page Application (SPA) frontend. The system manages warehouse operations including inventory, storage locations, inbound/outbound shipments, and related workflows. **The system is designed to conform to the ISA-95 (IEC 62264) standard** for enterprise–operations integration.

**Architecture:** Monorepo with multiple backend microservices sharing common libraries, and a single Vue.js SPA frontend that consumes the backend REST APIs.

---

## 1.1 ISA-95 Standard Compliance (IEC 62264)

This project follows the **ISA-95 / IEC 62264** international standard for integrating enterprise and control systems. ISA-95 compliance is an **architectural policy** — all domain modeling, entity naming, operations design, and future feature planning MUST align with the ISA-95 object and activity models.

### 1.1.1 ISA-95 Hierarchy Placement

| ISA-95 Level | Description | WMS Scope |
|---|---|---|
| **Level 4** | Business Planning & Logistics | ERP integration (future), customer orders, purchasing |
| **Level 3** | Manufacturing Operations Management | **This WMS** — Inventory Operations, Warehouse Execution |
| Level 2 | Monitoring & Supervisory Control | Barcode scanning, IoT sensors (future) |
| Level 1 | Sensing & Manipulation | Equipment controllers (out of scope) |
| Level 0 | Physical Process | Physical equipment (out of scope) |

This WMS primarily operates at **Level 3** (Operations Management) with Level 4 interfaces planned for ERP integration.

### 1.1.2 ISA-95 Operations Domains

| ISA-95 Domain | Status | WMS Microservice | Scope |
|---|---|---|---|
| **Inventory Operations** | Active | `Warehouse.Inventory.API` | Material tracking, movements, adjustments, stocktaking, transfers |
| **Personnel & Authorization** | Active | `Warehouse.Auth.API` | Personnel, roles, qualifications, audit |
| **Business Partner Management** | Active | `Warehouse.Customers.API` | Customers, accounts, contacts |
| **Production Operations** | Planned | Future: `Warehouse.Production.API` | Production orders, work orders, scheduling |
| **Quality Operations** | Planned | Future: `Warehouse.Quality.API` | Inspections, quality holds, certifications |
| **Procurement Operations** | Planned | Future: `Warehouse.Purchasing.API` | Purchase orders, inbound shipments, supplier management |
| **Fulfillment Operations** | Planned | Future: `Warehouse.Fulfillment.API` | Sales orders, picking, packing, outbound shipments |
| **Maintenance Operations** | Out of scope | — | Equipment maintenance |

### 1.1.3 ISA-95 Object Model — Terminology Mapping

All entity naming and domain concepts follow the ISA-95 object model. This mapping is the **canonical reference** for how WMS entities relate to the standard.

#### Material Model (ISA-95 Part 2, Section 7)

| ISA-95 Term | WMS Entity | DB Table | Notes |
|---|---|---|---|
| Material Definition | `Product` | `inventory.Products` | Master data defining what a material is |
| Material Class | `ProductCategory` | `inventory.ProductCategories` | Hierarchical classification of materials |
| Material Lot | `Batch` | `inventory.Batches` | Tracked quantity with shared properties (expiry, manufacturing date) |
| Material Sublot | — | — | Not yet implemented — subdivision of lots for finer traceability |
| Unit of Measure | `UnitOfMeasure` | `inventory.UnitsOfMeasure` | Standard measurement units (kg, pcs, L, etc.) |
| Bill of Material | `BillOfMaterials` | `inventory.BillOfMaterials` | Recipe/formula definition (parent product) |
| BOM Component | `BomLine` | `inventory.BomLines` | Component line within a BOM (child product + quantity) |
| Alternate Material | `ProductSubstitute` | `inventory.ProductSubstitutes` | Interchangeable material definition |
| Related Material | `ProductAccessory` | `inventory.ProductAccessories` | Associated/complementary material |

#### Equipment Model (ISA-95 Part 2, Section 5)

| ISA-95 Term | WMS Entity | DB Table | Notes |
|---|---|---|---|
| Enterprise | — | — | Organization level (implicit — single-tenant system) |
| Site | `WarehouseEntity` | `inventory.Warehouses` | Physical warehouse facility |
| Area | `Zone` | `inventory.Zones` | Functional subdivision within a site |
| Storage Zone | `Zone` | `inventory.Zones` | ISA-95 storage zone maps to WMS Zone |
| Storage Unit | `StorageLocation` | `inventory.StorageLocations` | Addressable position (Row, Shelf, Bin, Bulk) |
| Work Center | — | — | Not applicable — no production execution yet |
| Work Unit | — | — | Not applicable — no production execution yet |

#### Personnel Model (ISA-95 Part 2, Section 6)

| ISA-95 Term | WMS Entity | DB Table | Notes |
|---|---|---|---|
| Person | `User` | `auth.Users` | Individual operator/user identity |
| Personnel Class | `Role` | `auth.Roles` | Role-based classification of personnel |
| Qualification | `Permission` | `auth.Permissions` | Specific capability/authorization (resource:action) |
| Credential | `RefreshToken` | `auth.RefreshTokens` | Authentication credential |

#### Operations Event Model

| ISA-95 Term | WMS Entity | DB Table | Notes |
|---|---|---|---|
| Operations Event | `UserActionLog` | `auth.UserActionLogs` | Immutable audit record of operations |
| Stock Event | `StockMovement` | `inventory.StockMovements` | Immutable record of material quantity change |
| Inventory Count | `StocktakeSession` | `inventory.StocktakeSessions` | Physical inventory count event |
| Count Record | `StocktakeCount` | `inventory.StocktakeCounts` | Individual product count entry |

### 1.1.4 ISA-95 Inventory Operations Activity Model (Part 3)

This maps ISA-95 activity model functions to WMS implementation.

| ISA-95 Activity | WMS Concept | Controller / Service | Status |
|---|---|---|---|
| **Inventory Definition Management** | Product/Category/UOM CRUD | `ProductsController`, `ProductCategoriesController`, `UnitsOfMeasureController` | Implemented |
| **Material Inventory Tracking** | StockLevel per product/location/batch | `StockLevelsController` | Implemented |
| **Inventory Counting** | StocktakeSession + StocktakeCount | `StocktakeSessionsController`, `StocktakeCountsController` | Implemented |
| **Inventory Adjustment** | Approval workflow (Pending → Approved → Applied) | `InventoryAdjustmentsController` | Implemented |
| **Material Movement** | Immutable StockMovement records with reason codes | `StockMovementsController` | Implemented |
| **Material Transfer** | Inter-site transfer (Draft → Completed) | `WarehouseTransfersController` | Implemented |
| **Material Receipt** | Inbound shipment processing | — | Planned |
| **Material Shipment** | Outbound shipment processing | — | Planned |
| **Material Storage** | Location assignment and management | `StorageLocationsController` | Implemented |
| **Production Resource Management** | BOM/recipe definition | `BillOfMaterialsController` | Catalog only |

### 1.1.5 ISA-95 Information Exchange (Part 4 — Future)

When implementing ERP integration, the information exchange MUST follow ISA-95 Part 4 patterns:

| Exchange Type | Direction | Payload | Notes |
|---|---|---|---|
| Material Definition Download | ERP → WMS | Product master data | Sync material definitions from enterprise |
| Inventory Report | WMS → ERP | Stock levels by site/material | Periodic or on-demand inventory visibility |
| Material Movement Report | WMS → ERP | Movement transactions | Immutable event stream of stock changes |
| Production Order | ERP → WMS | Work order + BOM | Future: production execution |
| Shipment Request | ERP → WMS | Outbound order lines | Future: pick/pack/ship instructions |
| Receipt Confirmation | WMS → ERP | Inbound receipt details | Future: goods receipt posting |

The data format SHOULD follow B2MML (Business to Manufacturing Markup Language) or an equivalent JSON schema derived from ISA-95 object models.

### 1.1.6 ISA-95 Compliance Policy Rules

These rules are **MANDATORY** for all new development:

1.  **Entity Classification** — All new entities MUST be classified under an ISA-95 object model category (Material, Equipment, Personnel, or Operations Event). Add the mapping to the terminology table in this section.
2.  **Activity Model Alignment** — All new operations/endpoints MUST be mapped to an ISA-95 activity model function. If no matching function exists, document the extension rationale.
3.  **Spec References** — SDD specifications MUST reference the applicable ISA-95 part and section in their Context block (e.g., "Conforms to ISA-95 Part 2 Section 7 — Material Model").
4.  **Equipment Hierarchy** — The ISA-95 hierarchy (Enterprise → Site → Area → Work Center) MUST be respected. Do not introduce equipment entities that violate this hierarchy.
5.  **Material Traceability** — Material tracking MUST maintain the ISA-95 chain: Material Definition → Material Lot → Material Sublot. Do not bypass lot tracking for traceable materials.
6.  **Movement Types** — Stock movements MUST use ISA-95 aligned reason codes: Receipt, Shipment, Transfer, Adjustment, Count Adjustment, Production Consumption, Production Output.
7.  **Terminology Preference** — When naming new entities, properties, or API resources, PREFER ISA-95 terminology where it does not conflict with already-established WMS naming conventions. Existing entity names are grandfathered and do NOT need renaming.
8.  **Domain Boundaries** — New microservices MUST align with an ISA-95 operations domain. Do not create services that span multiple operations domains.
9.  **Information Exchange** — Future ERP/MES integration MUST follow ISA-95 Part 4 information exchange patterns. Do not design proprietary integration schemas without documenting the ISA-95 deviation.
10. **Immutable Events** — All operations that change material state (movements, adjustments, transfers, counts) MUST produce immutable event records, per ISA-95 operations event model.

### 1.1.6.1 Movement Reason Code Extensions

The `StockMovementReason` enum extends ISA-95 base movement types with finer-grained codes required for business reporting and audit. Each extended code maps back to an ISA-95 base type.

| WMS Reason Code | ISA-95 Base Type | Rationale |
|---|---|---|
| `PurchaseReceipt` | Receipt | Specialized for purchase order inbound receipts |
| `SalesDispatch` | Shipment | Specialized for sales order outbound fulfillment |
| `Adjustment` | Adjustment | Direct 1:1 mapping to ISA-95 base type |
| `Transfer` | Transfer | Direct 1:1 mapping to ISA-95 base type |
| `CustomerReturn` | Receipt | Goods returned by customer (inbound receipt) |
| `SupplierReturn` | Shipment | Goods returned to supplier (outbound shipment) |
| `ProductionConsumption` | Production Consumption | Direct 1:1 mapping to ISA-95 base type |
| `ProductionReceipt` | Production Output | Finished goods received from production |
| `WriteOff` | Adjustment | Irreversible loss — damage, expiry, theft |
| `StocktakeCorrection` | Count Adjustment | Variance correction from physical inventory count |
| `Other` | *(escape hatch)* | Edge cases not covered by specific codes; SHOULD be avoided in favor of specific codes |

**Rationale:** Business operations require finer granularity than the seven ISA-95 base movement types for accurate reporting, audit trails, and operational analytics. The extended codes preserve full traceability to ISA-95 base types while enabling domain-specific filtering and dashboards.

### 1.1.7 ISA-95 Compliance Checklist (New Features)

Add to the Quick Checklist (Section 12) when building features:

-   [ ] New entity classified under ISA-95 object model (Material / Equipment / Personnel / Event)
-   [ ] New operations mapped to ISA-95 activity model function
-   [ ] SDD spec references applicable ISA-95 part/section
-   [ ] Equipment hierarchy respected (Enterprise → Site → Area → Storage Unit)
-   [ ] Material traceability chain maintained (Definition → Lot → Sublot)
-   [ ] Stock movement reason codes align with ISA-95 movement types
-   [ ] New microservice maps to a single ISA-95 operations domain
-   [ ] Immutable event records produced for all state-changing operations

---

## 2. Solution Structure

```
Warehouse/
├── CLAUDE.md                                      ← You are here
├── docs/                                          ← SDD specifications
│   ├── README.md
│   ├── cross-reference-map.md
│   ├── core/                                      ← Core business pipeline specs
│   ├── domain/                                    ← Domain capability specs
│   ├── integration/                               ← External integration specs
│   ├── infrastructure/                            ← Cross-cutting concern specs
│   └── changes/
│       └── _TEMPLATE.md
├── frontend/                                      ← Vue.js 3 SPA (Single Page Application)
│   └── src/
│       ├── app/                                   ← App shell (main.ts, App.vue, router/)
│       ├── features/                              ← Feature domains
│       │   └── auth/                              ← Auth feature (api, types, views, composables, organisms)
│       └── shared/                                ← Cross-feature (stores, atoms, molecules, templates, i18n)
├── src/
│   ├── Warehouse.slnx                             ← .NET 8 solution
│   ├── Databases/
│   │   ├── Warehouse.Auth.DBModel/                ← Auth EF Core entities, AuthDbContext, migrations
│   │   │   └── Models/                            ← Auth entity models (User, Role, Permission, etc.)
│   │   ├── Warehouse.Customers.DBModel/           ← Customers EF Core entities, CustomersDbContext, migrations
│   │   │   └── Models/                            ← Customer entity models (Customer, Account, etc.)
│   │   └── Warehouse.Inventory.DBModel/           ← Inventory EF Core entities, InventoryDbContext, migrations
│   │       └── Models/                            ← Inventory entity models (Product, StockLevel, Warehouse, etc.)
│   ├── Interfaces/
│   │   ├── Auth/                                  ← Auth domain
│   │   │   ├── Warehouse.Auth.API/                ← Controllers, middleware, DI root
│   │   │   └── Warehouse.Auth.API.Tests/          ← NUnit tests
│   │   ├── Customers/                             ← Customers domain
│   │   │   ├── Warehouse.Customers.API/           ← Controllers, middleware, DI root
│   │   │   └── Warehouse.Customers.API.Tests/     ← NUnit tests
│   │   └── Inventory/                             ← Inventory domain
│   │       ├── Warehouse.Inventory.API/           ← Controllers, services, DI root
│   │       └── Warehouse.Inventory.API.Tests/     ← NUnit tests
│   ├── Warehouse.Common/                          ← Shared enums, helpers, extensions
│   │   ├── Enums/
│   │   ├── Extensions/
│   │   └── Helpers/
│   ├── Warehouse.GenericFiltering/                ← Reusable IQueryable dynamic filter library
│   ├── Warehouse.Mapping/                         ← AutoMapper profiles
│   │   └── Profiles/
│   └── Warehouse.ServiceModel/                    ← DTOs, request/response contracts
│       ├── DTOs/
│       ├── Requests/
│       └── Responses/
├── .claude/
│   └── settings.local.json
└── reporting/
```

### Project Reference Graph

```
Warehouse.Auth.API
  ├── Warehouse.Auth.DBModel
  │   └── Warehouse.Common
  ├── Warehouse.Common
  ├── Warehouse.Mapping
  │   ├── Warehouse.ServiceModel
  │   ├── Warehouse.Auth.DBModel
  │   └── Warehouse.Customers.DBModel
  ├── Warehouse.ServiceModel
  └── Warehouse.GenericFiltering
      └── Warehouse.Common

Warehouse.Customers.API
  ├── Warehouse.Customers.DBModel
  │   └── Warehouse.Common
  ├── Warehouse.Common
  ├── Warehouse.Mapping
  ├── Warehouse.ServiceModel
  └── Warehouse.GenericFiltering
      └── Warehouse.Common

Warehouse.Auth.API.Tests
  └── Warehouse.Auth.API

Warehouse.Customers.API.Tests
  ├── Warehouse.Customers.API
  └── Warehouse.Mapping

Warehouse.Inventory.API
  ├── Warehouse.Inventory.DBModel
  │   └── Warehouse.Common
  ├── Warehouse.Common
  ├── Warehouse.Infrastructure
  ├── Warehouse.Mapping
  ├── Warehouse.ServiceModel
  └── Warehouse.GenericFiltering
      └── Warehouse.Common

Warehouse.Inventory.API.Tests
  ├── Warehouse.Inventory.API
  └── Warehouse.Mapping
```

---

## 3. Domain Entities

### Auth Domain (`Warehouse.Auth.DBModel` — `AuthDbContext`)

| Entity | Schema | Table | Description |
|---|---|---|---|
| User | auth | Users | Application users |
| Role | auth | Roles | Authorization roles |
| Permission | auth | Permissions | Granular resource-action permissions |
| UserRole | auth | UserRoles | User-to-role join table |
| RolePermission | auth | RolePermissions | Role-to-permission join table |
| RefreshToken | auth | RefreshTokens | JWT refresh token storage |
| UserActionLog | auth | UserActionLogs | Immutable audit trail |

### Customers Domain (`Warehouse.Customers.DBModel` — `CustomersDbContext`)

| Entity | Schema | Table | Description |
|---|---|---|---|
| Customer | customers | Customers | Core customer entity |
| CustomerCategory | customers | CustomerCategories | Customer classification |
| CustomerAccount | customers | CustomerAccounts | Multi-currency accounts |
| CustomerAddress | customers | CustomerAddresses | Physical addresses |
| CustomerPhone | customers | CustomerPhones | Phone numbers |
| CustomerEmail | customers | CustomerEmails | Email addresses |

**Note:** `Customer.CreatedByUserId` and `Customer.ModifiedByUserId` are plain FK columns referencing `auth.Users` — no EF navigation property (cross-context boundary).

### Inventory Domain (`Warehouse.Inventory.DBModel` — `InventoryDbContext`)

| Entity | Schema | Table | Description |
|---|---|---|---|
| Product | inventory | Products | Product catalog item |
| ProductCategory | inventory | ProductCategories | Hierarchical product classification |
| UnitOfMeasure | inventory | UnitsOfMeasure | Measurement units (kg, pcs, etc.) |
| BillOfMaterials | inventory | BillOfMaterials | BOM header (parent product) |
| BomLine | inventory | BomLines | BOM line (child product + quantity) |
| ProductAccessory | inventory | ProductAccessories | Related product lookup |
| ProductSubstitute | inventory | ProductSubstitutes | Interchangeable product lookup |
| WarehouseEntity | inventory | Warehouses | Physical warehouse definition |
| Zone | inventory | Zones | Area within a warehouse |
| StorageLocation | inventory | StorageLocations | Specific storage position (row/shelf/bin) |
| StockLevel | inventory | StockLevels | Current quantity per product/warehouse/location |
| StockMovement | inventory | StockMovements | Immutable record of quantity changes |
| Batch | inventory | Batches | Lot/batch with expiry tracking |
| InventoryAdjustment | inventory | InventoryAdjustments | Adjustment header with approval workflow |
| InventoryAdjustmentLine | inventory | InventoryAdjustmentLines | Adjustment line (product/location/quantity) |
| WarehouseTransfer | inventory | WarehouseTransfers | Inter-warehouse transfer header |
| WarehouseTransferLine | inventory | WarehouseTransferLines | Transfer line (product/quantity/locations) |
| StocktakeSession | inventory | StocktakeSessions | Physical count event |
| StocktakeCount | inventory | StocktakeCounts | Individual count entry per product/location |

**Note:** `CreatedByUserId` and `ModifiedByUserId` columns reference `auth.Users` — plain FK, no EF navigation (cross-context boundary).

---

## 4. API Endpoints

### Auth API (`Warehouse.Auth.API` — port 5001)

See `docs/infrastructure/SDD-AUTH-001-authentication-and-authorization.md` for full endpoint documentation.

### Customers API (`Warehouse.Customers.API` — port 5002)

See `docs/domain/SDD-CUST-001-customers-and-accounts.md` for full endpoint documentation (27 endpoints).

### Inventory API (`Warehouse.Inventory.API` — port 5003)

See specs for full endpoint documentation:
- `docs/core/SDD-INV-001-products-and-catalog.md` — Products, categories, units of measure, BOM, accessories, substitutes
- `docs/core/SDD-INV-002-stock-management.md` — Stock levels, movements, adjustments, batches
- `docs/core/SDD-INV-003-warehouse-structure.md` — Warehouses, zones, storage locations, transfers
- `docs/core/SDD-INV-004-stocktaking.md` — Stocktake sessions, counts, variance reports

---

## 5. Authentication & Authorization

JWT-based authentication with RBAC (Role-Based Access Control). Implemented in `Warehouse.Auth.API`.

- **Access tokens:** JWT signed with HMAC-SHA256, 30-minute expiration
- **Refresh tokens:** Opaque random strings stored in DB, 7-day expiration, rotation on use
- **Authorization:** Permission-based via `[RequirePermission("resource:action")]` attribute
- **Password hashing:** BCrypt
- **Audit:** Immutable `UserActionLogs` table for all user management events
- **Spec:** `docs/infrastructure/SDD-AUTH-001-authentication-and-authorization.md`

---

## 6. Background Processing Architecture

None yet. Will be added as needed (e.g., Hosted Services for async processing).

---

## 7. External Integrations

None yet. Planned integrations:

- ERP system (details TBD)
- Vue.js 3 SPA frontend (consuming backend REST APIs)

---

## 8. Known Deviations from Persona Standards

| # | Deviation | Reason | Plan |
|---|---|---|---|
| 1 | `Nullable` enabled in `.csproj` | Default template setting | Review — may disable if not desired |
| 2 | `ImplicitUsings` enabled | Default template setting | Review — may disable for explicit control |
| 3 | Polly only on Customers.API | Auth.API has no outbound HTTP clients | Add Polly to Auth.API when it needs typed HttpClients |
| 4 | Mapping project references both DBModel projects | Shared AutoMapper profiles | Split into per-domain mapping if isolation becomes critical |

---

## 9. Configuration Keys

No custom configuration yet. Will be populated as `appsettings.json` grows.

---

## 10. GenericFiltering Syntax

The `Warehouse.GenericFiltering` project is scaffolded but empty. It will provide reusable `IQueryable` dynamic filtering when implemented.

---

## 11. SDD Documentation Structure (TWO-TIER)

### Tier 1 — System Specs (`SDD-*`)

Describe current implemented behavior. Source of truth.

| Category | Folder | Purpose |
|---|---|---|
| Core | `docs/core/` | Primary workflows and entities |
| Domain | `docs/domain/` | Secondary features, metadata |
| Integration | `docs/integration/` | External system integrations |
| Infrastructure | `docs/infrastructure/` | Auth, filtering, observability |

### Tier 2 — Change Specs (`CHG-*`)

Describe proposed changes. Live in `docs/changes/`.

| Prefix | Use For |
|---|---|
| `CHG-FEAT-NNN` | New features or capabilities |
| `CHG-ENH-NNN` | Enhancements to existing behavior |
| `CHG-FIX-NNN` | Bug fixes |
| `CHG-REFAC-NNN` | Refactoring (no behavior change) |
| `CHG-DEBT-NNN` | Technical debt reduction |

---

## 12. Naming Conventions

| Item | Convention | Example |
|---|---|---|
| Controller | `{Entity}Controller` | `InventoryController` |
| Service interface | `I{Entity}Service` | `IInventoryService` |
| Service implementation | `{Entity}Service` | `InventoryService` |
| Repository interface | `I{Entity}Repository` | `IInventoryRepository` |
| Repository implementation | `{Entity}Repository` | `InventoryRepository` |
| DTO | `{Entity}Dto` | `InventoryItemDto` |
| Request model | `{Action}{Entity}Request` | `CreateInventoryItemRequest` |
| Response model | `{Entity}Response` | `InventoryItemResponse` |
| AutoMapper profile | `{Entity}MappingProfile` | `InventoryMappingProfile` |
| Validator | `{Action}{Entity}RequestValidator` | `CreateInventoryItemRequestValidator` |
| EF Core config | `{Entity}Configuration` | `InventoryItemConfiguration` |
| DB entity | `{Entity}` (no suffix) | `InventoryItem` |
| Test class | `{ClassUnderTest}Tests` | `InventoryServiceTests` |

---

## 13. Quick Reference — Key File Paths

| Purpose | Path |
|---|---|
| Solution file | `src/Warehouse.slnx` |
| Auth API | `src/Interfaces/Auth/Warehouse.Auth.API/` |
| Auth API tests | `src/Interfaces/Auth/Warehouse.Auth.API.Tests/` |
| Auth DB models | `src/Databases/Warehouse.Auth.DBModel/Models/` |
| Auth DbContext | `src/Databases/Warehouse.Auth.DBModel/AuthDbContext.cs` |
| Customers API | `src/Interfaces/Customers/Warehouse.Customers.API/` |
| Customers API tests | `src/Interfaces/Customers/Warehouse.Customers.API.Tests/` |
| Customers DB models | `src/Databases/Warehouse.Customers.DBModel/Models/` |
| Customers DbContext | `src/Databases/Warehouse.Customers.DBModel/CustomersDbContext.cs` |
| Inventory API | `src/Interfaces/Inventory/Warehouse.Inventory.API/` |
| Inventory API tests | `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/` |
| Inventory DB models | `src/Databases/Warehouse.Inventory.DBModel/Models/` |
| Inventory DbContext | `src/Databases/Warehouse.Inventory.DBModel/InventoryDbContext.cs` |
| DTOs | `src/Warehouse.ServiceModel/DTOs/` |
| Request models | `src/Warehouse.ServiceModel/Requests/` |
| Response models | `src/Warehouse.ServiceModel/Responses/` |
| AutoMapper profiles | `src/Warehouse.Mapping/Profiles/` |
| Shared enums | `src/Warehouse.Common/Enums/` |
| Extensions | `src/Warehouse.Common/Extensions/` |
| SDD specs | `docs/` |
| Change template | `docs/changes/_TEMPLATE.md` |
