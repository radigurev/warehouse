namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the status values for the purchase order lifecycle state machine.
/// <para>Conforms to ISA-95 Part 3 -- Procurement Operations Activity Model.</para>
/// </summary>
public enum PurchaseOrderStatus
{
    /// <summary>
    /// PO is being drafted and can be edited. Initial status.
    /// </summary>
    Draft,

    /// <summary>
    /// PO has been confirmed and is awaiting goods receipt.
    /// </summary>
    Confirmed,

    /// <summary>
    /// At least one line has received goods but not all lines are fully received.
    /// </summary>
    PartiallyReceived,

    /// <summary>
    /// All lines have been fully received.
    /// </summary>
    Received,

    /// <summary>
    /// PO has been administratively closed.
    /// </summary>
    Closed,

    /// <summary>
    /// PO has been cancelled. No further actions allowed.
    /// </summary>
    Cancelled
}
