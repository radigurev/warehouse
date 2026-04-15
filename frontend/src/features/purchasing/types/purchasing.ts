// ---------------------------------------------------------------------------
// Enums (string unions matching backend C# enums)
// ---------------------------------------------------------------------------

export type PurchaseOrderStatus =
  | 'Draft'
  | 'Confirmed'
  | 'PartiallyReceived'
  | 'Received'
  | 'Closed'
  | 'Cancelled';

export type InspectionStatus = 'Pending' | 'Accepted' | 'Rejected' | 'Quarantined';

export type SupplierReturnStatus = 'Draft' | 'Confirmed' | 'Cancelled';

export type SupplierAddressType = 'Billing' | 'Shipping' | 'Both';

export type SupplierPhoneType = 'Mobile' | 'Landline' | 'Fax';

export type SupplierEmailType = 'General' | 'Billing' | 'Support';

export type PurchaseEventType =
  | 'PurchaseOrderCreated'
  | 'PurchaseOrderConfirmed'
  | 'PurchaseOrderCancelled'
  | 'PurchaseOrderClosed'
  | 'GoodsReceiptCreated'
  | 'GoodsReceiptCompleted'
  | 'InspectionCompleted'
  | 'SupplierReturnCreated'
  | 'SupplierReturnConfirmed'
  | 'SupplierReturnCancelled';

// ---------------------------------------------------------------------------
// Supplier DTOs
// ---------------------------------------------------------------------------

