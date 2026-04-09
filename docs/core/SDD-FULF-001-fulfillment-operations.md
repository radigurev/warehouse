# SDD-FULF-001 -- Fulfillment Operations

> Status: Implemented
> Last updated: 2026-04-06
> Owner: TBD
> Category: Core

## 1. Context & Scope

This spec defines the Fulfillment Operations domain for the Warehouse system. It covers sales order lifecycle, pick list generation, packing, dispatch (shipment creation), shipment tracking, carrier management, customer returns, and fulfillment event history. The Fulfillment service is the outbound counterpart to the Purchasing service (`SDD-PURCH-001`), responsible for all material movement from warehouse to customer. Like Purchasing, it uses MassTransit/RabbitMQ for inter-service messaging with the Inventory service.

**ISA-95 Conformance:** Conforms to ISA-95 Part 3 -- Fulfillment Operations Activity Model (Fulfillment Request, Material Movement -- outbound, Material Shipment). The Carrier entity extends the ISA-95 Equipment Model for logistics partners. All state-changing operations produce immutable operations event records per ISA-95 Operations Event Model. The Customer Return sub-domain conforms to ISA-95 Material Receipt (return) activity.

**Service:** `Warehouse.Fulfillment.API`
**Schema:** `fulfillment`
**Port:** 5005
**DbContext:** `FulfillmentDbContext`

**In scope:**
- Sales order CRUD with status machine (Draft, Confirmed, Picking, Packed, Shipped, Completed, Cancelled)
- Sales order lines referencing products from the Inventory domain and customers from the Customers domain
- Pick list generation from confirmed sales orders with stock reservation
- Packing workflow -- pack picked items into parcels with weight recording
- Dispatch -- create shipments from packed orders, publish event to create StockMovement (reason: Shipment)
- Shipment tracking with carrier updates
- Carrier management (carriers, service levels, rate tables)
- Customer returns (RMA) with status machine (Draft, Confirmed, Received, Closed, Cancelled) and StockMovement generation (reason: CustomerReturn)
- Fulfillment event history (immutable audit trail)
- MassTransit event publishing for cross-service integration

**Out of scope:**
- Label templates, ZPL/Zebra printing, QR codes, barcodes (future enhancement -- deferred to Phase 2+)
- Dispatch documents -- packing slips, delivery notes (future enhancement -- deferred to Phase 2+)
- Automated carrier rate comparison and selection (future enhancement)
- Wave picking and batch picking optimization (future enhancement)
- Partial shipment splitting (future enhancement -- a sales order ships as a single shipment in v1)
- Customer credit checks and payment terms enforcement (future Finance service)
- Frontend views (covered by separate UI spec when implemented)

**Related specs:**
- `SDD-AUTH-001` -- All endpoints require JWT authentication and permission-based authorization.
- `SDD-INV-001` -- Products referenced by SO lines, pick lists, and return lines.
- `SDD-INV-002` -- Stock movements created by dispatch and customer return events. Stock reservations updated by pick lists.
- `SDD-INV-003` -- Warehouses and storage locations referenced by pick lists, shipments, and returns.
- `SDD-CUST-001` -- Customers referenced by sales orders.
- `SDD-PURCH-001` -- Sister service (inbound); shares MassTransit patterns and event naming conventions.

---

## 2. Behavior

### 2.1 Sales Orders

#### 2.1.1 Create Sales Order

- The system MUST support creating a sales order with a customer ID, requested ship date (optional), ship-from warehouse ID, shipping address fields (street line 1, street line 2, city, state/province, postal code, country code), optional carrier ID, optional carrier service level ID, optional notes, and one or more lines.
- The system MUST auto-generate a unique SO number following the format `SO-{YYYYMMDD}-{sequential number padded to 4 digits}` (e.g., `SO-20260406-0001`). The sequential counter resets daily.
- The system MUST set the initial status to `Draft`.
- The system MUST calculate `TotalAmount` as the sum of all line totals.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.
- The system MUST return the created SO with its generated ID and SO number.

**Edge cases:**
- Creating an SO for a non-existent or soft-deleted customer MUST return a 404 Not Found error with code `CUSTOMER_NOT_FOUND`.
- Creating an SO for an inactive (deactivated) customer MUST return a 409 Conflict error with code `CUSTOMER_INACTIVE`.
- Creating an SO with no lines MUST return a 400 Validation error.
- Creating an SO with an invalid warehouse ID MUST return a 400 Validation error with code `INVALID_WAREHOUSE`.
- Creating an SO with a non-existent carrier ID MUST return a 400 Validation error with code `INVALID_CARRIER`.
- Creating an SO with a carrier service level ID that does not belong to the specified carrier MUST return a 400 Validation error with code `INVALID_SERVICE_LEVEL`.

#### 2.1.2 Add Sales Order Line

- Each SO line MUST specify a product ID, ordered quantity, unit price, and optional notes.
- The system MUST validate that the product ID references an existing, non-deleted product (cross-service: Inventory).
- The system MUST prevent duplicate product entries within the same SO. This MUST return a 409 Conflict error with code `DUPLICATE_SO_LINE`.
- Lines can only be added when the SO is in `Draft` status. Adding a line to a non-Draft SO MUST return a 409 Conflict error with code `SO_NOT_EDITABLE`.
- The system MUST recalculate `TotalAmount` on the SO header when a line is added.

#### 2.1.3 Update Sales Order Line

- The system MUST support updating line quantity, unit price, and notes.
- Lines can only be updated when the SO is in `Draft` status.
- The system MUST recalculate `TotalAmount` on the SO header when a line is updated.

#### 2.1.4 Remove Sales Order Line

- The system MUST support removing a line from an SO.
- Lines can only be removed when the SO is in `Draft` status.
- The system MUST prevent removing the last line of an SO. This MUST return a 409 Conflict error with code `SO_MUST_HAVE_LINES`.
- The system MUST recalculate `TotalAmount` on the SO header when a line is removed.

#### 2.1.5 Update Sales Order Header

- The system MUST support updating SO header fields: requested ship date, ship-from warehouse ID, shipping address fields, carrier ID, carrier service level ID, and notes.
- Header updates are only permitted when the SO is in `Draft` status. Attempting to update a non-Draft SO MUST return a 409 Conflict error with code `SO_NOT_EDITABLE`.
- The system MUST update `ModifiedAtUtc` and `ModifiedByUserId` on every update.

#### 2.1.6 Get Sales Order

- The system MUST support retrieving a single SO by ID, including customer details, all lines with product details, pick progress, and shipment reference (if shipped).
- Pick progress MUST show: ordered quantity, picked quantity, packed quantity, and shipped quantity per line.

#### 2.1.7 Search Sales Orders

- The system MUST support paginated listing of SOs with configurable page size and page number.
- The system MUST support filtering by: customer ID, status, SO number (exact or starts-with), date range (created date), warehouse ID.
- The system MUST support sorting by SO number, created date, requested ship date, and customer name.
- The system MUST default to sorting by created date descending (newest first).

#### 2.1.8 Sales Order Status Machine

The SO status machine defines the following valid transitions:

| From | To | Trigger | Rules |
|---|---|---|---|
| `Draft` | `Confirmed` | Manual confirm action | SO must have at least one line |
| `Draft` | `Cancelled` | Manual cancel action | No pick lists generated for this SO |
| `Confirmed` | `Picking` | Automatic when pick list is generated | At least one pick list exists for this SO |
| `Confirmed` | `Cancelled` | Manual cancel action | No pick lists generated for this SO |
| `Picking` | `Packed` | Automatic when all lines are fully packed | Every line's packed quantity >= ordered quantity |
| `Packed` | `Shipped` | Automatic when shipment is dispatched | Shipment created and dispatched |
| `Shipped` | `Completed` | Manual complete action | Administrative close after delivery confirmation |

- The system MUST enforce the status machine transitions. Any invalid transition MUST return a 409 Conflict error with code `INVALID_SO_STATUS_TRANSITION`.
- The system MUST record `ModifiedAtUtc` and `ModifiedByUserId` on every status change.
- The system MUST record `ConfirmedAtUtc` and `ConfirmedByUserId` when transitioning to `Confirmed`.
- The system MUST record `ShippedAtUtc` when transitioning to `Shipped`.
- The system MUST record `CompletedAtUtc` and `CompletedByUserId` when transitioning to `Completed`.
- Transitions to `Picking`, `Packed`, and `Shipped` are automatic and MUST NOT be triggered directly by API calls. These transitions MUST occur as a side effect of pick list generation, packing completion, and dispatch respectively.
- Cancellation of an SO that has pick lists MUST return a 409 Conflict error with code `SO_HAS_PICK_LISTS`.

**Edge cases:**
- Confirming an SO with no lines MUST return a 409 Conflict error.
- Attempting to cancel a Picking, Packed, or Shipped SO MUST return a 409 Conflict error with code `INVALID_SO_STATUS_TRANSITION`.
- Completing an already-completed SO MUST return a 409 Conflict error with code `SO_ALREADY_COMPLETED`.

### 2.2 Pick Lists

#### 2.2.1 Generate Pick List

- The system MUST support generating a pick list from a confirmed sales order.
- A pick list MUST reference an SO that is in `Confirmed` or `Picking` status. Generating a pick list against an SO in any other status MUST return a 409 Conflict error with code `SO_NOT_PICKABLE`.
- The system MUST auto-generate a unique pick list number following the format `PL-{YYYYMMDD}-{sequential number padded to 4 digits}` (e.g., `PL-20260406-0001`).
- The system MUST create pick list lines for each SO line specifying: the product, the warehouse, the source storage location (recommended location based on FIFO or available stock), and the quantity to pick.
- The system MUST set the initial status to `Pending`.
- The system MUST reserve stock for each pick list line by publishing a `StockReservationRequestedEvent` or directly updating `QuantityReserved` on the corresponding `StockLevel` via MassTransit. The reservation MUST increase `QuantityReserved` by the pick quantity.
- The system MUST validate that sufficient available stock (QuantityOnHand - QuantityReserved) exists at the specified location for each line. If insufficient stock exists for any line, the system MUST return a 409 Conflict error with code `INSUFFICIENT_STOCK`.
- The system MUST transition the SO to `Picking` status on the first pick list generation (if not already in `Picking`).
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.

