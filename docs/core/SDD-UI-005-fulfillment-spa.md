# SDD-UI-005 — Fulfillment Operations SPA

> Status: Active
> Last updated: 2026-04-09
> Owner: TBD
> Category: Core

## 1. Context & Scope

This spec defines the Vue.js SPA module for the Fulfillment Operations domain. It provides a web-based interface for managing sales orders, pick lists, packing, shipments, carriers, customer returns, and fulfillment event history. The SPA consumes the `Warehouse.Fulfillment.API` REST endpoints defined in `SDD-FULF-001`.

**ISA-95 Conformance:** This UI module supports the ISA-95 Part 3 Fulfillment Operations Activity Model. It enables operators to execute fulfillment request creation, pick list execution, packing workflows, dispatch (material shipment), shipment tracking, and customer return processing through the SPA interface.

**In scope:**
- Sales order management: list (paginated, filtered), create (with lines and shipping address), detail view (with pick/pack/ship progress), edit header/lines (Draft only), confirm, cancel, complete
- Sales order line management: add, edit, remove lines within SO detail view
- Pick list management: list (paginated, filtered), generate from confirmed SO, detail view with pick execution per line, cancel pick list
- Packing workflow: create parcels for picked SOs, add/remove items to parcels, update parcel weight/dimensions/tracking
- Shipment management: dispatch (create shipment from packed SO), list shipments, detail view with lines and parcels, update shipment status (tracking)
- Carrier management: list (paginated, filtered), create, detail (with service levels), edit, deactivate
- Carrier service level management: create, edit, delete within carrier detail
- Customer returns (RMA): list (paginated, filtered), create (with lines), detail view, confirm, receive, close, cancel
- Fulfillment event history viewer with filtering
- Sidebar navigation group for all Fulfillment views
- Form display mode support (modal/page) per `SDD-UI-002`
- Internationalization (English and Bulgarian) per `SDD-UI-001` Section 2.10
- StatusChip display for SO status, pick list status, shipment status, and return status

**Out of scope:**
- Label templates, ZPL/Zebra printing, QR codes, barcodes (deferred per `SDD-FULF-001`)
- Dispatch documents — packing slips, delivery notes (deferred per `SDD-FULF-001`)
- Automated carrier rate comparison and selection (deferred)
- Wave picking and batch picking optimization (deferred)
- Partial shipment splitting (v1 = one shipment per SO, per `SDD-FULF-001`)
- Customer credit checks and payment terms (deferred to Finance service)
- Mobile-responsive layout (desktop-first, per `SDD-UI-001`)
- Dark mode (future enhancement)

**Related specs:**
- `SDD-FULF-001` — Backend Fulfillment API (all endpoints consumed by this module)
- `SDD-UI-001` — Auth Administration SPA (base architecture, shared components, auth flow, i18n, layout)
- `SDD-UI-002` — Form Display Mode (modal vs page rendering for CRUD forms)
- `SDD-AUTH-001` — Authentication and authorization (JWT, permissions)
- `SDD-INV-001` — Products referenced by SO lines, pick lists, and return lines (product selector)
- `SDD-INV-003` — Warehouses and storage locations referenced by pick lists and returns (warehouse selector)
- `SDD-CUST-001` — Customers referenced by sales orders and returns (customer selector)
- `SDD-PURCH-001` — Sister service (inbound); shared UI patterns for order lifecycle

---

## 2. Behavior

### 2.1 Sidebar Navigation

- The sidebar MUST include a "Fulfillment" navigation group below the Purchasing group.
- The group MUST contain the following navigation items, each with an icon and translated label:
  - Sales Orders (`mdi-cart-outline`)
  - Pick Lists (`mdi-format-list-checks`)
  - Shipments (`mdi-truck-delivery`)
  - Carriers (`mdi-truck-fast-outline`)
  - Customer Returns (`mdi-package-variant-closed-remove`)
  - Fulfillment Events (`mdi-history`)
- The active navigation item MUST be visually highlighted based on the current route.
- The group MUST be collapsible (matching existing sidebar group behavior).

### 2.2 Sales Orders

#### 2.2.1 Sales Order List

- The SPA MUST display a paginated server-side data table of sales orders using `v-data-table-server`.
- Columns MUST include: SO Number, Customer Name, Status, Ship-From Warehouse, Requested Ship Date, Total Amount, Created Date.
- The table MUST support column filtering via `ColumnFilter` molecules for: SO Number (text, starts-with), Customer (autocomplete), Status (select from enum values), Warehouse (select), Date Range (date picker range on Created Date).
- The table MUST support sorting by SO Number, Created Date, Requested Ship Date, and Customer Name.
- The default sort MUST be Created Date descending.
- The Status column MUST render a `StatusChip` atom with color coding:
  - `Draft` — grey
  - `Confirmed` — blue
  - `Picking` — amber
  - `Packed` — indigo
  - `Shipped` — teal
  - `Completed` — green
  - `Cancelled` — red
- Each row MUST display action chips based on the SO's current status:
  - All statuses: View (detail link)
  - `Draft`: Edit, Confirm, Cancel
  - `Confirmed`: Cancel (only if no pick lists)
  - `Shipped`: Complete
- The SPA MUST provide a "Create Sales Order" button that respects `formDisplayMode`.
- The table MUST respect Vuetify density from the layout store.

#### 2.2.2 Sales Order Detail View

- The SPA MUST display the full SO details on a dedicated detail page accessible via `/fulfillment/sales-orders/:id`.
- The detail view MUST show:
  - **Header section:** SO Number, Status (StatusChip), Customer (linked to customer detail), Ship-From Warehouse, Requested Ship Date, Shipping Address (formatted block), Carrier + Service Level (if selected), Notes, Created By, Created At, Confirmed At, Shipped At, Completed At (where applicable).
  - **Lines section:** A data table of SO lines with columns: Product Code, Product Name, Ordered Quantity, Unit Price, Line Total, Picked Quantity, Packed Quantity, Shipped Quantity.
  - **Progress section:** Per line, a multi-step progress indicator or fraction text showing the fulfillment pipeline: `Ordered → Picked → Packed → Shipped`.
  - **Pick Lists tab:** List of pick lists generated for this SO (linked to pick list detail).
  - **Parcels tab:** List of parcels with packed items (visible when status is `Picking` or later).
  - **Shipment tab:** Shipment details and tracking history (visible when status is `Shipped` or later).
  - **Action buttons:** Status-dependent action buttons in a toolbar:
    - `Draft`: Edit, Confirm, Cancel
    - `Confirmed`: Generate Pick List, Cancel (only if no pick lists)
    - `Picking`: Generate Additional Pick List (if not fully allocated), View Pick Lists
    - `Packed`: Dispatch (Create Shipment)
    - `Shipped`: Complete, View Shipment
- Clicking "Confirm" MUST show a `ConfirmDialog` before calling `POST /api/v1/sales-orders/{id}/confirm`.
- Clicking "Cancel" MUST show a `ConfirmDialog` before calling `POST /api/v1/sales-orders/{id}/cancel`.
- Clicking "Complete" MUST show a `ConfirmDialog` before calling `POST /api/v1/sales-orders/{id}/complete`.
- After any status action, the SPA MUST refresh the detail view.
- The SPA MUST display a "Back to Sales Orders" link/button navigating to the SO list.

