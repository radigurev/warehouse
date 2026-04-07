namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for creating a new goods receipt against a purchase order.
/// </summary>
public sealed record CreateGoodsReceiptRequest
{
    /// <summary>
    /// Gets the purchase order ID. Required.
    /// </summary>
    public required int PurchaseOrderId { get; init; }

    /// <summary>
    /// Gets the receiving warehouse ID. Required.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the optional receiving location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the receipt notes. Optional, max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the collection of receipt lines. At least one required.
    /// </summary>
    public required IReadOnlyList<CreateGoodsReceiptLineRequest> Lines { get; init; }
}
