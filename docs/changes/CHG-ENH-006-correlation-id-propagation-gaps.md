# CHG-ENH-006 — Fix X-Correlation-ID Propagation Gaps

> Status: Implemented
> Last updated: 2026-04-16
> Owner: TBD
> Priority: P1

## 1. Context & Scope

**Why this change is needed:**
The `CorrelationIdMiddleware` correctly generates/reads correlation IDs on every inbound HTTP request and propagates them to NLog scope, response headers, and outbound HTTP calls. However, two critical propagation gaps exist: (1) MassTransit domain events are published without the originating request's correlation ID, making it impossible to trace a user action from API call through to the event log; and (2) the Vue.js frontend axios client does not send an `X-Correlation-ID` header, so browser-initiated requests cannot be correlated end-to-end from the SPA through the gateway to backend services and their published events.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [x] Frontend changes
- [ ] Configuration changes

**Explicitly excluded:**
- No changes to `CorrelationIdMiddleware` behavior (it works correctly)
- No changes to `CorrelationIdDelegatingHandler` (outbound HTTP propagation works)
- No changes to NLog configurations or Loki label setup
- No changes to Gateway YARP transforms
- No changes to `UseWarehousePipeline` middleware pipeline ordering
- No changes to REST API endpoint contracts, request DTOs, or response DTOs
- No changes to database schemas or migrations
- No changes to `ResilientPublisher` or `IResilientPublisher` (services that use it directly will inject `ICorrelationIdAccessor` alongside)
- Sub-record types (`ShipmentDispatchedLine`, `CustomerReturnReceivedLine`, `StockReservationLine`, `SupplierReturnCompletedLine`, `GoodsReceiptCompletedLine`) do NOT need correlation ID -- it belongs on the parent event only

**Related specs:**
- `SDD-INFRA-001` -- Shared Infrastructure Library (defines CorrelationIdMiddleware, CorrelationIdDelegatingHandler, AddCorrelationId)
- `SDD-OBS-001` -- Observability (defines NLog correlation ID integration, Loki label for correlation_id)
- `SDD-EVTLOG-001` -- Centralized Event Logging (consumers that store events -- will benefit from populated CorrelationId)
- `SDD-AUTH-001` -- Authentication (AuditService publishes AuthAuditLoggedEvent)
- `SDD-PURCH-001` -- Procurement Operations (PurchaseEventService, GoodsReceiptService, ReceivingInspectionService, SupplierReturnService publish events)
- `SDD-FULF-001` -- Fulfillment Operations (FulfillmentEventService, ShipmentService, PickListService, CustomerReturnService publish events)
- `SDD-CUST-001` -- Customers and Accounts (CustomerService publishes CustomerCreatedEvent and CustomerEventOccurredEvent)
- `SDD-INV-002` -- Stock Management (StockMovementService, InventoryAdjustmentService publish events)
- `SDD-INV-003` -- Warehouse Structure (WarehouseTransferService publishes events)

---

## 2. Behavior (RFC 2119)

All rules below describe the enhanced correlation ID propagation. No existing business logic changes.

### Group A -- Infrastructure: ICorrelationIdAccessor (NEW)

**Purpose:** Provide a single-responsibility abstraction for retrieving the current request's correlation ID, decoupled from `IHttpContextAccessor`.

- The system MUST define an `ICorrelationIdAccessor` interface in `Warehouse.Infrastructure` with a single read-only property `string? CorrelationId { get; }`.
- The system MUST provide a `CorrelationIdAccessor` implementation class in `Warehouse.Infrastructure` that reads the correlation ID from `IHttpContextAccessor.HttpContext.Items["CorrelationId"]`.
- `CorrelationIdAccessor` MUST return `null` when `HttpContext` is null (background tasks, MassTransit consumer context without HTTP request).
- `CorrelationIdAccessor` MUST return `null` when `HttpContext.Items["CorrelationId"]` is not present or is not a string.
- `CorrelationIdAccessor` MUST NOT throw exceptions under any circumstances.
- The `AddCorrelationId()` extension method in `ServiceCollectionExtensions` MUST register `ICorrelationIdAccessor` as scoped (same lifetime as `HttpContext`), implemented by `CorrelationIdAccessor`.
- The registration MUST be idempotent -- calling `AddCorrelationId()` multiple times MUST NOT register duplicate services.

