# Warehouse Microservices Plan — ISA-95 Aligned

> Created: 2026-04-02
> Updated: 2026-04-17
> Type: Greenfield — .NET 8 Microservices + Vue.js Frontend (Monorepo)
> Database: Single source of truth (SQL Server)
> Standard: ISA-95 / IEC 62264
> External integrations: ERP (planned, ISA-95 Part 4)
> Status: Phase 1 Complete, Phase 1.5 Complete, Phase 2 Complete, Phase 2.5 Complete

---

## ISA-95 Alignment

This plan structures the WMS according to **ISA-95 (IEC 62264)**, the international standard for enterprise–operations integration. Each microservice maps to exactly one ISA-95 operations domain (Rule 8). The system operates primarily at **Level 3** (Manufacturing Operations Management) with Level 4 interfaces for business planning and finance.

### ISA-95 Level Placement

| Level | Description | WMS Services |
|---|---|---|
| **Level 4** | Business Planning & Logistics | Finance, Planning |
| **Level 3** | Manufacturing Operations Management | Auth, Customers, Inventory, Purchasing, Fulfillment, Production, Quality |
| Level 2 | Monitoring & Supervisory Control | Barcode/IoT integration (future, within Fulfillment/Purchasing) |
| Level 1–0 | Physical Process | Out of scope |

### Original Plan → ISA-95 Restructuring

The original 9-service plan (2026-04-02) was restructured to comply with ISA-95 domain boundaries:

| Original Service | Problem | ISA-95 Restructuring |
|---|---|---|
| Orders | Mixed 3 domains: Fulfillment + Procurement + Planning | Split into **Purchasing**, **Fulfillment**, and **Planning** |
| Manufacturing | Mixed 2 domains: Production + Quality | Split into **Production** and **Quality** |
| Shipping | Subset of Fulfillment Operations | Absorbed into **Fulfillment** |
| Finance | Correctly scoped but reclassified | Reclassified as **Level 4** |
| Auth, Customers, Inventory | Clean single-domain mapping | **Unchanged** |
| Reporting, Admin | Cross-cutting, not ISA-95 domains | **Unchanged** |

**Result: 9 services → 11 services**, each with a clean single-domain boundary.

---

## Architecture Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Communication (sync) | Direct HTTP (typed HttpClient + Polly) | Synchronous inter-service calls; simple and debuggable |
| Communication (async) | RabbitMQ (Phase 2) | Event-driven messaging for decoupled inter-service workflows |
| Database | Shared SQL Server, schema-per-service | Single source of truth; schemas provide logical isolation |
| DB schema strategy | Schema-per-service (`auth.*`, `inventory.*`, `purchasing.*`, etc.) | Clear ownership, cross-schema FKs, single backup |
| DBModel | Separate DbContext per domain | Cross-context references via plain FK columns, no EF navigation across boundaries |
| Message broker | RabbitMQ (Phase 2) | Decouple inter-service events; avoid synchronous chains for operations like goods receiving |
| Distributed cache | Redis (Phase 1.5) | Caching, rate-limiting backend, session store, pub/sub for real-time updates |
| Auth | Self-issued JWT (short-lived access + refresh tokens) | No external IdP needed; OAuth2 can be layered later |
| API gateway | YARP (Phase 1.5) | Single entry point for Vue SPA; routing, rate limiting, health aggregation |
| Frontend | Vue.js 3 SPA (Vuetify + TypeScript + Pinia) | Single frontend calling all backend services via Vite proxy |
| Standard | ISA-95 / IEC 62264 | Every service maps to one ISA-95 operations domain |

---

## Build Phases

### Phase 1 — Foundation & Inventory Operations (COMPLETE)

#### 1. Warehouse.Auth.API — Personnel & Authorization
**ISA-95 Domain:** Personnel Model (Part 2, Section 6)
**Schema:** `auth`
**Port:** 5001
**Status:** Implemented

| Feature | ISA-95 Mapping | Status |
|---|---|---|
| JWT authentication | Credential management | Done |
| User CRUD | Person management | Done |
| Roles & permissions | Personnel Class + Qualification | Done |
| User action audit | Operations Event | Done |
| Password management | Credential lifecycle | Done |
| Database seeder | Initial data (admin user, all permissions) | Done |

---

#### 2. Warehouse.Customers.API — Business Partner Management
**ISA-95 Domain:** Business Partner Management (enterprise-level entity)
**Schema:** `customers`
**Port:** 5002
**Status:** Implemented

| Feature | ISA-95 Mapping | Status |
|---|---|---|
| Customer CRUD | Business partner definition | Done |
| Account management | Multi-currency partner accounts | Done |
| Contact information | Partner contact records | Done |
| Customer categories | Partner classification | Done |
| Account merge | Partner deduplication | Done |

---

#### 3. Warehouse.Inventory.API — Inventory Operations
**ISA-95 Domain:** Inventory Operations (Part 2, Sections 5 & 7; Part 3)
**Schema:** `inventory`
**Port:** 5003
**Status:** Implemented

| Feature | ISA-95 Activity | Status |
|---|---|---|
| Product catalog (CRUD, categories, UoM) | Inventory Definition Management | Done |
| BOM / recipes | Material Definition (Bill of Material) | Done |
| Product accessories & substitutes | Related Material / Alternate Material | Done |
| On-hand quantities (StockLevel) | Material Inventory Tracking | Done |
| Stock movements (immutable) | Material Movement (Operations Event) | Done |
| Inventory adjustments (approval workflow) | Inventory Adjustment | Done |
| Batch/lot tracking | Material Lot management | Done |
| Warehouses, zones, locations | Equipment Model (Site → Area → Storage Unit) | Done |
| Warehouse transfers | Material Transfer | Done |
| Stocktake sessions + counts | Inventory Counting | Done |
| Variance reports | Count variance analysis | Done |

