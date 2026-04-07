# Warehouse Microservices Plan — ISA-95 Aligned

> Created: 2026-04-02
> Updated: 2026-04-06
> Type: Greenfield — .NET 8 Microservices + Vue.js Frontend (Monorepo)
> Database: Single source of truth (SQL Server)
> Standard: ISA-95 / IEC 62264
> External integrations: ERP (planned, ISA-95 Part 4)
> Status: Phase 1 Complete, Phase 2 Planning

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

### Phase 1.5 — Infrastructure Prerequisites (NEXT — before Phase 2)

These cross-cutting infrastructure capabilities MUST be in place before adding inter-service communication in Phase 2. Without them, debugging and operating 5+ services becomes unmanageable.

#### API Gateway — `Warehouse.Gateway`
**Technology:** YARP (Yet Another Reverse Proxy)
**Port:** 5000
**Priority:** P1

| Feature | Description |
|---|---|
| **Reverse proxy** | Route `/api/v1/auth/*` → Auth:5001, `/api/v1/customers/*` → Customers:5002, etc. |
| **Rate limiting** | ASP.NET Core RateLimiting middleware — fixed/sliding window per client/IP |
| **Response aggregation** | Optional — combine calls to multiple services into single response |
| **Authentication passthrough** | Forward JWT tokens to downstream services |
| **Health aggregation** | Aggregate health status from all downstream services |

**Rationale:** The Vue SPA currently hits 3 different ports via Vite proxy. With 5+ services this is unsustainable. The gateway provides a single URL, centralizes rate limiting, and enables response aggregation.

---

#### Correlation ID Middleware
**Location:** `Warehouse.Infrastructure`
**Priority:** P1

| Feature | Description |
|---|---|
| **Generate** | Create `X-Correlation-ID` (GUID) if not present on incoming request |
| **Propagate** | Inject correlation ID into all outbound `HttpClient` requests |
| **Log enrichment** | Add `CorrelationId` to NLog structured log context |
| **Response header** | Return `X-Correlation-ID` in response for frontend debugging |

---

#### Centralized Logging
**Technology:** Seq (development) or ELK stack (production)
**Priority:** P1

| Feature | Description |
|---|---|
| **NLog sink** | Add Seq/Elasticsearch NLog target alongside existing file target |
| **Structured fields** | All log entries include `CorrelationId`, `ServiceName`, `UserId`, `RequestPath` |
| **Dashboard** | Seq UI or Kibana for log search, filtering, and alerting |
| **Retention** | Configure log retention policies per environment |

**Rationale:** Current NLog writes per-process log files. With multiple service instances, cross-service request debugging requires a centralized sink with correlation ID filtering.

---

#### Distributed Tracing
**Technology:** OpenTelemetry SDK → Jaeger or Zipkin
**Priority:** P1

| Feature | Description |
|---|---|
| **Auto-instrumentation** | ASP.NET Core, HttpClient, EF Core, SQL Client |
| **Custom spans** | Business-critical operations (adjustment apply, transfer complete, goods receiving) |
| **Trace propagation** | W3C TraceContext headers across inter-service HTTP calls |
| **Export** | OTLP exporter → Jaeger (dev) or cloud-hosted collector (prod) |

**Rationale:** Without distributed tracing, debugging a request that spans Auth → Purchasing → Inventory (e.g., goods receiving) requires manually correlating logs across 3 services. OpenTelemetry provides end-to-end request visibility.

---

#### Polly Resilience Policies
**Technology:** Polly 8.x / Microsoft.Extensions.Http.Resilience
**Location:** `Warehouse.Infrastructure`
**Priority:** P1

| Policy | Configuration |
|---|---|
| **Retry** | Exponential backoff, 3 attempts, jitter |
| **Circuit breaker** | Break after 5 consecutive failures, 30s recovery |
| **Timeout** | 10s per-request timeout |
| **Bulkhead** | Max concurrent calls per downstream service |

All inter-service `HttpClient` registrations MUST include the standard Polly policy pipeline. Shared via `Warehouse.Infrastructure` extension methods.

---

#### Distributed Cache — Redis
**Technology:** Redis (StackExchange.Redis / Microsoft.Extensions.Caching.StackExchangeRedis)
**Priority:** P1