**Affected files:**

| File | Change |
|---|---|
| `src/Warehouse.Infrastructure/Correlation/ICorrelationIdAccessor.cs` | New file -- interface definition |
| `src/Warehouse.Infrastructure/Correlation/CorrelationIdAccessor.cs` | New file -- implementation |
| `src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Modified -- register `ICorrelationIdAccessor` in `AddCorrelationId()` |

### Group B -- Event Contracts: Add CorrelationId Property (12 events)

All 12 event contracts listed below that currently lack a `CorrelationId` property MUST have one added.

- Each event contract MUST add a property: `public string? CorrelationId { get; init; }` with XML doc `/// <summary>Gets the correlation ID from the originating HTTP request.</summary>`.
- The property MUST be optional (nullable, not `required`) because events may be published from non-HTTP contexts (e.g., background tasks, consumers re-publishing).
- The property MUST NOT be added to sub-record types (line records within parent events).

**Event contracts requiring new `CorrelationId` property:**

| # | File | Event Record |
|---|---|---|
| 1 | `src/Warehouse.ServiceModel/Events/GoodsReceiptCompletedEvent.cs` | `GoodsReceiptCompletedEvent` |
| 2 | `src/Warehouse.ServiceModel/Events/GoodsReceiptLineAcceptedEvent.cs` | `GoodsReceiptLineAcceptedEvent` |
| 3 | `src/Warehouse.ServiceModel/Events/ShipmentDispatchedEvent.cs` | `ShipmentDispatchedEvent` |
| 4 | `src/Warehouse.ServiceModel/Events/CustomerReturnReceivedEvent.cs` | `CustomerReturnReceivedEvent` |
| 5 | `src/Warehouse.ServiceModel/Events/StockReservationRequestedEvent.cs` | `StockReservationRequestedEvent` |
| 6 | `src/Warehouse.ServiceModel/Events/StockReservationReleasedEvent.cs` | `StockReservationReleasedEvent` |
| 7 | `src/Warehouse.ServiceModel/Events/StockMovementRecordedEvent.cs` | `StockMovementRecordedEvent` |
| 8 | `src/Warehouse.ServiceModel/Events/InventoryAdjustmentAppliedEvent.cs` | `InventoryAdjustmentAppliedEvent` |
| 9 | `src/Warehouse.ServiceModel/Events/WarehouseTransferCompletedEvent.cs` | `WarehouseTransferCompletedEvent` |
| 10 | `src/Warehouse.ServiceModel/Events/CustomerCreatedEvent.cs` | `CustomerCreatedEvent` |
| 11 | `src/Warehouse.ServiceModel/Events/UserPermissionsChangedEvent.cs` | `UserPermissionsChangedEvent` |
| 12 | `src/Warehouse.ServiceModel/Events/SupplierReturnCompletedEvent.cs` | `SupplierReturnCompletedEvent` |

**Event contracts that already have the property (no change needed to the contract):**

| # | File | Event Record |
|---|---|---|
| 1 | `src/Warehouse.ServiceModel/Events/PurchaseEventOccurredEvent.cs` | `PurchaseEventOccurredEvent` |
| 2 | `src/Warehouse.ServiceModel/Events/FulfillmentEventOccurredEvent.cs` | `FulfillmentEventOccurredEvent` |
| 3 | `src/Warehouse.ServiceModel/Events/InventoryEventOccurredEvent.cs` | `InventoryEventOccurredEvent` |
| 4 | `src/Warehouse.ServiceModel/Events/CustomerEventOccurredEvent.cs` | `CustomerEventOccurredEvent` |
| 5 | `src/Warehouse.ServiceModel/Events/AuthAuditLoggedEvent.cs` | `AuthAuditLoggedEvent` |

### Group C -- Publisher Services: Populate CorrelationId on All Events

Every service that publishes MassTransit events MUST inject `ICorrelationIdAccessor` and populate the `CorrelationId` property on every event before publishing.