**ISA-95 compliance items to address in existing code:**
- [x] Add `RequiresBatchTracking` flag on Product to enforce Material Lot chain for traceable materials
- [x] Rename `PurchaseReceipt` → `Receipt`, `SalesDispatch` → `Shipment`, `StocktakeCorrection` → `CountAdjustment`, `ProductionReceipt` → `ProductionOutput` on `StockMovementReason`; enforce enum type (no free-text); add `StockMovementReferenceType` enum

---

### Phase 1.5 — Infrastructure Prerequisites (COMPLETE)

All cross-cutting infrastructure capabilities are in place. These were required before Phase 2 inter-service communication.

#### API Gateway — `Warehouse.Gateway`
**Technology:** YARP (Yet Another Reverse Proxy) 2.3.0
**Port:** 5000
**Priority:** P1
**Status:** Done

| Feature | Description | Status |
|---|---|---|
| **Reverse proxy** | 47 routes across 7 backend clusters (Auth, Customers, Inventory, Purchasing, Fulfillment, EventLog, Nomenclature) | Done |
| **Rate limiting** | Global per-IP (200 req/min) + named "fixed" policy (100 req/min) | Done |
| **Authentication passthrough** | Forward JWT tokens and X-Correlation-ID to downstream services | Done |
| **Health aggregation** | Aggregate `/health/ready` from all downstream services | Done |
| **Correlation ID forwarding** | Propagate X-Correlation-ID via YARP transform | Done |

---

#### Correlation ID Middleware
**Location:** `Warehouse.Infrastructure`
**Priority:** P1
**Status:** Done

| Feature | Description | Status |
|---|---|---|
| **Generate** | Create `X-Correlation-ID` (GUID) if not present on incoming request | Done |
| **Propagate** | Inject correlation ID into all outbound `HttpClient` requests + YARP transforms | Done |
| **Log enrichment** | Add `CorrelationId` to NLog structured log context | Done |
| **Response header** | Return `X-Correlation-ID` in response for frontend debugging | Done |

---

#### Centralized Logging
**Technology:** NLog → Loki 3.4.2 + Grafana 11.5.2
**Priority:** P1
**Status:** Done

| Feature | Description | Status |
|---|---|---|
| **NLog sink** | NLog.Targets.Loki pushes structured logs to Loki | Done |
| **Structured fields** | All log entries include `CorrelationId`, `ServiceName`, `UserId`, `RequestPath` | Done |
| **Dashboard** | Grafana at `localhost:3001` with pre-configured service monitoring dashboards | Done |
| **ASP.NET Core request logging** | Hosting.Diagnostics request logs for Loki tracing | Done |

---

#### Distributed Tracing
**Technology:** OpenTelemetry → Jaeger
**Priority:** P1
**Status:** Done

| Feature | Description | Status |
|---|---|---|
| **Auto-instrumentation** | ASP.NET Core, HttpClient, SQL Client | Done |
| **Trace propagation** | W3C TraceContext headers across inter-service HTTP calls | Done |
| **Export** | OTLP exporter → Jaeger at `localhost:16686` (ports 4317, 4318) | Done |

---

#### Polly Resilience Policies
**Technology:** Microsoft.Extensions.Http.Resilience 9.x
**Location:** `Warehouse.Infrastructure`
**Priority:** P1
**Status:** Ready (extension method available; no inter-service HttpClients yet)

| Policy | Configuration |
|---|---|
| **Retry** | 3 attempts, exponential backoff + jitter |
| **Circuit breaker** | 5-min throughput sampling, 15s break duration |
| **Timeout** | 10s per-attempt, 30s total |
| **Correlation ID propagation** | Auto-forward X-Correlation-ID on outbound calls |

Extension method: `AddWarehouseHttpClient<TClient, TImpl>(baseAddress)` in `Warehouse.Infrastructure`. First real use case will be cross-service validation (Fulfillment → Inventory, Fulfillment → Customers).

---

#### Distributed Cache — Redis
**Technology:** Redis 7.4 (StackExchange.Redis / Microsoft.Extensions.Caching.StackExchangeRedis)
**Priority:** P1
**Status:** Done

| Feature | Description | Status |
|---|---|---|
| **Lookup data caching** | Permissions, product categories, UoM, customer categories, supplier categories cached via `IDistributedCache` | Done |
| **Cache invalidation** | Write operations invalidate cache; paginated lists served from cached data | Done |

**Cache key convention:** `{service}:{entity}:all` (e.g., `auth:permissions:all`, `inventory:product-categories:all`)
**TTL:** Permissions 5 min, categories/UoM 30 min. Transactional data MUST NOT be cached.
**Extension method:** `services.AddWarehouseRedisCache(configuration)` — registered in Auth, Customers, Inventory, Purchasing, Fulfillment.

---

#### Message Broker — RabbitMQ
**Technology:** RabbitMQ 4.1 (MassTransit 8.x)
**Priority:** P1
**Status:** Done