export interface SupplierDto {
  id: number;
  code: string;
  name: string;
  taxId: string | null;
  categoryName: string | null;
  paymentTermDays: number | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface SupplierDetailDto {
  id: number;
  code: string;
  name: string;
  taxId: string | null;
  categoryId: number | null;
  categoryName: string | null;
  paymentTermDays: number | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  addresses: SupplierAddressDto[];
  phones: SupplierPhoneDto[];
  emails: SupplierEmailDto[];
}

export interface SupplierCategoryDto {
  id: number;
  name: string;
  description: string | null;
  createdAtUtc: string;
}

export interface SupplierAddressDto {
  id: number;
  addressType: string;
  streetLine1: string;
  streetLine2: string | null;
  city: string;
  stateProvince: string | null;
  postalCode: string;
  countryCode: string;
  isDefault: boolean;
  createdAtUtc: string;
}

export interface SupplierPhoneDto {
  id: number;
  phoneType: string;
  phoneNumber: string;
  extension: string | null;
  isPrimary: boolean;
  createdAtUtc: string;
}

export interface SupplierEmailDto {
  id: number;
  emailType: string;
  emailAddress: string;
  isPrimary: boolean;
  createdAtUtc: string;
}

// ---------------------------------------------------------------------------
// Purchase Order DTOs
// ---------------------------------------------------------------------------

export interface PurchaseOrderDto {
  id: number;
  orderNumber: string;
  supplierId: number;
  supplierName: string;
  status: string;
  destinationWarehouseId: number;
  expectedDeliveryDate: string | null;
  totalAmount: number;
  createdAtUtc: string;
}

export interface PurchaseOrderDetailDto {
  id: number;
  orderNumber: string;
  supplierId: number;
  supplierName: string;
  status: string;
  destinationWarehouseId: number;
  expectedDeliveryDate: string | null;
  notes: string | null;
  totalAmount: number;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
  closedAtUtc: string | null;
  lines: PurchaseOrderLineDto[];
}

export interface PurchaseOrderLineDto {
  id: number;
  productId: number;
  productName: string;
  productCode: string;
  orderedQuantity: number;
  unitPrice: number;
  lineTotal: number;
  receivedQuantity: number;
  remainingQuantity: number;
  notes: string | null;
}

// ---------------------------------------------------------------------------
// Goods Receipt DTOs
// ---------------------------------------------------------------------------

export interface GoodsReceiptDto {
  id: number;
  receiptNumber: string;
  purchaseOrderId: number;
  purchaseOrderNumber: string;
  warehouseId: number;
  status: string;
  receivedAtUtc: string;
  completedAtUtc: string | null;
}

export interface GoodsReceiptDetailDto {
  id: number;
  receiptNumber: string;
  purchaseOrderId: number;
  purchaseOrderNumber: string;
  warehouseId: number;
  locationId: number | null;
  status: string;
  notes: string | null;
  receivedAtUtc: string;
  completedAtUtc: string | null;
  lines: GoodsReceiptLineDto[];
}

export interface GoodsReceiptLineDto {
  id: number;
  purchaseOrderLineId: number;
  receivedQuantity: number;
  batchNumber: string | null;
  manufacturingDate: string | null;
  expiryDate: string | null;
  inspectionStatus: string;
  inspectionNote: string | null;
  inspectedAtUtc: string | null;
}

// ---------------------------------------------------------------------------
// Supplier Return DTOs
// ---------------------------------------------------------------------------

export interface SupplierReturnDto {
  id: number;
  returnNumber: string;
  supplierId: number;
  supplierName: string;
  status: string;
  reason: string;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
}

export interface SupplierReturnDetailDto {
  id: number;
  returnNumber: string;
  supplierId: number;
  supplierName: string;
  status: string;
  reason: string;
  notes: string | null;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
  lines: SupplierReturnLineDto[];
}

export interface SupplierReturnLineDto {
  id: number;
  productId: number;
  warehouseId: number;
  locationId: number | null;
  quantity: number;
  batchId: number | null;
  goodsReceiptLineId: number | null;
  notes: string | null;
}

// ---------------------------------------------------------------------------
// Purchase Event DTOs
// ---------------------------------------------------------------------------

export interface PurchaseEventDto {
  id: number;
  eventType: string;
  entityType: string;
  entityId: number;
  userId: number;
  occurredAtUtc: string;
  payload: string | null;
}

// ---------------------------------------------------------------------------
// Supplier Requests
// ---------------------------------------------------------------------------

export interface CreateSupplierRequest {
  name: string;
  code?: string | null;
  taxId?: string | null;
  categoryId?: number | null;
  paymentTermDays?: number | null;
  notes?: string | null;
}

export interface UpdateSupplierRequest {
  name: string;
  taxId?: string | null;
  categoryId?: number | null;
  paymentTermDays?: number | null;
  notes?: string | null;
}

export interface SearchSuppliersRequest {
  name?: string | null;
  code?: string | null;
  taxId?: string | null;
  categoryId?: number | null;
  includeDeleted: boolean;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Supplier Category Requests
// ---------------------------------------------------------------------------

export interface CreateSupplierCategoryRequest {
  name: string;
  description?: string | null;
}

export interface UpdateSupplierCategoryRequest {
  name: string;
  description?: string | null;
}

// ---------------------------------------------------------------------------
// Supplier Contact Requests
// ---------------------------------------------------------------------------

export interface CreateSupplierAddressRequest {
  addressType: string;
  streetLine1: string;
  streetLine2?: string | null;
  city: string;
  stateProvince?: string | null;
  postalCode: string;
  countryCode: string;
}

export interface UpdateSupplierAddressRequest {
  addressType: string;
  streetLine1: string;
  streetLine2?: string | null;
  city: string;
  stateProvince?: string | null;
  postalCode: string;
  countryCode: string;
  isDefault: boolean;
}

export interface CreateSupplierPhoneRequest {
  phoneType: string;
  phoneNumber: string;
  extension?: string | null;
}

export interface UpdateSupplierPhoneRequest {
  phoneType: string;
  phoneNumber: string;
  extension?: string | null;
  isPrimary: boolean;
}

export interface CreateSupplierEmailRequest {
  emailType: string;
  emailAddress: string;
}

export interface UpdateSupplierEmailRequest {
  emailType: string;
  emailAddress: string;
  isPrimary: boolean;
}

// ---------------------------------------------------------------------------
// Purchase Order Requests
// ---------------------------------------------------------------------------

export interface CreatePurchaseOrderRequest {
  supplierId: number;
  destinationWarehouseId: number;
  expectedDeliveryDate?: string | null;
  notes?: string | null;
  lines: CreatePurchaseOrderLineRequest[];
}

export interface CreatePurchaseOrderLineRequest {
  productId: number;
  orderedQuantity: number;
  unitPrice: number;
  notes?: string | null;
}

export interface UpdatePurchaseOrderRequest {
  destinationWarehouseId: number;
  expectedDeliveryDate?: string | null;
  notes?: string | null;
}

export interface UpdatePurchaseOrderLineRequest {
  orderedQuantity: number;
  unitPrice: number;
  notes?: string | null;
}

export interface SearchPurchaseOrdersRequest {
  supplierId?: number | null;
  status?: string | null;
  orderNumber?: string | null;
  destinationWarehouseId?: number | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
}

// ---------------------------------------------------------------------------
// Goods Receipt Requests
// ---------------------------------------------------------------------------

export interface CreateGoodsReceiptRequest {
  purchaseOrderId: number;
  warehouseId: number;
  locationId?: number | null;
  notes?: string | null;
  lines: CreateGoodsReceiptLineRequest[];
}

export interface CreateGoodsReceiptLineRequest {
  purchaseOrderLineId: number;
  receivedQuantity: number;
  batchNumber?: string | null;
  manufacturingDate?: string | null;
  expiryDate?: string | null;
}

export interface SearchGoodsReceiptsRequest {
  purchaseOrderId?: number | null;
  purchaseOrderNumber?: string | null;
  receiptNumber?: string | null;
  warehouseId?: number | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
}

export interface InspectLineRequest {
  inspectionStatus: string;
  inspectionNote?: string | null;
}

export interface ResolveQuarantineRequest {
  resolution: string;
  note?: string | null;
}

// ---------------------------------------------------------------------------
// Supplier Return Requests
// ---------------------------------------------------------------------------

export interface CreateSupplierReturnRequest {
  supplierId: number;
  reason: string;
  notes?: string | null;
  lines: CreateSupplierReturnLineRequest[];
}

export interface CreateSupplierReturnLineRequest {
  productId: number;
  warehouseId: number;
  locationId?: number | null;
  quantity: number;
  batchId?: number | null;
  goodsReceiptLineId?: number | null;
  notes?: string | null;
}

export interface SearchSupplierReturnsRequest {
  supplierId?: number | null;
  status?: string | null;
  returnNumber?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
}

// ---------------------------------------------------------------------------
// Purchase Events Requests
// ---------------------------------------------------------------------------

export interface SearchPurchaseEventsRequest {
  eventType?: string | null;
  entityType?: string | null;
  entityId?: number | null;
  userId?: number | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  page: number;
  pageSize: number;
}