**Edge cases:**
- Generating a pick list for a cancelled SO MUST return a 409 Conflict error.
- Generating a pick list when all SO lines are already fully allocated to existing pick lists MUST return a 409 Conflict error with code `SO_FULLY_ALLOCATED`.

#### 2.2.2 Confirm Pick (Pick Execution)

- The system MUST support confirming individual pick list lines as picked.
- Each pick confirmation MUST record: the actual quantity picked, the user who picked, and the pick timestamp.
- The actual picked quantity MAY differ from the requested quantity (short pick). If the actual quantity is less than requested, the system MUST update the reservation accordingly (release the difference).
- A pick list line cannot be confirmed more than once. Re-confirming MUST return a 409 Conflict error with code `LINE_ALREADY_PICKED`.
- When all lines of a pick list are confirmed, the pick list status MUST automatically transition to `Completed`.
- The system MUST record `PickedAtUtc` and `PickedByUserId` per line.

**Edge cases:**
- Confirming a pick with zero quantity MUST return a 400 Validation error.
- Confirming a pick with quantity greater than requested MUST return a 409 Conflict error with code `OVER_PICK`.

#### 2.2.3 Cancel Pick List

- The system MUST support cancelling a `Pending` pick list.
- Cancelling a pick list MUST release all stock reservations (decrease `QuantityReserved` by the pick quantities).
- Cancelling a completed pick list MUST return a 409 Conflict error with code `PICK_LIST_ALREADY_COMPLETED`.
- Cancelling a pick list that has some lines already picked MUST release only the un-picked reservations and set the pick list status to `Cancelled`.

#### 2.2.4 Get Pick List

- The system MUST support retrieving a single pick list by ID, including all lines with product details, location details, and pick status per line.

#### 2.2.5 List Pick Lists

- The system MUST support paginated listing of pick lists.
- The system MUST support filtering by: SO ID, SO number, status, warehouse ID, date range.
- The system MUST default to sorting by created date descending.

### 2.3 Packing

#### 2.3.1 Create Parcel

- The system MUST support creating a parcel for a sales order that is in `Picking` status (with at least one completed pick list) or `Packed` status (additional parcels).
- Each parcel MUST have: a parcel number (auto-generated, format `PKG-{YYYYMMDD}-{sequential number padded to 4 digits}`), optional weight (decimal, kg), optional dimensions (length, width, height in cm), optional tracking number, and optional notes.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.

#### 2.3.2 Add Items to Parcel

- The system MUST support adding items to a parcel by specifying: product ID, quantity, and the source pick list line ID.
- The packed quantity for a product across all parcels for an SO MUST NOT exceed the total picked quantity for that product. Exceeding MUST return a 409 Conflict error with code `OVER_PACK`.
- The system MUST validate that the pick list line has been confirmed (picked) before items can be packed from it.

#### 2.3.3 Update Parcel

- The system MUST support updating parcel weight, dimensions, tracking number, and notes.
- Updates are only permitted before the SO is dispatched. Updating a parcel on a shipped SO MUST return a 409 Conflict error with code `PARCEL_NOT_EDITABLE`.

#### 2.3.4 Remove Parcel

- The system MUST support removing a parcel from an SO.
- Removing a parcel MUST also remove all packed item associations.
- Removal is only permitted before the SO is dispatched.

#### 2.3.5 Packing Completion

- When all SO lines are fully packed (total packed quantity >= ordered quantity per line across all parcels), the system MUST automatically transition the SO to `Packed` status.
- The system SHOULD allow over-packing tolerance of 0% by default (exact match required). This MAY be configurable in future versions.

#### 2.3.6 List Parcels

- The system MUST support listing all parcels for a sales order, including packed items per parcel.

### 2.4 Dispatch (Shipment)

#### 2.4.1 Create Shipment

- The system MUST support creating a shipment from a `Packed` sales order.
- The system MUST auto-generate a unique shipment number following the format `SH-{YYYYMMDD}-{sequential number padded to 4 digits}` (e.g., `SH-20260406-0001`).
- The shipment MUST reference: the SO, all parcels of the SO, the carrier (optional), the carrier service level (optional), the shipping address (copied from the SO), and optional notes.
- The system MUST create shipment lines corresponding to each SO line with the shipped quantity.
- The system MUST set the initial shipment status to `Dispatched`.
- The system MUST record `DispatchedAtUtc` and `DispatchedByUserId` on creation.
- Only one shipment per SO is allowed in v1. Attempting to create a second shipment MUST return a 409 Conflict error with code `SO_ALREADY_SHIPPED`.

#### 2.4.2 Dispatch Event

- When a shipment is created, the system MUST publish a `ShipmentDispatchedEvent` via MassTransit.
- The event MUST contain: shipment ID, shipment number, SO ID, SO number, warehouse ID, dispatched by user ID, dispatched timestamp, and for each line: product ID, quantity, location ID, batch ID (if applicable).
- The Inventory service (consumer) MUST create a `StockMovement` with reason code `Shipment`, reference type `SalesOrder`, and reference ID set to the SO ID.
- The Inventory service (consumer) MUST release the stock reservation (decrease `QuantityReserved`) and decrease `QuantityOnHand` for each shipped product/location.
- The system MUST automatically transition the SO to `Shipped` status upon successful dispatch.

**Edge cases:**
- Creating a shipment for an SO that is not in `Packed` status MUST return a 409 Conflict error with code `SO_NOT_DISPATCHABLE`.
- Creating a shipment when any parcel has no packed items MUST return a 409 Conflict error with code `EMPTY_PARCEL`.

#### 2.4.3 Get Shipment

- The system MUST support retrieving a single shipment by ID, including all lines, parcels, carrier details, and tracking information.

#### 2.4.4 Search Shipments

- The system MUST support paginated listing of shipments.
- The system MUST support filtering by: SO ID, SO number, shipment number, carrier ID, status, date range (dispatched date).
- The system MUST default to sorting by dispatched date descending.

### 2.5 Shipment Tracking

#### 2.5.1 Update Shipment Status

- The system MUST support updating shipment status with a new status, optional tracking number, optional carrier tracking URL, and optional notes.
- Valid shipment statuses: `Dispatched`, `InTransit`, `OutForDelivery`, `Delivered`, `Failed`, `Returned`.
- Status transitions MUST follow a logical progression. The system MUST enforce the following rules:
  - `Dispatched` -> `InTransit`, `Delivered`, `Failed`
  - `InTransit` -> `OutForDelivery`, `Delivered`, `Failed`
  - `OutForDelivery` -> `Delivered`, `Failed`
  - `Failed` -> `Returned`
  - `Delivered` and `Returned` are terminal states.
- Invalid status transitions MUST return a 409 Conflict error with code `INVALID_SHIPMENT_STATUS_TRANSITION`.
- Each status update MUST be recorded as an immutable tracking entry with: status, timestamp, notes, and user ID.

#### 2.5.2 Get Tracking History

- The system MUST support retrieving the full tracking history for a shipment, ordered chronologically.

### 2.6 Carrier Management

#### 2.6.1 Create Carrier

- The system MUST support creating a carrier with: code, name, contact phone (optional), contact email (optional), website URL (optional), tracking URL template (optional), and notes (optional).
- The system MUST enforce unique carrier codes.
- The system MUST set `IsActive = true` by default.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.

**Edge cases:**
- Creating a carrier with a duplicate code MUST return a 409 Conflict error with code `DUPLICATE_CARRIER_CODE`.

#### 2.6.2 Update Carrier

- The system MUST support updating carrier fields: name, contact phone, contact email, website URL, tracking URL template, notes, and active status.
- The system MUST NOT allow changing the carrier code after creation.
- The system MUST update `ModifiedAtUtc` and `ModifiedByUserId` on every update.

#### 2.6.3 Deactivate Carrier

- The system MUST support deactivating a carrier by setting `IsActive = false`.
- Deactivating a carrier that is referenced by shipments with status `Dispatched` or `InTransit` SHOULD return a 409 Conflict error with code `CARRIER_HAS_ACTIVE_SHIPMENTS`.

#### 2.6.4 Get Carrier

- The system MUST support retrieving a single carrier by ID, including all service levels.

#### 2.6.5 List Carriers

- The system MUST support paginated listing of carriers.
- The system MUST support filtering by: name (contains), code (exact or starts-with), active status.
- The system MUST default to sorting by name ascending.

#### 2.6.6 Create Carrier Service Level

- The system MUST support creating service levels for a carrier with: name, code, estimated delivery days (optional), base rate (optional, decimal), per-kg rate (optional, decimal), and notes (optional).
- The system MUST enforce unique service level codes within the same carrier.

**Edge cases:**
- Creating a service level with a duplicate code within the same carrier MUST return a 409 Conflict error with code `DUPLICATE_SERVICE_LEVEL_CODE`.

#### 2.6.7 Update Carrier Service Level

- The system MUST support updating service level fields: name, estimated delivery days, base rate, per-kg rate, and notes.
- The system MUST NOT allow changing the service level code after creation.

#### 2.6.8 Delete Carrier Service Level

- The system MUST prevent deletion of a service level that is referenced by one or more shipments. This MUST return a 409 Conflict error with code `SERVICE_LEVEL_IN_USE`.

#### 2.6.9 List Carrier Service Levels

- The system MUST support listing all service levels for a carrier.

### 2.7 Customer Returns

#### 2.7.1 Create Customer Return

- The system MUST support creating a customer return (RMA) with: customer ID, sales order ID (optional -- reference to the original SO), reason, notes, and one or more return lines.
- Each return line MUST specify a product ID, warehouse ID (destination), location ID (optional), quantity, and optional notes.
- The system MUST auto-generate a unique return number following the format `RMA-{YYYYMMDD}-{sequential number padded to 4 digits}` (e.g., `RMA-20260406-0001`).
- The system MUST set the initial status to `Draft`.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.