| Feature | Description | Status |
|---|---|---|
| **Domain events** | Fire-and-forget publish via `IPublishEndpoint` (try/catch — RabbitMQ unavailability does not break main operations) | Done |
| **Event consumers** | EventLog.API consumes events from all domains via MassTransit consumers | Done |
| **Centralized event log** | All domain events flow to `Warehouse.EventLog.API` for immutable storage in `eventlog.OperationsEvents` | Done |
| **Management UI** | RabbitMQ management at `localhost:15672` (warehouse/warehouse) | Done |

**Published domain events (10):**

| Event | Published From | After |
|---|---|---|
| `StockMovementRecordedEvent` | Inventory — `StockMovementService` | SaveChangesAsync |
| `InventoryAdjustmentAppliedEvent` | Inventory — `InventoryAdjustmentService` | CommitAsync |
| `WarehouseTransferCompletedEvent` | Inventory — `WarehouseTransferService` | CommitAsync |
| `CustomerCreatedEvent` | Customers — `CustomerService` | SaveChangesAsync |
| `CustomerEventOccurredEvent` | Customers — various services | State changes |
| `InventoryEventOccurredEvent` | Inventory — various services | State changes |
| `PurchaseEventOccurredEvent` | Purchasing — various services | State changes |
| `FulfillmentEventOccurredEvent` | Fulfillment — various services | State changes |
| `AuthAuditLoggedEvent` | Auth — `UserActionLogService` | Audit actions |
| `GoodsReceiptLineAcceptedEvent` | Purchasing — `GoodsReceiptService` | Goods receipt acceptance |

**Event contracts:** Defined as `sealed record` in `src/Warehouse.ServiceModel/Events/`.
**Extension method:** `services.AddWarehouseMessageBus(configuration)` — registered in all publishing services.
**Testing:** `services.AddMassTransitTestHarness()` in test projects to avoid real RabbitMQ connections.

---

#### Feature Flags
**Technology:** Microsoft.FeatureManagement 4.x
**Priority:** P2
**Status:** Done

| Flag | Service | Purpose | Default |
|---|---|---|---|
| `EnableDatabaseSeeding` | Auth.API | Gates entire `DatabaseSeeder.SeedAsync()` | `true` |
| `EnableSeedDefaultAdmin` | Auth.API | Gates default admin user creation | `true` |
| `EnableNomenclatureSeeding` | Nomenclature.API | Gates `NomenclatureSeeder.SeedAsync()` | `true` |
| `EnableSeedCountries` | Nomenclature.API | Gates country seed data | `true` |
| `EnableSeedCurrencies` | Nomenclature.API | Gates currency seed data | `true` |
| `EnableSeedStateProvinces` | Nomenclature.API | Gates state/province seed data | `true` |
| `EnableSeedCities` | Nomenclature.API | Gates city seed data | `true` |

**Extension method:** `services.AddWarehouseFeatureFlags()` — registered in Auth, Nomenclature.
**Flag constants:** `src/Warehouse.Infrastructure/Configuration/FeatureFlags.cs`.

---

### Phase 2 — Procurement & Fulfillment Operations (COMPLETE)

#### 4. Warehouse.Purchasing.API — Procurement Operations
**ISA-95 Domain:** Procurement Operations (Part 3 — Material Receipt)
**Schema:** `purchasing`
**Port:** 5004
**Depends on:** Auth, Inventory, Customers, Nomenclature
**Priority:** P1
**Status:** Implemented
**Spec:** SDD-PURCH-001

| Feature | ISA-95 Activity | Status |
|---|---|---|
| **Supplier management** (CRUD, categories, contacts) | Business Partner (supplier) | Done |
| **Purchase orders** (Draft → Confirmed → PartiallyReceived → Received → Closed → Cancelled) | Procurement Request | Done |
| **PO lines** (product/quantity/price per PO) | Procurement Request detail | Done |
| **Goods receiving** (receive against PO lines, auto-create Batch + StockMovement) | Material Receipt | Done |
| **Receiving inspection** (accept/reject/quarantine per GR line) | Quality Test (basic) | Done |
| **Supplier returns** (Draft → Confirmed → Shipped → Completed → Cancelled) | Material Shipment (return) | Done |
| **Purchase events** (immutable audit log) | Operations Event log | Done |
| **Sequence generation** (auto-code for PO, GR, SR, SUPP) | — | Done |
| **Nomenclature integration** (cascading address dropdowns) | — | Done |

**Controllers:** SuppliersController, SupplierCategoriesController, SupplierContactsController, PurchaseOrdersController, GoodsReceiptsController, SupplierReturnsController, PurchaseEventsController
**Services:** 10 service classes + BasePurchasingEntityService base class
**Tests:** 80 unit tests (Warehouse.Purchasing.API.Tests)

**Frontend (SDD-UI-004 — Implemented):**
- Supplier list/detail/form with auto-code preview and category management
- Purchase order list/detail/form with line items
- Goods receiving workflow with per-line acceptance and batch auto-creation
- Supplier return list/detail/form
- Nomenclature cascading dropdowns for supplier addresses

---

#### 5. Warehouse.Fulfillment.API — Fulfillment Operations
**ISA-95 Domain:** Fulfillment Operations (Part 3 — Material Shipment)
**Schema:** `fulfillment`
**Port:** 5005
**Depends on:** Auth, Inventory, Customers, Nomenclature
**Priority:** P1
**Status:** Implemented
**Spec:** SDD-FULF-001

