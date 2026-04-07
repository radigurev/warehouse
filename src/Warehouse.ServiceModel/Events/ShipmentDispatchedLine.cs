namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Represents a single line in a shipment dispatched event.
/// </summary>
public sealed record ShipmentDispatchedLine
{
    /// <summary>
    /// Gets the shipment line ID.
    /// </summary>
    public required int ShipmentLineId { get; init; }

    /// <summary>
    /// Gets the product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the shipped quantity.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the optional batch ID.
    /// </summary>
    public int? BatchId { get; init; }
}