**Edge cases:**
- Creating a return for a non-existent or soft-deleted customer MUST return a 404 Not Found error with code `CUSTOMER_NOT_FOUND`.
- Creating a return referencing a non-existent SO MUST return a 404 Not Found error with code `SO_NOT_FOUND`.
- Creating a return with no lines MUST return a 400 Validation error.

#### 2.7.2 Confirm Customer Return

- The system MUST support confirming a `Draft` customer return, transitioning it to `Confirmed`.
- Confirming an already-confirmed return MUST return a 409 Conflict error with code `RETURN_ALREADY_CONFIRMED`.
- The system MUST record `ConfirmedAtUtc` and `ConfirmedByUserId` when confirmed.

#### 2.7.3 Receive Customer Return

- When a confirmed customer return is physically received, the system MUST support transitioning it to `Received`.
- On receiving, the system MUST publish a `CustomerReturnReceivedEvent` via MassTransit.
- The event MUST contain: return ID, return number, customer ID, SO ID (if applicable), received by user ID, received timestamp, and for each line: product ID, warehouse ID, location ID, quantity, batch ID (if applicable).
- The Inventory service (consumer) MUST create a `StockMovement` with reason code `CustomerReturn`, reference type `SalesOrder`, and reference ID set to the original SO ID (or null if no SO reference).
- The Inventory service (consumer) MUST increase `QuantityOnHand` for the returned products at the specified warehouse/location.
- The system MUST record `ReceivedAtUtc` and `ReceivedByUserId` when received.

**Edge cases:**
- Receiving a return that is not in `Confirmed` status MUST return a 409 Conflict error with code `RETURN_NOT_RECEIVABLE`.
- Receiving an already-received return MUST return a 409 Conflict error with code `RETURN_ALREADY_RECEIVED`.

#### 2.7.4 Close Customer Return

- The system MUST support closing a `Received` customer return, transitioning it to `Closed`.
- Closing a return that is not in `Received` status MUST return a 409 Conflict error with code `RETURN_NOT_CLOSEABLE`.
- The system MUST record `ClosedAtUtc` and `ClosedByUserId` when closed.

#### 2.7.5 Cancel Customer Return

- The system MUST support cancelling a `Draft` or `Confirmed` customer return.
- Cancelling a received or closed return MUST return a 409 Conflict error with code `INVALID_RETURN_STATUS_TRANSITION`.

#### 2.7.6 Customer Return Status Machine

| From | To | Trigger | Rules |
|---|---|---|---|
| `Draft` | `Confirmed` | Manual confirm action | Return must have at least one line |
| `Draft` | `Cancelled` | Manual cancel action | -- |
| `Confirmed` | `Received` | Manual receive action | Physical receipt of goods |
| `Confirmed` | `Cancelled` | Manual cancel action | -- |
| `Received` | `Closed` | Manual close action | Administrative close |

#### 2.7.7 Get Customer Return

- The system MUST support retrieving a single customer return by ID, including all lines with product and customer details.

#### 2.7.8 Search Customer Returns

- The system MUST support paginated listing of customer returns.
- The system MUST support filtering by: customer ID, status, return number, SO ID, date range.
- The system MUST default to sorting by created date descending.

### 2.8 Fulfillment Event History

- The system MUST maintain an immutable log of all fulfillment operations events.
- Events MUST include: event type, entity type, entity ID, user ID, timestamp, and a JSON payload with before/after state where applicable.
- Event types MUST include: `SalesOrderCreated`, `SalesOrderConfirmed`, `SalesOrderCancelled`, `SalesOrderCompleted`, `PickListGenerated`, `PickListCompleted`, `PickListCancelled`, `ParcelCreated`, `ShipmentDispatched`, `ShipmentStatusUpdated`, `CustomerReturnCreated`, `CustomerReturnConfirmed`, `CustomerReturnReceived`, `CustomerReturnClosed`, `CustomerReturnCancelled`.
- The system MUST support paginated querying of fulfillment events with filtering by event type, entity type, entity ID, user ID, and date range.

### 2.9 Inter-Service Event Contracts (MassTransit)

Event contracts are defined in `Warehouse.ServiceModel/Events/`.

#### 2.9.1 ShipmentDispatchedEvent

- The system MUST publish this event when a shipment is dispatched.
- The event contract MUST include:
  - `ShipmentId` (int)
  - `ShipmentNumber` (string)
  - `SalesOrderId` (int)
  - `SalesOrderNumber` (string)
  - `WarehouseId` (int)
  - `DispatchedByUserId` (int)
  - `DispatchedAtUtc` (DateTime)
  - `Lines` (collection of):
    - `ShipmentLineId` (int)
    - `ProductId` (int)
    - `Quantity` (decimal)
    - `LocationId` (int?)
    - `BatchId` (int?)
- Event naming follows the convention `{Domain}.{Entity}.{PastTenseVerb}`: `Fulfillment.Shipment.Dispatched`.

#### 2.9.2 CustomerReturnReceivedEvent

- The system MUST publish this event when a customer return is physically received.
- The event contract MUST include:
  - `CustomerReturnId` (int)
  - `CustomerReturnNumber` (string)
  - `CustomerId` (int)
  - `SalesOrderId` (int?)
  - `ReceivedByUserId` (int)
  - `ReceivedAtUtc` (DateTime)
  - `Lines` (collection of):
    - `CustomerReturnLineId` (int)
    - `ProductId` (int)
    - `WarehouseId` (int)
    - `LocationId` (int?)
    - `Quantity` (decimal)
    - `BatchId` (int?)
- Event naming: `Fulfillment.CustomerReturn.Received`.

#### 2.9.3 StockReservationRequestedEvent

- The system MUST publish this event when a pick list is generated to request stock reservation.
- The event contract MUST include:
  - `PickListId` (int)
  - `PickListNumber` (string)
  - `SalesOrderId` (int)
  - `SalesOrderNumber` (string)
  - `RequestedByUserId` (int)
  - `RequestedAtUtc` (DateTime)
  - `Lines` (collection of):
    - `PickListLineId` (int)
    - `ProductId` (int)
    - `WarehouseId` (int)
    - `LocationId` (int?)
    - `Quantity` (decimal)
- Event naming: `Fulfillment.StockReservation.Requested`.

#### 2.9.4 StockReservationReleasedEvent

- The system MUST publish this event when a pick list is cancelled or a short pick releases excess reservation.
- The event contract MUST include:
  - `PickListId` (int)
  - `PickListNumber` (string)
  - `SalesOrderId` (int)
  - `ReleasedByUserId` (int)
  - `ReleasedAtUtc` (DateTime)
  - `Lines` (collection of):
    - `PickListLineId` (int)
    - `ProductId` (int)
    - `WarehouseId` (int)
    - `LocationId` (int?)
    - `Quantity` (decimal)
- Event naming: `Fulfillment.StockReservation.Released`.

---

## 3. Validation Rules

### 3.1 Sales Order

| # | Field | Rule | Error Code |
|---|---|---|---|
| V1 | CustomerId | Required. Must reference an existing, active customer (cross-service: Customers). | `INVALID_CUSTOMER` |
| V2 | WarehouseId | Required. Must reference an existing, active warehouse (cross-service: Inventory). | `INVALID_WAREHOUSE` |
| V3 | RequestedShipDate | Optional. Must be today or a future date when provided. | `INVALID_SHIP_DATE` |
| V4 | ShippingStreetLine1 | Required. 1-200 characters. | `INVALID_SHIPPING_ADDRESS` |
| V5 | ShippingStreetLine2 | Optional. Max 200 characters. | `INVALID_SHIPPING_ADDRESS` |
| V6 | ShippingCity | Required. 1-100 characters. | `INVALID_SHIPPING_ADDRESS` |
| V7 | ShippingStateProvince | Optional. Max 100 characters. | `INVALID_SHIPPING_ADDRESS` |
| V8 | ShippingPostalCode | Required. 1-20 characters. | `INVALID_SHIPPING_ADDRESS` |
| V9 | ShippingCountryCode | Required. ISO 3166-1 alpha-2 (2-letter code). | `INVALID_SHIPPING_ADDRESS` |
| V10 | CarrierId | Optional. Must reference an existing, active carrier when provided. | `INVALID_CARRIER` |
| V11 | CarrierServiceLevelId | Optional. Must reference an existing service level belonging to the specified carrier when provided. | `INVALID_SERVICE_LEVEL` |
| V12 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |
| V13 | Lines | At least one line required on create. | `SO_MUST_HAVE_LINES` |

### 3.2 Sales Order Line

| # | Field | Rule | Error Code |
|---|---|---|---|
| V14 | ProductId | Required. Must reference an existing, non-deleted product (cross-service: Inventory). | `INVALID_PRODUCT` |
| V15 | OrderedQuantity | Required. Must be > 0. | `INVALID_QUANTITY` |
| V16 | UnitPrice | Required. Must be >= 0. | `INVALID_UNIT_PRICE` |
| V17 | ProductId | Must be unique within the same SO. | `DUPLICATE_SO_LINE` |
| V18 | Notes | Optional. Max 500 characters. | `INVALID_LINE_NOTES` |

### 3.3 Pick List

| # | Field | Rule | Error Code |
|---|---|---|---|
| V19 | SalesOrderId | Required. Must reference an SO in `Confirmed` or `Picking` status. | `SO_NOT_PICKABLE` |

### 3.4 Pick Confirmation

| # | Field | Rule | Error Code |
|---|---|---|---|
| V20 | ActualQuantity | Required. Must be > 0. | `INVALID_QUANTITY` |
| V21 | ActualQuantity | Must not exceed the requested pick quantity. | `OVER_PICK` |

### 3.5 Parcel

