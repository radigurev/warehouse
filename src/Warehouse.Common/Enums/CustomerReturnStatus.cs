namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the status values for the customer return (RMA) lifecycle.
/// <para>Conforms to ISA-95 Part 3 -- Material Receipt (return) activity.</para>
/// </summary>
public enum CustomerReturnStatus
{
    /// <summary>
    /// Return is being drafted and can be edited. Initial status.
    /// </summary>
    Draft,

    /// <summary>
    /// Return has been confirmed and is awaiting physical receipt.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Return goods have been physically received at the warehouse.
    /// </summary>
    Received,

    /// <summary>
    /// Return has been administratively closed.
    /// </summary>
    Closed,

    /// <summary>
    /// Return has been cancelled. No further actions allowed.
    /// </summary>
    Cancelled
}
