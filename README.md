# Warehouse Management System

A WMS built with .NET 8 microservices and a Vue.js 3 SPA frontend, designed to conform to the **ISA-95 (IEC 62264)** standard for enterprise-operations integration.

## Architecture

- **Backend:** .NET 8 REST API microservices (schema-per-service on shared SQL Server)
- **Frontend:** Vue.js 3 SPA with Vuetify, TypeScript, Pinia, Vue Router, vue-i18n (EN/BG)
- **Auth:** JWT (HMAC-SHA256) with refresh token rotation, RBAC via permissions
- **Database:** SQL Server / LocalDB, single database, schema-per-service isolation (`auth`, `customers`, `inventory`, `purchasing`, `fulfillment`, `eventlog`, `nomenclature`)
- **ORM:** EF Core 8 with separate DbContext per domain
- **Validation:** FluentValidation
- **Mapping:** AutoMapper
- **Logging:** NLog with structured message templates
- **API Versioning:** URL-based (`/api/v1/`)
- **Standard:** ISA-95 / IEC 62264 (Level 3 — Inventory Operations Management)

## Services

| Service | Port | Schema | Domain (ISA-95) | Status |
|---|---|---|---|---|
| Gateway (YARP) | 5000 | — | Cross-cutting (47 routes) | Implemented |
| Auth API | 5001 | `auth` | Personnel & Authorization | Implemented |
| Customers API | 5002 | `customers` | Business Partner Management | Implemented |
| Inventory API | 5003 | `inventory` | Inventory Operations | Implemented |
| Purchasing API | 5004 | `purchasing` | Procurement Operations | Implemented |
| Fulfillment API | 5005 | `fulfillment` | Fulfillment Operations | Implemented |
| EventLog API | 5006 | `eventlog` | Cross-cutting (event aggregation) | Implemented |
| Nomenclature API | 5007 | `nomenclature` | Cross-cutting (reference data) | Implemented |
| Frontend SPA | 3000 | — | — | Implemented |
| Production API | 5008 | — | Production Operations | Planned |
| Quality API | 5009 | — | Quality Operations | Planned |

## Solution Structure

```
Warehouse/
├── docker-compose.infrastructure.yml        Docker Compose (15 containers)
├── docker/                                  Docker configs (loki, grafana, nginx)
├── docs/                                    SDD specifications & change specs
│   ├── core/                                Core business pipeline specs (10 specs)
│   ├── domain/                              Domain capability specs (4 specs)
│   ├── infrastructure/                      Cross-cutting concern specs (7 specs)
│   └── changes/                             Change specs (20 specs)
├── frontend/                                Vue.js 3 SPA
│   └── src/
│       ├── app/                             App shell (main.ts, router)
│       ├── features/                        Feature domains (auth, customers, inventory, purchasing, fulfillment)
│       └── shared/                          Shared components, stores, i18n (EN/BG)
└── src/
    ├── Warehouse.slnx                       .NET 8 solution
    ├── Databases/
    │   ├── Warehouse.Auth.DBModel/          Auth EF Core entities & migrations
    │   ├── Warehouse.Customers.DBModel/     Customers EF Core entities & migrations
    │   ├── Warehouse.Inventory.DBModel/     Inventory EF Core entities & migrations
    │   ├── Warehouse.Purchasing.DBModel/    Purchasing EF Core entities & migrations
    │   ├── Warehouse.Fulfillment.DBModel/   Fulfillment EF Core entities & migrations
    │   ├── Warehouse.EventLog.DBModel/      EventLog EF Core entities & migrations
    │   └── Warehouse.Nomenclature.DBModel/  Nomenclature EF Core entities & migrations
    ├── Infrastructure/
    │   ├── Gateway/
    │   │   └── Warehouse.Gateway/           YARP reverse proxy (port 5000, 47 routes)
    │   └── EventLog/
    │       ├── Warehouse.EventLog.API/      MassTransit consumers, event queries (port 5006)
    │       └── Warehouse.EventLog.API.Tests/
    ├── Interfaces/
    │   ├── Auth/
    │   │   ├── Warehouse.Auth.API/          Auth controllers, services, middleware (port 5001)
    │   │   └── Warehouse.Auth.API.Tests/
    │   ├── Customers/
    │   │   ├── Warehouse.Customers.API/     Customers controllers & services (port 5002)
    │   │   └── Warehouse.Customers.API.Tests/
    │   ├── Inventory/
    │   │   ├── Warehouse.Inventory.API/     Inventory controllers & services (port 5003)
    │   │   └── Warehouse.Inventory.API.Tests/
    │   ├── Purchasing/
    │   │   ├── Warehouse.Purchasing.API/    Purchasing controllers & services (port 5004)
    │   │   └── Warehouse.Purchasing.API.Tests/
    │   ├── Fulfillment/
    │   │   ├── Warehouse.Fulfillment.API/   Fulfillment controllers & services (port 5005)
    │   │   └── Warehouse.Fulfillment.API.Tests/
    │   └── Nomenclature/
    │       ├── Warehouse.Nomenclature.API/  Nomenclature controllers & services (port 5007)
    │       └── Warehouse.Nomenclature.API.Tests/
    ├── Warehouse.Common/                    Shared enums, helpers, Result pattern
    ├── Warehouse.Infrastructure/            Shared middleware, auth, health checks, sequences
    ├── Warehouse.GenericFiltering/          Dynamic IQueryable filtering
    ├── Warehouse.Mapping/                   AutoMapper profiles
    └── Warehouse.ServiceModel/              DTOs, requests, responses, event contracts
```