- Each publisher service MUST accept `ICorrelationIdAccessor` via constructor injection.
- Each publisher service MUST use `_publishEndpoint.PublishWithCorrelationAsync(@event, _correlationIdAccessor, cancellationToken)` (from Group E) instead of `_publishEndpoint.Publish(...)` for all `ICorrelatedEvent` types.
- If `ICorrelationIdAccessor.CorrelationId` returns `null`, the event MUST be published with `CorrelationId = null` -- this MUST NOT prevent the event from being published.
- The fire-and-forget pattern (try/catch around Publish) MUST be preserved unchanged.

**Publisher services that need `ICorrelationIdAccessor` injected and CorrelationId populated:**

| # | Service File | Events Published | Domain |
|---|---|---|---|
| 1 | `src/Interfaces/Auth/Warehouse.Auth.API/Services/AuditService.cs` | `AuthAuditLoggedEvent` | Auth |
| 2 | `src/Interfaces/Auth/Warehouse.Auth.API/Services/UserService.cs` | `UserPermissionsChangedEvent` | Auth |
| 3 | `src/Interfaces/Auth/Warehouse.Auth.API/Services/RoleService.cs` | `UserPermissionsChangedEvent` | Auth |
| 4 | `src/Interfaces/Customers/Warehouse.Customers.API/Services/CustomerService.cs` | `CustomerCreatedEvent`, `CustomerEventOccurredEvent` | Customers |
| 5 | `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/Stock/StockMovementService.cs` | `StockMovementRecordedEvent`, `InventoryEventOccurredEvent` | Inventory |
| 6 | `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/Stock/InventoryAdjustmentService.cs` | `InventoryAdjustmentAppliedEvent`, `InventoryEventOccurredEvent` | Inventory |
| 7 | `src/Interfaces/Inventory/Warehouse.Inventory.API/Services/Warehouse/WarehouseTransferService.cs` | `WarehouseTransferCompletedEvent`, `InventoryEventOccurredEvent` | Inventory |
| 8 | `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/PurchaseEventService.cs` | `PurchaseEventOccurredEvent` | Purchasing |
| 9 | `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/GoodsReceiptService.cs` | `GoodsReceiptCompletedEvent` | Purchasing |
| 10 | `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/ReceivingInspectionService.cs` | `GoodsReceiptLineAcceptedEvent` | Purchasing |
| 11 | `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/SupplierReturnService.cs` | `SupplierReturnCompletedEvent` | Purchasing |
| 12 | `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/FulfillmentEventService.cs` | `FulfillmentEventOccurredEvent` | Fulfillment |
| 13 | `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/ShipmentService.cs` | `ShipmentDispatchedEvent` | Fulfillment |
| 14 | `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/PickListService.cs` | `StockReservationRequestedEvent`, `StockReservationReleasedEvent` | Fulfillment |
| 15 | `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/CustomerReturnService.cs` | `CustomerReturnReceivedEvent` | Fulfillment |

### Group D -- Frontend: Axios Client Correlation ID Header

- The frontend axios client (`frontend/src/shared/api/client.ts`) MUST add an `X-Correlation-ID` header to every outgoing request.
- The correlation ID MUST be generated as a UUID v4 string (using `crypto.randomUUID()` or equivalent).
- A new correlation ID MUST be generated per request (not reused across requests).
- The header MUST be added in the existing request interceptor, alongside the `Authorization` header.
- If `crypto.randomUUID()` is not available (older browser), the implementation SHOULD fall back to a manual UUID v4 generator function.
- The `X-Correlation-ID` header MUST NOT overwrite an already-present header (defensive check).

**Affected file:**

| File | Change |
|---|---|
| `frontend/src/shared/api/client.ts` | Modified -- add `X-Correlation-ID` header in request interceptor |

### Group E -- Infrastructure: ICorrelatedEvent Interface + PublishWithCorrelationAsync Helper (NEW)

**Purpose:** Reduce boilerplate in Group C. Instead of every service manually setting `CorrelationId = _correlationIdAccessor.CorrelationId` on every event, provide a marker interface and an extension method that does it automatically.

