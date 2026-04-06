// ---------------------------------------------------------------------------
// Enums (string unions matching backend C# enums)
// ---------------------------------------------------------------------------

export type StockMovementReason =
  | 'PurchaseReceipt'
  | 'SalesDispatch'
  | 'Adjustment'
  | 'Transfer'
  | 'CustomerReturn'
  | 'SupplierReturn'
  | 'ProductionConsumption'
  | 'ProductionReceipt'
  | 'WriteOff'
  | 'StocktakeCorrection'
  | 'Other';

export type MovementDirection = 'In' | 'Out';

export type AdjustmentStatus = 'Pending' | 'Approved' | 'Rejected' | 'Applied';

export type TransferStatus = 'Draft' | 'Completed' | 'Cancelled';

export type StocktakeStatus = 'Draft' | 'InProgress' | 'Completed' | 'Cancelled';

export type LocationType = 'Row' | 'Shelf' | 'Bin' | 'Bulk';

// ---------------------------------------------------------------------------
// Product DTOs
// ---------------------------------------------------------------------------

export interface ProductDto {
  id: number;
  code: string;
  name: string;
  description: string | null;
  categoryName: string | null;
  unitOfMeasureName: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface ProductDetailDto {
  id: number;
  code: string;
  name: string;
  description: string | null;
  sku: string | null;
  barcode: string | null;
  categoryId: number | null;
  categoryName: string | null;
  unitOfMeasureId: number;
  unitOfMeasureName: string;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface ProductCategoryDto {
  id: number;
  name: string;
  description: string | null;
  parentCategoryId: number | null;
  parentCategoryName: string | null;
  createdAtUtc: string;
}

export interface UnitOfMeasureDto {
  id: number;
  code: string;
  name: string;
  description: string | null;
  createdAtUtc: string;
}

// ---------------------------------------------------------------------------
// BOM DTOs
// ---------------------------------------------------------------------------

export interface BomDto {
  id: number;
  parentProductId: number;
  parentProductName: string;
  name: string | null;
  isActive: boolean;
  createdAtUtc: string;
  lines: BomLineDto[];
}

export interface BomLineDto {
  id: number;
  billOfMaterialsId: number;
  childProductId: number;
  childProductName: string;
  quantity: number;
  notes: string | null;
}

// ---------------------------------------------------------------------------
// Product Relationship DTOs
// ---------------------------------------------------------------------------

export interface ProductAccessoryDto {
  id: number;
  productId: number;
  accessoryProductId: number;
  accessoryProductName: string;
  createdAtUtc: string;
}

export interface ProductSubstituteDto {
  id: number;
  productId: number;
  substituteProductId: number;
  substituteProductName: string;
  createdAtUtc: string;
}

// ---------------------------------------------------------------------------
// Warehouse DTOs
// ---------------------------------------------------------------------------

export interface WarehouseDto {
  id: number;
  code: string;
  name: string;
  address: string | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface WarehouseDetailDto {
  id: number;
  code: string;
  name: string;
  address: string | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  zones: ZoneDto[];
}

export interface ZoneDto {
  id: number;
  code: string;
  name: string;
  warehouseId: number;
  warehouseName: string;
  description: string | null;
  createdAtUtc: string;
}

export interface ZoneDetailDto {
  id: number;
  code: string;
  name: string;
  warehouseId: number;
  warehouseName: string;
  description: string | null;
  createdAtUtc: string;
  storageLocations: StorageLocationDto[];
}

export interface StorageLocationDto {
  id: number;
  code: string;
  name: string;
  warehouseId: number;
  warehouseName: string;
  zoneId: number | null;
  zoneName: string | null;
  locationType: string;
  capacity: number | null;
  createdAtUtc: string;
}

// ---------------------------------------------------------------------------
// Stock DTOs
// ---------------------------------------------------------------------------

export interface StockLevelDto {
  id: number;
  productId: number;
  productName: string;
  productCode: string;
  warehouseId: number;
  warehouseName: string;
  locationId: number | null;
  locationName: string | null;
  quantityOnHand: number;
  reservedQuantity: number;
  availableQuantity: number;
}

export interface StockSummaryDto {
  productId: number;
  productName: string;
  productCode: string;
  totalOnHand: number;
  totalReserved: number;
  totalAvailable: number;
  warehouseBreakdown: StockLevelDto[];
}

export interface StockMovementDto {
  id: number;
  productId: number;
  productName: string;
  warehouseId: number;
  warehouseName: string;
  locationId: number | null;
  locationName: string | null;
  quantity: number;
  reasonCode: string;
  referenceNumber: string | null;
  notes: string | null;
  createdAtUtc: string;
  createdByUserId: number;
}

export interface BatchDto {
  id: number;
  productId: number;
  productName: string;
  batchNumber: string;
  expiryDate: string | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

// ---------------------------------------------------------------------------
// Adjustment DTOs
// ---------------------------------------------------------------------------

export interface InventoryAdjustmentDto {
  id: number;
  status: string;
  reason: string;
  notes: string | null;
  createdAtUtc: string;
  createdByUserId: number;
}

export interface InventoryAdjustmentDetailDto {
  id: number;
  status: string;
  reason: string;
  notes: string | null;
  createdAtUtc: string;
  createdByUserId: number;
  approvedAtUtc: string | null;
  approvedByUserId: number | null;
  rejectedAtUtc: string | null;
  rejectedByUserId: number | null;
  rejectionReason: string | null;
  appliedAtUtc: string | null;
  appliedByUserId: number | null;
  lines: InventoryAdjustmentLineDto[];
}

export interface InventoryAdjustmentLineDto {
  id: number;
  adjustmentId: number;
  productId: number;
  productName: string;
  warehouseId: number;
  locationId: number | null;
  locationName: string | null;
  expectedQuantity: number;
  actualQuantity: number;
  variance: number;
}

// ---------------------------------------------------------------------------
// Transfer DTOs
// ---------------------------------------------------------------------------

export interface WarehouseTransferDto {
  id: number;
  status: string;
  sourceWarehouseId: number;
  sourceWarehouseName: string;
  destinationWarehouseId: number;
  destinationWarehouseName: string;
  notes: string | null;
  createdAtUtc: string;
  createdByUserId: number;
}

export interface WarehouseTransferDetailDto {
  id: number;
  status: string;
  sourceWarehouseId: number;
  sourceWarehouseName: string;
  destinationWarehouseId: number;
  destinationWarehouseName: string;
  notes: string | null;
  createdAtUtc: string;
  createdByUserId: number;
  completedAtUtc: string | null;
  completedByUserId: number | null;
  lines: WarehouseTransferLineDto[];
}

export interface WarehouseTransferLineDto {
  id: number;
  transferId: number;
  productId: number;
  productName: string;
  quantity: number;
  sourceLocationId: number | null;
  destinationLocationId: number | null;
}

// ---------------------------------------------------------------------------
// Stocktake DTOs
// ---------------------------------------------------------------------------

export interface StocktakeSessionDto {
  id: number;
  name: string;
  status: string;
  warehouseId: number;
  warehouseName: string;
  zoneId: number | null;
  zoneName: string | null;
  createdAtUtc: string;
  createdByUserId: number;
}

export interface StocktakeSessionDetailDto {
  id: number;
  name: string;
  status: string;
  warehouseId: number;
  warehouseName: string;
  zoneId: number | null;
  zoneName: string | null;
  notes: string | null;
  createdAtUtc: string;
  createdByUserId: number;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  counts: StocktakeCountDto[];
}

export interface StocktakeCountDto {
  id: number;
  sessionId: number;
  productId: number;
  productName: string;
  locationId: number | null;
  locationName: string | null;
  expectedQuantity: number;
  actualQuantity: number;
  variance: number;
}

// ---------------------------------------------------------------------------
// Product Requests
// ---------------------------------------------------------------------------

export interface CreateProductRequest {
  code: string;
  name: string;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  categoryId?: number | null;
  unitOfMeasureId: number;
  notes?: string | null;
}

export interface UpdateProductRequest {
  name: string;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  categoryId?: number | null;
  unitOfMeasureId: number;
  notes?: string | null;
}

export interface SearchProductsRequest {
  name?: string | null;
  code?: string | null;
  categoryId?: number | null;
  includeDeleted: boolean;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Product Category Requests
// ---------------------------------------------------------------------------

export interface CreateProductCategoryRequest {
  name: string;
  description?: string | null;
  parentCategoryId?: number | null;
}

export interface UpdateProductCategoryRequest {
  name: string;
  description?: string | null;
  parentCategoryId?: number | null;
}

// ---------------------------------------------------------------------------
// Unit of Measure Requests
// ---------------------------------------------------------------------------

export interface CreateUnitOfMeasureRequest {
  code: string;
  name: string;
  description?: string | null;
}

export interface UpdateUnitOfMeasureRequest {
  name: string;
  description?: string | null;
}

// ---------------------------------------------------------------------------
// BOM Requests
// ---------------------------------------------------------------------------

export interface CreateBomRequest {
  parentProductId: number;
  name?: string | null;
  lines: CreateBomLineRequest[];
}

export interface CreateBomLineRequest {
  childProductId: number;
  quantity: number;
}

export interface UpdateBomRequest {
  name?: string | null;
}

export interface AddBomLineRequest {
  childProductId: number;
  quantity: number;
}

// ---------------------------------------------------------------------------
// Product Relationship Requests
// ---------------------------------------------------------------------------

export interface CreateProductAccessoryRequest {
  productId: number;
  accessoryProductId: number;
}

export interface CreateProductSubstituteRequest {
  productId: number;
  substituteProductId: number;
}

// ---------------------------------------------------------------------------
// Warehouse Requests
// ---------------------------------------------------------------------------

export interface CreateWarehouseRequest {
  code: string;
  name: string;
  address?: string | null;
  notes?: string | null;
}

export interface UpdateWarehouseRequest {
  name: string;
  address?: string | null;
  notes?: string | null;
}

export interface SearchWarehousesRequest {
  name?: string | null;
  code?: string | null;
  includeDeleted: boolean;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Zone Requests
// ---------------------------------------------------------------------------

export interface CreateZoneRequest {
  warehouseId: number;
  code: string;
  name: string;
  description?: string | null;
}

export interface UpdateZoneRequest {
  name: string;
  description?: string | null;
}

export interface SearchZonesRequest {
  warehouseId?: number | null;
  name?: string | null;
  code?: string | null;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Storage Location Requests
// ---------------------------------------------------------------------------

export interface CreateStorageLocationRequest {
  zoneId: number;
  code: string;
  name: string;
  locationType: string;
  capacity?: number | null;
}

export interface UpdateStorageLocationRequest {
  name: string;
  locationType: string;
  capacity?: number | null;
}

export interface SearchStorageLocationsRequest {
  zoneId?: number | null;
  warehouseId?: number | null;
  name?: string | null;
  code?: string | null;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Stock Level Requests
// ---------------------------------------------------------------------------

export interface SearchStockLevelsRequest {
  productId?: number | null;
  warehouseId?: number | null;
  locationId?: number | null;
  minQuantity?: number | null;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Stock Movement Requests
// ---------------------------------------------------------------------------

export interface SearchStockMovementsRequest {
  productId?: number | null;
  warehouseId?: number | null;
  reasonCode?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
  filter?: string | null;
}

export interface RecordStockMovementRequest {
  productId: number;
  warehouseId: number;
  locationId?: number | null;
  quantity: number;
  reasonCode: string;
  batchId?: number | null;
  referenceNumber?: string | null;
  notes?: string | null;
}

// ---------------------------------------------------------------------------
// Batch Requests
// ---------------------------------------------------------------------------

export interface CreateBatchRequest {
  productId: number;
  batchNumber: string;
  expiryDate?: string | null;
  notes?: string | null;
}

export interface UpdateBatchRequest {
  expiryDate?: string | null;
  notes?: string | null;
}

export interface SearchBatchesRequest {
  productId?: number | null;
  batchNumber?: string | null;
  includeExpired: boolean;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Adjustment Requests
// ---------------------------------------------------------------------------

export interface CreateAdjustmentRequest {
  warehouseId: number;
  reason: string;
  notes?: string | null;
  lines: CreateAdjustmentLineRequest[];
}

export interface CreateAdjustmentLineRequest {
  productId: number;
  locationId?: number | null;
  actualQuantity: number;
}

export interface ApproveAdjustmentRequest {
  notes?: string | null;
}

export interface RejectAdjustmentRequest {
  notes: string;
}

export interface SearchAdjustmentsRequest {
  status?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Transfer Requests
// ---------------------------------------------------------------------------

export interface CreateTransferRequest {
  sourceWarehouseId: number;
  destinationWarehouseId: number;
  notes?: string | null;
  lines: CreateTransferLineRequest[];
}

export interface CreateTransferLineRequest {
  productId: number;
  quantity: number;
  sourceLocationId?: number | null;
  destinationLocationId?: number | null;
}

export interface CompleteTransferRequest {
  notes?: string | null;
}

export interface SearchTransfersRequest {
  sourceWarehouseId?: number | null;
  destinationWarehouseId?: number | null;
  status?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  page: number;
  pageSize: number;
  filter?: string | null;
}

// ---------------------------------------------------------------------------
// Stocktake Requests
// ---------------------------------------------------------------------------

export interface CreateStocktakeSessionRequest {
  warehouseId: number;
  zoneId?: number | null;
  name: string;
  notes?: string | null;
}

export interface SearchStocktakeSessionsRequest {
  warehouseId?: number | null;
  status?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  page: number;
  pageSize: number;
  filter?: string | null;
}

export interface RecordStocktakeCountRequest {
  productId: number;
  locationId?: number | null;
  countedQuantity: number;
}

export interface UpdateStocktakeCountRequest {
  countedQuantity: number;
}
