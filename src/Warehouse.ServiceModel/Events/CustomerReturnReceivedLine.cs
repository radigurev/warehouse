namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Represents a single line in a customer return received event.
/// </summary>
public sealed record CustomerReturnReceivedLine
{
    /// <summary>
    /// Gets the customer return line ID.
    /// </summary>
    public required int CustomerReturnLineId { get; init; }

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
    /// Gets the returned quantity.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the optional batch ID.
    /// </summary>
    public int? BatchId { get; init; }
}
