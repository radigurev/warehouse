namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for a purchase order line (used in create PO and add line).
/// </summary>
public sealed record CreatePurchaseOrderLineRequest
{
    /// <summary>
    /// Gets the product ID. Required.
    /// </summary>
    public required int ProductId { get; init; }

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
