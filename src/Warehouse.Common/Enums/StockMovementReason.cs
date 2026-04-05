namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the reason codes for stock movements.
/// </summary>
public enum StockMovementReason
{
    /// <summary>
    /// Stock received from a purchase order.
    /// </summary>
    PurchaseReceipt,

    /// <summary>
    /// Stock dispatched for a sales order.
    /// </summary>
    SalesDispatch,

    /// <summary>
    /// Manual inventory adjustment.
    /// </summary>
    Adjustment,

    /// <summary>
    /// Inter-warehouse transfer.
    /// </summary>
    Transfer,

    /// <summary>
    /// Stock returned by a customer.
    /// </summary>
    CustomerReturn,

    /// <summary>
    /// Stock returned to a supplier.
    /// </summary>
    SupplierReturn,

    /// <summary>
    /// Stock consumed in production.
    /// </summary>
    ProductionConsumption,

    /// <summary>
    /// Finished goods received from production.
    /// </summary>
    ProductionReceipt,

    /// <summary>
    /// Stock written off due to damage or loss.
    /// </summary>
    WriteOff,

    /// <summary>
    /// Correction from stocktake variance.
    /// </summary>
    StocktakeCorrection,

    /// <summary>
    /// Other reason not covered by standard codes.
    /// </summary>
    Other
}