#### 2.2.3 Create Sales Order

- The create form MUST include:
  - **Header fields:** Customer (autocomplete selector, required), Ship-From Warehouse (select, required), Requested Ship Date (date picker, optional), Carrier (select, optional), Carrier Service Level (select, optional — filtered by selected carrier), Notes (textarea, optional).
  - **Shipping Address fields:** Street Line 1 (text, required), Street Line 2 (text, optional), City (text, required), State/Province (text, optional), Postal Code (text, required), Country Code (text, required — 2-letter ISO).
  - **Lines section:** An editable table where the user can add one or more lines. Each line MUST have: Product (autocomplete selector), Ordered Quantity (number input), Unit Price (number input), Notes (text input, optional).
- The SPA MUST support adding multiple lines before submitting.
- The SPA MUST support removing a line from the form (client-side only, before submit).
- The SPA MUST calculate and display the line total (Ordered Quantity x Unit Price) per line and a grand total.
- When a Customer is selected, the SPA SHOULD auto-populate the shipping address from the customer's default shipping address (if available via `SDD-CUST-001` customer detail endpoint). The user MUST be able to override the auto-populated address.
- On submit, the SPA MUST call `POST /api/v1/sales-orders` with header, shipping address, and all lines.
- On success, the SPA MUST navigate to the newly created SO's detail page and display a success notification.

**Edge cases:**
- If the selected customer is inactive, the API will return a 409 — the SPA MUST display the error as a snackbar notification.
- If a selected product is already on the SO (duplicate line), the client SHOULD prevent adding it before submitting.
- Selecting a carrier service level without a carrier MUST be prevented (service levels are carrier-dependent).

#### 2.2.4 Edit Sales Order

- The edit form MUST only be accessible when the SO is in `Draft` status.
- The SPA MUST pre-populate the form with the SO's current header values, shipping address, and lines.
- Header fields (Customer excluded — read-only after creation): Warehouse, Requested Ship Date, Carrier, Service Level, Notes, Shipping Address.
- The SPA MUST NOT allow changing the Customer after SO creation.
- Lines can be added, updated, or removed:
  - Adding a line MUST call `POST /api/v1/sales-orders/{soId}/lines`.
  - Updating a line MUST call `PUT /api/v1/sales-orders/{soId}/lines/{lineId}`.
  - Removing a line MUST show a `ConfirmDialog` before calling `DELETE /api/v1/sales-orders/{soId}/lines/{lineId}`.
- Updating the header MUST call `PUT /api/v1/sales-orders/{id}`.
- On success, the SPA MUST navigate back to the SO detail page.

**Edge cases:**
- Removing the last line MUST display the API error ("Sales order must have at least one line").
- If another user confirms the SO while editing, the next save returns a 409 — display error and suggest refreshing.

### 2.3 Pick Lists

#### 2.3.1 Pick List List

- The SPA MUST display a paginated server-side data table of pick lists.
- Columns MUST include: Pick List Number, SO Number, Warehouse, Status, Created Date.
- The table MUST support column filtering for: Pick List Number (text), SO Number (text), Status (select: Pending, Completed, Cancelled), Warehouse (select), Date Range.
- The default sort MUST be Created Date descending.
- Status chips MUST use color coding:
  - `Pending` — amber
  - `Completed` — green
  - `Cancelled` — red
- Each row MUST display action chips: View. For `Pending` status: Cancel.

#### 2.3.2 Generate Pick List

- From the SO detail view (Confirmed or Picking status), clicking "Generate Pick List" MUST call `POST /api/v1/pick-lists` with the SO ID.
- The SPA MUST show a loading indicator during pick list generation.
- On success, the SPA MUST refresh the SO detail view (status may change to Picking) and navigate to the new pick list's detail page.
- If insufficient stock exists, the API returns a 409 with `INSUFFICIENT_STOCK` — the SPA MUST display the error as a snackbar.
- If the SO is fully allocated, the API returns a 409 with `SO_FULLY_ALLOCATED` — the SPA MUST display the error.

#### 2.3.3 Pick List Detail View

- The SPA MUST display the full pick list details on a dedicated page accessible via `/fulfillment/pick-lists/:id`.
- The detail view MUST show:
  - **Header:** Pick List Number, SO Number (linked), Warehouse, Status (StatusChip), Created By, Created Date.
  - **Lines section:** A data table with columns: Product Code, Product Name, Source Location, Requested Quantity, Actual Picked Quantity, Picked By, Picked At, Pick Status (StatusChip per line).
- Each un-picked line (Actual Picked Quantity is null) MUST display a "Pick" action button.
- Clicking "Pick" MUST open a dialog with:
  - **Read-only info:** Product, Location, Requested Quantity.
  - **Input fields:** Actual Quantity Picked (number input, required, must be > 0 and <= requested quantity).
- On submit, the SPA MUST call `POST /api/v1/pick-lists/{id}/lines/{lineId}/pick` with the actual quantity.
- On success, the SPA MUST refresh the pick list detail. If all lines are picked, the status auto-transitions to `Completed`.
- The detail view MUST display a "Cancel Pick List" button when status is `Pending`.
- Cancelling MUST show a `ConfirmDialog` before calling `POST /api/v1/pick-lists/{id}/cancel`.

**Edge cases:**
- Picking zero quantity MUST be prevented client-side.
- Picking more than requested MUST display the `OVER_PICK` error.
- Picking a line already picked MUST display the `LINE_ALREADY_PICKED` error and refresh the detail.

### 2.4 Packing

#### 2.4.1 Parcels Management (within SO Detail)

- The Parcels tab on the SO detail view MUST display all parcels for the SO.
- Each parcel MUST show: Parcel Number, Weight (kg), Dimensions (L x W x H cm), Tracking Number, item count.
- The tab MUST provide a "Create Parcel" button when the SO is in `Picking` (with completed picks) or `Packed` status.

#### 2.4.2 Create Parcel

- Clicking "Create Parcel" MUST open a dialog with:
  - **Fields:** Weight (number, optional), Length (number, optional), Width (number, optional), Height (number, optional), Tracking Number (text, optional), Notes (textarea, optional).
- On submit, the SPA MUST call `POST /api/v1/sales-orders/{soId}/parcels`.
- On success, the SPA MUST refresh the Parcels tab and display a success notification.

#### 2.4.3 Add Items to Parcel

- Clicking a parcel row MUST expand or navigate to show the parcel's packed items.
- The expanded view MUST provide an "Add Item" button.
- The "Add Item" dialog MUST include: Product (select — from SO line products that have been picked), Pick List Line (select — from confirmed pick lines for the product), Quantity (number input, required).
- The SPA MUST prevent packing more than the total picked quantity (client-side check against `OVER_PACK`).
- On submit, the SPA MUST call `POST /api/v1/sales-orders/{soId}/parcels/{parcelId}/items`.
- On success, the SPA MUST refresh the parcel detail.

#### 2.4.4 Update Parcel

- The SPA MUST support editing parcel weight, dimensions, tracking number, and notes.
- Updates are only permitted before dispatch.
- The SPA MUST call `PUT /api/v1/sales-orders/{soId}/parcels/{parcelId}`.

#### 2.4.5 Remove Parcel

- The SPA MUST show a `ConfirmDialog` before calling `DELETE /api/v1/sales-orders/{soId}/parcels/{parcelId}`.
- Removal is only permitted before dispatch.

