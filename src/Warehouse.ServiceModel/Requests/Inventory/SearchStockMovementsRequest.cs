namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for searching stock movements with pagination.
/// </summary>
public sealed record SearchStockMovementsRequest
{
    /// <summary>
    /// Gets the product ID filter. Optional.
    /// </summary>
    public int? ProductId { get; init; }

    /// <summary>
    /// Gets the warehouse ID filter. Optional.
    /// </summary>
    public int? WarehouseId { get; init; }

    /// <summary>
    /// Gets the reason code filter. Optional.
    /// </summary>
    public string? ReasonCode { get; init; }

    /// <summary>
    /// Gets the start date filter (inclusive). Optional.
    /// </summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>
    /// Gets the end date filter (inclusive). Optional.
    /// </summary>
    public DateTime? DateTo { get; init; }

    /// <summary>
    /// Gets the field name to sort by. Supports: createdAtUtc, quantity. Optional.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Gets whether to sort in descending order. Defaults to false.
    /// </summary>
    public bool SortDescending { get; init; }

    /// <summary>
    /// Gets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the number of items per page. Defaults to 20.
    /// </summary>
    public int PageSize { get; init; } = 25;

    /// <summary>
    /// Gets the generic filter expression string. Optional.
    /// </summary>
    public string? Filter { get; init; }
}