| Feature | ISA-95 Activity | Status |
|---|---|---|
| **Sales orders** (Draft → Confirmed → Picking → Packed → Shipped → Completed → Cancelled) | Fulfillment Request | Done |
| **SO lines** (product/quantity/price per SO) | Fulfillment Request detail | Done |
| **Pick lists** (auto-generate from confirmed SO; line-by-line pick execution) | Material Movement (outbound) | Done |
| **Packing** (create parcels, assign items, record weights) | Material Movement (staging) | Done |
| **Dispatch / Shipments** (create shipment from packed SO) | Material Shipment | Done |
| **Shipment tracking** (immutable status history entries) | Operations Event | Done |
| **Carrier management** (carriers + service levels) | Equipment definition (logistics) | Done |
| **Customer returns** (Draft → Confirmed → Received → Closed → Cancelled) | Material Receipt (return) | Done |
| **Fulfillment events** (immutable audit log) | Operations Event log | Done |
| **Sequence generation** (auto-code for SO, PL, PKG, SHP, CR) | — | Done |

**Controllers:** SalesOrdersController, PickListsController, PackingController, ShipmentsController, CarriersController, CustomerReturnsController, FulfillmentEventsController
**Services:** 7 service classes + BaseFulfillmentEntityService base class
**Tests:** 128 unit tests (Warehouse.Fulfillment.API.Tests)

**Deferred features (Phase 3+):**
- Labels & printing (ZPL/Zebra, QR codes, barcodes)
- Dispatch documents (packing slips, delivery notes)

**Frontend (SDD-UI-005 — Implemented):**
- Sales order list/detail/form with line items, packing, and dispatch actions
- Pick list generation and line-by-line execution UI
- Shipment list with tracking history
- Carrier management with service levels
- Customer return list/detail/form
- Nomenclature cascading dropdowns for customer addresses

---

### Phase 2.5 — Supporting Services (COMPLETE)

#### 6. Warehouse.EventLog.API — Centralized Event Logging
**ISA-95 Domain:** Cross-cutting (Operations Event aggregation)
**Schema:** `eventlog`
**Port:** 5006
**Depends on:** Auth (JWT), all domain services (consumes events)
**Status:** Implemented
**Spec:** SDD-EVTLOG-001

| Feature | Description | Status |
|---|---|---|
| **MassTransit consumers** | 5 consumers: AuthAuditLogged, CustomerEventOccurred, InventoryEventOccurred, PurchaseEventOccurred, FulfillmentEventOccurred | Done |
| **Event mapping strategies** | Strategy pattern per domain for mapping domain events → OperationsEvent records | Done |
| **OperationsEvent factory** | Factory pattern for consistent event record creation | Done |
| **Query endpoint** | `GET /api/v1/events` with filtering, pagination, and search | Done |
| **Immutable storage** | All events stored as immutable records in `eventlog.OperationsEvents` | Done |

**Key design patterns:** Factory (OperationsEventFactory), Strategy (per-domain event mapping), Consumer pattern (MassTransit)

---

#### 7. Warehouse.Nomenclature.API — Reference Data
**ISA-95 Domain:** Cross-cutting (shared reference data)
**Schema:** `nomenclature`
**Port:** 5007
**Depends on:** Auth (JWT)
**Status:** Implemented
**Spec:** SDD-NOM-001

| Feature | Description | Status |
|---|---|---|
| **Countries** | Country CRUD with ISO codes | Done |
| **State/Provinces** | State/Province CRUD (hierarchical under Country) | Done |
| **Cities** | City CRUD (hierarchical under State/Province) | Done |
| **Currencies** | Currency CRUD with ISO 4217 codes | Done |
| **Database seeding** | Feature-flagged seeder for countries, currencies, state/provinces, cities | Done |
| **Cascading dropdowns** | Frontend integration with Country → State → City cascading selection | Done |

**Integrated with:** Customers.API (customer addresses), Purchasing.API (supplier addresses), Fulfillment.API (customer return/shipping addresses)
**Controllers:** CountriesController, StateProvincesController, CitiesController, CurrenciesController
**Tests:** Warehouse.Nomenclature.API.Tests

---

### Phase 3 — Production & Quality Operations

#### 8. Warehouse.Production.API — Production Operations
**ISA-95 Domain:** Production Operations (Part 3 — Production Execution)
**Schema:** `production`
**Port:** 5008
**Depends on:** Auth, Inventory
**Priority:** P2

| Feature | ISA-95 Activity | Description |
|---|---|---|
| **Production orders** | Production Request | Create from BOM; status machine (Draft → Released → InProgress → Completed) |
| **Material consumption** | Material Movement (consumption) | Deduct raw materials from stock; create StockMovement (reason: `ProductionConsumption`) |
| **Finished goods receipt** | Material Movement (production output) | Add produced items to stock; create StockMovement (reason: `ProductionOutput`) |
| **Production time tracking** | Operations Performance | Log work hours per order/operation/work center |
| **Work centers** | Equipment Model (Work Center) | Define production stations/lines with capacity |
| **Production scheduling** | Operations Schedule | Sequence and schedule production orders |
| **Production costs** | Operations Performance (cost) | Material + labor cost calculation per order |
| **COGS reporting** | Operations Performance (aggregated) | Cost of goods sold derived from production data |

**Key ISA-95 integration points:**
- Material consumption MUST create immutable `StockMovement` records with reason code `ProductionConsumption`
- Finished goods MUST create immutable `StockMovement` records with reason code `ProductionOutput`
- Production should follow ISA-95 Request → Schedule → Performance pattern
- Work centers extend the Equipment hierarchy: Site → Area → Work Center → Work Unit
- BOM (already in Inventory) defines the recipe; Production executes it

**Frontend scope:**
- Production order list/detail/form
- Production execution dashboard (start/stop/complete)
- Material consumption entry
- Time tracking entry
- Work center management
- Cost breakdown reports

