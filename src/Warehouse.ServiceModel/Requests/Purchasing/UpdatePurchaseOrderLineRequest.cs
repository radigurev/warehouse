namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for updating a purchase order line.
/// </summary>
public sealed record UpdatePurchaseOrderLineRequest
{
    /// <summary>
    /// Gets the ordered quantity. Required, must be greater than 0.
    /// </summary>
    public required decimal OrderedQuantity { get; init; }

    /// <summary>
    /// Gets the unit price. Required, must be greater than or equal to 0.
    /// </summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>
    /// Gets the line notes. Optional, max 500 characters.
    /// </summary>
    public string? Notes { get; init; }
}
