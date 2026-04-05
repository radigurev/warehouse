namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for approving an inventory adjustment.
/// </summary>
public sealed record ApproveAdjustmentRequest
{
    /// <summary>
    /// Gets the optional approval notes. Max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }
}