| Feature | Description |
|---|---|
| **Response caching** | Cache frequently-read, rarely-changed data: product catalog, permission lookups, UoM lists, customer categories |
| **Rate limiting backend** | Redis-backed sliding window counters for gateway rate limiting (shared state across gateway instances) |
| **Session / token store** | Optional: faster refresh token validation than SQL round-trips |
| **Pub/sub** | Real-time cache invalidation across service instances (e.g., permission change broadcasts) |
| **Distributed locks** | Prevent concurrent operations on the same resource (e.g., two concurrent adjustment applies on the same stock level) |

**Rationale:** Without a distributed cache, every permission check and product lookup hits SQL Server. As service count and traffic grow, this becomes a bottleneck. Redis provides sub-millisecond reads, shared state for rate limiting, and pub/sub for cache invalidation.

**Integration pattern:**
- Add `IDistributedCache` registration via `Warehouse.Infrastructure` shared extension methods
- Services use `IDistributedCache` (not `IConnectionMultiplexer` directly) for standard caching
- Use `IConnectionMultiplexer` only for pub/sub and distributed locks
- Cache keys follow convention: `{service}:{entity}:{id}` (e.g., `auth:permissions:user:42`)
- TTL defaults: permission lookups 5 min, product catalog 15 min, UoM/categories 30 min

---

#### Message Broker — RabbitMQ
**Technology:** RabbitMQ (MassTransit as the abstraction layer)
**Priority:** P1 (Phase 2 — before first inter-service workflow)

| Feature | Description |
|---|---|
| **Domain events** | Publish events when operations complete: `GoodsReceived`, `OrderDispatched`, `StockAdjusted`, `TransferCompleted` |
| **Event consumers** | Services subscribe to events they care about (e.g., Inventory consumes `GoodsReceived` from Purchasing to create stock movements) |
| **Saga / state machine** | MassTransit sagas for multi-step workflows (e.g., goods receiving: validate PO → create batch → record stock movement → update PO status) |
| **Retry / dead letter** | Automatic retry with exponential backoff; dead-letter queue for poison messages |
| **Outbox pattern** | Transactional outbox to guarantee event publication alongside database writes (no dual-write problem) |

**Rationale:** Phase 2 introduces the first inter-service workflows. Goods receiving in Purchasing needs to create stock movements in Inventory. Fulfillment dispatching needs to deduct stock. Doing all of this synchronously with HTTP creates tight coupling, cascading failures, and long request chains. RabbitMQ decouples producers from consumers, enables retry/dead-letter handling, and allows services to evolve independently.

**Event naming convention:** `{Domain}.{Entity}.{PastTenseVerb}` (e.g., `Purchasing.GoodsReceipt.Completed`, `Fulfillment.Shipment.Dispatched`, `Inventory.Stock.Adjusted`)

**Integration pattern:**
- MassTransit as the abstraction layer (supports RabbitMQ, Azure Service Bus, Amazon SQS — swap without code changes)
- Each service has its own exchange and queue (auto-configured by MassTransit)
- Event contracts defined in `Warehouse.ServiceModel/Events/` (shared across services)
- Transactional outbox via MassTransit + EF Core (`AddEntityFrameworkOutbox`)
- Consumers registered in each service's `Program.cs` via MassTransit DI

**Key Phase 2 event flows:**
```
Purchasing: GoodsReceipt.Completed
  → Inventory: Create StockMovement (Receipt) + Create/Update Batch
  → Quality: Create Inspection Order (if inspection required)

Fulfillment: Shipment.Dispatched
  → Inventory: Create StockMovement (Shipment) + Release reservation

Inventory: Stock.BelowReorderPoint
  → Planning: Suggest Purchase Order (if auto-reorder enabled)
```

---

#### Feature Flags
**Technology:** Microsoft.FeatureManagement
**Priority:** P2

| Feature | Description |
|---|---|
| **Flag definition** | `appsettings.json` feature flag section per service |
| **Percentage rollout** | Gradual feature enablement by percentage |
| **User targeting** | Enable features for specific users/roles |
| **Kill switch** | Disable features instantly without redeployment |

**Rationale:** Useful for gradual rollout of Phase 2 features (e.g., enable goods receiving for one warehouse before all).

---

### Phase 2 — Procurement & Fulfillment Operations

#### 4. Warehouse.Purchasing.API — Procurement Operations
**ISA-95 Domain:** Procurement Operations (Part 3 — Material Receipt)
**Schema:** `purchasing`
**Port:** 5004
**Depends on:** Auth, Inventory, Customers
**Priority:** P1