#### 2.4.6 Packing Completion

- When all SO lines are fully packed (per-line packed quantity >= ordered quantity), the SO auto-transitions to `Packed`.
- The SPA MUST detect this status change on refresh and update the UI accordingly (action buttons, status chip).

### 2.5 Shipments

#### 2.5.1 Dispatch (Create Shipment)

- From the SO detail view (Packed status), clicking "Dispatch" MUST open a dialog with:
  - **Fields:** Carrier (select, optional — pre-populated from SO if set), Carrier Service Level (select, optional — filtered by carrier), Notes (textarea, optional).
- On submit, the SPA MUST call `POST /api/v1/shipments` with the SO ID and optional carrier/service level.
- On success, the SPA MUST refresh the SO detail (status transitions to `Shipped`) and display a success notification.
- The SPA MUST navigate to or display the Shipment tab with the newly created shipment.

**Edge cases:**
- If any parcel has no items (`EMPTY_PARCEL`), the API returns 409 — the SPA MUST display the error.
- If the SO is not in `Packed` status (`SO_NOT_DISPATCHABLE`), the SPA MUST display the error.

#### 2.5.2 Shipment List

- The SPA MUST display a paginated server-side data table of shipments.
- Columns MUST include: Shipment Number, SO Number, Carrier, Status, Dispatched Date, Tracking Number.
- The table MUST support column filtering for: Shipment Number (text), SO Number (text), Carrier (select), Status (select), Date Range (on dispatched date).
- The default sort MUST be Dispatched Date descending.
- Status chips MUST use color coding:
  - `Dispatched` — blue
  - `InTransit` — amber
  - `OutForDelivery` — indigo
  - `Delivered` — green
  - `Failed` — red
  - `Returned` — dark grey
- Each row MUST display action chips: View. For non-terminal statuses: Update Status.

#### 2.5.3 Shipment Detail View

- The SPA MUST display the full shipment details on a dedicated page accessible via `/fulfillment/shipments/:id`.
- The detail view MUST show:
  - **Header:** Shipment Number, SO Number (linked), Carrier + Service Level, Status (StatusChip), Tracking Number, Tracking URL (clickable link if available), Shipping Address, Dispatched By, Dispatched At.
  - **Lines section:** A data table of shipment lines with columns: Product Code, Product Name, Shipped Quantity.
  - **Parcels section:** List of parcels with weight, dimensions, tracking.
  - **Tracking History section:** Chronological list of status updates with: Status, Timestamp, Notes, Updated By.
- Action buttons based on current status:
  - Non-terminal statuses: "Update Status" button.

#### 2.5.4 Update Shipment Status

- Clicking "Update Status" MUST open a dialog with:
  - **Fields:** New Status (select — only valid transitions from current status), Tracking Number (text, optional), Tracking URL (text, optional), Notes (textarea, optional).
- Valid transitions MUST be enforced client-side:
  - `Dispatched` → `InTransit`, `Delivered`, `Failed`
  - `InTransit` → `OutForDelivery`, `Delivered`, `Failed`
  - `OutForDelivery` → `Delivered`, `Failed`
  - `Failed` → `Returned`
- On submit, the SPA MUST call `POST /api/v1/shipments/{id}/status`.
- On success, the SPA MUST refresh the shipment detail and append the new entry to the tracking history.

### 2.6 Carriers

#### 2.6.1 Carrier List

- The SPA MUST display a paginated data table of carriers.
- Columns MUST include: Code, Name, Contact Email, Active Status.
- The table MUST support column filtering for: Name (text, contains), Code (text, starts-with), Active status (select).
- The default sort MUST be Name ascending.
- Each row MUST display action chips: View, Edit, Deactivate (if active).
- The SPA MUST provide a "Create Carrier" button.

#### 2.6.2 Carrier Detail View

- The SPA MUST display the full carrier details on a dedicated page accessible via `/fulfillment/carriers/:id`.
- The detail view MUST show:
  - **Header:** Code, Name, Contact Phone, Contact Email, Website URL (clickable), Tracking URL Template, Notes, Active status.
  - **Service Levels section:** A data table with columns: Code, Name, Estimated Delivery Days, Base Rate, Per-Kg Rate. Actions: Edit, Delete.
- Service level management (add/edit/delete) MUST use dialog forms within the detail view regardless of `formDisplayMode`.

#### 2.6.3 Create Carrier

- The create form MUST include: Code (text, required), Name (text, required), Contact Phone (text, optional), Contact Email (email, optional), Website URL (URL, optional), Tracking URL Template (text, optional — hint: use `{trackingNumber}` placeholder), Notes (textarea, optional).
- On success, the SPA MUST navigate to the carrier detail page.

#### 2.6.4 Edit Carrier

- The edit form MUST pre-populate with the carrier's current values.
- The Code field MUST be read-only (not editable after creation).
- On success, the SPA MUST navigate to the carrier detail page.

#### 2.6.5 Deactivate Carrier

- The SPA MUST show a `ConfirmDialog` before calling `POST /api/v1/carriers/{id}/deactivate`.
- If the carrier has active shipments, the API returns 409 — the SPA MUST display the error.
- On success, the SPA MUST refresh the list.

#### 2.6.6 Create/Edit/Delete Service Level

- Creating a service level MUST open a dialog with: Code (text, required), Name (text, required), Estimated Delivery Days (number, optional), Base Rate (number, optional), Per-Kg Rate (number, optional), Notes (textarea, optional).
- The Code field MUST be read-only on edit.
- Deleting a service level MUST show a `ConfirmDialog`. If in use by shipments, the API returns 409 — display the error.
- These dialogs MUST always render as modals (inline sub-entity forms).

### 2.7 Customer Returns

#### 2.7.1 Customer Return List

- The SPA MUST display a paginated server-side data table of customer returns.
- Columns MUST include: Return Number, Customer Name, Status, SO Number (if referenced), Reason (truncated), Created Date.
- The table MUST support column filtering for: Return Number (text), Customer (autocomplete), Status (select: Draft, Confirmed, Received, Closed, Cancelled), SO Number (text), Date Range.
- The default sort MUST be Created Date descending.
- Status chips MUST use color coding:
  - `Draft` — grey
  - `Confirmed` — blue
  - `Received` — teal
  - `Closed` — green
  - `Cancelled` — red
- Each row MUST display action chips based on status:
  - `Draft`: View, Confirm, Cancel
  - `Confirmed`: View, Receive, Cancel
  - `Received`: View, Close
  - `Closed`, `Cancelled`: View
- The SPA MUST provide a "Create Customer Return" button.

#### 2.7.2 Customer Return Detail View

- The SPA MUST display the full customer return details on a dedicated page accessible via `/fulfillment/customer-returns/:id`.
- The detail view MUST show:
  - **Header:** Return Number, Customer (linked to customer detail), Status (StatusChip), SO Number (linked to SO detail, if referenced), Reason, Notes, Created By, Created Date, Confirmed By/Date, Received By/Date, Closed By/Date (where applicable).
  - **Lines section:** A data table with columns: Product Code, Product Name, Warehouse, Location, Quantity, Notes.
- Action buttons based on status:
  - `Draft`: Confirm, Cancel
  - `Confirmed`: Receive, Cancel
  - `Received`: Close
