namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when a pick list is cancelled or a short pick releases excess reservation.
/// Event naming: Fulfillment.StockReservation.Released.
/// </summary>
public sealed record StockReservationReleasedEvent
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
    /// Gets the user who released the reservation.
    /// </summary>
    public required int ReleasedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the reservation was released.
    /// </summary>
    public required DateTime ReleasedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of released reservation lines.
    /// </summary>
    public required IReadOnlyList<StockReservationLine> Lines { get; init; }
}