| # | Field | Rule | Error Code |
|---|---|---|---|
| V22 | SalesOrderId | Required. SO must be in `Picking` (with completed picks) or `Packed` status. | `SO_NOT_PACKABLE` |
| V23 | Weight | Optional. Must be > 0 when provided (decimal, kg). | `INVALID_WEIGHT` |
| V24 | Length | Optional. Must be > 0 when provided (decimal, cm). | `INVALID_DIMENSIONS` |
| V25 | Width | Optional. Must be > 0 when provided (decimal, cm). | `INVALID_DIMENSIONS` |
| V26 | Height | Optional. Must be > 0 when provided (decimal, cm). | `INVALID_DIMENSIONS` |
| V27 | TrackingNumber | Optional. Max 100 characters. | `INVALID_TRACKING_NUMBER` |
| V28 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |

### 3.6 Parcel Item

| # | Field | Rule | Error Code |
|---|---|---|---|
| V29 | ProductId | Required. Must reference a product on the SO. | `INVALID_PRODUCT` |
| V30 | Quantity | Required. Must be > 0. | `INVALID_QUANTITY` |
| V31 | PickListLineId | Required. Must reference a confirmed (picked) pick list line. | `INVALID_PICK_LINE` |
| V32 | Quantity (cross-field) | Total packed quantity across all parcels for a product must not exceed total picked quantity. | `OVER_PACK` |

### 3.7 Shipment

| # | Field | Rule | Error Code |
|---|---|---|---|
| V33 | SalesOrderId | Required. SO must be in `Packed` status. | `SO_NOT_DISPATCHABLE` |
| V34 | CarrierId | Optional. Must reference an existing, active carrier when provided. | `INVALID_CARRIER` |
| V35 | CarrierServiceLevelId | Optional. Must reference a service level of the specified carrier when provided. | `INVALID_SERVICE_LEVEL` |
| V36 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |

### 3.8 Shipment Status Update

| # | Field | Rule | Error Code |
|---|---|---|---|
| V37 | Status | Required. Must be a valid shipment status value. | `INVALID_SHIPMENT_STATUS` |
| V38 | Status (state-based) | Must follow valid transition rules. | `INVALID_SHIPMENT_STATUS_TRANSITION` |
| V39 | TrackingNumber | Optional. Max 100 characters. | `INVALID_TRACKING_NUMBER` |
| V40 | TrackingUrl | Optional. Max 500 characters. Must be a valid URL format when provided. | `INVALID_TRACKING_URL` |
| V41 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |

### 3.9 Carrier

| # | Field | Rule | Error Code |
|---|---|---|---|
| V42 | Code | Required. 1-20 characters. Alphanumeric + hyphens. | `INVALID_CARRIER_CODE` |
| V43 | Code | Must be unique across all carriers. | `DUPLICATE_CARRIER_CODE` |
| V44 | Name | Required. 1-200 characters. | `INVALID_CARRIER_NAME` |
| V45 | ContactPhone | Optional. Max 20 characters. | `INVALID_PHONE` |
| V46 | ContactEmail | Optional. Valid email format. Max 256 characters. | `INVALID_EMAIL` |
| V47 | WebsiteUrl | Optional. Max 500 characters. Must be a valid URL format when provided. | `INVALID_URL` |
| V48 | TrackingUrlTemplate | Optional. Max 500 characters. SHOULD contain `{trackingNumber}` placeholder. | `INVALID_URL_TEMPLATE` |
| V49 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |

### 3.10 Carrier Service Level

| # | Field | Rule | Error Code |
|---|---|---|---|
| V50 | Code | Required. 1-20 characters. Alphanumeric + hyphens. | `INVALID_SERVICE_LEVEL_CODE` |
| V51 | Code | Must be unique within the same carrier. | `DUPLICATE_SERVICE_LEVEL_CODE` |
| V52 | Name | Required. 1-100 characters. | `INVALID_SERVICE_LEVEL_NAME` |
| V53 | EstimatedDeliveryDays | Optional. Must be >= 1 and <= 365 when provided. | `INVALID_DELIVERY_DAYS` |
| V54 | BaseRate | Optional. Must be >= 0 when provided. | `INVALID_RATE` |
| V55 | PerKgRate | Optional. Must be >= 0 when provided. | `INVALID_RATE` |
| V56 | Notes | Optional. Max 500 characters. | `INVALID_NOTES` |

### 3.11 Customer Return

| # | Field | Rule | Error Code |
|---|---|---|---|
| V57 | CustomerId | Required. Must reference an existing, active customer. | `INVALID_CUSTOMER` |
| V58 | SalesOrderId | Optional. Must reference an existing SO when provided. | `INVALID_SO_REFERENCE` |
| V59 | Reason | Required. 1-500 characters. | `INVALID_RETURN_REASON` |
| V60 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |
| V61 | Lines | At least one line required on create. | `RETURN_MUST_HAVE_LINES` |

### 3.12 Customer Return Line

| # | Field | Rule | Error Code |
|---|---|---|---|
| V62 | ProductId | Required. Must reference an existing product. | `INVALID_PRODUCT` |
| V63 | WarehouseId | Required. Must reference an existing warehouse. | `INVALID_WAREHOUSE` |
| V64 | LocationId | Optional. Must reference an existing location within the warehouse when provided. | `INVALID_LOCATION` |
| V65 | Quantity | Required. Must be > 0. | `INVALID_QUANTITY` |
| V66 | Notes | Optional. Max 500 characters. | `INVALID_LINE_NOTES` |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| **Sales Order errors** | | | | |
| E1 | Sales order not found | 404 | `SO_NOT_FOUND` | Sales order not found. |
| E2 | SO line not found | 404 | `SO_LINE_NOT_FOUND` | Sales order line not found. |
| E3 | SO not editable (not Draft) | 409 | `SO_NOT_EDITABLE` | Sales order can only be edited in Draft status. |
| E4 | Invalid SO status transition | 409 | `INVALID_SO_STATUS_TRANSITION` | Cannot transition sales order from {from} to {to}. |
| E5 | SO must have lines | 409 | `SO_MUST_HAVE_LINES` | Sales order must have at least one line. |
| E6 | Duplicate SO line (same product) | 409 | `DUPLICATE_SO_LINE` | This product is already on the sales order. |
| E7 | SO has pick lists (cannot cancel) | 409 | `SO_HAS_PICK_LISTS` | Cannot cancel sales order -- pick lists have been generated. |
| E8 | SO already completed | 409 | `SO_ALREADY_COMPLETED` | Sales order is already completed. |
| E9 | SO already shipped | 409 | `SO_ALREADY_SHIPPED` | Sales order has already been shipped. |
| E10 | Customer not found (or soft-deleted) | 404 | `CUSTOMER_NOT_FOUND` | Customer not found. |
| E11 | Customer inactive | 409 | `CUSTOMER_INACTIVE` | The customer is inactive and cannot be used for new sales orders. |
| **Pick List errors** | | | | |
| E12 | Pick list not found | 404 | `PICK_LIST_NOT_FOUND` | Pick list not found. |
| E13 | SO not pickable | 409 | `SO_NOT_PICKABLE` | Sales order is not in a pickable status. |
| E14 | Insufficient stock for pick | 409 | `INSUFFICIENT_STOCK` | Insufficient available stock at the specified location. |
| E15 | SO fully allocated | 409 | `SO_FULLY_ALLOCATED` | All sales order lines are already allocated to pick lists. |
| E16 | Pick list line already picked | 409 | `LINE_ALREADY_PICKED` | This pick list line has already been picked. |
| E17 | Over-pick | 409 | `OVER_PICK` | Picked quantity exceeds the requested quantity. |
| E18 | Pick list already completed | 409 | `PICK_LIST_ALREADY_COMPLETED` | Pick list has already been completed. |
| E19 | Pick list line not found | 404 | `PICK_LIST_LINE_NOT_FOUND` | Pick list line not found. |
| **Packing errors** | | | | |
| E20 | Parcel not found | 404 | `PARCEL_NOT_FOUND` | Parcel not found. |
| E21 | SO not packable | 409 | `SO_NOT_PACKABLE` | Sales order is not in a packable status. |
| E22 | Over-pack | 409 | `OVER_PACK` | Packed quantity exceeds the picked quantity for this product. |
| E23 | Parcel not editable (SO shipped) | 409 | `PARCEL_NOT_EDITABLE` | Parcel cannot be edited after shipment dispatch. |
| E24 | Invalid pick line reference | 400 | `INVALID_PICK_LINE` | The specified pick list line has not been picked or does not exist. |
| E25 | Empty parcel on dispatch | 409 | `EMPTY_PARCEL` | Cannot dispatch -- one or more parcels have no packed items. |
| **Shipment errors** | | | | |
| E26 | Shipment not found | 404 | `SHIPMENT_NOT_FOUND` | Shipment not found. |
| E27 | SO not dispatchable | 409 | `SO_NOT_DISPATCHABLE` | Sales order is not in a dispatchable status (must be Packed). |
| E28 | Invalid shipment status transition | 409 | `INVALID_SHIPMENT_STATUS_TRANSITION` | Cannot transition shipment from {from} to {to}. |
| **Carrier errors** | | | | |
| E29 | Carrier not found | 404 | `CARRIER_NOT_FOUND` | Carrier not found. |
| E30 | Duplicate carrier code | 409 | `DUPLICATE_CARRIER_CODE` | A carrier with this code already exists. |
| E31 | Carrier has active shipments | 409 | `CARRIER_HAS_ACTIVE_SHIPMENTS` | Cannot deactivate carrier -- active shipments exist. |
| E32 | Service level not found | 404 | `SERVICE_LEVEL_NOT_FOUND` | Carrier service level not found. |
| E33 | Duplicate service level code | 409 | `DUPLICATE_SERVICE_LEVEL_CODE` | A service level with this code already exists for this carrier. |
| E34 | Service level in use | 409 | `SERVICE_LEVEL_IN_USE` | Cannot delete service level -- it is referenced by shipments. |
| E35 | Invalid carrier reference | 400 | `INVALID_CARRIER` | The specified carrier does not exist or is inactive. |
| E36 | Invalid service level reference | 400 | `INVALID_SERVICE_LEVEL` | The specified service level does not belong to the carrier. |
| **Customer Return errors** | | | | |
| E37 | Customer return not found | 404 | `RETURN_NOT_FOUND` | Customer return not found. |
| E38 | Return already confirmed | 409 | `RETURN_ALREADY_CONFIRMED` | Customer return has already been confirmed. |
| E39 | Return already received | 409 | `RETURN_ALREADY_RECEIVED` | Customer return has already been received. |
| E40 | Return not receivable | 409 | `RETURN_NOT_RECEIVABLE` | Customer return is not in Confirmed status. |
| E41 | Return not closeable | 409 | `RETURN_NOT_CLOSEABLE` | Customer return is not in Received status. |
| E42 | Invalid return status transition | 409 | `INVALID_RETURN_STATUS_TRANSITION` | Cannot transition customer return from {from} to {to}. |
| E43 | Return must have lines | 400 | `RETURN_MUST_HAVE_LINES` | Customer return must have at least one line. |
| **Cross-service errors** | | | | |
| E44 | Invalid product reference | 400 | `INVALID_PRODUCT` | The specified product does not exist or is inactive. |
| E45 | Invalid warehouse reference | 400 | `INVALID_WAREHOUSE` | The specified warehouse does not exist or is inactive. |
| E46 | Invalid location reference | 400 | `INVALID_LOCATION` | The specified location does not exist or does not belong to the warehouse. |
| E47 | Invalid SO reference | 400 | `INVALID_SO_REFERENCE` | The specified sales order does not exist. |
| **Common errors** | | | | |
| E48 | Validation failure (field-level) | 400 | `VALIDATION_ERROR` | One or more fields are invalid. (ProblemDetails with field errors) |
| E49 | Insufficient permissions | 403 | `FORBIDDEN` | You do not have permission to perform this action. |
| E50 | Unauthenticated | 401 | `UNAUTHORIZED` | Authentication is required. |

