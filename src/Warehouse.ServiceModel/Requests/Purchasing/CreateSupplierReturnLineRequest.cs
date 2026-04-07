namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for a supplier return line.
/// </summary>
public sealed record CreateSupplierReturnLineRequest
{
    /// <summary>
    /// Gets the product ID. Required.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the warehouse ID. Required.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the return quantity. Required, must be greater than 0.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the optional batch ID.
    /// </summary>
    public int? BatchId { get; init; }

    /// <summary>
    /// Gets the optional goods receipt line ID.
    /// </summary>
    public int? GoodsReceiptLineId { get; init; }

    /// <summary>
    /// Gets the line notes. Optional, max 500 characters.
    /// </summary>
    public string? Notes { get; init; }
}