- The system MUST define an `ICorrelatedEvent` interface in `Warehouse.ServiceModel/Events/` with a single property: `string? CorrelationId { get; init; }`.
- All 17 event contracts (12 from Group B + 5 existing) MUST implement `ICorrelatedEvent`.
- Sub-record types (`ShipmentDispatchedLine`, `CustomerReturnReceivedLine`, `StockReservationLine`, `SupplierReturnCompletedLine`, `GoodsReceiptCompletedLine`) MUST NOT implement `ICorrelatedEvent`.
- The system MUST define a `CorrelatedPublishExtensions` static class in `Warehouse.Infrastructure/Correlation/` with an extension method:
  ```
  PublishWithCorrelationAsync<T>(this IPublishEndpoint endpoint, T @event, ICorrelationIdAccessor accessor, CancellationToken ct) where T : class, ICorrelatedEvent
  ```
- The extension method MUST set `@event.CorrelationId` to `accessor.CorrelationId` before calling `endpoint.Publish(@event, ct)`.
- The extension method MUST NOT wrap in try/catch -- the existing fire-and-forget try/catch in each service remains unchanged.
- Publisher services (Group C) MUST use `_publishEndpoint.PublishWithCorrelationAsync(event, _correlationIdAccessor, ct)` instead of manually setting `CorrelationId` and calling `Publish(...)`.

**Affected files:**

| File | Change |
|---|---|
| `src/Warehouse.ServiceModel/Events/ICorrelatedEvent.cs` | New file -- marker interface |
| `src/Warehouse.Infrastructure/Correlation/CorrelatedPublishExtensions.cs` | New file -- extension method |
| All 17 event contract files | Modified -- add `: ICorrelatedEvent` to class declaration |

### Cross-Cutting Rules

- This enhancement MUST NOT change any externally observable business behavior. Only traceability metadata is added.
- Existing unit and integration tests MUST continue to pass. New constructor parameters for `ICorrelationIdAccessor` require mock setup updates in test fixtures, but no behavioral assertions should change.
- The `ICorrelationIdAccessor` MUST be registered in `AddCorrelationId()` which is already called by every service's `Program.cs` (either directly or via `AddWarehousePermissionValidation`). No new `Program.cs` changes are needed.
- The `CorrelationId` property on events MUST NOT be marked `required` because: (a) consumers re-publishing events may not have an HTTP context, (b) backward compatibility with events already in RabbitMQ queues.

**Edge cases:**

- If a service publishes an event from a MassTransit consumer (not from an HTTP request), `ICorrelationIdAccessor.CorrelationId` MUST return `null` and the event MUST be published with `CorrelationId = null`. The system MUST NOT throw.
- If the frontend's `crypto.randomUUID()` is called in a non-secure context (HTTP without TLS), some browsers may throw. The fallback SHOULD handle this gracefully.
- If the Gateway strips the `X-Correlation-ID` header before proxying (it does not currently), the middleware would generate a new one. This is the existing behavior and MUST NOT change.

---

## 3. Validation Rules

No new validation rules on API inputs. The `CorrelationId` property is internally populated, not user-supplied.

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | `ICorrelationIdAccessor.CorrelationId` | Returns `string?` -- null if no HTTP context | N/A (internal) |
| V2 | Event `CorrelationId` property | Optional (`string?`), not validated | N/A (internal) |
| V3 | Frontend `X-Correlation-ID` header | UUID v4 format, generated per request | N/A (client-side) |

---

## 4. Error Rules

No new error conditions. This enhancement adds metadata propagation only.

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | `ICorrelationIdAccessor` returns null (no HTTP context) | N/A | N/A | Event published with `CorrelationId = null`. No error. |
| E2 | `crypto.randomUUID()` unavailable in browser | N/A | N/A | Fallback UUID generation. No error to user. |

---

## 5. Versioning Notes

**API version impact:** None -- no REST endpoint changes, no request/response DTO changes, no URL route changes.

**Database migration required:** No

**Backwards compatibility:** Fully compatible. The new `CorrelationId` property on events is optional (`string?`). Events already in RabbitMQ queues without this property will deserialize with `CorrelationId = null`. Consumers that do not use the property are unaffected.

**MassTransit contract compatibility:** Adding an optional property to a `sealed record` event is a non-breaking change for MassTransit message deserialization. Messages published before this change will have `CorrelationId = null` when consumed after deployment.