---

#### 9. Warehouse.Quality.API — Quality Operations
**ISA-95 Domain:** Quality Operations (Part 3 — Quality Test Execution)
**Schema:** `quality`
**Port:** 5009
**Depends on:** Auth, Inventory, Purchasing (optional), Production (optional)
**Priority:** P2

| Feature | ISA-95 Activity | Description |
|---|---|---|
| **Quality tests** | Quality Test Definition | Define test procedures, acceptance criteria, measurement specs |
| **Inspection orders** | Quality Test Request | Create inspections triggered by receiving, production, or manual request |
| **Test execution** | Quality Test Execution | Record test results (pass/fail/conditional) per inspection point |
| **Quality holds** | Material status management | Place material on hold (quarantine); block from picking/shipping |
| **RMA management** | Quality Test (return analysis) | Return merchandise authorization — create, investigate, resolve |
| **Rework tracking** | Production rework | Track rework materials and labor; link to original production order |
| **Certifications** | Quality certificate | Generate certificates of analysis / conformance per batch |
| **Non-conformance reports** | Operations Event (quality) | Document deviations, root cause, corrective actions |

**Key ISA-95 integration points:**
- Quality holds MUST update material status to prevent fulfillment of held stock
- Inspection results that reject material MUST create `StockMovement` records with reason code `Adjustment`
- RMA resolution that returns material to stock MUST create appropriate `StockMovement` records
- Quality tests on incoming goods integrate with Purchasing (receiving inspection)
- Quality tests on produced goods integrate with Production (in-process/final inspection)

**Frontend scope:**
- Test definition management
- Inspection order list/detail
- Test execution entry (measurements, pass/fail)
- Quality hold management
- RMA workflow
- Non-conformance report form
- Certificate generation

---

### Phase 4 — Business Planning (Level 4)

#### 10. Warehouse.Finance.API — Financial Management
**ISA-95 Level:** Level 4 — Business Planning & Logistics
**Schema:** `finance`
**Port:** 5010
**Depends on:** Auth, Customers, Purchasing, Fulfillment
**Priority:** P2

| Feature | Description |
|---|---|
| **Invoicing** | Generate invoices from fulfilled orders and received POs |
| Invoice types | Sales, purchase, proforma, credit notes, customs |
| Invoice email & export | Send by email; export to accounting (CSV/XML) |
| **Payments** | Record and manage payments; match to invoices |
| Payment import | Import from bank files (XML/CSV) |
| Outstanding debts | Debtors/creditors tracking and aging |
| **Billing** | Rule definitions, price lists, discount rules |
| Monthly fees | Recurring fee calculation |
| Billing procedures | Sequential billing generation |
| **Prepayments & vouchers** | Prepaid balances, voucher creation and redemption |

**ISA-95 note:** Finance is a Level 4 concern. It consumes data from Level 3 operations (fulfilled orders, received goods) but does not directly participate in operations execution. ISA-95 Part 4 information exchange patterns apply when integrating with external ERP/accounting systems.

---

#### 11. Warehouse.Planning.API — Business Planning
**ISA-95 Level:** Level 4 — Business Planning & Logistics
**Schema:** `planning`
**Port:** 5011
**Depends on:** Auth, Inventory, Purchasing, Fulfillment, Production
**Priority:** P3

| Feature | ISA-95 Mapping | Description |
|---|---|---|
| Sales forecasting | Demand planning | Forecast demand from historical sales |
| Purchase planning | Procurement planning | Suggest POs based on stock levels, min/max rules, lead times |
| Materials planning (MRP) | Production planning | Calculate material requirements from BOM, demand, and current stock |
| Reorder points | Inventory planning | Automatic reorder triggers per product/warehouse |
| Capacity planning | Resource planning | Match production demand to work center capacity |

**ISA-95 note:** Planning is a Level 4 activity that generates Operations Requests for Level 3 services. MRP generates suggested Purchase Orders (→ Purchasing) and Production Orders (→ Production). This follows the ISA-95 Part 4 information exchange pattern: Level 4 sends requests, Level 3 responds with performance data.

---

### Phase 5 — Cross-Cutting Services

#### 12. Warehouse.Reporting.API — Reports & Dashboards
**ISA-95 Level:** Cross-cutting (reads from all levels)
**Schema:** Read-only access to all schemas
**Port:** 5012
**Depends on:** Auth, read access to all service databases
**Priority:** P3

| Feature | Description |
|---|---|
| Product movement history | Movement timeline per product/batch |
| Inventory snapshots | Point-in-time stock position reports |
| Financial summaries | Trial balance, revenue, costs |
| Exchange rates | Currency rate management |
| Operations KPIs | Fulfillment rate, receiving accuracy, stocktake variance |
| Custom reports | Configurable report builder |
| Export | Excel/CSV/PDF export |

---

#### 13. Warehouse.Admin.API — System Administration
**ISA-95 Level:** Cross-cutting
**Schema:** `admin`
**Port:** 5013
**Depends on:** Auth
**Priority:** P3 (built incrementally)

| Feature | Description |
|---|---|
| System settings | Global configuration key/value store |
| Cross-service audit trail | Consolidated audit log from all services |
| Error explorer | Application error browser |
| Template management | Document/email/label templates |
| Activity log | User activity tracking across services |
| Database migrations | Migration management UI |

---

## Summary

