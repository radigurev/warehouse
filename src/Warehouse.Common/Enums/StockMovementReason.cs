namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the reason codes for stock movements.
/// <para>Extends ISA-95 base movement types (Receipt, Shipment, Transfer, Adjustment,
/// Count Adjustment, Production Consumption, Production Output) with finer-grained
/// codes for business reporting and audit. See CLAUDE.md section 1.1.6.1 for the
/// full ISA-95 mapping table.</para>
/// </summary>
public enum StockMovementReason
{
    /// <summary>
    /// Stock received from a purchase order. ISA-95 base type: Receipt.
    /// </summary>
    PurchaseReceipt,

    /// <summary>
    /// Stock dispatched for a sales order. ISA-95 base type: Shipment.
    /// </summary>
    SalesDispatch,

    /// <summary>
    /// Manual inventory adjustment. ISA-95 base type: Adjustment.
    /// </summary>
    Adjustment,

    /// <summary>
    /// Inter-warehouse transfer. ISA-95 base type: Transfer.
    /// </summary>
    Transfer,

    /// <summary>
    /// Stock returned by a customer. ISA-95 base type: Receipt.
    /// </summary>
    CustomerReturn,

    /// <summary>
    /// Stock returned to a supplier. ISA-95 base type: Shipment.
    /// </summary>
    SupplierReturn,

    /// <summary>
    /// Stock consumed in production. ISA-95 base type: Production Consumption.
    /// </summary>
    ProductionConsumption,

    /// <summary>
    /// Finished goods received from production. ISA-95 base type: Production Output.
    /// </summary>
    ProductionReceipt,

    /// <summary>
    /// Stock written off due to damage or loss. ISA-95 base type: Adjustment.
    /// </summary>
    WriteOff,

    /// <summary>
    /// Correction from stocktake variance. ISA-95 base type: Count Adjustment.
    /// </summary>
    StocktakeCorrection,

    /// <summary>
    /// Other reason not covered by standard codes. Escape hatch; SHOULD be avoided.
    /// </summary>
    Other
}
