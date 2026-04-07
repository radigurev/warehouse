namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when a pick list is generated to request stock reservation.
/// Event naming: Fulfillment.StockReservation.Requested.
/// </summary>
public sealed record StockReservationRequestedEvent
{
    /// <summary>
    /// Gets the pick list ID.
    /// </summary>
    public required int PickListId { get; init; }

    /// <summary>
    /// Gets the pick list number.
    /// </summary>
    public required string PickListNumber { get; init; }

    /// <summary>
    /// Gets the sales order ID.
    /// </summary>
    public required int SalesOrderId { get; init; }

    /// <summary>
    /// Gets the sales order number.
    /// </summary>
    public required string SalesOrderNumber { get; init; }

    /// <summary>
    /// Gets the user who requested the reservation.
    /// </summary>
    public required int RequestedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the reservation was requested.
    /// </summary>
    public required DateTime RequestedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of reservation lines.
    /// </summary>
    public required IReadOnlyList<StockReservationLine> Lines { get; init; }
}