| # | Service | ISA-95 Domain | Level | Port | Priority | Depends on | Status |
|---|---|---|---|---|---|---|---|
| 1 | **Auth** | Personnel & Authorization | L3 | 5001 | P0 | — | **Done** |
| 2 | **Customers** | Business Partner Management | L3 | 5002 | P0 | Auth | **Done** |
| 3 | **Inventory** | Inventory Operations | L3 | 5003 | P1 | Auth, Customers | **Done** |
| — | **Gateway** | Cross-cutting | — | 5000 | P1 | All services | **Done** |
| — | **Infrastructure** | Cross-cutting | — | — | P1 | — | **Done** (all 9 components) |
| 4 | **Purchasing** | Procurement Operations | L3 | 5004 | P1 | Auth, Inventory, Customers, Nomenclature | **Done** |
| 5 | **Fulfillment** | Fulfillment Operations | L3 | 5005 | P1 | Auth, Inventory, Customers, Nomenclature | **Done** |
| 6 | **EventLog** | Cross-cutting (event aggregation) | — | 5006 | P1 | Auth, all (consumes events) | **Done** |
| 7 | **Nomenclature** | Cross-cutting (reference data) | — | 5007 | P1 | Auth | **Done** |
| 8 | **Production** | Production Operations | L3 | 5008 | P2 | Auth, Inventory | Not started |
| 9 | **Quality** | Quality Operations | L3 | 5009 | P2 | Auth, Inventory | Not started |
| 10 | **Finance** | Financial Management | L4 | 5010 | P2 | Auth, Customers, Purchasing, Fulfillment | Not started |
| 11 | **Planning** | Business Planning | L4 | 5011 | P3 | Auth, Inventory, Purchasing, Fulfillment, Production | Not started |
| 12 | **Reporting** | Cross-cutting | — | 5012 | P3 | Auth, all (read) | Not started |
| 13 | **Admin** | Cross-cutting | — | 5013 | P3 | Auth | Not started |

### Infrastructure Components (Phase 1.5 — ALL COMPLETE)

| # | Component | Technology | Priority | Status |
|---|---|---|---|---|
| I1 | **API Gateway** | YARP 2.3.0 | P1 | **Done** — port 5000, 47 routes across 7 clusters, health aggregation, rate limiting (200 req/min per IP), correlation ID forwarding |
| I2 | **Correlation IDs** | Custom middleware (Warehouse.Infrastructure) | P1 | **Done** — generate/propagate X-Correlation-ID, NLog enrichment, YARP transform forwarding |
| I3 | **Centralized Logging** | NLog → Loki 3.4.2 + Grafana 11.5.2 | P1 | **Done** — all 7 services push to Loki; Grafana dashboards at localhost:3001 |
| I4 | **Distributed Tracing** | OpenTelemetry → Jaeger | P1 | **Done** — auto-instrumentation (ASP.NET Core, HttpClient, SQL); Jaeger at localhost:16686 |
| I5 | **Polly Resilience** | Microsoft.Extensions.Http.Resilience 9.x | P1 | **Ready** — `AddWarehouseHttpClient<T,TImpl>()` available; first use case: cross-service validations |
| I6 | **Distributed Cache** | Redis 7.4 (StackExchange.Redis) | P1 | **Done** — caching in Auth, Customers, Inventory, Purchasing, Fulfillment (permissions, categories, UoM) |
| I7 | **Message Broker** | RabbitMQ 4.1 (MassTransit 8.x) | P1 | **Done** — 10 domain events published; EventLog.API consumes all; management UI at localhost:15672 |
| I8 | **Rate Limiting** | ASP.NET Core RateLimiting (at gateway) | P2 | **Done** — global per-IP (200/min) + named "fixed" policy (100/min) |
| I9 | **Feature Flags** | Microsoft.FeatureManagement 4.x | P2 | **Done** — gates database seeding in Auth.API + Nomenclature.API (7 flags total) |
| I10 | **Sequence Generation** | Custom (Warehouse.Infrastructure) | P1 | **Done** — centralized number generation (12 sequence definitions); SDD-INFRA-003 |
| I11 | **Centralized Event Log** | MassTransit consumers + EventLogDbContext | P1 | **Done** — `Warehouse.EventLog.API` (port 5006); SDD-EVTLOG-001 |

---

## Monorepo Structure (current)