- Clicking "Confirm" MUST show a `ConfirmDialog` before calling `POST /api/v1/customer-returns/{id}/confirm`.
- Clicking "Receive" MUST show a `ConfirmDialog` before calling `POST /api/v1/customer-returns/{id}/receive`.
- Clicking "Close" MUST show a `ConfirmDialog` before calling `POST /api/v1/customer-returns/{id}/close`.
- Clicking "Cancel" MUST show a `ConfirmDialog` before calling `POST /api/v1/customer-returns/{id}/cancel`.

#### 2.7.3 Create Customer Return

- The create form MUST include:
  - **Header fields:** Customer (autocomplete, required), Sales Order (autocomplete, optional — filtered by customer), Reason (text, required), Notes (textarea, optional).
  - **Lines section:** Editable table where the user can add lines with: Product (autocomplete, required), Warehouse (select, required — destination), Location (select, optional — filtered by warehouse), Quantity (number, required), Notes (text, optional).
- When a Sales Order is selected, the SPA SHOULD pre-populate the product selector with products from that SO to assist the user.
- On submit, the SPA MUST call `POST /api/v1/customer-returns` with header and lines.
- On success, the SPA MUST navigate to the customer return detail page.

**Edge cases:**
- Creating a return for a non-existent or soft-deleted customer returns 404 — display error.
- Creating a return referencing a non-existent SO returns 404 — display error.

### 2.8 Fulfillment Event History

- The SPA MUST display a paginated server-side data table of fulfillment events.
- Columns MUST include: Date/Time, Event Type, Entity Type, Entity ID, User, Details (truncated).
- The table MUST support column filtering for: Event Type (select from defined types), Entity Type (select), Date Range.
- Fulfillment events are read-only — no create, edit, or delete actions.
- Clicking a row or "View" chip SHOULD expand the row or open a dialog showing the full JSON payload.
- Event type filter options MUST include: `SalesOrderCreated`, `SalesOrderConfirmed`, `SalesOrderCancelled`, `SalesOrderCompleted`, `PickListGenerated`, `PickListCompleted`, `PickListCancelled`, `ParcelCreated`, `ShipmentDispatched`, `ShipmentStatusUpdated`, `CustomerReturnCreated`, `CustomerReturnConfirmed`, `CustomerReturnReceived`, `CustomerReturnClosed`, `CustomerReturnCancelled`.

### 2.9 Cross-Domain Selectors

- When creating or editing SOs, pick lists, or customer returns, the SPA MUST provide autocomplete selectors for cross-domain entities:
  - **Customer selector:** Calls Customers API (`GET /api/v1/customers`) with search-as-you-type. Displays Customer Name + Code. Returns Customer ID.
  - **Product selector:** Calls Inventory API (`GET /api/v1/products`) with search-as-you-type. Displays Product Code + Name. Returns Product ID.
  - **Warehouse selector:** Calls Inventory API (`GET /api/v1/warehouses`). Displays Warehouse Name. Returns Warehouse ID.
  - **Storage Location selector:** Filtered by selected warehouse. Displays Location Code. Returns Location ID.
  - **Carrier selector:** Calls Fulfillment API (`GET /api/v1/carriers`). Displays Carrier Name. Returns Carrier ID.
  - **Service Level selector:** Filtered by selected carrier. Displays Service Level Name. Returns Service Level ID.
- All autocomplete selectors MUST debounce API calls (300ms minimum).

### 2.10 Notifications and Error Handling

- All error handling follows the base patterns defined in `SDD-UI-001` Section 2.8.
- The SPA MUST display success notifications (snackbar) after successful mutations.
- Validation errors (400) MUST be displayed inline on form fields.
- Conflict errors (409) MUST be displayed as snackbar notifications with the API error message.
- Not Found errors (404) MUST display a "Not Found" message with a back link to the list page.
- Loading indicators MUST be shown during all API calls.

---

## 3. Validation Rules

Client-side validation mirrors the backend validation defined in `SDD-FULF-001` Section 3. The SPA performs client-side validation before submitting to the API, but the API remains the authoritative validator.

### 3.1 Sales Order Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V1 | CustomerId | Required. | Please select a customer. |
| V2 | WarehouseId | Required. | Please select a ship-from warehouse. |
| V3 | RequestedShipDate | Optional. Must be today or future. | Requested ship date must be today or a future date. |
| V4 | ShippingStreetLine1 | Required. 1-200 characters. | Street address is required. |
| V5 | ShippingStreetLine2 | Optional. Max 200 characters. | Street line 2 must be under 200 characters. |
| V6 | ShippingCity | Required. 1-100 characters. | City is required. |
| V7 | ShippingStateProvince | Optional. Max 100 characters. | State/province must be under 100 characters. |
| V8 | ShippingPostalCode | Required. 1-20 characters. | Postal code is required. |
| V9 | ShippingCountryCode | Required. 2-letter ISO code. | Please enter a valid 2-letter country code. |
| V10 | CarrierId | Optional. | — |
| V11 | CarrierServiceLevelId | Optional. Must belong to selected carrier. | Service level must belong to the selected carrier. |
| V12 | Notes | Optional. Max 2000 characters. | Notes must be under 2000 characters. |
| V13 | Lines | At least one line required. | Sales order must have at least one line. |

### 3.2 Sales Order Line Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V14 | ProductId | Required. | Please select a product. |
| V15 | OrderedQuantity | Required. Must be > 0. | Quantity must be greater than zero. |
| V16 | UnitPrice | Required. Must be >= 0. | Unit price must be zero or greater. |
| V17 | Notes | Optional. Max 500 characters. | Notes must be under 500 characters. |

### 3.3 Pick Confirmation Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V18 | ActualQuantity | Required. Must be > 0. | Picked quantity must be greater than zero. |
| V19 | ActualQuantity | Must not exceed requested quantity. | Picked quantity cannot exceed the requested quantity. |

### 3.4 Parcel Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V20 | Weight | Optional. Must be > 0 when provided. | Weight must be greater than zero. |
| V21 | Length | Optional. Must be > 0 when provided. | Length must be greater than zero. |
| V22 | Width | Optional. Must be > 0 when provided. | Width must be greater than zero. |
| V23 | Height | Optional. Must be > 0 when provided. | Height must be greater than zero. |
| V24 | TrackingNumber | Optional. Max 100 characters. | Tracking number must be under 100 characters. |
| V25 | Notes | Optional. Max 2000 characters. | Notes must be under 2000 characters. |

### 3.5 Parcel Item Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V26 | ProductId | Required. Must be a product on the SO. | Please select a product from the sales order. |
| V27 | Quantity | Required. Must be > 0. | Quantity must be greater than zero. |
| V28 | PickListLineId | Required. Must be a confirmed pick line. | Please select a picked line. |

### 3.6 Shipment Status Update Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V29 | Status | Required. Must be a valid status value. | Please select a status. |
| V30 | Status (transition) | Must follow valid transition rules. | Invalid status transition. |
| V31 | TrackingNumber | Optional. Max 100 characters. | Tracking number must be under 100 characters. |
| V32 | TrackingUrl | Optional. Max 500 characters. Valid URL format. | Please enter a valid URL. |
| V33 | Notes | Optional. Max 2000 characters. | Notes must be under 2000 characters. |

