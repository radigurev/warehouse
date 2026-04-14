// ---------------------------------------------------------------------------
// Enums (string unions matching backend C# enums)
// ---------------------------------------------------------------------------

export type SalesOrderStatus =
  | 'Draft'
  | 'Confirmed'
  | 'Picking'
  | 'Packed'
  | 'Shipped'
  | 'Completed'
  | 'Cancelled';

export type PickListStatus = 'Pending' | 'Completed' | 'Cancelled';

export type PickLineStatus = 'Pending' | 'Picked';

export type ShipmentStatus =
  | 'Dispatched'
  | 'InTransit'
  | 'OutForDelivery'
  | 'Delivered'
  | 'Failed'
  | 'Returned';

export type CustomerReturnStatus =
  | 'Draft'
  | 'Confirmed'
  | 'Received'
  | 'Closed'
  | 'Cancelled';

export type FulfillmentEventType =
  | 'SalesOrderCreated'
  | 'SalesOrderConfirmed'
  | 'SalesOrderCancelled'
  | 'SalesOrderCompleted'
  | 'PickListGenerated'
  | 'PickListCompleted'
  | 'PickListCancelled'
  | 'ParcelCreated'
  | 'ShipmentDispatched'
  | 'ShipmentStatusUpdated'
  | 'CustomerReturnCreated'
  | 'CustomerReturnConfirmed'
  | 'CustomerReturnReceived'
  | 'CustomerReturnClosed'
  | 'CustomerReturnCancelled';

// ---------------------------------------------------------------------------
// Sales Order DTOs
// ---------------------------------------------------------------------------

export interface SalesOrderDto {
  id: number;
  orderNumber: string;
  customerId: number;
  customerName: string;
  status: SalesOrderStatus;
  warehouseId: number;
  warehouseName: string;
  requestedShipDate: string | null;
  totalAmount: number;
  createdAtUtc: string;
}

export interface SalesOrderDetailDto {
  id: number;
  orderNumber: string;
  customerId: number;
  customerName: string;
  status: SalesOrderStatus;
  warehouseId: number;
  warehouseName: string;
  requestedShipDate: string | null;
  carrierId: number | null;
  carrierName: string | null;
  carrierServiceLevelId: number | null;
  carrierServiceLevelName: string | null;
  shippingStreetLine1: string;
  shippingStreetLine2: string | null;
  shippingCity: string;
  shippingStateProvince: string | null;
  shippingPostalCode: string;
  shippingCountryCode: string;
  notes: string | null;
  totalAmount: number;
  createdByUserId: number;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
  shippedAtUtc: string | null;
  completedAtUtc: string | null;
  lines: SalesOrderLineDto[];
  pickLists: PickListSummaryDto[];
  parcels: ParcelDto[];
  shipment: ShipmentSummaryDto | null;
}

export interface SalesOrderLineDto {
  id: number;
  productId: number;
  productCode: string;
  productName: string;
  orderedQuantity: number;
  unitPrice: number;
  lineTotal: number;
  pickedQuantity: number;
  packedQuantity: number;
  shippedQuantity: number;
  notes: string | null;
}

// ---------------------------------------------------------------------------
// Pick List DTOs
// ---------------------------------------------------------------------------

export interface PickListDto {
  id: number;
  pickListNumber: string;
  salesOrderId: number;
  salesOrderNumber: string;
  warehouseId: number;
  warehouseName: string;
  status: PickListStatus;
  createdAtUtc: string;
}

export interface PickListDetailDto {
  id: number;
  pickListNumber: string;
  salesOrderId: number;
  salesOrderNumber: string;
  warehouseId: number;
  warehouseName: string;
  status: PickListStatus;
  createdByUserId: number;
  createdAtUtc: string;
  lines: PickListLineDto[];
}

export interface PickListLineDto {
  id: number;
  productId: number;
  productCode: string;
  productName: string;
  sourceLocationId: number | null;
  sourceLocationCode: string | null;
  requestedQuantity: number;
  actualPickedQuantity: number | null;
  pickedByUserId: number | null;
  pickedAtUtc: string | null;
  status: PickLineStatus;
}

export interface PickListSummaryDto {
  id: number;
  pickListNumber: string;
  status: PickListStatus;
  createdAtUtc: string;
}

// ---------------------------------------------------------------------------
// Parcel DTOs
// ---------------------------------------------------------------------------

export interface ParcelDto {
  id: number;
  parcelNumber: string;
  weightKg: number | null;
  lengthCm: number | null;
  widthCm: number | null;
  heightCm: number | null;
  trackingNumber: string | null;
  notes: string | null;
  items: ParcelItemDto[];
}