## Getting Started

### Prerequisites

- Docker & Docker Compose
- (Optional for local dev) .NET 8 SDK, Node.js 18+, `dotnet-ef` global tool

### Running with Docker (recommended)

```bash
docker compose -f docker-compose.infrastructure.yml up -d
```

This starts all 15 containers (7 APIs + gateway + frontend + 6 infrastructure services).

| Service | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API Gateway | http://localhost:5000 |
| Auth API | http://localhost:5001 |
| Customers API | http://localhost:5002 |
| Inventory API | http://localhost:5003 |
| Purchasing API | http://localhost:5004 |
| Fulfillment API | http://localhost:5005 |
| EventLog API | http://localhost:5006 |
| Nomenclature API | http://localhost:5007 |
| Grafana | http://localhost:3001 (admin/admin) |
| Jaeger | http://localhost:16686 |
| RabbitMQ | http://localhost:15672 (warehouse/warehouse) |
| Swagger (per service) | http://localhost:{port}/swagger |

### Default Admin Credentials

The Auth API seeds a default admin user on first startup:

- **Username:** `admin`
- **Password:** `Admin123!`

## Domain Overview

### Auth (7 entities)
Users, Roles, Permissions (RBAC), refresh tokens, audit trail.

### Customers (6 entities)
Customers with categories, multi-currency accounts, addresses, phones, emails.

### Inventory (19 entities)
- **Catalog:** Products, categories, units of measure, BOM, accessories, substitutes
- **Warehouse:** Warehouses, zones, storage locations
- **Stock:** Stock levels, movements, batches
- **Operations:** Inventory adjustments (approval workflow), warehouse transfers, stocktake sessions

### Purchasing (14 entities)
- **Suppliers:** Suppliers, categories, addresses, phones, emails
- **Purchase Orders:** PO header + lines with state machine (Draft → Confirmed → PartiallyReceived → Received → Closed)
- **Goods Receiving:** Receipt header + lines with inspection (accept/reject/quarantine), auto-batch creation
- **Supplier Returns:** Return header + lines with state machine (Draft → Confirmed → Shipped → Completed)
- **Audit:** Immutable purchase event log

### Fulfillment (14 entities)
- **Sales Orders:** SO header + lines with state machine (Draft → Confirmed → Picking → Packed → Shipped → Completed)
- **Pick Lists:** Pick header + lines generated from confirmed SOs
- **Packing:** Parcels + parcel items for packed goods
- **Shipments:** Shipment header + lines + tracking entries with status history
- **Carriers:** Carrier definitions + service levels
- **Customer Returns:** Return header + lines with state machine (Draft → Confirmed → Received → Closed)
- **Audit:** Immutable fulfillment event log

### EventLog (1 entity)
Centralized immutable operations event log. Consumes domain events from all services via MassTransit.

### Nomenclature (4 entities)
Reference data: Countries, State/Provinces, Cities, Currencies. Seeded on startup. Provides cascading dropdowns for address forms.

## Configuration

All `appsettings.json` files are gitignored. Only `.template` files are tracked. Copy and fill in real values locally. Never commit connection strings, passwords, or API keys.

## Infrastructure (all implemented)

| # | Concern | Technology | Status |
|---|---|---|---|
| 1 | **API Gateway** | YARP 2.3.0 (port 5000, 47 routes) | Done |
| 2 | **Health Checks** | AspNetCore.Diagnostics.HealthChecks (liveness + readiness per service) | Done |
| 3 | **Correlation IDs** | Custom middleware — X-Correlation-ID generation, propagation, NLog enrichment | Done |
| 4 | **Centralized Logging** | NLog → Loki 3.4.2 + Grafana 11.5.2 | Done |
| 5 | **Distributed Tracing** | OpenTelemetry → Jaeger (ASP.NET Core, HttpClient, SQL auto-instrumentation) | Done |
| 6 | **Resilience** | Microsoft.Extensions.Http.Resilience 9.x (extension method ready for inter-service calls) | Ready |
| 7 | **API Documentation** | Swashbuckle.AspNetCore (Swagger per service with JWT Bearer) | Done |
| 8 | **Distributed Cache** | Redis 7.4 (caching permissions, categories, UoM across services) | Done |
| 9 | **Message Broker** | RabbitMQ 4.1 + MassTransit 8.x (10 domain events, 5 consumers) | Done |
| 10 | **Rate Limiting** | ASP.NET Core RateLimiting at gateway (200 req/min per IP) | Done |
| 11 | **Feature Flags** | Microsoft.FeatureManagement 4.x (gates DB seeding in Auth + Nomenclature) | Done |
| 12 | **Sequence Generation** | Centralized number generation (12 sequence definitions) | Done |
| 13 | **Centralized Event Log** | EventLog.API — MassTransit consumers for all domain events | Done |

## Documentation

See [`docs/README.md`](docs/README.md) for the full SDD specification registry and documentation conventions.
See [`source/MICROSERVICES-PLAN.md`](source/MICROSERVICES-PLAN.md) for the full service roadmap and ISA-95 domain alignment.