All error responses MUST use ProblemDetails (RFC 7807) format:

```json
{
  "type": "https://warehouse.local/errors/{error-code}",
  "title": "Short error title",
  "status": 400,
  "detail": "Human-readable description.",
  "instance": "/api/v1/sales-orders/{id}",
  "errors": {}
}
```

---

## 5. API Endpoints

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| **Sales Orders** | | | | |
| POST | `/api/v1/sales-orders` | Create SO (with lines) | Yes | `sales-orders:create` |
| GET | `/api/v1/sales-orders` | List/search SOs (paginated) | Yes | `sales-orders:read` |
| GET | `/api/v1/sales-orders/{id}` | Get SO by ID (with lines, progress) | Yes | `sales-orders:read` |
| PUT | `/api/v1/sales-orders/{id}` | Update SO header | Yes | `sales-orders:update` |
| POST | `/api/v1/sales-orders/{id}/confirm` | Confirm SO (Draft -> Confirmed) | Yes | `sales-orders:update` |
| POST | `/api/v1/sales-orders/{id}/cancel` | Cancel SO | Yes | `sales-orders:update` |
| POST | `/api/v1/sales-orders/{id}/complete` | Complete SO (Shipped -> Completed) | Yes | `sales-orders:update` |
| **Sales Order Lines** | | | | |
| POST | `/api/v1/sales-orders/{soId}/lines` | Add line to SO | Yes | `sales-orders:update` |
| PUT | `/api/v1/sales-orders/{soId}/lines/{lineId}` | Update SO line | Yes | `sales-orders:update` |
| DELETE | `/api/v1/sales-orders/{soId}/lines/{lineId}` | Remove SO line | Yes | `sales-orders:update` |
| **Pick Lists** | | | | |
| POST | `/api/v1/pick-lists` | Generate pick list for SO | Yes | `pick-lists:create` |
| GET | `/api/v1/pick-lists` | List pick lists (paginated) | Yes | `pick-lists:read` |
| GET | `/api/v1/pick-lists/{id}` | Get pick list by ID (with lines) | Yes | `pick-lists:read` |
| POST | `/api/v1/pick-lists/{id}/lines/{lineId}/pick` | Confirm pick for a line | Yes | `pick-lists:update` |
| POST | `/api/v1/pick-lists/{id}/cancel` | Cancel pick list | Yes | `pick-lists:update` |
| **Packing** | | | | |
| POST | `/api/v1/sales-orders/{soId}/parcels` | Create parcel | Yes | `packing:create` |
| GET | `/api/v1/sales-orders/{soId}/parcels` | List parcels for SO | Yes | `packing:read` |
| GET | `/api/v1/sales-orders/{soId}/parcels/{parcelId}` | Get parcel by ID (with items) | Yes | `packing:read` |
| PUT | `/api/v1/sales-orders/{soId}/parcels/{parcelId}` | Update parcel | Yes | `packing:update` |
| DELETE | `/api/v1/sales-orders/{soId}/parcels/{parcelId}` | Remove parcel | Yes | `packing:update` |
| POST | `/api/v1/sales-orders/{soId}/parcels/{parcelId}/items` | Add items to parcel | Yes | `packing:update` |
| DELETE | `/api/v1/sales-orders/{soId}/parcels/{parcelId}/items/{itemId}` | Remove item from parcel | Yes | `packing:update` |
| **Shipments** | | | | |
| POST | `/api/v1/shipments` | Create shipment (dispatch) | Yes | `shipments:create` |
| GET | `/api/v1/shipments` | List/search shipments (paginated) | Yes | `shipments:read` |
| GET | `/api/v1/shipments/{id}` | Get shipment by ID (with lines, parcels) | Yes | `shipments:read` |
| POST | `/api/v1/shipments/{id}/status` | Update shipment status | Yes | `shipments:update` |
| GET | `/api/v1/shipments/{id}/tracking` | Get shipment tracking history | Yes | `shipments:read` |
| **Carriers** | | | | |
| POST | `/api/v1/carriers` | Create carrier | Yes | `carriers:create` |
| GET | `/api/v1/carriers` | List carriers (paginated) | Yes | `carriers:read` |
| GET | `/api/v1/carriers/{id}` | Get carrier by ID (with service levels) | Yes | `carriers:read` |
| PUT | `/api/v1/carriers/{id}` | Update carrier | Yes | `carriers:update` |
| POST | `/api/v1/carriers/{id}/deactivate` | Deactivate carrier | Yes | `carriers:update` |
| **Carrier Service Levels** | | | | |
| POST | `/api/v1/carriers/{carrierId}/service-levels` | Create service level | Yes | `carriers:update` |
| GET | `/api/v1/carriers/{carrierId}/service-levels` | List service levels for carrier | Yes | `carriers:read` |
| PUT | `/api/v1/carriers/{carrierId}/service-levels/{levelId}` | Update service level | Yes | `carriers:update` |
| DELETE | `/api/v1/carriers/{carrierId}/service-levels/{levelId}` | Delete service level | Yes | `carriers:update` |
| **Customer Returns** | | | | |
| POST | `/api/v1/customer-returns` | Create customer return (with lines) | Yes | `customer-returns:create` |
| GET | `/api/v1/customer-returns` | List customer returns (paginated) | Yes | `customer-returns:read` |
| GET | `/api/v1/customer-returns/{id}` | Get customer return by ID (with lines) | Yes | `customer-returns:read` |
| POST | `/api/v1/customer-returns/{id}/confirm` | Confirm return | Yes | `customer-returns:update` |
| POST | `/api/v1/customer-returns/{id}/receive` | Receive return | Yes | `customer-returns:update` |
| POST | `/api/v1/customer-returns/{id}/close` | Close return | Yes | `customer-returns:update` |
| POST | `/api/v1/customer-returns/{id}/cancel` | Cancel return | Yes | `customer-returns:update` |
| **Fulfillment Events** | | | | |
| GET | `/api/v1/fulfillment-events` | List fulfillment events (paginated) | Yes | `fulfillment-events:read` |
| **Health** | | | | |
| GET | `/health` | Liveness check | No | -- |
| GET | `/health/ready` | Readiness check (DB + RabbitMQ) | No | -- |

---

## 6. Database Schema

**Schema name:** `fulfillment`

### Tables

**fulfillment.SalesOrders**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| OrderNumber | NVARCHAR(20) | NOT NULL, UNIQUE |
| CustomerId | INT | NOT NULL (cross-schema ref to customers.Customers -- no EF navigation) |
| Status | NVARCHAR(30) | NOT NULL, DEFAULT 'Draft' |
| WarehouseId | INT | NOT NULL (cross-schema ref to inventory.Warehouses -- no EF navigation) |
| RequestedShipDate | DATE | NULL |
| ShippingStreetLine1 | NVARCHAR(200) | NOT NULL |
| ShippingStreetLine2 | NVARCHAR(200) | NULL |
| ShippingCity | NVARCHAR(100) | NOT NULL |
| ShippingStateProvince | NVARCHAR(100) | NULL |
| ShippingPostalCode | NVARCHAR(20) | NOT NULL |
| ShippingCountryCode | NVARCHAR(2) | NOT NULL |
| CarrierId | INT | NULL, FK -> fulfillment.Carriers(Id) |
| CarrierServiceLevelId | INT | NULL, FK -> fulfillment.CarrierServiceLevels(Id) |
| Notes | NVARCHAR(2000) | NULL |
| TotalAmount | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| ModifiedAtUtc | DATETIME2(7) | NULL |
| ModifiedByUserId | INT | NULL (cross-schema ref to auth.Users) |
| ConfirmedAtUtc | DATETIME2(7) | NULL |
| ConfirmedByUserId | INT | NULL (cross-schema ref to auth.Users) |
| ShippedAtUtc | DATETIME2(7) | NULL |
| CompletedAtUtc | DATETIME2(7) | NULL |
| CompletedByUserId | INT | NULL (cross-schema ref to auth.Users) |

