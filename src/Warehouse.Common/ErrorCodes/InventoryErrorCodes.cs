namespace Warehouse.Common.ErrorCodes;

/// <summary>
/// Error codes for the Inventory domain (products, stock, warehouses, adjustments, transfers, stocktake).
/// </summary>
public static class InventoryErrorCodes
{
    /// <summary>Product not found.</summary>
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";

    /// <summary>Product already active.</summary>
    public const string ProductAlreadyActive = "PRODUCT_ALREADY_ACTIVE";

    /// <summary>Duplicate product code.</summary>
    public const string DuplicateProductCode = "DUPLICATE_PRODUCT_CODE";

    /// <summary>Invalid product reference.</summary>
    public const string InvalidProduct = "INVALID_PRODUCT";

    /// <summary>Invalid category reference.</summary>
    public const string InvalidCategory = "INVALID_CATEGORY";

    /// <summary>Invalid unit of measure reference.</summary>
    public const string InvalidUnitOfMeasure = "INVALID_UNIT_OF_MEASURE";

    /// <summary>Warehouse not found.</summary>
    public const string WarehouseNotFound = "WAREHOUSE_NOT_FOUND";

    /// <summary>Warehouse already active.</summary>
    public const string WarehouseAlreadyActive = "WAREHOUSE_ALREADY_ACTIVE";

    /// <summary>Duplicate warehouse code.</summary>
    public const string DuplicateWarehouseCode = "DUPLICATE_WAREHOUSE_CODE";

    /// <summary>Invalid warehouse reference.</summary>
    public const string InvalidWarehouse = "INVALID_WAREHOUSE";

    /// <summary>Zone not found.</summary>
    public const string ZoneNotFound = "ZONE_NOT_FOUND";

    /// <summary>Duplicate zone code.</summary>
    public const string DuplicateZoneCode = "DUPLICATE_ZONE_CODE";

    /// <summary>Invalid zone reference.</summary>
    public const string InvalidZone = "INVALID_ZONE";

    /// <summary>Zone has storage locations.</summary>
    public const string ZoneHasLocations = "ZONE_HAS_LOCATIONS";

    /// <summary>Storage location not found.</summary>
    public const string LocationNotFound = "LOCATION_NOT_FOUND";

    /// <summary>Duplicate location code.</summary>
    public const string DuplicateLocationCode = "DUPLICATE_LOCATION_CODE";

    /// <summary>Location has stock and cannot be deleted.</summary>
    public const string LocationHasStock = "LOCATION_HAS_STOCK";

    /// <summary>Stock level not found.</summary>
    public const string StockLevelNotFound = "STOCK_LEVEL_NOT_FOUND";

    /// <summary>Batch not found.</summary>
    public const string BatchNotFound = "BATCH_NOT_FOUND";

    /// <summary>Duplicate batch number.</summary>
    public const string DuplicateBatchNumber = "DUPLICATE_BATCH_NUMBER";

    /// <summary>Duplicate product category name.</summary>
    public const string DuplicateCategoryName = "DUPLICATE_CATEGORY_NAME";

    /// <summary>Duplicate unit code.</summary>
    public const string DuplicateUnitCode = "DUPLICATE_UNIT_CODE";

    /// <summary>Unit of measure is in use.</summary>
    public const string UnitInUse = "UNIT_IN_USE";

    /// <summary>Adjustment not found.</summary>
    public const string AdjustmentNotFound = "ADJUSTMENT_NOT_FOUND";

    /// <summary>Adjustment is not in Pending status.</summary>
    public const string AdjustmentNotPending = "ADJUSTMENT_NOT_PENDING";

    /// <summary>Adjustment is not in Approved status.</summary>
    public const string AdjustmentNotApproved = "ADJUSTMENT_NOT_APPROVED";

    /// <summary>Transfer not found.</summary>
    public const string TransferNotFound = "TRANSFER_NOT_FOUND";

    /// <summary>Transfer is not in Draft status.</summary>
    public const string TransferNotDraft = "TRANSFER_NOT_DRAFT";

    /// <summary>Transfer source and destination are the same warehouse.</summary>
    public const string TransferSameWarehouse = "TRANSFER_SAME_WAREHOUSE";

    /// <summary>Session not found.</summary>
    public const string SessionNotFound = "SESSION_NOT_FOUND";

    /// <summary>Session is not in Draft status.</summary>
    public const string SessionNotDraft = "SESSION_NOT_DRAFT";

    /// <summary>Session is not in InProgress status.</summary>
    public const string SessionNotInProgress = "SESSION_NOT_IN_PROGRESS";

    /// <summary>Session is not in Completed status.</summary>
    public const string SessionNotCompleted = "SESSION_NOT_COMPLETED";

    /// <summary>Session cannot be cancelled in its current status.</summary>
    public const string SessionCannotCancel = "SESSION_CANNOT_CANCEL";

    /// <summary>Count entry not found.</summary>
    public const string CountEntryNotFound = "COUNT_ENTRY_NOT_FOUND";

    /// <summary>Duplicate count entry.</summary>
    public const string DuplicateCountEntry = "DUPLICATE_COUNT_ENTRY";

    /// <summary>BOM not found.</summary>
    public const string BomNotFound = "BOM_NOT_FOUND";

    /// <summary>BOM already exists for this product.</summary>
    public const string BomAlreadyExists = "BOM_ALREADY_EXISTS";

    /// <summary>BOM references itself.</summary>
    public const string BomSelfReference = "BOM_SELF_REFERENCE";

    /// <summary>BOM line not found.</summary>
    public const string BomLineNotFound = "BOM_LINE_NOT_FOUND";

    /// <summary>Duplicate BOM line.</summary>
    public const string DuplicateBomLine = "DUPLICATE_BOM_LINE";

    /// <summary>Duplicate accessory reference.</summary>
    public const string DuplicateAccessory = "DUPLICATE_ACCESSORY";

    /// <summary>Accessory not found.</summary>
    public const string AccessoryNotFound = "ACCESSORY_NOT_FOUND";

    /// <summary>Duplicate substitute reference.</summary>
    public const string DuplicateSubstitute = "DUPLICATE_SUBSTITUTE";

    /// <summary>Substitute not found.</summary>
    public const string SubstituteNotFound = "SUBSTITUTE_NOT_FOUND";
}
