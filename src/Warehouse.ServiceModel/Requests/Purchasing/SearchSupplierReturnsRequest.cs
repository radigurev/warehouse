namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for searching and filtering supplier returns with pagination.
/// </summary>
public sealed record SearchSupplierReturnsRequest
{
    /// <summary>
    /// Gets the supplier ID filter. Optional.
    /// </summary>
    public int? SupplierId { get; init; }

    /// <summary>
    /// Gets the status filter. Optional.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets the return number filter. Optional.
    /// </summary>
    public string? ReturnNumber { get; init; }

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
