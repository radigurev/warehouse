namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for searching and filtering sales orders with pagination.
/// </summary>
public sealed record SearchSalesOrdersRequest
{
    /// <summary>Gets the customer ID filter. Optional.</summary>
    public int? CustomerId { get; init; }

    /// <summary>Gets the status filter. Optional.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the SO number filter (exact or starts-with). Optional.</summary>
    public string? OrderNumber { get; init; }

    /// <summary>Gets the warehouse ID filter. Optional.</summary>
    public int? WarehouseId { get; init; }

    /// <summary>Gets the date range start filter (created date). Optional.</summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>Gets the date range end filter (created date). Optional.</summary>
    public DateTime? DateTo { get; init; }

    /// <summary>Gets the field name to sort by. Supports: orderNumber, createdAtUtc, requestedShipDate. Optional.</summary>
    public string? SortBy { get; init; }

    /// <summary>Gets whether to sort in descending order. Defaults to true (newest first).</summary>
    public bool SortDescending { get; init; } = true;

    /// <summary>Gets the page number (1-based). Defaults to 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Gets the number of items per page. Defaults to 25.</summary>
    public int PageSize { get; init; } = 25;
}
