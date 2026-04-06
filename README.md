# Warehouse Management System

A WMS built with .NET 8 microservices and a Vue.js 3 SPA frontend, designed to conform to the **ISA-95 (IEC 62264)** standard for enterprise-operations integration.

## Architecture

- **Backend:** .NET 8 REST API microservices (schema-per-service on shared SQL Server)
- **Frontend:** Vue.js 3 SPA with Vuetify, TypeScript, Pinia, Vue Router, vue-i18n (EN/BG)
- **Auth:** JWT (HMAC-SHA256) with refresh token rotation, RBAC via permissions
- **Database:** SQL Server / LocalDB, single database, schema-per-service isolation (`auth`, `customers`, `inventory`)
- **ORM:** EF Core 8 with separate DbContext per domain
- **Validation:** FluentValidation
- **Mapping:** AutoMapper
- **Logging:** NLog with structured message templates
- **API Versioning:** URL-based (`/api/v1/`)
- **Standard:** ISA-95 / IEC 62264 (Level 3 — Inventory Operations Management)

## Services

| Service | Port | Schema | Domain (ISA-95) | Status |
|---|---|---|---|---|
| Auth API | 5001 | `auth` | Personnel & Authorization | Implemented |
| Customers API | 5002 | `customers` | Business Partner Management | Implemented |
| Inventory API | 5003 | `inventory` | Inventory Operations | Implemented |
| Frontend SPA | 3000 | — | — | Implemented |
| Purchasing API | TBD | TBD | Procurement Operations | Planned |
| Fulfillment API | TBD | TBD | Fulfillment Operations | Planned |
| Quality API | TBD | TBD | Quality Operations | Planned |
| Production API | TBD | TBD | Production Operations | Planned |

## Solution Structure

```
Warehouse/
├── docs/                                    SDD specifications & change specs
│   ├── core/                                Core business pipeline specs
│   ├── domain/                              Domain capability specs
│   ├── infrastructure/                      Cross-cutting concern specs
│   └── changes/                             Proposed change specs
├── frontend/                                Vue.js 3 SPA
│   └── src/
│       ├── app/                             App shell (main.ts, router)
│       ├── features/                        Feature domains (auth, customers, inventory)
│       └── shared/                          Shared components, stores, i18n
└── src/
    ├── Warehouse.slnx                       .NET 8 solution
    ├── Databases/
    │   ├── Warehouse.Auth.DBModel/          Auth EF Core entities & migrations
    │   ├── Warehouse.Customers.DBModel/     Customers EF Core entities & migrations
    │   └── Warehouse.Inventory.DBModel/     Inventory EF Core entities & migrations
    ├── Interfaces/
    │   ├── Auth/
    │   │   ├── Warehouse.Auth.API/          Auth controllers, services, middleware
    │   │   └── Warehouse.Auth.API.Tests/    Auth NUnit tests
    │   ├── Customers/
    │   │   ├── Warehouse.Customers.API/     Customers controllers & services
    │   │   └── Warehouse.Customers.API.Tests/
    │   └── Inventory/
    │       ├── Warehouse.Inventory.API/     Inventory controllers & services
    │       └── Warehouse.Inventory.API.Tests/
    ├── Warehouse.Common/                    Shared enums, helpers, Result pattern
    ├── Warehouse.Infrastructure/            Shared middleware, auth, health checks
    ├── Warehouse.GenericFiltering/          Dynamic IQueryable filtering
    ├── Warehouse.Mapping/                   AutoMapper profiles
    └── Warehouse.ServiceModel/              DTOs, requests, responses
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server or LocalDB
- Node.js 18+
- `dotnet-ef` global tool (`dotnet tool install --global dotnet-ef`)

### Setup

1. Clone the repo
2. Copy `appsettings.json.template` to `appsettings.json` in each API project and fill in your connection string and JWT secret:
   - `src/Interfaces/Auth/Warehouse.Auth.API/`
   - `src/Interfaces/Customers/Warehouse.Customers.API/`
   - `src/Interfaces/Inventory/Warehouse.Inventory.API/`

   Example connection string for LocalDB:
   ```
   Server=(localdb)\MSSQLLocalDB;Database=Warehouse;Trusted_Connection=True;TrustServerCertificate=True;
   ```

3. Apply database migrations:
   ```bash
   cd src
   dotnet ef database update --project Databases/Warehouse.Auth.DBModel --startup-project Interfaces/Auth/Warehouse.Auth.API
   dotnet ef database update --project Databases/Warehouse.Customers.DBModel --startup-project Interfaces/Customers/Warehouse.Customers.API
   dotnet ef database update --project Databases/Warehouse.Inventory.DBModel --startup-project Interfaces/Inventory/Warehouse.Inventory.API
   ```

4. Install frontend dependencies:
   ```bash
   cd frontend
   npm install
   ```

### Running

Start all services:

```bash
# Terminal 1 — Auth API
cd src/Interfaces/Auth/Warehouse.Auth.API && dotnet run

# Terminal 2 — Customers API
cd src/Interfaces/Customers/Warehouse.Customers.API && dotnet run

# Terminal 3 — Inventory API
cd src/Interfaces/Inventory/Warehouse.Inventory.API && dotnet run

# Terminal 4 — Frontend
cd frontend && npm run dev
```

| Service | URL |
|---|---|
| Auth API | http://localhost:5001 |
| Customers API | http://localhost:5002 |
| Inventory API | http://localhost:5003 |
| Frontend | http://localhost:3000 |
| Swagger (Auth) | http://localhost:5001/swagger |
| Swagger (Customers) | http://localhost:5002/swagger |
| Swagger (Inventory) | http://localhost:5003/swagger |

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

## Configuration

All `appsettings.json` files are gitignored. Only `.template` files are tracked. Copy and fill in real values locally. Never commit connection strings, passwords, or API keys.

## Documentation

See [`docs/README.md`](docs/README.md) for the full SDD specification registry and documentation conventions.