**fulfillment.SalesOrderLines**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| SalesOrderId | INT | NOT NULL, FK -> fulfillment.SalesOrders(Id) |
| ProductId | INT | NOT NULL (cross-schema ref to inventory.Products -- no EF navigation) |
| OrderedQuantity | DECIMAL(18,4) | NOT NULL |
| UnitPrice | DECIMAL(18,4) | NOT NULL |
| LineTotal | DECIMAL(18,4) | NOT NULL (computed: OrderedQuantity * UnitPrice) |
| PickedQuantity | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| PackedQuantity | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| ShippedQuantity | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| Notes | NVARCHAR(500) | NULL |

**fulfillment.PickLists**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| PickListNumber | NVARCHAR(20) | NOT NULL, UNIQUE |
| SalesOrderId | INT | NOT NULL, FK -> fulfillment.SalesOrders(Id) |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'Pending' |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| CompletedAtUtc | DATETIME2(7) | NULL |

**fulfillment.PickListLines**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| PickListId | INT | NOT NULL, FK -> fulfillment.PickLists(Id) |
| SalesOrderLineId | INT | NOT NULL, FK -> fulfillment.SalesOrderLines(Id) |
| ProductId | INT | NOT NULL (cross-schema ref to inventory.Products) |
| WarehouseId | INT | NOT NULL (cross-schema ref to inventory.Warehouses) |
| LocationId | INT | NULL (cross-schema ref to inventory.StorageLocations) |
| RequestedQuantity | DECIMAL(18,4) | NOT NULL |
| ActualQuantity | DECIMAL(18,4) | NULL |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'Pending' |
| PickedAtUtc | DATETIME2(7) | NULL |
| PickedByUserId | INT | NULL (cross-schema ref to auth.Users) |

**fulfillment.Parcels**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ParcelNumber | NVARCHAR(20) | NOT NULL, UNIQUE |
| SalesOrderId | INT | NOT NULL, FK -> fulfillment.SalesOrders(Id) |
| Weight | DECIMAL(10,3) | NULL |
| Length | DECIMAL(10,2) | NULL |
| Width | DECIMAL(10,2) | NULL |
| Height | DECIMAL(10,2) | NULL |
| TrackingNumber | NVARCHAR(100) | NULL |
| Notes | NVARCHAR(2000) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**fulfillment.ParcelItems**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ParcelId | INT | NOT NULL, FK -> fulfillment.Parcels(Id) |
| PickListLineId | INT | NOT NULL, FK -> fulfillment.PickListLines(Id) |
| ProductId | INT | NOT NULL (cross-schema ref to inventory.Products) |
| Quantity | DECIMAL(18,4) | NOT NULL |

**fulfillment.Shipments**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ShipmentNumber | NVARCHAR(20) | NOT NULL, UNIQUE |
| SalesOrderId | INT | NOT NULL, FK -> fulfillment.SalesOrders(Id), UNIQUE |
| CarrierId | INT | NULL, FK -> fulfillment.Carriers(Id) |
| CarrierServiceLevelId | INT | NULL, FK -> fulfillment.CarrierServiceLevels(Id) |
| Status | NVARCHAR(30) | NOT NULL, DEFAULT 'Dispatched' |
| ShippingStreetLine1 | NVARCHAR(200) | NOT NULL |
| ShippingStreetLine2 | NVARCHAR(200) | NULL |
| ShippingCity | NVARCHAR(100) | NOT NULL |
| ShippingStateProvince | NVARCHAR(100) | NULL |
| ShippingPostalCode | NVARCHAR(20) | NOT NULL |
| ShippingCountryCode | NVARCHAR(2) | NOT NULL |
| TrackingNumber | NVARCHAR(100) | NULL |
| TrackingUrl | NVARCHAR(500) | NULL |
| Notes | NVARCHAR(2000) | NULL |
| DispatchedAtUtc | DATETIME2(7) | NOT NULL |
| DispatchedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |

**fulfillment.ShipmentLines**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ShipmentId | INT | NOT NULL, FK -> fulfillment.Shipments(Id) |
| SalesOrderLineId | INT | NOT NULL, FK -> fulfillment.SalesOrderLines(Id) |
| ProductId | INT | NOT NULL (cross-schema ref to inventory.Products) |
| Quantity | DECIMAL(18,4) | NOT NULL |
| LocationId | INT | NULL (cross-schema ref to inventory.StorageLocations) |
| BatchId | INT | NULL (cross-schema ref to inventory.Batches) |

**fulfillment.ShipmentTrackingEntries**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ShipmentId | INT | NOT NULL, FK -> fulfillment.Shipments(Id) |
| Status | NVARCHAR(30) | NOT NULL |
| Notes | NVARCHAR(2000) | NULL |
| OccurredAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| RecordedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |

**fulfillment.Carriers**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Code | NVARCHAR(20) | NOT NULL, UNIQUE |
| Name | NVARCHAR(200) | NOT NULL |
| ContactPhone | NVARCHAR(20) | NULL |
| ContactEmail | NVARCHAR(256) | NULL |
| WebsiteUrl | NVARCHAR(500) | NULL |
| TrackingUrlTemplate | NVARCHAR(500) | NULL |
| Notes | NVARCHAR(2000) | NULL |
| IsActive | BIT | NOT NULL, DEFAULT 1 |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| ModifiedAtUtc | DATETIME2(7) | NULL |
| ModifiedByUserId | INT | NULL (cross-schema ref to auth.Users) |

**fulfillment.CarrierServiceLevels**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| CarrierId | INT | NOT NULL, FK -> fulfillment.Carriers(Id) |
| Code | NVARCHAR(20) | NOT NULL |
| Name | NVARCHAR(100) | NOT NULL |
| EstimatedDeliveryDays | INT | NULL |
| BaseRate | DECIMAL(18,4) | NULL |
| PerKgRate | DECIMAL(18,4) | NULL |
| Notes | NVARCHAR(500) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**fulfillment.CustomerReturns**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ReturnNumber | NVARCHAR(20) | NOT NULL, UNIQUE |
| CustomerId | INT | NOT NULL (cross-schema ref to customers.Customers -- no EF navigation) |
| SalesOrderId | INT | NULL, FK -> fulfillment.SalesOrders(Id) |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'Draft' |
| Reason | NVARCHAR(500) | NOT NULL |
| Notes | NVARCHAR(2000) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| ConfirmedAtUtc | DATETIME2(7) | NULL |
| ConfirmedByUserId | INT | NULL (cross-schema ref to auth.Users) |
| ReceivedAtUtc | DATETIME2(7) | NULL |
| ReceivedByUserId | INT | NULL (cross-schema ref to auth.Users) |
| ClosedAtUtc | DATETIME2(7) | NULL |
| ClosedByUserId | INT | NULL (cross-schema ref to auth.Users) |

**fulfillment.CustomerReturnLines**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| CustomerReturnId | INT | NOT NULL, FK -> fulfillment.CustomerReturns(Id) |
| ProductId | INT | NOT NULL (cross-schema ref to inventory.Products) |
| WarehouseId | INT | NOT NULL (cross-schema ref to inventory.Warehouses) |
| LocationId | INT | NULL (cross-schema ref to inventory.StorageLocations) |
| Quantity | DECIMAL(18,4) | NOT NULL |
| BatchId | INT | NULL (cross-schema ref to inventory.Batches) |
| Notes | NVARCHAR(500) | NULL |

**fulfillment.FulfillmentEvents**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| EventType | NVARCHAR(50) | NOT NULL |
| EntityType | NVARCHAR(50) | NOT NULL |
| EntityId | INT | NOT NULL |
| UserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| OccurredAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| Payload | NVARCHAR(MAX) | NULL |

### Indexes

| Name | Table | Columns | Type |
|---|---|---|---|
| IX_SalesOrders_OrderNumber | SalesOrders | OrderNumber | Unique |
| IX_SalesOrders_CustomerId | SalesOrders | CustomerId | Non-unique |
| IX_SalesOrders_Status | SalesOrders | Status | Non-unique |
| IX_SalesOrders_CreatedAtUtc | SalesOrders | CreatedAtUtc | Non-unique |
| IX_SalesOrders_WarehouseId | SalesOrders | WarehouseId | Non-unique |
| IX_SalesOrderLines_SalesOrderId | SalesOrderLines | SalesOrderId | Non-unique |
| IX_SalesOrderLines_ProductId | SalesOrderLines | ProductId | Non-unique |
| IX_SalesOrderLines_SOId_ProductId | SalesOrderLines | SalesOrderId, ProductId | Unique |
| IX_PickLists_PickListNumber | PickLists | PickListNumber | Unique |
| IX_PickLists_SalesOrderId | PickLists | SalesOrderId | Non-unique |
| IX_PickLists_Status | PickLists | Status | Non-unique |
| IX_PickLists_CreatedAtUtc | PickLists | CreatedAtUtc | Non-unique |
| IX_PickListLines_PickListId | PickListLines | PickListId | Non-unique |
| IX_PickListLines_SalesOrderLineId | PickListLines | SalesOrderLineId | Non-unique |
| IX_PickListLines_ProductId | PickListLines | ProductId | Non-unique |
| IX_Parcels_ParcelNumber | Parcels | ParcelNumber | Unique |
| IX_Parcels_SalesOrderId | Parcels | SalesOrderId | Non-unique |
| IX_ParcelItems_ParcelId | ParcelItems | ParcelId | Non-unique |
| IX_ParcelItems_PickListLineId | ParcelItems | PickListLineId | Non-unique |
| IX_Shipments_ShipmentNumber | Shipments | ShipmentNumber | Unique |
| IX_Shipments_SalesOrderId | Shipments | SalesOrderId | Unique |
| IX_Shipments_CarrierId | Shipments | CarrierId | Non-unique |
| IX_Shipments_Status | Shipments | Status | Non-unique |
| IX_Shipments_DispatchedAtUtc | Shipments | DispatchedAtUtc | Non-unique |
| IX_ShipmentLines_ShipmentId | ShipmentLines | ShipmentId | Non-unique |
| IX_ShipmentTrackingEntries_ShipmentId | ShipmentTrackingEntries | ShipmentId | Non-unique |
| IX_Carriers_Code | Carriers | Code | Unique |
| IX_Carriers_Name | Carriers | Name | Non-unique |
| IX_CarrierServiceLevels_CarrierId | CarrierServiceLevels | CarrierId | Non-unique |
| IX_CarrierServiceLevels_CarrierId_Code | CarrierServiceLevels | CarrierId, Code | Unique |
| IX_CustomerReturns_ReturnNumber | CustomerReturns | ReturnNumber | Unique |
| IX_CustomerReturns_CustomerId | CustomerReturns | CustomerId | Non-unique |
| IX_CustomerReturns_Status | CustomerReturns | Status | Non-unique |
| IX_CustomerReturns_SalesOrderId | CustomerReturns | SalesOrderId | Non-unique |
| IX_CustomerReturns_CreatedAtUtc | CustomerReturns | CreatedAtUtc | Non-unique |
| IX_CustomerReturnLines_CustomerReturnId | CustomerReturnLines | CustomerReturnId | Non-unique |
| IX_FulfillmentEvents_EventType | FulfillmentEvents | EventType | Non-unique |
| IX_FulfillmentEvents_EntityType_EntityId | FulfillmentEvents | EntityType, EntityId | Non-unique |
| IX_FulfillmentEvents_OccurredAtUtc | FulfillmentEvents | OccurredAtUtc | Non-unique |