export interface ParcelItemDto {
  id: number;
  productId: number;
  productCode: string;
  productName: string;
  pickListLineId: number;
  quantity: number;
}

// ---------------------------------------------------------------------------
// Shipment DTOs
// ---------------------------------------------------------------------------

export interface ShipmentDto {
  id: number;
  shipmentNumber: string;
  salesOrderId: number;
  salesOrderNumber: string;
  carrierId: number | null;
  carrierName: string | null;
  status: ShipmentStatus;
  dispatchedAtUtc: string;
  trackingNumber: string | null;
}

export interface ShipmentDetailDto {
  id: number;
  shipmentNumber: string;
  salesOrderId: number;
  salesOrderNumber: string;
  carrierId: number | null;
  carrierName: string | null;
  carrierServiceLevelId: number | null;
  carrierServiceLevelName: string | null;
  status: ShipmentStatus;
  trackingNumber: string | null;
  trackingUrl: string | null;
  shippingStreetLine1: string;
  shippingStreetLine2: string | null;
  shippingCity: string;
  shippingStateProvince: string | null;
  shippingPostalCode: string;
  shippingCountryCode: string;
  dispatchedByUserId: number;
  dispatchedAtUtc: string;
  notes: string | null;
  lines: ShipmentLineDto[];
  parcels: ShipmentParcelDto[];
  trackingHistory: ShipmentTrackingEntryDto[];
}

export interface ShipmentLineDto {
  id: number;
  productId: number;
  productCode: string;
  productName: string;
  shippedQuantity: number;
}

export interface ShipmentParcelDto {
  id: number;
  parcelNumber: string;
  weightKg: number | null;
  lengthCm: number | null;
  widthCm: number | null;
  heightCm: number | null;
  trackingNumber: string | null;
}

export interface ShipmentTrackingEntryDto {
  id: number;
  status: ShipmentStatus;
  notes: string | null;
  updatedByUserId: number;
  updatedAtUtc: string;
}

export interface ShipmentSummaryDto {
  id: number;
  shipmentNumber: string;
  status: ShipmentStatus;
  dispatchedAtUtc: string;
}

// ---------------------------------------------------------------------------
// Carrier DTOs
// ---------------------------------------------------------------------------

