namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for updating an existing batch.
/// </summary>
public sealed record UpdateBatchRequest
{
    /// <summary>
    /// Gets the optional expiry date.
    /// </summary>
    public DateOnly? ExpiryDate { get; init; }

    /// <summary>
    /// Gets the optional notes. Max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }
}
