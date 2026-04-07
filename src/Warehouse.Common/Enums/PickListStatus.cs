namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the status values for the pick list lifecycle.
/// <para>Conforms to ISA-95 Part 3 -- Fulfillment Operations Activity Model (Material Movement).</para>
/// </summary>
public enum PickListStatus
{
    /// <summary>
    /// Pick list has been generated but picking has not started.
    /// </summary>
    Pending,

    /// <summary>
    /// All lines have been picked (confirmed).
    /// </summary>
    Completed,

    /// <summary>
    /// Pick list has been cancelled and reservations released.
    /// </summary>
    Cancelled
}
