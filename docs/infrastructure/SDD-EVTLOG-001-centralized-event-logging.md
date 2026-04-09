# SDD-EVTLOG-001 — Centralized Event Logging Service

> Status: Implemented
> Last updated: 2026-04-09
> Owner: TBD
> Category: Infrastructure

## 1. Context & Scope

This spec defines a centralized Event Logging microservice that consolidates all operations event records from across the Warehouse system into a single, queryable data store. Currently, operations events are recorded independently in three silos: `auth.UserActionLogs` (Auth domain), `purchasing.PurchaseEvents` (Purchasing domain), and `fulfillment.FulfillmentEvents` (Fulfillment domain). Each silo has its own entity, service, controller, and database table — creating fragmentation in auditability, cross-domain event correlation, and operations visibility.

The Event Log service subscribes to domain event messages via MassTransit/RabbitMQ and persists them using a **Table Per Type (TPT)** inheritance model in EF Core. This provides a unified base table for cross-domain queries while preserving domain-specific columns in dedicated derived tables. The service exposes read-only REST endpoints for querying events across all domains or filtered to a single domain.

**ISA-95 Conformance:** Conforms to ISA-95 Part 2 — Operations Event Model. This service centralizes the Operations Event records from all ISA-95 operations domains (Personnel/Authorization, Procurement, Fulfillment, Inventory, Customer Management) into a single Operations Event Management function. All records are immutable, per ISA-95 Rule 10. The CorrelationId field supports ISA-95 Part 4 traceability by linking related events across domain boundaries.

**Service:** `Warehouse.EventLog.API`
**Schema:** `eventlog`
**Port:** 5006
**DbContext:** `EventLogDbContext`