export interface CarrierDto {
  id: number;
  code: string;
  name: string;
  contactEmail: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CarrierDetailDto {
  id: number;
  code: string;
  name: string;
  contactPhone: string | null;
  contactEmail: string | null;
  websiteUrl: string | null;
  trackingUrlTemplate: string | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  serviceLevels: CarrierServiceLevelDto[];
}

export interface CarrierServiceLevelDto {
  id: number;
  code: string;
  name: string;
  estimatedDeliveryDays: number | null;
  baseRate: number | null;
  perKgRate: number | null;
  notes: string | null;
}

// ---------------------------------------------------------------------------
// Customer Return DTOs
// ---------------------------------------------------------------------------

export interface CustomerReturnDto {
  id: number;
  returnNumber: string;
  customerId: number;
  customerName: string;
  status: CustomerReturnStatus;
  salesOrderId: number | null;
  salesOrderNumber: string | null;
  reason: string;
  createdAtUtc: string;
}

export interface CustomerReturnDetailDto {
  id: number;
  returnNumber: string;
  customerId: number;
  customerName: string;
  status: CustomerReturnStatus;
  salesOrderId: number | null;
  salesOrderNumber: string | null;
  reason: string;
  notes: string | null;
  createdByUserId: number;
  createdAtUtc: string;
  confirmedByUserId: number | null;
  confirmedAtUtc: string | null;
  receivedByUserId: number | null;
  receivedAtUtc: string | null;
  closedByUserId: number | null;
  closedAtUtc: string | null;
  lines: CustomerReturnLineDto[];
}

export interface CustomerReturnLineDto {
  id: number;
  productId: number;
  productCode: string;
  productName: string;
  warehouseId: number;
  warehouseName: string;
  locationId: number | null;
  locationCode: string | null;
  quantity: number;
  notes: string | null;
}

// ---------------------------------------------------------------------------
// Fulfillment Event DTOs
// ---------------------------------------------------------------------------

export interface FulfillmentEventDto {
  id: number;
  eventType: string;
  entityType: string;
  entityId: number;
  userId: number;
  occurredAtUtc: string;
  payload: string | null;
}

// ---------------------------------------------------------------------------
// Sales Order Requests
// ---------------------------------------------------------------------------

export interface CreateSalesOrderRequest {
  customerId: number;
  warehouseId: number;
  requestedShipDate?: string | null;
  carrierId?: number | null;
  carrierServiceLevelId?: number | null;
  shippingStreetLine1: string;
  shippingStreetLine2?: string | null;
  shippingCity: string;
  shippingStateProvince?: string | null;
  shippingPostalCode: string;
  shippingCountryCode: string;
  notes?: string | null;
  lines: CreateSalesOrderLineRequest[];
}

export interface CreateSalesOrderLineRequest {
  productId: number;
  orderedQuantity: number;
  unitPrice: number;
  notes?: string | null;
}

export interface UpdateSalesOrderRequest {
  warehouseId: number;
  requestedShipDate?: string | null;
  carrierId?: number | null;
  carrierServiceLevelId?: number | null;
  shippingStreetLine1: string;
  shippingStreetLine2?: string | null;
  shippingCity: string;
  shippingStateProvince?: string | null;
  shippingPostalCode: string;
  shippingCountryCode: string;
  notes?: string | null;
}

export interface UpdateSalesOrderLineRequest {
  orderedQuantity: number;
  unitPrice: number;
  notes?: string | null;
}

export interface SearchSalesOrdersRequest {
  customerId?: number | null;
  status?: string | null;
  orderNumber?: string | null;
  warehouseId?: number | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Pick List Requests
// ---------------------------------------------------------------------------

export interface CreatePickListRequest {
  salesOrderId: number;
}

export interface PickLineRequest {
  actualQuantity: number;
}

export interface SearchPickListsRequest {
  salesOrderId?: number | null;
  salesOrderNumber?: string | null;
  pickListNumber?: string | null;
  status?: string | null;
  warehouseId?: number | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Parcel Requests
// ---------------------------------------------------------------------------

export interface CreateParcelRequest {
  weightKg?: number | null;
  lengthCm?: number | null;
  widthCm?: number | null;
  heightCm?: number | null;
  trackingNumber?: string | null;
  notes?: string | null;
}

export interface UpdateParcelRequest {
  weightKg?: number | null;
  lengthCm?: number | null;
  widthCm?: number | null;
  heightCm?: number | null;
  trackingNumber?: string | null;
  notes?: string | null;
}

export interface AddParcelItemRequest {
  productId: number;
  pickListLineId: number;
  quantity: number;
}

// ---------------------------------------------------------------------------
// Shipment Requests
// ---------------------------------------------------------------------------

export interface CreateShipmentRequest {
  salesOrderId: number;
  carrierId?: number | null;
  carrierServiceLevelId?: number | null;
  notes?: string | null;
}

export interface UpdateShipmentStatusRequest {
  status: string;
  trackingNumber?: string | null;
  trackingUrl?: string | null;
  notes?: string | null;
}

export interface SearchShipmentsRequest {
  salesOrderNumber?: string | null;
  shipmentNumber?: string | null;
  carrierId?: number | null;
  status?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Carrier Requests
// ---------------------------------------------------------------------------

export interface CreateCarrierRequest {
  code: string;
  name: string;
  contactPhone?: string | null;
  contactEmail?: string | null;
  websiteUrl?: string | null;
  trackingUrlTemplate?: string | null;
  notes?: string | null;
}

export interface UpdateCarrierRequest {
  name: string;
  contactPhone?: string | null;
  contactEmail?: string | null;
  websiteUrl?: string | null;
  trackingUrlTemplate?: string | null;
  notes?: string | null;
}

export interface SearchCarriersRequest {
  name?: string | null;
  code?: string | null;
  isActive?: boolean | null;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
  filter?: string | null;
}

export interface CreateServiceLevelRequest {
  code: string;
  name: string;
  estimatedDeliveryDays?: number | null;
  baseRate?: number | null;
  perKgRate?: number | null;
  notes?: string | null;
}

export interface UpdateServiceLevelRequest {
  name: string;
  estimatedDeliveryDays?: number | null;
  baseRate?: number | null;
  perKgRate?: number | null;
  notes?: string | null;
}

// ---------------------------------------------------------------------------
// Customer Return Requests
// ---------------------------------------------------------------------------

export interface CreateCustomerReturnRequest {
  customerId: number;
  salesOrderId?: number | null;
  reason: string;
  notes?: string | null;
  lines: CreateCustomerReturnLineRequest[];
}

export interface CreateCustomerReturnLineRequest {
  productId: number;
  warehouseId: number;
  locationId?: number | null;
  quantity: number;
  notes?: string | null;
}

export interface SearchCustomerReturnsRequest {
  customerId?: number | null;
  status?: string | null;
  returnNumber?: string | null;
  salesOrderNumber?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Fulfillment Events Requests
// ---------------------------------------------------------------------------

export interface SearchFulfillmentEventsRequest {
  eventType?: string | null;
  entityType?: string | null;
  entityId?: number | null;
  userId?: number | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  page: number;
  pageSize: number;
}