---

## 7. Versioning Notes

- **v1 -- Initial specification (2026-04-06)**
  - Sales order CRUD with status machine (Draft -> Confirmed -> Picking -> Packed -> Shipped -> Completed)
  - Sales order lines referencing cross-service product catalog and customer records
  - Pick list generation with stock reservation via MassTransit events
  - Pick execution (confirm individual lines) with short-pick handling
  - Packing workflow: parcels with items, weight, dimensions, tracking
  - Dispatch: shipment creation from packed orders with StockMovement event
  - Shipment tracking with status history
  - Carrier management with service levels and rate tables
  - Customer returns (RMA) with status machine (Draft -> Confirmed -> Received -> Closed)
  - Immutable fulfillment event history
  - MassTransit event contracts: ShipmentDispatchedEvent, CustomerReturnReceivedEvent, StockReservationRequestedEvent, StockReservationReleasedEvent
  - ProblemDetails error responses
  - Full validation rule set
  - Database schema on `fulfillment` schema
  - Single shipment per SO (v1 limitation -- partial shipments deferred)
  - Labels/printing and dispatch documents deferred to future versions

---

## 8. Test Plan

### Unit Tests -- SalesOrderServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedSO` [Unit]
- `CreateAsync_NonExistentCustomer_ReturnsNotFound` [Unit]
- `CreateAsync_InactiveCustomer_ReturnsConflict` [Unit]
- `CreateAsync_NoLines_ReturnsValidationError` [Unit]
- `CreateAsync_InvalidWarehouseId_ReturnsValidationError` [Unit]
- `CreateAsync_InvalidCarrierId_ReturnsValidationError` [Unit]
- `CreateAsync_ServiceLevelNotBelongingToCarrier_ReturnsValidationError` [Unit]
- `CreateAsync_GeneratesSONumber` [Unit]
- `CreateAsync_CalculatesTotalAmount` [Unit]
- `UpdateHeaderAsync_DraftSO_ReturnsUpdatedSO` [Unit]
- `UpdateHeaderAsync_ConfirmedSO_ReturnsConflictError` [Unit]
- `AddLineAsync_DraftSO_ReturnsCreatedLine` [Unit]
- `AddLineAsync_ConfirmedSO_ReturnsConflictError` [Unit]
- `AddLineAsync_DuplicateProduct_ReturnsConflictError` [Unit]
- `AddLineAsync_RecalculatesTotalAmount` [Unit]
- `UpdateLineAsync_DraftSO_ReturnsUpdatedLine` [Unit]
- `UpdateLineAsync_RecalculatesTotalAmount` [Unit]
- `RemoveLineAsync_DraftSO_RemovesSuccessfully` [Unit]
- `RemoveLineAsync_LastLine_ReturnsConflictError` [Unit]
- `RemoveLineAsync_RecalculatesTotalAmount` [Unit]
- `GetByIdAsync_ExistingSO_ReturnsSOWithLinesAndProgress` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]
- `SearchAsync_DefaultSort_SortsByCreatedDateDescending` [Unit]

### Unit Tests -- SalesOrderStatusTests

- `ConfirmAsync_DraftSO_TransitionsToConfirmed` [Unit]
- `ConfirmAsync_NonDraftSO_ReturnsConflictError` [Unit]
- `ConfirmAsync_SOWithNoLines_ReturnsConflictError` [Unit]
- `CancelAsync_DraftSO_TransitionsToCancelled` [Unit]
- `CancelAsync_ConfirmedSOWithNoPickLists_TransitionsToCancelled` [Unit]
- `CancelAsync_SOWithPickLists_ReturnsConflictError` [Unit]
- `CancelAsync_PickingSO_ReturnsConflictError` [Unit]
- `CancelAsync_PackedSO_ReturnsConflictError` [Unit]
- `CancelAsync_ShippedSO_ReturnsConflictError` [Unit]
- `CompleteAsync_ShippedSO_TransitionsToCompleted` [Unit]
- `CompleteAsync_NonShippedSO_ReturnsConflictError` [Unit]
- `CompleteAsync_AlreadyCompletedSO_ReturnsConflictError` [Unit]

### Unit Tests -- PickListServiceTests

- `GenerateAsync_ConfirmedSO_ReturnsCreatedPickList` [Unit]
- `GenerateAsync_CancelledSO_ReturnsConflictError` [Unit]
- `GenerateAsync_DraftSO_ReturnsConflictError` [Unit]
- `GenerateAsync_InsufficientStock_ReturnsConflictError` [Unit]
- `GenerateAsync_FullyAllocatedSO_ReturnsConflictError` [Unit]
- `GenerateAsync_GeneratesPickListNumber` [Unit]
- `GenerateAsync_PublishesStockReservationRequestedEvent` [Unit]
- `GenerateAsync_TransitionsSOToPicking` [Unit]
- `ConfirmPickAsync_ValidRequest_SetsLineAsPicked` [Unit]
- `ConfirmPickAsync_AlreadyPicked_ReturnsConflictError` [Unit]
- `ConfirmPickAsync_OverPick_ReturnsConflictError` [Unit]
- `ConfirmPickAsync_ZeroQuantity_ReturnsValidationError` [Unit]
- `ConfirmPickAsync_ShortPick_ReleasesExcessReservation` [Unit]
- `ConfirmPickAsync_AllLinesPicked_TransitionsPickListToCompleted` [Unit]
- `CancelAsync_PendingPickList_ReleasesReservations` [Unit]
- `CancelAsync_CompletedPickList_ReturnsConflictError` [Unit]
- `CancelAsync_PartiallyPickedList_ReleasesUnpickedReservations` [Unit]
- `GetByIdAsync_ExistingPickList_ReturnsPickListWithLines` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]

### Unit Tests -- PackingServiceTests

- `CreateParcelAsync_PickingSO_ReturnsCreatedParcel` [Unit]
- `CreateParcelAsync_DraftSO_ReturnsConflictError` [Unit]
- `CreateParcelAsync_GeneratesParcelNumber` [Unit]
- `AddItemAsync_ValidRequest_ReturnsCreatedItem` [Unit]
- `AddItemAsync_OverPack_ReturnsConflictError` [Unit]
- `AddItemAsync_UnpickedLine_ReturnsConflictError` [Unit]
- `UpdateParcelAsync_ValidRequest_ReturnsUpdatedParcel` [Unit]
- `UpdateParcelAsync_ShippedSO_ReturnsConflictError` [Unit]
- `RemoveParcelAsync_ValidRequest_RemovesParcelAndItems` [Unit]
- `RemoveParcelAsync_ShippedSO_ReturnsConflictError` [Unit]
- `PackingCompletion_AllLinesPacked_TransitionsSOToPacked` [Unit]
- `ListParcelsAsync_ExistingSO_ReturnsParcelsWithItems` [Unit]

### Unit Tests -- ShipmentServiceTests

- `CreateAsync_PackedSO_ReturnsCreatedShipment` [Unit]
- `CreateAsync_NonPackedSO_ReturnsConflictError` [Unit]
- `CreateAsync_AlreadyShippedSO_ReturnsConflictError` [Unit]
- `CreateAsync_EmptyParcel_ReturnsConflictError` [Unit]
- `CreateAsync_GeneratesShipmentNumber` [Unit]
- `CreateAsync_PublishesShipmentDispatchedEvent` [Unit]
- `CreateAsync_TransitionsSOToShipped` [Unit]
- `CreateAsync_CopiesShippingAddressFromSO` [Unit]
- `UpdateStatusAsync_ValidTransition_UpdatesStatus` [Unit]
- `UpdateStatusAsync_InvalidTransition_ReturnsConflictError` [Unit]
- `UpdateStatusAsync_TerminalState_ReturnsConflictError` [Unit]
- `UpdateStatusAsync_RecordsTrackingEntry` [Unit]
- `GetByIdAsync_ExistingShipment_ReturnsShipmentWithDetails` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]
- `GetTrackingHistoryAsync_ReturnsChronologicalEntries` [Unit]