| Feature | ISA-95 Activity | Description |
|---|---|---|
| **Supplier management** | Business Partner (supplier) | Supplier CRUD, categories, contacts, terms |
| **Purchase orders** | Procurement Request | PO CRUD with status machine (Draft → Confirmed → Partially Received → Received → Closed) |
| **PO lines** | Procurement Request detail | Line items referencing products, quantities, pricing |
| **Goods receiving** | Material Receipt | Receive items against PO lines; auto-create Batch + StockMovement (reason: `Receipt`) |
| **Receiving inspection** | Quality Test (basic) | Accept/reject/quarantine received goods; flag for Quality service when available |
| **Supplier returns** | Material Shipment (return) | Return defective goods; create StockMovement (reason: `SupplierReturn`) |
| **Purchase history** | Operations Event log | Immutable record of all procurement events |

**Key ISA-95 integration points:**
- Goods receiving MUST create immutable `StockMovement` records with reason code `Receipt`
- Goods receiving MUST auto-create `Batch` records, connecting Material Lot to the receipt event
- This resolves the current gap where batches are disconnected from arrivals
- Supplier returns MUST create `StockMovement` records with reason code `SupplierReturn`

**Frontend scope:**
- Supplier list/detail/form views
- Purchase order list/detail/form with line items
- Goods receiving workflow (scan/enter received quantities per PO line)
- Receiving inspection checklist

---

#### 5. Warehouse.Fulfillment.API — Fulfillment Operations
**ISA-95 Domain:** Fulfillment Operations (Part 3 — Material Shipment)
**Schema:** `fulfillment`
**Port:** 5005
**Depends on:** Auth, Inventory, Customers
**Priority:** P1

| Feature | ISA-95 Activity | Description |
|---|---|---|
| **Sales orders** | Fulfillment Request | SO CRUD with status machine (Draft → Confirmed → Picking → Packed → Shipped → Completed) |
| **SO lines** | Fulfillment Request detail | Line items referencing products, quantities, pricing |
| **Pick lists** | Material Movement (outbound) | Generate pick instructions from confirmed orders |
| **Packing** | Material Movement (staging) | Pack picked items into parcels; record parcel weight |
| **Dispatch** | Material Shipment | Create shipment from packed order; create StockMovement (reason: `Shipment`) |
| **Shipment tracking** | Operations Event | Track shipment status and carrier updates |
| **Carrier management** | Equipment definition (logistics) | Carrier definitions, service levels, rate tables |
| **Customer returns** | Material Receipt (return) | RMA intake; create StockMovement (reason: `CustomerReturn`) |
| **Labels & printing** | Operations documentation | Label templates, ZPL/Zebra printing, QR codes, barcodes |
| **Dispatch documents** | Operations documentation | Generate packing slips, delivery notes |

**Key ISA-95 integration points:**
- Dispatch MUST create immutable `StockMovement` records with reason code `Shipment`
- Pick lists MUST reserve stock (update `QuantityReserved` on `StockLevel`)
- Customer returns MUST create `StockMovement` records with reason code `CustomerReturn`
- All state transitions MUST produce immutable operations events

**Frontend scope:**
- Sales order list/detail/form with line items
- Pick list generation and execution UI
- Packing workflow (assign items to parcels)
- Shipment tracking dashboard
- Carrier management
- Label template designer and print preview

---

### Phase 3 — Production & Quality Operations

#### 6. Warehouse.Production.API — Production Operations
**ISA-95 Domain:** Production Operations (Part 3 — Production Execution)
**Schema:** `production`
**Port:** 5006
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

#### 7. Warehouse.Quality.API — Quality Operations
**ISA-95 Domain:** Quality Operations (Part 3 — Quality Test Execution)
**Schema:** `quality`
**Port:** 5007
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

#### 8. Warehouse.Finance.API — Financial Management
**ISA-95 Level:** Level 4 — Business Planning & Logistics
**Schema:** `finance`
**Port:** 5008
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

#### 9. Warehouse.Planning.API — Business Planning
**ISA-95 Level:** Level 4 — Business Planning & Logistics
**Schema:** `planning`
**Port:** 5009
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

#### 10. Warehouse.Reporting.API — Reports & Dashboards
**ISA-95 Level:** Cross-cutting (reads from all levels)
**Schema:** Read-only access to all schemas
**Port:** 5010
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

