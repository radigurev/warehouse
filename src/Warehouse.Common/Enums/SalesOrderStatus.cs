namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the status values for the sales order lifecycle state machine.
/// <para>Conforms to ISA-95 Part 3 -- Fulfillment Operations Activity Model.</para>
/// </summary>
public enum SalesOrderStatus
{
    /// <summary>
    /// SO is being drafted and can be edited. Initial status.
    /// </summary>
    Draft,

    /// <summary>
    /// SO has been confirmed and is ready for picking.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Pick list(s) have been generated; picking is in progress.
    /// </summary>
    Picking,

    /// <summary>
    /// All lines are fully packed into parcels.
    /// </summary>
    Packed,

    /// <summary>
    /// Shipment has been dispatched.
    /// </summary>
    Shipped,

    /// <summary>
    /// SO has been administratively completed after delivery confirmation.
    /// </summary>
    Completed,

    /// <summary>
    /// SO has been cancelled. No further actions allowed.
    /// </summary>
    Cancelled
}
