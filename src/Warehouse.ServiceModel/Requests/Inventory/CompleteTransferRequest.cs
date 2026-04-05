namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for completing a warehouse transfer.
/// </summary>
public sealed record CompleteTransferRequest
{
    /// <summary>
    /// Gets the optional notes. Max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }
}
