namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for searching and filtering pick lists with pagination.
/// </summary>
public sealed record SearchPickListsRequest
{
    /// <summary>Gets the sales order ID filter. Optional.</summary>
    public int? SalesOrderId { get; init; }

    /// <summary>Gets the SO number filter (starts-with). Optional.</summary>
    public string? SalesOrderNumber { get; init; }

    /// <summary>Gets the status filter. Optional.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the warehouse ID filter. Optional.</summary>
    public int? WarehouseId { get; init; }

    /// <summary>Gets the date range start filter. Optional.</summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>Gets the date range end filter. Optional.</summary>
    public DateTime? DateTo { get; init; }

    /// <summary>Gets whether to sort in descending order. Defaults to true.</summary>
    public bool SortDescending { get; init; } = true;

    /// <summary>Gets the page number (1-based). Defaults to 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Gets the number of items per page. Defaults to 25.</summary>
    public int PageSize { get; init; } = 25;
}
