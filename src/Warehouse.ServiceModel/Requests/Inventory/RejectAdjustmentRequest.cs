namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for rejecting an inventory adjustment.
/// </summary>
public sealed record RejectAdjustmentRequest
{
    /// <summary>
    /// Gets the rejection reason. Required, 1-500 characters.
    /// </summary>
    public required string Notes { get; init; }
}
