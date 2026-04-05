namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for creating a new batch for a product.
/// </summary>
public sealed record CreateBatchRequest
{
    /// <summary>
    /// Gets the product ID. Required.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the batch number. Required, 1-50 characters.
    /// </summary>
    public required string BatchNumber { get; init; }

    /// <summary>
    /// Gets the optional expiry date.
    /// </summary>
    public DateOnly? ExpiryDate { get; init; }

    /// <summary>
    /// Gets the optional notes. Max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }
}