---

## 6. Test Plan

### Unit Tests

**Group A -- ICorrelationIdAccessor:**

- [ ] `[Unit] CorrelationIdAccessor_WithHttpContext_ReturnsStoredCorrelationId` -- verify accessor returns the correlation ID from `HttpContext.Items["CorrelationId"]`
- [ ] `[Unit] CorrelationIdAccessor_NullHttpContext_ReturnsNull` -- verify accessor returns null when `IHttpContextAccessor.HttpContext` is null
- [ ] `[Unit] CorrelationIdAccessor_MissingItemKey_ReturnsNull` -- verify accessor returns null when `HttpContext.Items` does not contain the `"CorrelationId"` key
- [ ] `[Unit] CorrelationIdAccessor_NonStringValue_ReturnsNull` -- verify accessor returns null when `HttpContext.Items["CorrelationId"]` contains a non-string value
- [ ] `[Unit] AddCorrelationId_RegistersICorrelationIdAccessor` -- verify that calling `AddCorrelationId()` registers `ICorrelationIdAccessor` in the service collection

**Group E -- ICorrelatedEvent + PublishWithCorrelationAsync:**

- [ ] `[Unit] PublishWithCorrelationAsync_SetsCorrelationIdFromAccessor` -- verify the extension method sets `CorrelationId` on the event before publishing
- [ ] `[Unit] PublishWithCorrelationAsync_NullCorrelationId_PublishesWithNull` -- verify the event is published with null when accessor returns null
- [ ] `[Unit] PublishWithCorrelationAsync_CallsPublishOnEndpoint` -- verify the extension method calls `IPublishEndpoint.Publish` with the event and cancellation token
- [ ] `[Unit] AllEventContracts_ImplementICorrelatedEvent` -- verify all 17 event types implement `ICorrelatedEvent` via reflection

**Group B -- Event contracts (compile-time verification):**

- [ ] `[Unit] GoodsReceiptCompletedEvent_HasCorrelationIdProperty` -- verify the property exists and accepts a string value
- [ ] `[Unit] ShipmentDispatchedEvent_HasCorrelationIdProperty` -- verify the property exists and accepts a string value
- [ ] `[Unit] AllEventContracts_HaveCorrelationIdProperty` -- verify all 17 event contracts (12 new + 5 existing) have a `string? CorrelationId` property via reflection

**Group C -- Publisher services populate CorrelationId:**