### Unit Tests -- CarrierServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedCarrier` [Unit]
- `CreateAsync_DuplicateCode_ReturnsConflictError` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedCarrier` [Unit]
- `UpdateAsync_CannotChangeCode` [Unit]
- `DeactivateAsync_ActiveCarrier_SetsInactive` [Unit]
- `DeactivateAsync_CarrierWithActiveShipments_ReturnsConflict` [Unit]
- `GetByIdAsync_ExistingCarrier_ReturnsCarrierWithServiceLevels` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]
- `SearchAsync_DefaultSort_SortsByNameAscending` [Unit]

### Unit Tests -- CarrierServiceLevelServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedServiceLevel` [Unit]
- `CreateAsync_DuplicateCodeForCarrier_ReturnsConflictError` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedServiceLevel` [Unit]
- `UpdateAsync_CannotChangeCode` [Unit]
- `DeleteAsync_ServiceLevelWithShipments_ReturnsConflict` [Unit]
- `DeleteAsync_UnusedServiceLevel_DeletesSuccessfully` [Unit]
- `ListAsync_ReturnsServiceLevelsForCarrier` [Unit]

### Unit Tests -- CustomerReturnServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedReturn` [Unit]
- `CreateAsync_NonExistentCustomer_ReturnsNotFound` [Unit]
- `CreateAsync_NonExistentSO_ReturnsNotFound` [Unit]
- `CreateAsync_NoLines_ReturnsValidationError` [Unit]
- `CreateAsync_GeneratesReturnNumber` [Unit]
- `ConfirmAsync_DraftReturn_TransitionsToConfirmed` [Unit]
- `ConfirmAsync_AlreadyConfirmed_ReturnsConflictError` [Unit]
- `ReceiveAsync_ConfirmedReturn_PublishesCustomerReturnReceivedEvent` [Unit]
- `ReceiveAsync_NonConfirmedReturn_ReturnsConflictError` [Unit]
- `ReceiveAsync_AlreadyReceived_ReturnsConflictError` [Unit]
- `CloseAsync_ReceivedReturn_TransitionsToClosed` [Unit]
- `CloseAsync_NonReceivedReturn_ReturnsConflictError` [Unit]
- `CancelAsync_DraftReturn_TransitionsToCancelled` [Unit]
- `CancelAsync_ConfirmedReturn_TransitionsToCancelled` [Unit]
- `CancelAsync_ReceivedReturn_ReturnsConflictError` [Unit]
- `GetByIdAsync_ExistingReturn_ReturnsReturnWithLines` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]

### Unit Tests -- FulfillmentEventServiceTests

- `RecordEventAsync_ValidEvent_PersistsEvent` [Unit]
- `SearchAsync_ByEventType_ReturnsMatchingEvents` [Unit]
- `SearchAsync_ByEntityTypeAndId_ReturnsMatchingEvents` [Unit]
- `SearchAsync_ByDateRange_ReturnsMatchingEvents` [Unit]

### Unit Tests -- Validation

- `CreateSalesOrderRequestValidator_MissingCustomerId_Fails` [Unit]
- `CreateSalesOrderRequestValidator_EmptyLines_Fails` [Unit]
- `CreateSalesOrderRequestValidator_PastShipDate_Fails` [Unit]
- `CreateSalesOrderRequestValidator_MissingShippingAddress_Fails` [Unit]
- `CreateSalesOrderRequestValidator_InvalidCountryCode_Fails` [Unit]
- `CreateSOLineRequestValidator_ZeroQuantity_Fails` [Unit]
- `CreateSOLineRequestValidator_NegativeUnitPrice_Fails` [Unit]
- `ConfirmPickRequestValidator_ZeroQuantity_Fails` [Unit]
- `ConfirmPickRequestValidator_NegativeQuantity_Fails` [Unit]
- `CreateParcelRequestValidator_NegativeWeight_Fails` [Unit]
- `CreateParcelRequestValidator_NegativeDimensions_Fails` [Unit]
- `AddParcelItemRequestValidator_ZeroQuantity_Fails` [Unit]
- `UpdateShipmentStatusRequestValidator_InvalidStatus_Fails` [Unit]
- `CreateCarrierRequestValidator_MissingCode_Fails` [Unit]
- `CreateCarrierRequestValidator_MissingName_Fails` [Unit]
- `CreateCarrierRequestValidator_CodeTooLong_Fails` [Unit]
- `CreateCarrierRequestValidator_InvalidCodeCharacters_Fails` [Unit]
- `CreateServiceLevelRequestValidator_MissingCode_Fails` [Unit]
- `CreateServiceLevelRequestValidator_MissingName_Fails` [Unit]
- `CreateServiceLevelRequestValidator_NegativeRate_Fails` [Unit]
- `CreateServiceLevelRequestValidator_DeliveryDaysOutOfRange_Fails` [Unit]
- `CreateCustomerReturnRequestValidator_MissingCustomerId_Fails` [Unit]
- `CreateCustomerReturnRequestValidator_EmptyReason_Fails` [Unit]
- `CreateCustomerReturnRequestValidator_EmptyLines_Fails` [Unit]
- `CreateReturnLineRequestValidator_ZeroQuantity_Fails` [Unit]

### Integration Tests -- SalesOrdersControllerTests

- `CreateSO_ValidPayload_Returns201` [Integration]
- `CreateSO_InactiveCustomer_Returns409` [Integration]
- `CreateSO_NonExistentCustomer_Returns404` [Integration]
- `CreateSO_InvalidPayload_Returns400ProblemDetails` [Integration]
- `CreateSO_Unauthenticated_Returns401` [Integration]
- `CreateSO_InsufficientPermissions_Returns403` [Integration]
- `GetSO_ExistingId_Returns200WithLinesAndProgress` [Integration]
- `UpdateSOHeader_DraftSO_Returns200` [Integration]
- `UpdateSOHeader_ConfirmedSO_Returns409` [Integration]
- `AddSOLine_DraftSO_Returns201` [Integration]
- `AddSOLine_ConfirmedSO_Returns409` [Integration]
- `ConfirmSO_DraftSO_Returns200` [Integration]
- `CancelSO_DraftSO_Returns200` [Integration]
- `CancelSO_WithPickLists_Returns409` [Integration]
- `CompleteSO_ShippedSO_Returns200` [Integration]
- `SearchSOs_WithFilters_ReturnsMatchingResults` [Integration]
- `SearchSOs_Pagination_ReturnsCorrectPage` [Integration]

### Integration Tests -- PickListsControllerTests

- `GeneratePickList_ConfirmedSO_Returns201` [Integration]
- `GeneratePickList_CancelledSO_Returns409` [Integration]
- `GeneratePickList_InsufficientStock_Returns409` [Integration]
- `ConfirmPick_ValidRequest_Returns200` [Integration]
- `ConfirmPick_AlreadyPicked_Returns409` [Integration]
- `ConfirmPick_OverPick_Returns409` [Integration]
- `CancelPickList_PendingList_Returns200` [Integration]
- `CancelPickList_CompletedList_Returns409` [Integration]
- `GetPickList_ExistingId_Returns200WithLines` [Integration]
- `SearchPickLists_WithFilters_ReturnsMatchingResults` [Integration]

### Integration Tests -- PackingControllerTests

- `CreateParcel_PickingSO_Returns201` [Integration]
- `CreateParcel_DraftSO_Returns409` [Integration]
- `AddItemToParcel_ValidRequest_Returns201` [Integration]
- `AddItemToParcel_OverPack_Returns409` [Integration]
- `UpdateParcel_ValidPayload_Returns200` [Integration]
- `RemoveParcel_Returns204` [Integration]
- `ListParcels_ReturnsAllForSO` [Integration]

### Integration Tests -- ShipmentsControllerTests

- `CreateShipment_PackedSO_Returns201` [Integration]
- `CreateShipment_NonPackedSO_Returns409` [Integration]
- `CreateShipment_AlreadyShippedSO_Returns409` [Integration]
- `CreateShipment_PublishesEvent_Returns201` [Integration]
- `UpdateStatus_ValidTransition_Returns200` [Integration]
- `UpdateStatus_InvalidTransition_Returns409` [Integration]
- `GetShipment_ExistingId_Returns200WithDetails` [Integration]
- `GetTracking_Returns200WithHistory` [Integration]
- `SearchShipments_WithFilters_ReturnsMatchingResults` [Integration]

### Integration Tests -- CarriersControllerTests

- `CreateCarrier_ValidPayload_Returns201` [Integration]
- `CreateCarrier_DuplicateCode_Returns409` [Integration]
- `CreateCarrier_InvalidPayload_Returns400ProblemDetails` [Integration]
- `GetCarrier_ExistingId_Returns200WithServiceLevels` [Integration]
- `UpdateCarrier_ValidPayload_Returns200` [Integration]
- `DeactivateCarrier_ActiveCarrier_Returns200` [Integration]
- `ListCarriers_Returns200` [Integration]
- `CreateServiceLevel_ValidPayload_Returns201` [Integration]
- `CreateServiceLevel_DuplicateCode_Returns409` [Integration]
- `DeleteServiceLevel_WithShipments_Returns409` [Integration]
- `DeleteServiceLevel_Unused_Returns204` [Integration]

### Integration Tests -- CustomerReturnsControllerTests

- `CreateReturn_ValidPayload_Returns201` [Integration]
- `CreateReturn_InvalidPayload_Returns400ProblemDetails` [Integration]
- `CreateReturn_NonExistentCustomer_Returns404` [Integration]
- `ConfirmReturn_DraftReturn_Returns200` [Integration]
- `ConfirmReturn_AlreadyConfirmed_Returns409` [Integration]
- `ReceiveReturn_ConfirmedReturn_PublishesEvent_Returns200` [Integration]
- `ReceiveReturn_NonConfirmedReturn_Returns409` [Integration]
- `CloseReturn_ReceivedReturn_Returns200` [Integration]
- `CancelReturn_DraftReturn_Returns200` [Integration]
- `CancelReturn_ReceivedReturn_Returns409` [Integration]
- `GetReturn_ExistingId_Returns200WithLines` [Integration]
- `SearchReturns_WithFilters_ReturnsMatchingResults` [Integration]

### Integration Tests -- FulfillmentEventsControllerTests

- `ListEvents_ByEventType_Returns200` [Integration]
- `ListEvents_ByEntityTypeAndId_Returns200` [Integration]
- `ListEvents_ByDateRange_Returns200` [Integration]
- `ListEvents_Pagination_ReturnsCorrectPage` [Integration]

---