```
Warehouse/
├── CLAUDE.md
├── README.md
├── docker-compose.infrastructure.yml    ← Docker Compose for all services + infrastructure
├── docker/                              ← Docker configs (loki, grafana, nginx)
├── docs/
│   ├── core/               ← Inventory, Purchasing, Fulfillment specs + UI specs
│   ├── domain/             ← Customers, Nomenclature specs + UI feature specs
│   ├── infrastructure/     ← Auth, EventLog, Infra, Observability specs
│   └── changes/            ← 20 change specs (CHG-ENH-*, CHG-REFAC-*)
├── frontend/               ← Vue.js 3 SPA (port 3000)
│   └── src/
│       ├── app/            ← Shell, router (6 route files)
│       ├── features/       ← auth, customers, inventory, purchasing, fulfillment
│       └── shared/         ← Components, stores, i18n (EN/BG)
└── src/
    ├── Warehouse.slnx
    ├── Databases/
    │   ├── Warehouse.Auth.DBModel/          ← auth schema
    │   ├── Warehouse.Customers.DBModel/     ← customers schema
    │   ├── Warehouse.Inventory.DBModel/     ← inventory schema
    │   ├── Warehouse.Purchasing.DBModel/    ← purchasing schema
    │   ├── Warehouse.Fulfillment.DBModel/   ← fulfillment schema
    │   ├── Warehouse.EventLog.DBModel/      ← eventlog schema
    │   └── Warehouse.Nomenclature.DBModel/  ← nomenclature schema
    ├── Infrastructure/
    │   ├── Gateway/
    │   │   └── Warehouse.Gateway/           ← YARP reverse proxy (port 5000, 47 routes)
    │   └── EventLog/
    │       ├── Warehouse.EventLog.API/      ← MassTransit consumers, query endpoint (port 5006)
    │       └── Warehouse.EventLog.API.Tests/
    ├── Interfaces/
    │   ├── Auth/
    │   │   ├── Warehouse.Auth.API/          ← port 5001
    │   │   └── Warehouse.Auth.API.Tests/
    │   ├── Customers/
    │   │   ├── Warehouse.Customers.API/     ← port 5002
    │   │   └── Warehouse.Customers.API.Tests/
    │   ├── Inventory/
    │   │   ├── Warehouse.Inventory.API/     ← port 5003
    │   │   └── Warehouse.Inventory.API.Tests/
    │   ├── Purchasing/
    │   │   ├── Warehouse.Purchasing.API/    ← port 5004
    │   │   └── Warehouse.Purchasing.API.Tests/
    │   ├── Fulfillment/
    │   │   ├── Warehouse.Fulfillment.API/   ← port 5005
    │   │   └── Warehouse.Fulfillment.API.Tests/
    │   └── Nomenclature/
    │       ├── Warehouse.Nomenclature.API/  ← port 5007
    │       └── Warehouse.Nomenclature.API.Tests/
    ├── Warehouse.Common/
    ├── Warehouse.Infrastructure/
    ├── Warehouse.GenericFiltering/
    ├── Warehouse.Mapping/
    └── Warehouse.ServiceModel/
```

### Future additions (Phase 3+)

```
    ├── Databases/
    │   ├── Warehouse.Production.DBModel/    ← production schema (planned)
    │   ├── Warehouse.Quality.DBModel/       ← quality schema (planned)
    │   ├── Warehouse.Finance.DBModel/       ← finance schema (planned)
    │   └── Warehouse.Planning.DBModel/      ← planning schema (planned)
    ├── Interfaces/
    │   ├── Production/                      ← port 5008 (planned)
    │   ├── Quality/                         ← port 5009 (planned)
    │   ├── Finance/                         ← port 5010 (planned)
    │   ├── Planning/                        ← port 5011 (planned)
    │   ├── Reporting/                       ← port 5012 (planned)
    │   └── Admin/                           ← port 5013 (planned)
```

---

## Docker Compose (`docker-compose.infrastructure.yml`)

All services and infrastructure run via Docker Compose:

```bash
docker compose -f docker-compose.infrastructure.yml up -d
```

| Container | Port | Purpose |
|---|---|---|
| `warehouse-sqlserver` | 1433 | SQL Server 2022 (sa / Warehouse@Dev123) |
| `warehouse-redis` | 6379 | Distributed cache |
| `warehouse-rabbitmq` | 5672, 15672 | Message broker + management UI |
| `warehouse-loki` | 3100 | Log aggregation |
| `warehouse-grafana` | 3001 | Monitoring dashboards (admin/admin) |
| `warehouse-jaeger` | 4317, 4318, 16686 | Distributed tracing |
| `warehouse-auth-api` | 5001 | Auth API |
| `warehouse-customers-api` | 5002 | Customers API |
| `warehouse-inventory-api` | 5003 | Inventory API |
| `warehouse-purchasing-api` | 5004 | Purchasing API |
| `warehouse-fulfillment-api` | 5005 | Fulfillment API |
| `warehouse-eventlog-api` | 5006 | EventLog API |
| `warehouse-nomenclature-api` | 5007 | Nomenclature API |
| `warehouse-gateway` | 5000 | YARP API Gateway |
| `warehouse-frontend` | 3000 | Vue.js SPA (Nginx) |

**Total: 15 containers** (7 backend APIs + 1 gateway + 1 frontend + 6 infrastructure)

---

## Deferred Items

Items identified during validation that are intentionally deferred. These do NOT block the services from running — all business logic works. They are about completeness and hardening.

### Cross-Service Validations (blocked on Polly typed HttpClients)

Both Purchasing and Fulfillment have MUST rules that require calling other microservices over HTTP to validate foreign references. These are marked with `// TODO:` comments in the service code. Implementing them is the **first real Polly use case** — it requires creating typed HttpClient interfaces and registering them via `AddWarehouseHttpClient<T,TImpl>()`.

| Service | Validation | Calls | Spec Reference | Error Code |
|---|---|---|---|---|
| Purchasing | Supplier return: check stock before confirm | Inventory.API | SDD-PURCH-001 §2.7.2 | INSUFFICIENT_STOCK (409) |
| Fulfillment | SO create: check customer exists + active | Customers.API | SDD-FULF-001 §2.1.1 | CUSTOMER_NOT_FOUND (404), CUSTOMER_INACTIVE (409) |
| Fulfillment | SO create: check warehouse exists | Inventory.API | SDD-FULF-001 §2.1.1 | INVALID_WAREHOUSE (400) |
| Fulfillment | SO add line: check product exists | Inventory.API | SDD-FULF-001 §2.1.2 | INVALID_PRODUCT (400) |
| Fulfillment | Pick list: check stock availability | Inventory.API | SDD-FULF-001 §2.2.1 | INSUFFICIENT_STOCK (409) |
| Fulfillment | Return create: check customer exists | Customers.API | SDD-FULF-001 §2.7.1 | CUSTOMER_NOT_FOUND (404) |

