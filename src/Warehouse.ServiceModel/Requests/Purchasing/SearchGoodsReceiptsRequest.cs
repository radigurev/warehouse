namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for searching and filtering goods receipts with pagination.
/// </summary>
public sealed record SearchGoodsReceiptsRequest
{
    /// <summary>
    /// Gets the purchase order ID filter. Optional.
    /// </summary>
    public int? PurchaseOrderId { get; init; }

    /// <summary>
    /// Gets the PO number filter. Optional.
    /// </summary>
    public string? PurchaseOrderNumber { get; init; }

    /// <summary>
    /// Gets the receipt number filter. Optional.
    /// </summary>
    public string? ReceiptNumber { get; init; }

    /// <summary>
    /// Gets the warehouse ID filter. Optional.
    /// </summary>
    public int? WarehouseId { get; init; }

    /// <summary>
    /// Gets the date range start filter. Optional.
    /// </summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>
    /// Gets the date range end filter. Optional.
    /// </summary>
    public DateTime? DateTo { get; init; }

    /// <summary>
    /// Gets whether to sort in descending order. Defaults to true (newest first).
    /// </summary>
    public bool SortDescending { get; init; } = true;

    /// <summary>
    /// Gets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the number of items per page. Defaults to 25.
    /// </summary>
    public int PageSize { get; init; } = 25;
}
