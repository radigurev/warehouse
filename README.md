# Warehouse

Warehouse Management System — .NET 8 microservices backend with Vue.js frontend.

## Architecture

- **Backend:** .NET 8 REST API microservices (schema-per-service on shared SQL Server)
- **Frontend:** Vue.js SPA (planned)
- **Auth:** Self-issued JWT (access + refresh token rotation)
- **Database:** SQL Server, single database, schema-per-service isolation

## Services

| # | Service | Schema | Status |
|---|---|---|---|
| 1 | Auth | `auth` | Implemented |
| 2 | Customers | `customers` | Planned |
| 3 | Inventory | `inventory` | Planned |
| 4 | Orders | `orders` | Planned |
| 5 | Manufacturing | `manufacturing` | Planned |
| 6 | Finance | `finance` | Planned |
| 7 | Shipping | `shipping` | Planned |
| 8 | Reporting | `reporting` | Planned |
| 9 | Admin | `admin` | Planned |

## Solution Structure

```
Warehouse/
├── docs/                              SDD specifications
├── frontend/                          Vue.js SPA (planned)
├── source/                            Reference documentation
└── src/
    ├── Warehouse.slnx                 .NET solution
    ├── Databases/
    │   └── Warehouse.DBModel/         Shared EF Core entities and DbContext
    ├── Interfaces/
    │   ├── Warehouse.Auth.API/        Auth microservice
    │   └── Warehouse.Auth.API.Tests/  Auth tests
    ├── Warehouse.Common/              Shared enums, helpers, Result pattern
    ├── Warehouse.GenericFiltering/    Dynamic IQueryable filtering
    ├── Warehouse.Mapping/             AutoMapper profiles
    └── Warehouse.ServiceModel/        DTOs, requests, responses
```

## Getting Started

1. Clone the repo
2. Copy `appsettings.json.template` to `appsettings.json` and fill in your connection string and JWT secret
3. Run migrations or create the database
4. `dotnet run --project src/Interfaces/Warehouse.Auth.API`
5. Swagger UI at `http://localhost:5206/swagger`

## Configuration

All `appsettings.json` files are gitignored. Only `.template` files are tracked. Copy and fill in real values locally.
