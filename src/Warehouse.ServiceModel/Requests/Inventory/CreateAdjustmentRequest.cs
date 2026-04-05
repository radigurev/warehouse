namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for creating an inventory adjustment with lines.
/// </summary>
public sealed record CreateAdjustmentRequest
{
    /// <summary>
    /// Gets the warehouse ID. Required.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the adjustment reason. Required, 1-200 characters.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the optional notes. Max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the adjustment lines. At least one line is required.
    /// </summary>
    public required IReadOnlyList<CreateAdjustmentLineRequest> Lines { get; init; }
}