### 3.7 Carrier Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V34 | Code | Required. 1-20 characters. Alphanumeric + hyphens. | Carrier code is required (max 20 characters, letters, numbers, hyphens). |
| V35 | Name | Required. 1-200 characters. | Carrier name is required (max 200 characters). |
| V36 | ContactPhone | Optional. Max 20 characters. | Phone must be under 20 characters. |
| V37 | ContactEmail | Optional. Valid email format. Max 256 characters. | Please enter a valid email address. |
| V38 | WebsiteUrl | Optional. Max 500 characters. Valid URL. | Please enter a valid URL. |
| V39 | TrackingUrlTemplate | Optional. Max 500 characters. | Tracking URL template must be under 500 characters. |
| V40 | Notes | Optional. Max 2000 characters. | Notes must be under 2000 characters. |

### 3.8 Carrier Service Level Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V41 | Code | Required. 1-20 characters. Alphanumeric + hyphens. | Service level code is required (max 20 characters). |
| V42 | Name | Required. 1-100 characters. | Service level name is required (max 100 characters). |
| V43 | EstimatedDeliveryDays | Optional. 1-365 when provided. | Estimated delivery days must be between 1 and 365. |
| V44 | BaseRate | Optional. Must be >= 0 when provided. | Base rate must be zero or greater. |
| V45 | PerKgRate | Optional. Must be >= 0 when provided. | Per-kg rate must be zero or greater. |
| V46 | Notes | Optional. Max 500 characters. | Notes must be under 500 characters. |

### 3.9 Customer Return Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V47 | CustomerId | Required. | Please select a customer. |
| V48 | SalesOrderId | Optional. | — |
| V49 | Reason | Required. 1-500 characters. | Reason is required (max 500 characters). |
| V50 | Notes | Optional. Max 2000 characters. | Notes must be under 2000 characters. |
| V51 | Lines | At least one line required. | Customer return must have at least one line. |

### 3.10 Customer Return Line Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V52 | ProductId | Required. | Please select a product. |
| V53 | WarehouseId | Required. | Please select a warehouse. |
| V54 | Quantity | Required. Must be > 0. | Quantity must be greater than zero. |
| V55 | Notes | Optional. Max 500 characters. | Notes must be under 500 characters. |

---

## 4. Error Rules

All error handling maps to the ProblemDetails responses defined in `SDD-FULF-001` Section 4.

| # | API Error Code | HTTP Status | UI Behavior |
|---|---|---|---|
| **Sales Order errors** | | | |
| E1 | `SO_NOT_FOUND` | 404 | Display Not Found page with link to SO list. |
| E2 | `SO_LINE_NOT_FOUND` | 404 | Display snackbar: "Sales order line not found." Refresh detail. |
| E3 | `SO_NOT_EDITABLE` | 409 | Display snackbar: "Sales order can only be edited in Draft status." |
| E4 | `INVALID_SO_STATUS_TRANSITION` | 409 | Display snackbar with API message (includes from/to statuses). |
| E5 | `SO_MUST_HAVE_LINES` | 409 | Display snackbar: "Sales order must have at least one line." |
| E6 | `DUPLICATE_SO_LINE` | 409 | Display inline error: "This product is already on the sales order." |
| E7 | `SO_HAS_PICK_LISTS` | 409 | Display snackbar: "Cannot cancel — pick lists have been generated." |
| E8 | `SO_ALREADY_COMPLETED` | 409 | Display snackbar: "Sales order is already completed." Refresh detail. |
| E9 | `SO_ALREADY_SHIPPED` | 409 | Display snackbar: "Sales order has already been shipped." Refresh detail. |
| E10 | `CUSTOMER_NOT_FOUND` | 404 | Display snackbar: "Customer not found." |
| E11 | `CUSTOMER_INACTIVE` | 409 | Display snackbar: "The customer is inactive and cannot be used for new sales orders." |
| **Pick List errors** | | | |
| E12 | `PICK_LIST_NOT_FOUND` | 404 | Display Not Found page with link to pick list list. |
| E13 | `SO_NOT_PICKABLE` | 409 | Display snackbar: "Sales order is not in a pickable status." |
| E14 | `INSUFFICIENT_STOCK` | 409 | Display snackbar: "Insufficient available stock at the specified location." |
| E15 | `SO_FULLY_ALLOCATED` | 409 | Display snackbar: "All lines are already allocated to pick lists." |
| E16 | `LINE_ALREADY_PICKED` | 409 | Display snackbar: "This line has already been picked." Refresh detail. |
| E17 | `OVER_PICK` | 409 | Display inline error on quantity: "Picked quantity exceeds the requested quantity." |
| E18 | `PICK_LIST_ALREADY_COMPLETED` | 409 | Display snackbar: "Pick list has already been completed." Refresh detail. |
| E19 | `PICK_LIST_LINE_NOT_FOUND` | 404 | Display snackbar: "Pick list line not found." |
| **Packing errors** | | | |
| E20 | `PARCEL_NOT_FOUND` | 404 | Display snackbar: "Parcel not found." Refresh parcels tab. |
| E21 | `SO_NOT_PACKABLE` | 409 | Display snackbar: "Sales order is not in a packable status." |
| E22 | `OVER_PACK` | 409 | Display snackbar: "Packed quantity exceeds the picked quantity for this product." |
| E23 | `PARCEL_NOT_EDITABLE` | 409 | Display snackbar: "Parcel cannot be edited after shipment dispatch." |
| E24 | `INVALID_PICK_LINE` | 400 | Display inline error: "The selected pick line has not been picked or does not exist." |
| E25 | `EMPTY_PARCEL` | 409 | Display snackbar: "Cannot dispatch — one or more parcels have no packed items." |
| **Shipment errors** | | | |
| E26 | `SHIPMENT_NOT_FOUND` | 404 | Display Not Found page with link to shipments list. |
| E27 | `SO_NOT_DISPATCHABLE` | 409 | Display snackbar: "Sales order is not in a dispatchable status (must be Packed)." |
| E28 | `INVALID_SHIPMENT_STATUS_TRANSITION` | 409 | Display snackbar with API message (includes from/to statuses). |
| **Carrier errors** | | | |
| E29 | `CARRIER_NOT_FOUND` | 404 | Display Not Found page with link to carriers list. |
| E30 | `DUPLICATE_CARRIER_CODE` | 409 | Display inline error on Code field: "A carrier with this code already exists." |
| E31 | `CARRIER_HAS_ACTIVE_SHIPMENTS` | 409 | Display snackbar: "Cannot deactivate — active shipments exist." |
| E32 | `SERVICE_LEVEL_NOT_FOUND` | 404 | Display snackbar: "Service level not found." Refresh detail. |
| E33 | `DUPLICATE_SERVICE_LEVEL_CODE` | 409 | Display inline error on Code field: "A service level with this code already exists for this carrier." |
| E34 | `SERVICE_LEVEL_IN_USE` | 409 | Display snackbar: "Cannot delete — referenced by shipments." |
| E35 | `INVALID_CARRIER` | 400 | Display inline error on Carrier field: "The selected carrier does not exist or is inactive." |
| E36 | `INVALID_SERVICE_LEVEL` | 400 | Display inline error on Service Level field: "The selected service level does not belong to the carrier." |
| **Customer Return errors** | | | |
| E37 | `RETURN_NOT_FOUND` | 404 | Display Not Found page with link to returns list. |
| E38 | `RETURN_ALREADY_CONFIRMED` | 409 | Display snackbar: "Customer return has already been confirmed." Refresh detail. |
| E39 | `RETURN_ALREADY_RECEIVED` | 409 | Display snackbar: "Customer return has already been received." Refresh detail. |
| E40 | `RETURN_NOT_RECEIVABLE` | 409 | Display snackbar: "Customer return is not in Confirmed status." |
| E41 | `RETURN_NOT_CLOSEABLE` | 409 | Display snackbar: "Customer return is not in Received status." |
| E42 | `INVALID_RETURN_STATUS_TRANSITION` | 409 | Display snackbar with API message. |
| E43 | `RETURN_MUST_HAVE_LINES` | 400 | Display inline error: "Customer return must have at least one line." |
| **Cross-service errors** | | | |
| E44 | `INVALID_PRODUCT` | 400 | Display inline error on Product field: "The selected product does not exist or is inactive." |
| E45 | `INVALID_WAREHOUSE` | 400 | Display inline error on Warehouse field: "The selected warehouse does not exist or is inactive." |
| E46 | `INVALID_LOCATION` | 400 | Display inline error on Location field: "The selected location is not valid for this warehouse." |
| E47 | `INVALID_SO_REFERENCE` | 400 | Display inline error: "The specified sales order does not exist." |
| **Common errors** | | | |
| E48 | `VALIDATION_ERROR` | 400 | Map `errors` object from ProblemDetails to inline field errors. |
| E49 | `FORBIDDEN` | 403 | Display snackbar: "You do not have permission to perform this action." |
| E50 | `UNAUTHORIZED` | 401 | Trigger token refresh per `SDD-UI-001` Section 2.1. |
| E51 | Network error | — | Display snackbar: "Unable to connect to the server. Please check your connection." |
| E52 | Unhandled 500 | 500 | Display snackbar: "An unexpected error occurred. Please try again." |

