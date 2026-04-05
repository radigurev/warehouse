namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for creating a warehouse transfer with lines.
/// </summary>
public sealed record CreateTransferRequest
{
    /// <summary>
    /// Gets the source warehouse ID. Required.
    /// </summary>
    public required int SourceWarehouseId { get; init; }

    /// <summary>
    /// Gets the destination warehouse ID. Required.
    /// </summary>
    public required int DestinationWarehouseId { get; init; }

    /// <summary>
    /// Gets the optional notes. Max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the transfer lines. At least one line is required.
    /// </summary>
    public required IReadOnlyList<CreateTransferLineRequest> Lines { get; init; }
}
