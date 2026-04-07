namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Represents a single line in a stock reservation request.
/// </summary>
public sealed record StockReservationLine
{
    /// <summary>
    /// Gets the pick list line ID.
    /// </summary>
    public required int PickListLineId { get; init; }

    /// <summary>
    /// Gets the product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the warehouse ID.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the quantity to reserve.
    /// </summary>
    public required decimal Quantity { get; init; }
}