- [ ] `[Unit] AuditService_LogAsync_PublishesAuthAuditLoggedEvent_WithCorrelationId` -- verify the published event includes the correlation ID from the accessor
- [ ] `[Unit] AuditService_LogAsync_NullCorrelationId_PublishesEventWithNull` -- verify the event is published with null correlation ID when accessor returns null
- [ ] `[Unit] CustomerService_CreateAsync_PublishesCustomerCreatedEvent_WithCorrelationId` -- verify correlation ID propagates to CustomerCreatedEvent
- [ ] `[Unit] CustomerService_CreateAsync_PublishesCustomerEventOccurredEvent_WithCorrelationId` -- verify correlation ID propagates to CustomerEventOccurredEvent
- [ ] `[Unit] PurchaseEventService_RecordEventAsync_PublishesPurchaseEventOccurredEvent_WithCorrelationId` -- verify correlation ID propagates to PurchaseEventOccurredEvent
- [ ] `[Unit] GoodsReceiptService_CompleteAsync_PublishesGoodsReceiptCompletedEvent_WithCorrelationId` -- verify correlation ID propagates to GoodsReceiptCompletedEvent
- [ ] `[Unit] ReceivingInspectionService_AcceptAsync_PublishesGoodsReceiptLineAcceptedEvent_WithCorrelationId` -- verify correlation ID propagates to GoodsReceiptLineAcceptedEvent
- [ ] `[Unit] SupplierReturnService_ConfirmAsync_PublishesSupplierReturnCompletedEvent_WithCorrelationId` -- verify correlation ID propagates to SupplierReturnCompletedEvent
- [ ] `[Unit] StockMovementService_RecordAsync_PublishesStockMovementRecordedEvent_WithCorrelationId` -- verify correlation ID propagates to StockMovementRecordedEvent
- [ ] `[Unit] StockMovementService_RecordAsync_PublishesInventoryEventOccurredEvent_WithCorrelationId` -- verify correlation ID propagates to InventoryEventOccurredEvent
- [ ] `[Unit] InventoryAdjustmentService_ApplyAsync_PublishesInventoryAdjustmentAppliedEvent_WithCorrelationId` -- verify correlation ID propagates to InventoryAdjustmentAppliedEvent
- [ ] `[Unit] WarehouseTransferService_CompleteAsync_PublishesWarehouseTransferCompletedEvent_WithCorrelationId` -- verify correlation ID propagates to WarehouseTransferCompletedEvent
- [ ] `[Unit] FulfillmentEventService_RecordEventAsync_PublishesFulfillmentEventOccurredEvent_WithCorrelationId` -- verify correlation ID propagates to FulfillmentEventOccurredEvent
- [ ] `[Unit] ShipmentService_DispatchAsync_PublishesShipmentDispatchedEvent_WithCorrelationId` -- verify correlation ID propagates to ShipmentDispatchedEvent
- [ ] `[Unit] PickListService_GenerateAsync_PublishesStockReservationRequestedEvent_WithCorrelationId` -- verify correlation ID propagates to StockReservationRequestedEvent
- [ ] `[Unit] PickListService_CancelAsync_PublishesStockReservationReleasedEvent_WithCorrelationId` -- verify correlation ID propagates to StockReservationReleasedEvent
- [ ] `[Unit] CustomerReturnService_ReceiveAsync_PublishesCustomerReturnReceivedEvent_WithCorrelationId` -- verify correlation ID propagates to CustomerReturnReceivedEvent
- [ ] `[Unit] UserService_AssignRoleAsync_PublishesUserPermissionsChangedEvent_WithCorrelationId` -- verify correlation ID propagates to UserPermissionsChangedEvent
- [ ] `[Unit] RoleService_UpdatePermissionsAsync_PublishesUserPermissionsChangedEvent_WithCorrelationId` -- verify correlation ID propagates to UserPermissionsChangedEvent

**Regression (all groups):**

- [ ] `[Unit] AllExistingAuthServiceTests_PassUnchanged` -- full Auth.API.Tests suite green (mock setup may need ICorrelationIdAccessor)
- [ ] `[Unit] AllExistingCustomersServiceTests_PassUnchanged` -- full Customers.API.Tests suite green
- [ ] `[Unit] AllExistingInventoryServiceTests_PassUnchanged` -- full Inventory.API.Tests suite green
- [ ] `[Unit] AllExistingPurchasingServiceTests_PassUnchanged` -- full Purchasing.API.Tests suite green
- [ ] `[Unit] AllExistingFulfillmentServiceTests_PassUnchanged` -- full Fulfillment.API.Tests suite green

### Integration Tests

- [ ] `[Integration] Request_WithCorrelationId_PropagatedToPublishedEvent` -- send an HTTP request with a known `X-Correlation-ID` header, capture the published MassTransit event via test harness, and verify the event's `CorrelationId` matches the request header value
- [ ] `[Integration] Request_WithoutCorrelationId_GeneratedCorrelationId_PropagatedToEvent` -- send an HTTP request without a correlation ID header, verify the middleware generates one, and verify the published event contains the generated correlation ID

---

## 7. Detailed Design

### API Changes

None. No REST endpoints, request DTOs, response DTOs, or URL routes are modified.

### Data Model Changes

None. No database entities, columns, or migrations are affected. The `CorrelationId` property on events is a MassTransit message contract property, not a database column.

### Service Layer Changes

#### New Files (4 -- Groups A + E)

