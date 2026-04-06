namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the reason codes for stock movements per ISA-95 terminology.
/// <para>Maps to ISA-95 base movement types: Receipt, Shipment, Transfer, Adjustment,
/// Count Adjustment, Production Consumption, Production Output.</para>
/// </summary>
public enum StockMovementReason
{
    /// <summary>
    /// Stock received (inbound). ISA-95 base type: Receipt.
    /// </summary>
    Receipt,

    /// <summary>
    /// Stock shipped (outbound). ISA-95 base type: Shipment.
    /// </summary>
    Shipment,

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
    /// Finished goods output from production. ISA-95 base type: Production Output.
    /// </summary>
    ProductionOutput,

    /// <summary>
    /// Stock written off due to damage or loss. ISA-95 base type: Adjustment.
    /// </summary>
    WriteOff,

    /// <summary>
    /// Correction from stocktake variance. ISA-95 base type: Count Adjustment.
    /// </summary>
    CountAdjustment,

    /// <summary>
    /// Other reason not covered by standard codes. Escape hatch; SHOULD be avoided.
    /// </summary>
    Other
}
