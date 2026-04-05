namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for a single transfer line.
/// </summary>
public sealed record CreateTransferLineRequest
{
    /// <summary>
    /// Gets the product ID. Required.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the transfer quantity. Must be greater than zero.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the optional source location ID.
    /// </summary>
    public int? SourceLocationId { get; init; }

    /// <summary>
    /// Gets the optional destination location ID.
    /// </summary>
    public int? DestinationLocationId { get; init; }
}