| # | File | Type | Contents |
|---|---|---|---|
| 1 | `src/Warehouse.Infrastructure/Correlation/ICorrelationIdAccessor.cs` | Interface | `string? CorrelationId { get; }` |
| 2 | `src/Warehouse.Infrastructure/Correlation/CorrelationIdAccessor.cs` | Class | Reads from `IHttpContextAccessor.HttpContext.Items["CorrelationId"]` |
| 3 | `src/Warehouse.ServiceModel/Events/ICorrelatedEvent.cs` | Interface | `string? CorrelationId { get; init; }` -- marker for correlated events |
| 4 | `src/Warehouse.Infrastructure/Correlation/CorrelatedPublishExtensions.cs` | Static class | `PublishWithCorrelationAsync<T>` extension on `IPublishEndpoint` |

#### Modified Infrastructure (1)

| # | File | Change |
|---|---|---|
| 1 | `src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Add `services.TryAddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>()` in `AddCorrelationId()` |

#### Modified Event Contracts (17 -- add `CorrelationId` property to 12, add `: ICorrelatedEvent` to all 17)

| # | File |
|---|---|
| 1 | `src/Warehouse.ServiceModel/Events/GoodsReceiptCompletedEvent.cs` |
| 2 | `src/Warehouse.ServiceModel/Events/GoodsReceiptLineAcceptedEvent.cs` |
| 3 | `src/Warehouse.ServiceModel/Events/ShipmentDispatchedEvent.cs` |
| 4 | `src/Warehouse.ServiceModel/Events/CustomerReturnReceivedEvent.cs` |
| 5 | `src/Warehouse.ServiceModel/Events/StockReservationRequestedEvent.cs` |
| 6 | `src/Warehouse.ServiceModel/Events/StockReservationReleasedEvent.cs` |
| 7 | `src/Warehouse.ServiceModel/Events/StockMovementRecordedEvent.cs` |
| 8 | `src/Warehouse.ServiceModel/Events/InventoryAdjustmentAppliedEvent.cs` |
| 9 | `src/Warehouse.ServiceModel/Events/WarehouseTransferCompletedEvent.cs` |
| 10 | `src/Warehouse.ServiceModel/Events/CustomerCreatedEvent.cs` |
| 11 | `src/Warehouse.ServiceModel/Events/UserPermissionsChangedEvent.cs` |
| 12 | `src/Warehouse.ServiceModel/Events/SupplierReturnCompletedEvent.cs` |
| 13 | `src/Warehouse.ServiceModel/Events/PurchaseEventOccurredEvent.cs` |
| 14 | `src/Warehouse.ServiceModel/Events/FulfillmentEventOccurredEvent.cs` |
| 15 | `src/Warehouse.ServiceModel/Events/InventoryEventOccurredEvent.cs` |
| 16 | `src/Warehouse.ServiceModel/Events/CustomerEventOccurredEvent.cs` |
| 17 | `src/Warehouse.ServiceModel/Events/AuthAuditLoggedEvent.cs` |

Events 13-17 already have the `CorrelationId` property -- they only need `: ICorrelatedEvent` added to their declaration.

#### Modified Publisher Services (15 -- inject `ICorrelationIdAccessor`, use `PublishWithCorrelationAsync`)

| # | Service File | Domain | Constructor Change | Publish Site Change |
|---|---|---|---|---|
| 1 | `AuditService.cs` | Auth | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `AuthAuditLoggedEvent` |
| 2 | `UserService.cs` | Auth | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `UserPermissionsChangedEvent` |
| 3 | `RoleService.cs` | Auth | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `UserPermissionsChangedEvent` |
| 4 | `CustomerService.cs` | Customers | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `CustomerCreatedEvent` and `CustomerEventOccurredEvent` |
| 5 | `StockMovementService.cs` | Inventory | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `StockMovementRecordedEvent` and `InventoryEventOccurredEvent` |
| 6 | `InventoryAdjustmentService.cs` | Inventory | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `InventoryAdjustmentAppliedEvent` and `InventoryEventOccurredEvent` |
| 7 | `WarehouseTransferService.cs` | Inventory | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `WarehouseTransferCompletedEvent` and `InventoryEventOccurredEvent` |
| 8 | `PurchaseEventService.cs` | Purchasing | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `PurchaseEventOccurredEvent` |
| 9 | `GoodsReceiptService.cs` | Purchasing | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `GoodsReceiptCompletedEvent` |
| 10 | `ReceivingInspectionService.cs` | Purchasing | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `GoodsReceiptLineAcceptedEvent` |
| 11 | `SupplierReturnService.cs` | Purchasing | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `SupplierReturnCompletedEvent` |
| 12 | `FulfillmentEventService.cs` | Fulfillment | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `FulfillmentEventOccurredEvent` |
| 13 | `ShipmentService.cs` | Fulfillment | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `ShipmentDispatchedEvent` |
| 14 | `PickListService.cs` | Fulfillment | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `StockReservationRequestedEvent` and `StockReservationReleasedEvent` |
| 15 | `CustomerReturnService.cs` | Fulfillment | Add `ICorrelationIdAccessor` param | Set `CorrelationId` on `CustomerReturnReceivedEvent` |

#### Modified Frontend (1)

| # | File | Change |
|---|---|---|
| 1 | `frontend/src/shared/api/client.ts` | Add `X-Correlation-ID: crypto.randomUUID()` in request interceptor |

---

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-INFRA-001 | Enhancement: `AddCorrelationId()` now also registers `ICorrelationIdAccessor`. Spec Section 2.2/2.3 should note the new accessor. |
| SDD-OBS-001 | No behavior change -- correlation ID logging already works. Events now carry the same ID for end-to-end tracing. |
| SDD-EVTLOG-001 | Enhancement: consumed events now include `CorrelationId`. EventLog consumers and DTOs may surface this in the future. |
| SDD-AUTH-001 | No behavior change -- `AuditService`, `UserService`, `RoleService` gain `ICorrelationIdAccessor` constructor param. |
| SDD-PURCH-001 | No behavior change -- 4 publisher services gain `ICorrelationIdAccessor` constructor param. |
| SDD-FULF-001 | No behavior change -- 4 publisher services gain `ICorrelationIdAccessor` constructor param. |
| SDD-CUST-001 | No behavior change -- `CustomerService` gains `ICorrelationIdAccessor` constructor param. |
| SDD-INV-002 | No behavior change -- `StockMovementService`, `InventoryAdjustmentService` gain `ICorrelationIdAccessor` constructor param. |
| SDD-INV-003 | No behavior change -- `WarehouseTransferService` gains `ICorrelationIdAccessor` constructor param. |

---

## Migration Plan

1. **Pre-deployment:** Implement all 5 groups. Groups A and E first, then B, then C. Group D is independent of all backend groups. Run full test suite across all 6 service test projects.
2. **Deployment:** Deploy updated assemblies and rebuilt frontend. No schema changes, no configuration changes, no new infrastructure containers.
3. **Post-deployment:** Verify all health checks pass. Send a test request with a known `X-Correlation-ID` header to any endpoint that triggers event publishing (e.g., create a stock movement). Query the RabbitMQ management UI or EventLog API to verify the correlation ID appears on the published event.
4. **Rollback:** Revert to previous commit. Events in RabbitMQ queues published with `CorrelationId` will have the property ignored by older consumers (MassTransit ignores unknown properties). No data migration needed.

## Implementation Order

The five groups should be implemented in this sequence for minimal risk:

1. **Group A** (ICorrelationIdAccessor) -- infrastructure foundation, no dependencies
2. **Group E** (ICorrelatedEvent + PublishWithCorrelationAsync) -- depends on A for accessor type
3. **Group B** (event contracts) -- add properties + `: ICorrelatedEvent`, depends on E for interface
4. **Group C** (publisher services) -- depends on A, B, and E being complete
5. **Group D** (frontend) -- independent of backend, can be done in parallel with any group

## Resolved Questions

- [x] **EventLog consumers:** Already persist `CorrelationId = message.CorrelationId` in all 5 consumers (`AuthAuditLoggedEventConsumer`, `PurchaseEventOccurredEventConsumer`, `FulfillmentEventOccurredEventConsumer`, `InventoryEventOccurredEventConsumer`, `CustomerEventOccurredEventConsumer`). The `OperationsEvent` entity already has the `CorrelationId` column. **No consumer changes needed** -- once publishers populate the property (Group C), it flows through to the database automatically.
- [x] **PublishWithCorrelationAsync helper:** Yes -- add an `ICorrelatedEvent` marker interface and an extension method to reduce per-service boilerplate. See Group E below.