#### 11. Warehouse.Admin.API — System Administration
**ISA-95 Level:** Cross-cutting
**Schema:** `admin`
**Port:** 5011
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
| — | **Infrastructure** | Cross-cutting | — | — | P1 | — | Partial (I1, I2 done; I3–I5 blocked) |
| 4 | **Purchasing** | Procurement Operations | L3 | 5004 | P1 | Auth, Inventory, Customers | Not started |
| 5 | **Fulfillment** | Fulfillment Operations | L3 | 5005 | P1 | Auth, Inventory, Customers | Not started |
| 6 | **Production** | Production Operations | L3 | 5006 | P2 | Auth, Inventory | Not started |
| 7 | **Quality** | Quality Operations | L3 | 5007 | P2 | Auth, Inventory | Not started |
| 8 | **Finance** | Financial Management | L4 | 5008 | P2 | Auth, Customers, Purchasing, Fulfillment | Not started |
| 9 | **Planning** | Business Planning | L4 | 5009 | P3 | Auth, Inventory, Purchasing, Fulfillment, Production | Not started |
| 10 | **Reporting** | Cross-cutting | — | 5010 | P3 | Auth, all (read) | Not started |
| 11 | **Admin** | Cross-cutting | — | 5011 | P3 | Auth | Not started |

### Infrastructure Components (Phase 1.5)

| # | Component | Technology | Priority | Status |
|---|---|---|---|---|
| I1 | **API Gateway** | YARP 2.3.0 | P1 | **Done** — port 5000, 22 routes, health aggregation, rate limiting (200 req/min per IP) |
| I2 | **Correlation IDs** | Custom middleware (Warehouse.Infrastructure) | P1 | **Done** — generate/propagate X-Correlation-ID, NLog enrichment |
| I3 | **Centralized Logging** | NLog → Loki 3.4.2 + Grafana 11.5.2 | P1 | **Done** — all services push to Loki; Grafana at localhost:3001 |
| I4 | **Distributed Tracing** | OpenTelemetry → Jaeger | P1 | **Done** — auto-instrumentation (ASP.NET Core, HttpClient, SQL); Jaeger at localhost:16686 |
| I5 | **Polly Resilience** | Microsoft.Extensions.Http.Resilience 9.x | P1 | **Ready** — `AddWarehouseHttpClient<T,TImpl>()` available; no inter-service HttpClients yet (Phase 2) |
| I6 | **Distributed Cache** | Redis 7.4 (StackExchange.Redis) | P1 | **Done** — caching permissions, categories, UoM in Auth/Customers/Inventory |
| I7 | **Message Broker** | RabbitMQ 4.1 (MassTransit 8.x) | P1 | **Done** — 4 domain events published (StockMovement, Adjustment, Transfer, Customer); management UI at localhost:15672 |
| I8 | **Rate Limiting** | ASP.NET Core RateLimiting (at gateway) | P2 | **Done** — global per-IP (200/min) + named "fixed" policy (100/min) |
| I9 | **Feature Flags** | Microsoft.FeatureManagement 4.x | P2 | **Done** — gates database seeding in Auth.API; `FeatureFlags.cs` constants in Infrastructure |

---

## Monorepo Structure (target)