**To implement:** Create `IInventoryClient` and `ICustomerClient` typed HttpClient interfaces, register with `AddWarehouseHttpClient`, inject into services, replace TODO comments with actual calls.

### Missing Tests

| Category | Purchasing | Fulfillment | Notes |
|---|---|---|---|
| Integration tests (WebApplicationFactory) | 87 planned | 85 planned | Need test base with auth helpers, MassTransit test harness |
| FluentValidation unit tests | 29 planned | 24 planned | Validators exist and work — tests verify individual rules |
| Contact service unit tests (Purchasing only) | 11 planned | — | SupplierAddress/Phone/Email service tests |
| Additional unit test gaps | ~8 | ~20 | Edge cases from spec test plan not yet covered |

### Minor Code Items

| Item | Service | Priority |
|---|---|---|
| Add update validators (UpdateSupplierRequest, etc.) | Purchasing | P2 |
| Add contact validators (address, phone, email) | Purchasing | P2 |

---

## ISA-95 Compliance Backlog (Existing Code)

These items should be addressed before or during Phase 2 to ensure the foundation is ISA-95 compliant:

| # | Item | Priority | Effort | Description |
|---|---|---|---|---|
| 1 | ~~Enforce batch tracking~~ | ~~High~~ | ~~Medium~~ | **Done** — `RequiresBatchTracking` flag on Product; StockMovementService rejects movements without BatchId when enabled. |
| 2 | ~~Align reason code names~~ | ~~Medium~~ | ~~Low~~ | **Done** — Renamed to ISA-95 terminology (Receipt, Shipment, CountAdjustment, ProductionOutput); ReasonCode + ReferenceType now use enums with EF string conversion. |
| 3 | Add Material Sublot | Low | Medium | Add `Sublot` entity under Batch for finer traceability (e.g., split a batch across multiple locations). Completes the ISA-95 three-level Material chain. |
| 4 | ISA-95 Part 4 message schema | Low | High | Define JSON schemas derived from B2MML for ERP information exchange. Needed when ERP integration begins. |

---

## Progress Tracker

| # | Service | ISA-95 Domain | Spec | Implement | Test | Validate | Status |
|---|---|---|---|---|---|---|---|
| 1 | Auth | Personnel & Authorization | SDD-AUTH-001 | Done | Partial | Missing | **Implemented** |
| 2 | Customers | Business Partner Management | SDD-CUST-001 | Done | Partial | Missing | **Implemented** |
| 3 | Inventory | Inventory Operations | SDD-INV-001..005 | Done | Partial | Missing | **Implemented** |
| — | Gateway (YARP) | Cross-cutting | SDD-INFRA-002 | Done | 20 planned | Validated | **Implemented** |
| — | Correlation IDs | Cross-cutting | SDD-INFRA-001 | Done | 31 unit | Validated | **Implemented** |
| — | Centralized Logging | Cross-cutting | SDD-OBS-001 | Done | 16 planned | Validated | **Implemented** (Loki + Grafana) |
| — | Distributed Tracing | Cross-cutting | SDD-OBS-001 | Done | (incl. above) | Validated | **Implemented** (OpenTelemetry → Jaeger) |
| — | Polly Resilience | Cross-cutting | SDD-INFRA-001 | Done | (incl. above) | Validated | **Ready** (extension method; awaiting cross-service calls) |
| — | Redis Cache | Cross-cutting | SDD-INFRA-001 | Done | (incl. above) | Validated | **Implemented** (Auth, Customers, Inventory, Purchasing, Fulfillment) |
| — | RabbitMQ | Cross-cutting | SDD-INFRA-001 | Done | (incl. above) | Validated | **Implemented** (10 events, 5 consumers) |
| — | Rate Limiting | Cross-cutting | SDD-INFRA-002 | Done | (incl. above) | Validated | **Implemented** (per-IP on Gateway) |
| — | Feature Flags | Cross-cutting | SDD-INFRA-001 | Done | (incl. above) | Validated | **Implemented** (Auth + Nomenclature seeders) |
| — | Sequence Generation | Cross-cutting | SDD-INFRA-003 | Done | Done | Validated | **Implemented** (12 sequence definitions) |
| 4 | Purchasing | Procurement Operations | SDD-PURCH-001 | Done | 80 unit | Validated | **Implemented** |
| 5 | Fulfillment | Fulfillment Operations | SDD-FULF-001 | Done | 128 unit | Validated | **Implemented** |
| 6 | EventLog | Cross-cutting | SDD-EVTLOG-001 | Done | Done | Validated | **Implemented** |
| 7 | Nomenclature | Cross-cutting | SDD-NOM-001 | Done | Done | Validated | **Implemented** |
| — | Purchasing SPA | — | SDD-UI-004 | Done | — | — | **Implemented** |
| — | Fulfillment SPA | — | SDD-UI-005 | Done | — | — | **Implemented** |
| — | Batch on Receipt | — | SDD-INV-005 | Done | Done | Validated | **Implemented** |
| 8 | Production | Production Operations | — | — | — | — | Not started |
| 9 | Quality | Quality Operations | — | — | — | — | Not started |
| 10 | Finance | Financial Management | — | — | — | — | Not started |
| 11 | Planning | Business Planning | — | — | — | — | Not started |
| 12 | Reporting | Cross-cutting | — | — | — | — | Not started |
| 13 | Admin | Cross-cutting | — | — | — | — | Not started |