---

## 5. Routes

| Route | View | Auth Required | Description |
|---|---|---|---|
| `/fulfillment/sales-orders` | SalesOrdersView | Yes | SO list with filtering |
| `/fulfillment/sales-orders/create` | SalesOrderCreatePage | Yes | Create SO with lines |
| `/fulfillment/sales-orders/:id` | SalesOrderDetailView | Yes | SO detail with lines, progress, tabs |
| `/fulfillment/sales-orders/:id/edit` | SalesOrderEditPage | Yes | Edit SO header and lines |
| `/fulfillment/pick-lists` | PickListsView | Yes | Pick list list with filtering |
| `/fulfillment/pick-lists/:id` | PickListDetailView | Yes | Pick list detail with pick execution |
| `/fulfillment/shipments` | ShipmentsView | Yes | Shipment list with filtering |
| `/fulfillment/shipments/:id` | ShipmentDetailView | Yes | Shipment detail with tracking |
| `/fulfillment/carriers` | CarriersView | Yes | Carrier list |
| `/fulfillment/carriers/create` | CarrierCreatePage | Yes | Create carrier |
| `/fulfillment/carriers/:id` | CarrierDetailView | Yes | Carrier detail with service levels |
| `/fulfillment/carriers/:id/edit` | CarrierEditPage | Yes | Edit carrier |
| `/fulfillment/customer-returns` | CustomerReturnsView | Yes | Return list with filtering |
| `/fulfillment/customer-returns/create` | CustomerReturnCreatePage | Yes | Create return with lines |
| `/fulfillment/customer-returns/:id` | CustomerReturnDetailView | Yes | Return detail |
| `/fulfillment/fulfillment-events` | FulfillmentEventsView | Yes | Fulfillment event history |

---

## 6. Versioning Notes

- **v1 — Initial specification (2026-04-09)**
  - Sales order lifecycle management (list, create with lines and shipping address, detail with multi-tab progress, edit, confirm, cancel, complete)
  - Pick list management (list, generate from SO, detail with pick execution, cancel)
  - Packing workflow (create parcels, add/remove items, update parcel details)
  - Shipment management (dispatch from packed SO, list, detail with tracking, status updates)
  - Carrier management (list, create, detail with service levels, edit, deactivate)
  - Carrier service level management (create, edit, delete within carrier detail)
  - Customer return lifecycle (list, create with lines, detail, confirm, receive, close, cancel)
  - Fulfillment event history viewer
  - Client-side validation mirroring `SDD-FULF-001` rules
  - StatusChip display for all status-driven entities
  - Form display mode support per `SDD-UI-002`
  - i18n: English and Bulgarian

---

## 7. Test Plan

### Unit Tests — Sales Order List

- `salesOrdersView_LoadsTable_DisplaysColumns` [Unit]
- `salesOrdersView_FilterByStatus_FiltersResults` [Unit]
- `salesOrdersView_FilterByCustomer_FiltersResults` [Unit]
- `salesOrdersView_DefaultSort_CreatedDateDescending` [Unit]
- `salesOrdersView_StatusChip_DisplaysCorrectColor` [Unit]
- `salesOrdersView_ActionChips_DraftShowsEditConfirmCancel` [Unit]
- `salesOrdersView_ActionChips_ConfirmedShowsCancel` [Unit]
- `salesOrdersView_ActionChips_ShippedShowsComplete` [Unit]
- `salesOrdersView_CreateButton_RespectsFormDisplayMode` [Unit]

### Unit Tests — Sales Order Detail

- `salesOrderDetail_LoadsSOWithLines` [Unit]
- `salesOrderDetail_DisplaysFulfillmentProgress` [Unit]
- `salesOrderDetail_DraftStatus_ShowsEditConfirmCancelButtons` [Unit]
- `salesOrderDetail_ConfirmedStatus_ShowsGeneratePickListButton` [Unit]
- `salesOrderDetail_PackedStatus_ShowsDispatchButton` [Unit]
- `salesOrderDetail_ShippedStatus_ShowsCompleteButton` [Unit]
- `salesOrderDetail_DisplaysPickListsTab` [Unit]
- `salesOrderDetail_DisplaysParcelsTab` [Unit]
- `salesOrderDetail_DisplaysShipmentTab` [Unit]
- `salesOrderDetail_ConfirmAction_ShowsConfirmDialog` [Unit]
- `salesOrderDetail_CancelAction_ShowsConfirmDialog` [Unit]
- `salesOrderDetail_CompleteAction_ShowsConfirmDialog` [Unit]

### Unit Tests — Sales Order Create

- `salesOrderCreate_EmptyCustomer_ShowsValidationError` [Unit]
- `salesOrderCreate_EmptyWarehouse_ShowsValidationError` [Unit]
- `salesOrderCreate_EmptyStreetLine1_ShowsValidationError` [Unit]
- `salesOrderCreate_EmptyCity_ShowsValidationError` [Unit]
- `salesOrderCreate_EmptyPostalCode_ShowsValidationError` [Unit]
- `salesOrderCreate_InvalidCountryCode_ShowsValidationError` [Unit]
- `salesOrderCreate_NoLines_ShowsValidationError` [Unit]
- `salesOrderCreate_LineMissingProduct_ShowsError` [Unit]
- `salesOrderCreate_LineZeroQuantity_ShowsError` [Unit]
- `salesOrderCreate_CalculatesLineTotals` [Unit]
- `salesOrderCreate_CalculatesGrandTotal` [Unit]
- `salesOrderCreate_DuplicateProduct_ShowsError` [Unit]
- `salesOrderCreate_CustomerSelected_AutoPopulatesAddress` [Unit]
- `salesOrderCreate_ServiceLevelWithoutCarrier_Prevented` [Unit]
- `salesOrderCreate_SuccessfulSubmit_NavigatesToDetail` [Unit]