```
Warehouse/
├── CLAUDE.md
├── README.md
├── docs/
│   ├── core/               ← Inventory, Purchasing, Fulfillment, Production specs
│   ├── domain/             ← Customers, Quality specs
│   ├── infrastructure/     ← Auth, Finance, Planning, Reporting, Admin specs
│   └── changes/
├── frontend/               ← Vue.js 3 SPA
│   └── src/
│       ├── app/            ← Shell, router
│       ├── features/       ← auth, customers, inventory, purchasing, fulfillment, production, quality, finance, planning, admin
│       └── shared/         ← Components, stores, i18n
└── src/
    ├── Warehouse.slnx
    ├── Gateway/
    │   └── Warehouse.Gateway/             ← YARP reverse proxy (port 5000)
    ├── Databases/
    │   ├── Warehouse.Auth.DBModel/
    │   ├── Warehouse.Customers.DBModel/
    │   ├── Warehouse.Inventory.DBModel/
    │   ├── Warehouse.Purchasing.DBModel/
    │   ├── Warehouse.Fulfillment.DBModel/
    │   ├── Warehouse.Production.DBModel/
    │   ├── Warehouse.Quality.DBModel/
    │   ├── Warehouse.Finance.DBModel/
    │   └── Warehouse.Planning.DBModel/
    ├── Interfaces/
    │   ├── Auth/
    │   │   ├── Warehouse.Auth.API/
    │   │   └── Warehouse.Auth.API.Tests/
    │   ├── Customers/
    │   │   ├── Warehouse.Customers.API/
    │   │   └── Warehouse.Customers.API.Tests/
    │   ├── Inventory/
    │   │   ├── Warehouse.Inventory.API/
    │   │   └── Warehouse.Inventory.API.Tests/
    │   ├── Purchasing/
    │   │   ├── Warehouse.Purchasing.API/
    │   │   └── Warehouse.Purchasing.API.Tests/
    │   ├── Fulfillment/
    │   │   ├── Warehouse.Fulfillment.API/
    │   │   └── Warehouse.Fulfillment.API.Tests/
    │   ├── Production/
    │   │   ├── Warehouse.Production.API/
    │   │   └── Warehouse.Production.API.Tests/
    │   ├── Quality/
    │   │   ├── Warehouse.Quality.API/
    │   │   └── Warehouse.Quality.API.Tests/
    │   ├── Finance/
    │   │   ├── Warehouse.Finance.API/
    │   │   └── Warehouse.Finance.API.Tests/
    │   ├── Planning/
    │   │   ├── Warehouse.Planning.API/
    │   │   └── Warehouse.Planning.API.Tests/
    │   ├── Reporting/
    │   │   ├── Warehouse.Reporting.API/
    │   │   └── Warehouse.Reporting.API.Tests/
    │   └── Admin/
    │       ├── Warehouse.Admin.API/
    │       └── Warehouse.Admin.API.Tests/
    ├── Warehouse.Common/
    ├── Warehouse.Infrastructure/
    ├── Warehouse.GenericFiltering/
    ├── Warehouse.Mapping/
    └── Warehouse.ServiceModel/
```

---

## Phase 2 Deferred Items

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
| Document Redis caching behavior in specs | Both | P3 |
| Update spec status from Draft to Active | Both | P3 |

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
| 1 | Auth | Personnel & Authorization | SDD-AUTH-001 | Done | Partial | — | **Implemented** |
| 2 | Customers | Business Partner Management | SDD-CUST-001 | Done | Partial | — | **Implemented** |
| 3 | Inventory | Inventory Operations | SDD-INV-001..004 | Done | Partial | — | **Implemented** |
| I1 | Gateway (YARP) | Cross-cutting | — | Done | — | — | **Implemented** |
| I2 | Correlation IDs | Cross-cutting | — | Done | — | — | **Implemented** |
| I3 | Centralized Logging | Cross-cutting | — | Done | — | — | **Implemented** (Loki + Grafana) |
| I4 | Distributed Tracing | Cross-cutting | — | Done | — | — | **Implemented** (OpenTelemetry → Jaeger) |
| I5 | Polly Resilience | Cross-cutting | — | Done | — | — | **Ready** (extension method; no consumers until Phase 2) |
| I6 | Redis (Distributed Cache) | Cross-cutting | — | Done | — | — | **Implemented** (caching in Auth, Customers, Inventory) |
| I7 | RabbitMQ (Message Broker) | Cross-cutting | — | Done | — | — | **Implemented** (4 events published from Customers, Inventory) |
| I8 | Rate Limiting | Cross-cutting | — | Done | — | — | **Implemented** (per-IP on Gateway) |
| I9 | Feature Flags | Cross-cutting | — | Done | — | — | **Implemented** (gates Auth seeder) |
| 4 | Purchasing | Procurement Operations | SDD-PURCH-001 | Done | 80 unit | Validated | **Implemented** |
| 5 | Fulfillment | Fulfillment Operations | SDD-FULF-001 | Done | 128 unit | Validated | **Implemented** |
| 6 | Production | Production Operations | — | — | — | — | Not started |
| 7 | Quality | Quality Operations | — | — | — | — | Not started |
| 8 | Finance | Financial Management | — | — | — | — | Not started |
| 9 | Planning | Business Planning | — | — | — | — | Not started |
| 10 | Reporting | Cross-cutting | — | — | — | — | Not started |
| 11 | Admin | Cross-cutting | — | — | — | — | Not started |