**In scope:**
- TPT entity hierarchy: base `OperationsEvent` with derived types per domain (Auth, Purchasing, Fulfillment, Inventory, Customers)
- MassTransit consumers that receive domain event messages and persist them as typed event records
- New domain event contracts (`AuthAuditLoggedEvent`, `PurchaseEventOccurredEvent`, `FulfillmentEventOccurredEvent`, `InventoryEventOccurredEvent`, `CustomerEventOccurredEvent`) published by source services
- Read-only REST API for unified cross-domain event querying with domain, event type, entity, user, date range, and correlation ID filters
- Single event detail retrieval with full payload
- Health checks (liveness + readiness: DB + RabbitMQ)
- Correlation ID propagation (events carry the originating request's correlation ID)
- NLog structured logging to Loki (per existing infrastructure)

**Out of scope:**
- Event replay or reprocessing (future enhancement)
- Real-time event streaming to frontend via WebSocket/SSE (future enhancement)
- Event archival or retention policies (future enhancement)
- Dashboard or analytics aggregation (future — Grafana can query the API)
- Migration of historical data from domain-local event tables (separate migration task)
- Removal of domain-local event tables from source services (deferred — dual-write during transition)
- Write endpoints (events are recorded only via MassTransit consumers — no direct POST API)
- Frontend views (covered by `SDD-UI-004`, `SDD-UI-005`, and future `SDD-UI-006`)

**Related specs:**
- `SDD-AUTH-001` — Auth domain publishes `AuthAuditLoggedEvent` for user management operations
- `SDD-PURCH-001` — Purchasing domain publishes `PurchaseEventOccurredEvent` for procurement operations
- `SDD-FULF-001` — Fulfillment domain publishes `FulfillmentEventOccurredEvent` for fulfillment operations
- `SDD-INV-002` — Inventory domain publishes `InventoryEventOccurredEvent` for stock operations
- `SDD-CUST-001` — Customers domain publishes `CustomerEventOccurredEvent` for customer operations
- `SDD-OBS-001` — Observability (NLog, Loki, correlation IDs)
- `SDD-INFRA-001` — Shared infrastructure library (BaseApiController, health checks, MassTransit registration)
- `SDD-INFRA-002` — API Gateway (routes to EventLog service)

---

## 2. Behavior

### 2.1 TPT Entity Hierarchy

The service MUST use EF Core Table Per Type (TPT) inheritance to model operations events.

#### 2.1.1 Base Entity: OperationsEvent

- The base `OperationsEvent` entity MUST contain all columns shared across every domain:
  - `Id` (long, IDENTITY) — unique event identifier
  - `Domain` (string) — ISA-95 operations domain discriminator: `Auth`, `Purchasing`, `Fulfillment`, `Inventory`, `Customers`
  - `EventType` (string) — domain-specific event classification (e.g., `PurchaseOrderCreated`, `UserCreated`, `ShipmentDispatched`)
  - `EntityType` (string) — the entity that was acted upon (e.g., `PurchaseOrder`, `User`, `StockMovement`)
  - `EntityId` (int) — the primary key of the affected entity in its source domain
  - `UserId` (int) — the user who triggered the event (cross-schema reference to `auth.Users`)
  - `OccurredAtUtc` (DateTime) — when the event occurred in the source system
  - `ReceivedAtUtc` (DateTime) — when the EventLog service received and persisted the event
  - `Payload` (string, nullable) — JSON representation of event-specific data (before/after state)
  - `CorrelationId` (string, nullable) — infrastructure correlation ID linking the event to its originating HTTP request
- The base table MUST be named `eventlog.OperationsEvents`.
- All records MUST be immutable — no UPDATE or DELETE operations are permitted on any event table.

#### 2.1.2 Derived Entity: AuthEvent

- Extends `OperationsEvent` with columns specific to the Auth/Personnel domain:
  - `Action` (string) — the auth action performed (e.g., `CreateUser`, `AssignRole`, `Login`, `Logout`)
  - `Resource` (string) — the auth resource acted upon (e.g., `users`, `roles`, `permissions`)
  - `IpAddress` (string, nullable) — the client IP address that initiated the action
  - `Username` (string, nullable) — denormalized username for display without cross-service lookup
- The derived table MUST be named `eventlog.AuthEvents`.
- `Domain` MUST always be `Auth` for records in this table.

#### 2.1.3 Derived Entity: PurchaseEvent

- Extends `OperationsEvent` with columns specific to the Procurement domain:
  - `SupplierName` (string, nullable) — denormalized supplier name for display
  - `DocumentNumber` (string, nullable) — the document reference (PO number, GR number, SR number)
- The derived table MUST be named `eventlog.PurchaseEvents`.
- `Domain` MUST always be `Purchasing` for records in this table.

#### 2.1.4 Derived Entity: FulfillmentEvent

- Extends `OperationsEvent` with columns specific to the Fulfillment domain:
  - `CustomerName` (string, nullable) — denormalized customer name for display
  - `DocumentNumber` (string, nullable) — the document reference (SO number, PL number, SH number, RMA number)
- The derived table MUST be named `eventlog.FulfillmentEvents`.
- `Domain` MUST always be `Fulfillment` for records in this table.

#### 2.1.5 Derived Entity: InventoryEvent

- Extends `OperationsEvent` with columns specific to the Inventory domain:
  - `WarehouseName` (string, nullable) — denormalized warehouse name for display
  - `ProductInfo` (string, nullable) — denormalized product code + name for display
- The derived table MUST be named `eventlog.InventoryEvents`.
- `Domain` MUST always be `Inventory` for records in this table.

#### 2.1.6 Derived Entity: CustomerEvent

- Extends `OperationsEvent` with columns specific to the Customer Management domain:
  - `CustomerName` (string, nullable) — denormalized customer name for display
  - `CustomerCode` (string, nullable) — denormalized customer code for display
- The derived table MUST be named `eventlog.CustomerEvents`.
- `Domain` MUST always be `Customers` for records in this table.

### 2.2 MassTransit Event Contracts

Each source service MUST publish a domain-specific event message when recording an operations event. These are NEW event contracts distinct from the existing business events (e.g., `GoodsReceiptCompletedEvent`). The business events remain unchanged and serve their existing cross-service integration purpose.

#### 2.2.1 AuthAuditLoggedEvent

- Published by `Warehouse.Auth.API` when `AuditService.LogAsync` records an audit entry.
- Contract fields:
  - `UserId` (int?) — user who performed the action
  - `Action` (string) — auth action (e.g., `CreateUser`, `Login`)
  - `Resource` (string) — auth resource (e.g., `users`, `roles`)
  - `Details` (string?) — JSON details of the action
  - `IpAddress` (string?) — client IP
  - `Username` (string?) — denormalized username
  - `OccurredAtUtc` (DateTime) — when the action occurred
  - `CorrelationId` (string?) — correlation ID from the HTTP request

#### 2.2.2 PurchaseEventOccurredEvent

- Published by `Warehouse.Purchasing.API` when `PurchaseEventService.RecordEventAsync` records a purchase event.
- Contract fields:
  - `EventType` (string) — e.g., `PurchaseOrderCreated`, `GoodsReceiptCompleted`
  - `EntityType` (string) — e.g., `PurchaseOrder`, `GoodsReceipt`
  - `EntityId` (int) — source entity PK
  - `UserId` (int) — user who triggered the event
  - `OccurredAtUtc` (DateTime) — when the event occurred
  - `Payload` (string?) — JSON event payload
  - `SupplierName` (string?) — denormalized supplier name
  - `DocumentNumber` (string?) — PO/GR/SR number
  - `CorrelationId` (string?) — correlation ID from the HTTP request

#### 2.2.3 FulfillmentEventOccurredEvent

- Published by `Warehouse.Fulfillment.API` when `FulfillmentEventService.RecordEventAsync` records a fulfillment event.
- Contract fields:
  - `EventType` (string) — e.g., `SalesOrderCreated`, `ShipmentDispatched`
  - `EntityType` (string) — e.g., `SalesOrder`, `Shipment`
  - `EntityId` (int) — source entity PK
  - `UserId` (int) — user who triggered the event
  - `OccurredAtUtc` (DateTime) — when the event occurred
  - `Payload` (string?) — JSON event payload
  - `CustomerName` (string?) — denormalized customer name
  - `DocumentNumber` (string?) — SO/PL/SH/RMA number
  - `CorrelationId` (string?) — correlation ID from the HTTP request

#### 2.2.4 InventoryEventOccurredEvent

- Published by `Warehouse.Inventory.API` when inventory operations events occur (stock movements, adjustments, transfers, stocktake).
- Contract fields:
  - `EventType` (string) — e.g., `StockMovementRecorded`, `AdjustmentApplied`, `TransferCompleted`
  - `EntityType` (string) — e.g., `StockMovement`, `InventoryAdjustment`, `WarehouseTransfer`
  - `EntityId` (int) — source entity PK
  - `UserId` (int) — user who triggered the event
  - `OccurredAtUtc` (DateTime) — when the event occurred
  - `Payload` (string?) — JSON event payload
  - `WarehouseName` (string?) — denormalized warehouse name
  - `ProductInfo` (string?) — denormalized product code + name
  - `CorrelationId` (string?) — correlation ID from the HTTP request

#### 2.2.5 CustomerEventOccurredEvent

- Published by `Warehouse.Customers.API` when customer operations events occur.
- Contract fields:
  - `EventType` (string) — e.g., `CustomerCreated`, `CustomerDeactivated`
  - `EntityType` (string) — e.g., `Customer`, `CustomerAccount`
  - `EntityId` (int) — source entity PK
  - `UserId` (int) — user who triggered the event
  - `OccurredAtUtc` (DateTime) — when the event occurred
  - `Payload` (string?) — JSON event payload
  - `CustomerName` (string?) — denormalized customer name
  - `CustomerCode` (string?) — denormalized customer code
  - `CorrelationId` (string?) — correlation ID from the HTTP request

### 2.3 MassTransit Consumers

The EventLog service MUST register MassTransit consumers for each domain event contract.

#### 2.3.1 Consumer Behavior (all consumers)

- Each consumer MUST create the appropriate derived entity type, populate all fields from the event message, set `ReceivedAtUtc` to the current UTC time, and save to the database.
- Consumers MUST be idempotent — if a duplicate message is received (same Domain + EventType + EntityType + EntityId + OccurredAtUtc), the consumer MUST skip insertion without throwing an error.
- Consumers MUST NOT throw exceptions for persistence failures — they MUST log the error via NLog and allow the message to be acknowledged. Failed events SHOULD be logged at `Error` level with the full event payload for manual recovery.
- Consumers MUST NOT call back to source services to enrich data. All denormalized fields (SupplierName, CustomerName, etc.) MUST be provided in the event message by the publishing service.

#### 2.3.2 Consumer Registration

- The service MUST register all consumers via `services.AddMassTransit(x => { x.AddConsumer<T>(); })` in the DI composition root.
- Each consumer MUST have its own RabbitMQ queue (auto-named by MassTransit convention: `event-log-{event-type}`).
- The service MUST use `AddWarehouseMessageBus(configuration)` from `Warehouse.Infrastructure` for MassTransit configuration.

### 2.4 Source Service Changes

Each source service MUST be modified to publish its domain event message alongside its existing local event recording. This is a **dual-write** strategy during the transition period.

#### 2.4.1 Auth Domain Changes

- `AuditService.LogAsync` MUST publish an `AuthAuditLoggedEvent` via `IPublishEndpoint` after successfully recording to `auth.UserActionLogs`.
- Publishing MUST be fire-and-forget (try/catch with warning log) — RabbitMQ unavailability MUST NOT break audit logging.
- The event MUST include the correlation ID from the current HTTP request context (via `IHttpContextAccessor` or NLog scope).

#### 2.4.2 Purchasing Domain Changes

- `PurchaseEventService.RecordEventAsync` MUST publish a `PurchaseEventOccurredEvent` via `IPublishEndpoint` after successfully recording to `purchasing.PurchaseEvents`.
- The event MUST include denormalized SupplierName and DocumentNumber from the current operation context.
- Publishing MUST be fire-and-forget.

#### 2.4.3 Fulfillment Domain Changes

- `FulfillmentEventService.RecordEventAsync` MUST publish a `FulfillmentEventOccurredEvent` via `IPublishEndpoint` after successfully recording to `fulfillment.FulfillmentEvents`.
- The event MUST include denormalized CustomerName and DocumentNumber.
- Publishing MUST be fire-and-forget.

#### 2.4.4 Inventory Domain Changes

- Inventory operations that change material state (stock movements, adjustments applied, transfers completed, stocktake finalized) MUST publish an `InventoryEventOccurredEvent` via `IPublishEndpoint`.
- The event MUST include denormalized WarehouseName and ProductInfo.
- Publishing MUST be fire-and-forget.

#### 2.4.5 Customers Domain Changes

- `CustomerService` MUST publish a `CustomerEventOccurredEvent` when creating, updating, deactivating, or reactivating a customer.
- The event MUST include denormalized CustomerName and CustomerCode.
- Publishing MUST be fire-and-forget.

### 2.5 Read-Only REST API

The EventLog service MUST expose read-only endpoints for querying the consolidated event log. No write endpoints are exposed — all writes occur via MassTransit consumers.

#### 2.5.1 Search Events (Unified)

- `GET /api/v1/events` MUST return a paginated list of operations events across all domains.
- The endpoint MUST support the following query parameters:
  - `domain` (string, optional) — filter by ISA-95 domain: `Auth`, `Purchasing`, `Fulfillment`, `Inventory`, `Customers`
  - `eventType` (string, optional) — filter by event type (exact match)
  - `entityType` (string, optional) — filter by entity type (exact match)
  - `entityId` (int, optional) — filter by source entity ID (requires `entityType`)
  - `userId` (int, optional) — filter by user who triggered the event
  - `correlationId` (string, optional) — filter by correlation ID (returns all events in a request chain)
  - `dateFrom` (DateTime, optional) — filter events on or after this timestamp
  - `dateTo` (DateTime, optional) — filter events on or before this timestamp
  - `page` (int, default 1) — page number
  - `pageSize` (int, default 25, max 100) — page size
  - `sortBy` (string, default `occurredAtUtc`) — sort field
  - `sortDirection` (string, default `desc`) — sort direction
- The response MUST use `PaginatedResponse<OperationsEventDto>`.
- The default sort MUST be `OccurredAtUtc` descending (newest first).
- When `domain` is specified, the response MUST include domain-specific fields from the derived table.
- When `domain` is not specified, the response MUST include only base fields (no domain-specific columns) for consistent shape.

#### 2.5.2 Get Event by ID

- `GET /api/v1/events/{id}` MUST return a single event by its ID, including all base and domain-specific fields.
- The response MUST include the full `Payload` JSON.
- If the event does not exist, MUST return 404 with ProblemDetails.

#### 2.5.3 Get Event Types

- `GET /api/v1/events/types` MUST return a list of distinct event types, optionally filtered by domain.
- This supports populating filter dropdowns in the frontend.

#### 2.5.4 Get Entity Types

- `GET /api/v1/events/entity-types` MUST return a list of distinct entity types, optionally filtered by domain.
- This supports populating filter dropdowns in the frontend.

#### 2.5.5 Correlation Timeline

- `GET /api/v1/events/correlation/{correlationId}` MUST return all events sharing the given correlation ID, sorted by `OccurredAtUtc` ascending (chronological order).
- This provides a cross-domain timeline of everything that happened during a single user request.

### 2.6 Response DTOs

#### 2.6.1 OperationsEventDto (base)

- `Id` (long)
- `Domain` (string)
- `EventType` (string)
- `EntityType` (string)
- `EntityId` (int)
- `UserId` (int)
- `OccurredAtUtc` (DateTime)
- `ReceivedAtUtc` (DateTime)
- `CorrelationId` (string?)
- `Payload` (string?) — only included in single-event detail, excluded from list responses

#### 2.6.2 AuthEventDto (extends OperationsEventDto)

- `Action` (string)
- `Resource` (string)
- `IpAddress` (string?)
- `Username` (string?)

#### 2.6.3 PurchaseEventDto (extends OperationsEventDto)

- `SupplierName` (string?)
- `DocumentNumber` (string?)

#### 2.6.4 FulfillmentEventDto (extends OperationsEventDto)

- `CustomerName` (string?)
- `DocumentNumber` (string?)

#### 2.6.5 InventoryEventDto (extends OperationsEventDto)

- `WarehouseName` (string?)
- `ProductInfo` (string?)

#### 2.6.6 CustomerEventDto (extends OperationsEventDto)

- `CustomerName` (string?)
- `CustomerCode` (string?)

### 2.7 Immutability

- The service MUST NOT expose any endpoints that modify or delete event records.
- The EF Core DbContext MUST override `SaveChanges` to reject `Modified` and `Deleted` entity states on all event entities. Any attempt MUST throw an `InvalidOperationException`.
- Database-level constraints SHOULD include a trigger or policy to prevent UPDATE/DELETE on event tables (defense in depth).

### 2.8 Health Checks

- The service MUST register liveness (`/health/live`) and readiness (`/health/ready`) health checks.
- Readiness MUST check:
  - SQL Server database connectivity (EventLog database)
  - RabbitMQ connectivity (MassTransit bus health)

---

## 3. Validation Rules

### 3.1 Search Parameters

| # | Field | Rule | Error Code |
|---|---|---|---|
| V1 | domain | Optional. Must be one of: `Auth`, `Purchasing`, `Fulfillment`, `Inventory`, `Customers`. | `INVALID_DOMAIN` |
| V2 | eventType | Optional. Max 100 characters. | `INVALID_EVENT_TYPE` |
| V3 | entityType | Optional. Max 100 characters. | `INVALID_ENTITY_TYPE` |
| V4 | entityId | Optional. Must be > 0 when provided. Requires `entityType`. | `INVALID_ENTITY_ID` |
| V5 | userId | Optional. Must be > 0 when provided. | `INVALID_USER_ID` |
| V6 | correlationId | Optional. Max 36 characters (GUID format). | `INVALID_CORRELATION_ID` |
| V7 | dateFrom | Optional. Must be a valid UTC timestamp. | `INVALID_DATE_FROM` |
| V8 | dateTo | Optional. Must be a valid UTC timestamp. Must be >= dateFrom when both provided. | `INVALID_DATE_TO` |
| V9 | page | Optional. Must be >= 1. Default 1. | `INVALID_PAGE` |
| V10 | pageSize | Optional. Must be 1-100. Default 25. | `INVALID_PAGE_SIZE` |
| V11 | sortBy | Optional. Must be one of: `occurredAtUtc`, `receivedAtUtc`, `eventType`, `entityType`, `domain`. Default `occurredAtUtc`. | `INVALID_SORT_FIELD` |
| V12 | sortDirection | Optional. Must be `asc` or `desc`. Default `desc`. | `INVALID_SORT_DIRECTION` |

### 3.2 Event ID

| # | Field | Rule | Error Code |
|---|---|---|---|
| V13 | id | Required. Must be > 0. | `INVALID_EVENT_ID` |

### 3.3 Correlation ID (path parameter)

| # | Field | Rule | Error Code |
|---|---|---|---|
| V14 | correlationId | Required. Must be a valid GUID string (36 characters). | `INVALID_CORRELATION_ID` |

### 3.4 Consumer Validation (internal)

| # | Field | Rule | Behavior |
|---|---|---|---|
| V15 | EventType | Required in all event messages. | Log error, skip record. |
| V16 | UserId | Required (except Auth where nullable). | Log error, skip record. |
| V17 | OccurredAtUtc | Required. Must not be in the future (tolerance: 5 seconds for clock skew). | Log warning, accept with current UTC if missing. |
| V18 | Duplicate check | Same Domain + EventType + EntityType + EntityId + OccurredAtUtc. | Skip silently (idempotent). |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Event not found | 404 | `EVENT_NOT_FOUND` | Operations event not found. |
| E2 | Invalid domain filter | 400 | `INVALID_DOMAIN` | Domain must be one of: Auth, Purchasing, Fulfillment, Inventory, Customers. |
| E3 | EntityId without EntityType | 400 | `ENTITY_TYPE_REQUIRED` | EntityType is required when filtering by EntityId. |
| E4 | dateFrom > dateTo | 400 | `INVALID_DATE_RANGE` | dateFrom must be on or before dateTo. |
| E5 | Correlation ID not found (no events match) | 200 | — | Returns empty list (not 404 — absence of events is not an error). |
| E6 | Validation failure (field-level) | 400 | `VALIDATION_ERROR` | One or more query parameters are invalid. (ProblemDetails with field errors) |
| E7 | Insufficient permissions | 403 | `FORBIDDEN` | You do not have permission to perform this action. |
| E8 | Unauthenticated | 401 | `UNAUTHORIZED` | Authentication is required. |

All error responses MUST use ProblemDetails (RFC 7807) format.

---

## 5. API Endpoints

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| GET | `/api/v1/events` | Search events across all domains (paginated) | Yes | `events:read` |
| GET | `/api/v1/events/{id}` | Get single event by ID (with full payload) | Yes | `events:read` |
| GET | `/api/v1/events/types` | List distinct event types (optional domain filter) | Yes | `events:read` |
| GET | `/api/v1/events/entity-types` | List distinct entity types (optional domain filter) | Yes | `events:read` |
| GET | `/api/v1/events/correlation/{correlationId}` | Get all events for a correlation ID (timeline) | Yes | `events:read` |
| GET | `/health` | Liveness check | No | — |
| GET | `/health/ready` | Readiness check (DB + RabbitMQ) | No | — |

---

## 6. Database Schema

**Schema name:** `eventlog`

### 6.1 Base Table — `eventlog.OperationsEvents`

| Column | Type | Constraints |
|---|---|---|
| Id | BIGINT | PK, IDENTITY(1,1) |
| Domain | NVARCHAR(50) | NOT NULL |
| EventType | NVARCHAR(100) | NOT NULL |
| EntityType | NVARCHAR(100) | NOT NULL |
| EntityId | INT | NOT NULL |
| UserId | INT | NOT NULL |
| OccurredAtUtc | DATETIME2(7) | NOT NULL |
| ReceivedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| Payload | NVARCHAR(MAX) | NULL |
| CorrelationId | NVARCHAR(36) | NULL |

**Indexes:**

| Name | Columns | Type |
|---|---|---|
| IX_OperationsEvents_Domain_OccurredAt | (Domain, OccurredAtUtc DESC) | Nonclustered |
| IX_OperationsEvents_EventType | (EventType) | Nonclustered |
| IX_OperationsEvents_EntityType_EntityId | (EntityType, EntityId) | Nonclustered |
| IX_OperationsEvents_UserId | (UserId) | Nonclustered |
| IX_OperationsEvents_CorrelationId | (CorrelationId) WHERE CorrelationId IS NOT NULL | Filtered, Nonclustered |
| IX_OperationsEvents_OccurredAtUtc | (OccurredAtUtc DESC) | Nonclustered |
| UQ_OperationsEvents_Dedup | (Domain, EventType, EntityType, EntityId, OccurredAtUtc) | Unique, Nonclustered |

### 6.2 Derived Table — `eventlog.AuthEvents`

| Column | Type | Constraints |
|---|---|---|
| Id | BIGINT | PK, FK -> eventlog.OperationsEvents(Id) |
| Action | NVARCHAR(50) | NOT NULL |
| Resource | NVARCHAR(100) | NOT NULL |
| IpAddress | NVARCHAR(45) | NULL |
| Username | NVARCHAR(50) | NULL |

### 6.3 Derived Table — `eventlog.PurchaseEvents`

| Column | Type | Constraints |
|---|---|---|
| Id | BIGINT | PK, FK -> eventlog.OperationsEvents(Id) |
| SupplierName | NVARCHAR(200) | NULL |
| DocumentNumber | NVARCHAR(50) | NULL |

### 6.4 Derived Table — `eventlog.FulfillmentEvents`

| Column | Type | Constraints |
|---|---|---|
| Id | BIGINT | PK, FK -> eventlog.OperationsEvents(Id) |
| CustomerName | NVARCHAR(200) | NULL |
| DocumentNumber | NVARCHAR(50) | NULL |

### 6.5 Derived Table — `eventlog.InventoryEvents`

| Column | Type | Constraints |
|---|---|---|
| Id | BIGINT | PK, FK -> eventlog.OperationsEvents(Id) |
| WarehouseName | NVARCHAR(200) | NULL |
| ProductInfo | NVARCHAR(300) | NULL |

### 6.6 Derived Table — `eventlog.CustomerEvents`

| Column | Type | Constraints |
|---|---|---|
| Id | BIGINT | PK, FK -> eventlog.OperationsEvents(Id) |
| CustomerName | NVARCHAR(200) | NULL |
| CustomerCode | NVARCHAR(50) | NULL |

### 6.7 EF Core TPT Configuration

```
modelBuilder.Entity<OperationsEvent>().ToTable("OperationsEvents", "eventlog");
modelBuilder.Entity<AuthEvent>().ToTable("AuthEvents", "eventlog");
modelBuilder.Entity<PurchaseEvent>().ToTable("PurchaseEvents", "eventlog");
modelBuilder.Entity<FulfillmentEvent>().ToTable("FulfillmentEvents", "eventlog");
modelBuilder.Entity<InventoryEvent>().ToTable("InventoryEvents", "eventlog");
modelBuilder.Entity<CustomerEvent>().ToTable("CustomerEvents", "eventlog");
```

- EF Core MUST use Fluent API for all table, column, and index configuration — no Data Annotations on the entity classes.
- The `Domain` column MUST NOT be configured as a TPT discriminator — EF Core uses the table name for TPT discrimination. The `Domain` column is an application-level field for query filtering.

---

## 7. Migration Path

### 7.1 Transition Strategy (Dual Write)

1. **Phase A — Deploy EventLog service** with MassTransit consumers. Source services are not yet publishing domain event messages. The EventLog database is empty.
2. **Phase B — Modify source services** to publish domain event messages (fire-and-forget) alongside their existing local event recording. Both the domain-local table and EventLog receive events.
3. **Phase C — Historical data migration** (out of scope for this spec). A one-time migration script copies historical events from `auth.UserActionLogs`, `purchasing.PurchaseEvents`, and `fulfillment.FulfillmentEvents` into `eventlog.*Events` tables.
4. **Phase D — Frontend migration** (out of scope). Update frontend event viewers to consume EventLog API instead of domain-specific APIs.
5. **Phase E — Deprecate domain-local event recording** (out of scope). Remove `RecordEventAsync` calls and domain-local event tables after confidence in the centralized service.

### 7.2 Backward Compatibility

- During Phase B, domain-local event endpoints (`GET /api/v1/purchase-events`, `GET /api/v1/fulfillment-events`, `GET /api/v1/audit`) MUST continue to function unchanged.
- The EventLog service is additive — it does not require changes to existing consumers or event contracts.

---

## 8. Versioning Notes

- **v1 — Initial specification (2026-04-09)**
  - TPT entity hierarchy with 5 domain-specific derived types
  - MassTransit consumers for 5 domain event contracts
  - Read-only REST API with unified search, domain filtering, and correlation timeline
  - Fire-and-forget publishing from source services (dual-write transition)
  - Immutability enforcement at ORM and API levels
  - ISA-95 Operations Event Model alignment

---

## 9. Test Plan

### Unit Tests — MassTransit Consumers

- `AuthAuditLoggedEventConsumer_ValidEvent_PersistsAuthEvent` [Unit]
- `AuthAuditLoggedEventConsumer_SetsReceivedAtUtc` [Unit]
- `AuthAuditLoggedEventConsumer_SetsDomainToAuth` [Unit]
- `AuthAuditLoggedEventConsumer_NullUserId_PersistsWithNullUserId` [Unit]
- `AuthAuditLoggedEventConsumer_DuplicateEvent_SkipsSilently` [Unit]
- `AuthAuditLoggedEventConsumer_PersistenceFailure_LogsErrorAndAcknowledges` [Unit]
- `PurchaseEventOccurredEventConsumer_ValidEvent_PersistsPurchaseEvent` [Unit]
- `PurchaseEventOccurredEventConsumer_IncludesSupplierNameAndDocumentNumber` [Unit]
- `PurchaseEventOccurredEventConsumer_DuplicateEvent_SkipsSilently` [Unit]
- `FulfillmentEventOccurredEventConsumer_ValidEvent_PersistsFulfillmentEvent` [Unit]
- `FulfillmentEventOccurredEventConsumer_IncludesCustomerNameAndDocumentNumber` [Unit]
- `FulfillmentEventOccurredEventConsumer_DuplicateEvent_SkipsSilently` [Unit]
- `InventoryEventOccurredEventConsumer_ValidEvent_PersistsInventoryEvent` [Unit]
- `InventoryEventOccurredEventConsumer_IncludesWarehouseNameAndProductInfo` [Unit]
- `CustomerEventOccurredEventConsumer_ValidEvent_PersistsCustomerEvent` [Unit]
- `CustomerEventOccurredEventConsumer_IncludesCustomerNameAndCode` [Unit]

### Unit Tests — Idempotency

- `consumer_ExactDuplicate_DomainEventTypeEntityTypeEntityIdOccurredAt_Skips` [Unit]
- `consumer_SameEntityDifferentTimestamp_PersistsBoth` [Unit]
- `consumer_SameTimestampDifferentEntity_PersistsBoth` [Unit]

### Unit Tests — Immutability

- `dbContext_AttemptUpdate_ThrowsInvalidOperationException` [Unit]
- `dbContext_AttemptDelete_ThrowsInvalidOperationException` [Unit]
- `dbContext_Insert_Succeeds` [Unit]

### Unit Tests — Event Service (Query)

- `searchEvents_NoFilters_ReturnsAllEventsPaginated` [Unit]
- `searchEvents_FilterByDomain_ReturnsOnlyDomainEvents` [Unit]
- `searchEvents_FilterByEventType_ReturnsMatchingEvents` [Unit]
- `searchEvents_FilterByEntityTypeAndEntityId_ReturnsEntityHistory` [Unit]
- `searchEvents_FilterByUserId_ReturnsUserEvents` [Unit]
- `searchEvents_FilterByCorrelationId_ReturnsCorrelatedEvents` [Unit]
- `searchEvents_FilterByDateRange_ReturnsEventsInRange` [Unit]
- `searchEvents_DefaultSort_OccurredAtUtcDescending` [Unit]
- `searchEvents_SortByEventType_Ascending` [Unit]
- `searchEvents_PaginationPage2_ReturnsCorrectSlice` [Unit]
- `searchEvents_DomainFilter_Auth_ReturnsAuthEventDtoWithIpAddress` [Unit]
- `searchEvents_DomainFilter_Purchasing_ReturnsPurchaseEventDtoWithSupplierName` [Unit]
- `searchEvents_DomainFilter_Fulfillment_ReturnsFulfillmentEventDtoWithCustomerName` [Unit]
- `searchEvents_NoDomainFilter_ReturnsBaseDtoWithoutDomainFields` [Unit]
- `searchEvents_EmptyResult_ReturnsEmptyPagedResponse` [Unit]

### Unit Tests — Get Event by ID

- `getById_ExistingAuthEvent_ReturnsAuthEventDtoWithPayload` [Unit]
- `getById_ExistingPurchaseEvent_ReturnsPurchaseEventDtoWithPayload` [Unit]
- `getById_NonExistentId_Returns404` [Unit]

### Unit Tests — Get Types and Entity Types

- `getTypes_NoDomainFilter_ReturnsAllDistinctTypes` [Unit]
- `getTypes_DomainFilter_ReturnsTypesForDomain` [Unit]
- `getEntityTypes_NoDomainFilter_ReturnsAllDistinctEntityTypes` [Unit]
- `getEntityTypes_DomainFilter_ReturnsEntityTypesForDomain` [Unit]

### Unit Tests — Correlation Timeline

- `correlationTimeline_ValidCorrelationId_ReturnsChronologicalEvents` [Unit]
- `correlationTimeline_NoMatchingEvents_ReturnsEmptyList` [Unit]
- `correlationTimeline_SortsAscendingByOccurredAtUtc` [Unit]

### Unit Tests — Validation

- `searchEvents_InvalidDomain_Returns400` [Unit]
- `searchEvents_EntityIdWithoutEntityType_Returns400` [Unit]
- `searchEvents_DateFromAfterDateTo_Returns400` [Unit]
- `searchEvents_PageSizeExceeds100_Returns400` [Unit]
- `searchEvents_PageZero_Returns400` [Unit]
- `searchEvents_InvalidSortField_Returns400` [Unit]

### Integration Tests — End-to-End Consumer Flow

- `publishAuthAuditEvent_ConsumerPersists_QueryReturnsEvent` [Integration]
- `publishPurchaseEvent_ConsumerPersists_QueryReturnsEvent` [Integration]
- `publishFulfillmentEvent_ConsumerPersists_QueryReturnsEvent` [Integration]
- `publishInventoryEvent_ConsumerPersists_QueryReturnsEvent` [Integration]
- `publishCustomerEvent_ConsumerPersists_QueryReturnsEvent` [Integration]
- `publishMultipleDomainEvents_UnifiedSearch_ReturnsAllDomains` [Integration]
- `publishEventsWithCorrelationId_CorrelationTimeline_ReturnsAll` [Integration]

### Integration Tests — API Endpoints

- `getEvents_Authenticated_Returns200WithPaginatedEvents` [Integration]
- `getEvents_Unauthenticated_Returns401` [Integration]
- `getEvents_Forbidden_Returns403` [Integration]
- `getEventById_ExistingEvent_Returns200WithFullPayload` [Integration]
- `getEventById_NonExistent_Returns404ProblemDetails` [Integration]
- `getTypes_Returns200WithDistinctTypes` [Integration]
- `getEntityTypes_Returns200WithDistinctEntityTypes` [Integration]
- `correlationTimeline_Returns200WithChronologicalEvents` [Integration]

### Integration Tests — TPT Querying

- `tptQuery_AuthDomain_JoinsAuthEventsTable` [Integration]
- `tptQuery_PurchasingDomain_JoinsPurchaseEventsTable` [Integration]
- `tptQuery_NoDomainFilter_QueriesBaseTableOnly` [Integration]
- `tptQuery_MixedDomains_ReturnsCorrectDerivedTypes` [Integration]

### EF/ORM Tests — Schema

- `migration_CreatesOperationsEventsTable_WithAllColumns` [EF]
- `migration_CreatesAuthEventsTable_WithForeignKey` [EF]
- `migration_CreatesPurchaseEventsTable_WithForeignKey` [EF]
- `migration_CreatesFulfillmentEventsTable_WithForeignKey` [EF]
- `migration_CreatesInventoryEventsTable_WithForeignKey` [EF]
- `migration_CreatesCustomerEventsTable_WithForeignKey` [EF]
- `migration_CreatesDeduplicationUniqueIndex` [EF]
- `migration_CreatesCorrelationIdFilteredIndex` [EF]

---

## Key Files

- `src/Infrastructure/EventLog/Warehouse.EventLog.API/Program.cs` (new — DI root, MassTransit registration, health checks)
- `src/Infrastructure/EventLog/Warehouse.EventLog.API/Controllers/EventsController.cs` (new — read-only query endpoints)
- `src/Infrastructure/EventLog/Warehouse.EventLog.API/Services/EventQueryService.cs` (new — search, get, correlation)
- `src/Infrastructure/EventLog/Warehouse.EventLog.API/Services/Interfaces/IEventQueryService.cs` (new — service interface)
- `src/Infrastructure/EventLog/Warehouse.EventLog.API/Consumers/AuthAuditLoggedEventConsumer.cs` (new)
- `src/Infrastructure/EventLog/Warehouse.EventLog.API/Consumers/PurchaseEventOccurredEventConsumer.cs` (new)
- `src/Infrastructure/EventLog/Warehouse.EventLog.API/Consumers/FulfillmentEventOccurredEventConsumer.cs` (new)
- `src/Infrastructure/EventLog/Warehouse.EventLog.API/Consumers/InventoryEventOccurredEventConsumer.cs` (new)
- `src/Infrastructure/EventLog/Warehouse.EventLog.API/Consumers/CustomerEventOccurredEventConsumer.cs` (new)
- `src/Infrastructure/EventLog/Warehouse.EventLog.API/Warehouse.EventLog.API.csproj` (new)
- `src/Infrastructure/EventLog/Warehouse.EventLog.API.Tests/` (new — NUnit test project)
- `src/Databases/Warehouse.EventLog.DBModel/EventLogDbContext.cs` (new — EF Core TPT configuration)
- `src/Databases/Warehouse.EventLog.DBModel/Models/OperationsEvent.cs` (new — base entity)
- `src/Databases/Warehouse.EventLog.DBModel/Models/AuthEvent.cs` (new — derived)
- `src/Databases/Warehouse.EventLog.DBModel/Models/PurchaseEvent.cs` (new — derived)
- `src/Databases/Warehouse.EventLog.DBModel/Models/FulfillmentEvent.cs` (new — derived)
- `src/Databases/Warehouse.EventLog.DBModel/Models/InventoryEvent.cs` (new — derived)
- `src/Databases/Warehouse.EventLog.DBModel/Models/CustomerEvent.cs` (new — derived)
- `src/Databases/Warehouse.EventLog.DBModel/Configuration/` (new — EF Fluent API configs)
- `src/Databases/Warehouse.EventLog.DBModel/Warehouse.EventLog.DBModel.csproj` (new)
- `src/Warehouse.ServiceModel/Events/AuthAuditLoggedEvent.cs` (new — MassTransit contract)
- `src/Warehouse.ServiceModel/Events/PurchaseEventOccurredEvent.cs` (new — MassTransit contract)
- `src/Warehouse.ServiceModel/Events/FulfillmentEventOccurredEvent.cs` (new — MassTransit contract)
- `src/Warehouse.ServiceModel/Events/InventoryEventOccurredEvent.cs` (new — MassTransit contract)
- `src/Warehouse.ServiceModel/Events/CustomerEventOccurredEvent.cs` (new — MassTransit contract)
- `src/Warehouse.ServiceModel/DTOs/EventLog/OperationsEventDto.cs` (new — base DTO)
- `src/Warehouse.ServiceModel/DTOs/EventLog/AuthEventDto.cs` (new — derived DTO)
- `src/Warehouse.ServiceModel/DTOs/EventLog/PurchaseEventDto.cs` (new — derived DTO)
- `src/Warehouse.ServiceModel/DTOs/EventLog/FulfillmentEventDto.cs` (new — derived DTO)
- `src/Warehouse.ServiceModel/DTOs/EventLog/InventoryEventDto.cs` (new — derived DTO)
- `src/Warehouse.ServiceModel/DTOs/EventLog/CustomerEventDto.cs` (new — derived DTO)
- `src/Warehouse.ServiceModel/Requests/EventLog/SearchEventsRequest.cs` (new)
- `src/Warehouse.Mapping/Profiles/EventLog/EventLogMappingProfile.cs` (new — AutoMapper profile)
- `src/Interfaces/Auth/Warehouse.Auth.API/Services/AuditService.cs` (modified — publish AuthAuditLoggedEvent)
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/PurchaseEventService.cs` (modified — publish PurchaseEventOccurredEvent)
- `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/FulfillmentEventService.cs` (modified — publish FulfillmentEventOccurredEvent)