### Unit Tests — Sales Order Edit

- `salesOrderEdit_PrePopulatesHeaderAndAddress` [Unit]
- `salesOrderEdit_PrePopulatesLines` [Unit]
- `salesOrderEdit_CustomerFieldReadOnly` [Unit]
- `salesOrderEdit_NonDraftSO_ShowsNotEditableError` [Unit]
- `salesOrderEdit_RemoveLastLine_ShowsError` [Unit]

### Unit Tests — Pick Lists

- `pickListsView_LoadsTable_DisplaysColumns` [Unit]
- `pickListsView_FilterByStatus_FiltersResults` [Unit]
- `pickListsView_StatusChip_DisplaysCorrectColor` [Unit]
- `pickListDetail_LoadsPickListWithLines` [Unit]
- `pickListDetail_UnpickedLine_ShowsPickButton` [Unit]
- `pickListDetail_PickedLine_ShowsPickedStatus` [Unit]
- `pickListDetail_PickDialog_RequiresQuantity` [Unit]
- `pickListDetail_PickDialog_ZeroQuantity_ShowsError` [Unit]
- `pickListDetail_PickDialog_OverPick_ShowsError` [Unit]
- `pickListDetail_PickDialog_SuccessfulPick_RefreshesDetail` [Unit]
- `pickListDetail_AllLinesPicked_StatusTransitionsToCompleted` [Unit]
- `pickListDetail_CancelButton_ShowsConfirmDialog` [Unit]
- `pickListGenerate_InsufficientStock_ShowsError` [Unit]
- `pickListGenerate_SOFullyAllocated_ShowsError` [Unit]

### Unit Tests — Packing

- `parcelsTab_DisplaysParcelsList` [Unit]
- `parcelCreate_ValidSubmit_RefreshesList` [Unit]
- `parcelCreate_NegativeWeight_ShowsError` [Unit]
- `parcelAddItem_DisplaysPickedProducts` [Unit]
- `parcelAddItem_OverPack_ShowsError` [Unit]
- `parcelAddItem_SuccessfulAdd_RefreshesParcel` [Unit]
- `parcelUpdate_PrePopulatesFields` [Unit]
- `parcelRemove_ShowsConfirmDialog` [Unit]
- `packingCompletion_AllLinesPacked_SOTransitionsToPacked` [Unit]

### Unit Tests — Shipments

- `shipmentsView_LoadsTable_DisplaysColumns` [Unit]
- `shipmentsView_FilterByStatus_FiltersResults` [Unit]
- `shipmentsView_StatusChip_DisplaysCorrectColor` [Unit]
- `shipmentDetail_LoadsShipmentWithTracking` [Unit]
- `shipmentDetail_DisplaysLines` [Unit]
- `shipmentDetail_DisplaysParcels` [Unit]
- `shipmentDetail_DisplaysTrackingHistory` [Unit]
- `shipmentDetail_NonTerminalStatus_ShowsUpdateStatusButton` [Unit]
- `shipmentStatusUpdate_DisplaysValidTransitionsOnly` [Unit]
- `shipmentStatusUpdate_SuccessfulUpdate_RefreshesDetail` [Unit]
- `shipmentStatusUpdate_InvalidTransition_ShowsError` [Unit]
- `dispatch_PackedSO_CreatesShipment` [Unit]
- `dispatch_EmptyParcel_ShowsError` [Unit]
- `dispatch_SONotPacked_ShowsError` [Unit]

### Unit Tests — Carriers

- `carriersView_LoadsTable_DisplaysColumns` [Unit]
- `carriersView_FilterByActive_FiltersResults` [Unit]
- `carrierDetail_LoadsCarrierWithServiceLevels` [Unit]
- `carrierCreate_EmptyCode_ShowsError` [Unit]
- `carrierCreate_EmptyName_ShowsError` [Unit]
- `carrierCreate_SuccessfulSubmit_NavigatesToDetail` [Unit]
- `carrierEdit_PrePopulatesFields` [Unit]
- `carrierEdit_CodeFieldReadOnly` [Unit]
- `carrierDeactivate_ShowsConfirmDialog` [Unit]
- `carrierDeactivate_ActiveShipments_ShowsError` [Unit]
- `serviceLevelCreate_DuplicateCode_ShowsError` [Unit]
- `serviceLevelDelete_InUse_ShowsError` [Unit]
- `serviceLevelDelete_ShowsConfirmDialog` [Unit]

### Unit Tests — Customer Returns

- `customerReturnsView_LoadsTable_DisplaysColumns` [Unit]
- `customerReturnsView_StatusChip_DisplaysCorrectColor` [Unit]
- `customerReturnsView_ActionChips_DraftShowsConfirmCancel` [Unit]
- `customerReturnsView_ActionChips_ConfirmedShowsReceiveCancel` [Unit]
- `customerReturnsView_ActionChips_ReceivedShowsClose` [Unit]
- `customerReturnDetail_LoadsReturnWithLines` [Unit]
- `customerReturnDetail_DraftStatus_ShowsConfirmCancelButtons` [Unit]
- `customerReturnDetail_ConfirmedStatus_ShowsReceiveCancelButtons` [Unit]
- `customerReturnDetail_ReceivedStatus_ShowsCloseButton` [Unit]
- `customerReturnCreate_EmptyCustomer_ShowsError` [Unit]
- `customerReturnCreate_EmptyReason_ShowsError` [Unit]
- `customerReturnCreate_NoLines_ShowsError` [Unit]
- `customerReturnCreate_SOSelected_PrePopulatesProducts` [Unit]
- `customerReturnCreate_SuccessfulSubmit_NavigatesToDetail` [Unit]

### Unit Tests — Fulfillment Events

- `fulfillmentEventsView_LoadsTable_DisplaysColumns` [Unit]
- `fulfillmentEventsView_FilterByEventType_FiltersResults` [Unit]
- `fulfillmentEventsView_ExpandRow_ShowsJsonPayload` [Unit]

### Unit Tests — Validation

- `salesOrderForm_MissingShippingAddress_ShowsErrors` [Unit]
- `salesOrderForm_InvalidCountryCode_ShowsError` [Unit]
- `pickDialog_ZeroQuantity_ShowsError` [Unit]
- `pickDialog_ExceedsRequested_ShowsError` [Unit]
- `parcelForm_NegativeDimensions_ShowsError` [Unit]
- `carrierForm_InvalidEmailFormat_ShowsError` [Unit]
- `carrierForm_InvalidWebsiteUrl_ShowsError` [Unit]
- `returnForm_MissingReason_ShowsError` [Unit]

### Unit Tests — i18n

- `i18n_FulfillmentLabels_TranslatedInEnglish` [Unit]
- `i18n_FulfillmentLabels_TranslatedInBulgarian` [Unit]
- `i18n_FulfillmentPageTitles_TranslatedInBulgarian` [Unit]
- `i18n_FulfillmentStatusLabels_TranslatedInBulgarian` [Unit]
- `i18n_FulfillmentValidationMessages_TranslatedInBulgarian` [Unit]
- `i18n_ShipmentStatusLabels_TranslatedInBulgarian` [Unit]
- `i18n_ReturnStatusLabels_TranslatedInBulgarian` [Unit]

