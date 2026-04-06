namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the reference source types for stock movements per ISA-95 traceability requirements.
/// </summary>
public enum StockMovementReferenceType
{
    /// <summary>
    /// Manual movement with no specific source document.
    /// </summary>
    Manual,

    /// <summary>
    /// Movement from a purchase order receipt.
    /// </summary>
    PurchaseOrder,

    /// <summary>
    /// Movement from a sales order fulfillment.
    /// </summary>
    SalesOrder,

    /// <summary>
    /// Movement from an inventory adjustment.
    /// </summary>
    InventoryAdjustment,

    /// <summary>
    /// Movement from a warehouse transfer.
    /// </summary>
    WarehouseTransfer,

    /// <summary>
    /// Movement from a stocktake session.
    /// </summary>
    StocktakeSession,

    /// <summary>
    /// Movement from a production order.
    /// </summary>
    ProductionOrder,

    /// <summary>
    /// Movement from a quality inspection.
    /// </summary>
    QualityInspection
}