### Integration Tests — Sales Order Flows

- `salesOrderList_LoadsAndDisplaysOrders` [Integration]
- `salesOrderCreate_SubmitsAndRedirectsToDetail` [Integration]
- `salesOrderDetail_ConfirmAction_UpdatesStatus` [Integration]
- `salesOrderDetail_CancelAction_UpdatesStatus` [Integration]
- `salesOrderDetail_CompleteAction_UpdatesStatus` [Integration]
- `salesOrderEdit_UpdatesHeaderAndRefreshes` [Integration]
- `salesOrderEdit_AddLine_RefreshesLines` [Integration]
- `salesOrderEdit_RemoveLine_RefreshesLines` [Integration]

### Integration Tests — Pick List Flows

- `pickListGenerate_CreatesPickListAndUpdatesSOStatus` [Integration]
- `pickListDetail_PickLine_UpdatesQuantityAndStatus` [Integration]
- `pickListDetail_AllLinesPicked_TransitionsToCompleted` [Integration]
- `pickListCancel_ReleasesReservations` [Integration]

### Integration Tests — Packing Flows

- `parcelCreate_AddsToSOParcelsTab` [Integration]
- `parcelAddItem_UpdatesPackedQuantity` [Integration]
- `allLinesPacked_SOTransitionsToPacked` [Integration]

### Integration Tests — Shipment Flows

- `dispatch_CreatesShipmentAndUpdatesSOToShipped` [Integration]
- `shipmentDetail_UpdateStatus_AppendsTrackingHistory` [Integration]
- `shipmentList_LoadsAndDisplaysShipments` [Integration]

### Integration Tests — Carrier Flows

- `carrierList_LoadsAndDisplaysCarriers` [Integration]
- `carrierCreate_SubmitsAndRedirectsToDetail` [Integration]
- `carrierDetail_CreateServiceLevel_AppearsInList` [Integration]
- `carrierDeactivate_WithActiveShipments_ShowsError` [Integration]

### Integration Tests — Customer Return Flows

- `customerReturnCreate_SubmitsAndRedirectsToDetail` [Integration]
- `customerReturnDetail_ConfirmAction_UpdatesStatus` [Integration]
- `customerReturnDetail_ReceiveAction_UpdatesStatus` [Integration]
- `customerReturnDetail_CloseAction_UpdatesStatus` [Integration]
- `customerReturnDetail_CancelDraft_UpdatesStatus` [Integration]

### Integration Tests — Navigation

- `fulfillmentSidebar_AllLinksNavigateCorrectly` [Integration]
- `salesOrderDetail_BackButton_NavigatesToList` [Integration]
- `routeGuard_UnauthenticatedAccess_RedirectsToLogin` [Integration]

---

## Key Files

- `frontend/src/features/fulfillment/api/sales-orders.ts` (new)
- `frontend/src/features/fulfillment/api/pick-lists.ts` (new)
- `frontend/src/features/fulfillment/api/shipments.ts` (new)
- `frontend/src/features/fulfillment/api/carriers.ts` (new)
- `frontend/src/features/fulfillment/api/customer-returns.ts` (new)
- `frontend/src/features/fulfillment/api/fulfillment-events.ts` (new)
- `frontend/src/features/fulfillment/types/fulfillment.ts` (new)
- `frontend/src/features/fulfillment/composables/useSalesOrdersView.ts` (new)
- `frontend/src/features/fulfillment/composables/useSalesOrderDetailView.ts` (new)
- `frontend/src/features/fulfillment/composables/usePickListsView.ts` (new)
- `frontend/src/features/fulfillment/composables/usePickListDetailView.ts` (new)
- `frontend/src/features/fulfillment/composables/useShipmentsView.ts` (new)
- `frontend/src/features/fulfillment/composables/useShipmentDetailView.ts` (new)
- `frontend/src/features/fulfillment/composables/useCarriersView.ts` (new)
- `frontend/src/features/fulfillment/composables/useCarrierDetailView.ts` (new)
- `frontend/src/features/fulfillment/composables/useCustomerReturnsView.ts` (new)
- `frontend/src/features/fulfillment/composables/useCustomerReturnDetailView.ts` (new)
- `frontend/src/features/fulfillment/composables/useFulfillmentEventsView.ts` (new)
- `frontend/src/features/fulfillment/views/sales-orders/SalesOrdersView.vue` (new)
- `frontend/src/features/fulfillment/views/sales-orders/SalesOrderCreatePage.vue` (new)
- `frontend/src/features/fulfillment/views/sales-orders/SalesOrderDetailView.vue` (new)
- `frontend/src/features/fulfillment/views/sales-orders/SalesOrderEditPage.vue` (new)
- `frontend/src/features/fulfillment/views/pick-lists/PickListsView.vue` (new)
- `frontend/src/features/fulfillment/views/pick-lists/PickListDetailView.vue` (new)
- `frontend/src/features/fulfillment/views/shipments/ShipmentsView.vue` (new)
- `frontend/src/features/fulfillment/views/shipments/ShipmentDetailView.vue` (new)
- `frontend/src/features/fulfillment/views/carriers/CarriersView.vue` (new)
- `frontend/src/features/fulfillment/views/carriers/CarrierCreatePage.vue` (new)
- `frontend/src/features/fulfillment/views/carriers/CarrierDetailView.vue` (new)
- `frontend/src/features/fulfillment/views/carriers/CarrierEditPage.vue` (new)
- `frontend/src/features/fulfillment/views/customer-returns/CustomerReturnsView.vue` (new)
- `frontend/src/features/fulfillment/views/customer-returns/CustomerReturnCreatePage.vue` (new)
- `frontend/src/features/fulfillment/views/customer-returns/CustomerReturnDetailView.vue` (new)
- `frontend/src/features/fulfillment/views/fulfillment-events/FulfillmentEventsView.vue` (new)
- `frontend/src/features/fulfillment/components/organisms/SalesOrderFormDialog.vue` (new)
- `frontend/src/features/fulfillment/components/organisms/PickDialog.vue` (new)
- `frontend/src/features/fulfillment/components/organisms/ParcelFormDialog.vue` (new)
- `frontend/src/features/fulfillment/components/organisms/ParcelItemDialog.vue` (new)
- `frontend/src/features/fulfillment/components/organisms/DispatchDialog.vue` (new)
- `frontend/src/features/fulfillment/components/organisms/ShipmentStatusUpdateDialog.vue` (new)
- `frontend/src/features/fulfillment/components/organisms/CarrierFormDialog.vue` (new)
- `frontend/src/features/fulfillment/components/organisms/ServiceLevelFormDialog.vue` (new)
- `frontend/src/features/fulfillment/components/organisms/CustomerReturnFormDialog.vue` (new)
- `frontend/src/app/router/fulfillment.routes.ts` (new)
- `frontend/src/shared/i18n/locales/en.ts` (modified — add fulfillment keys)
- `frontend/src/shared/i18n/locales/bg.ts` (modified — add fulfillment keys)
- `frontend/src/shared/components/templates/DefaultLayout.vue` (modified — add fulfillment nav group)
