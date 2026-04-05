namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for searching warehouse transfers with pagination.
/// </summary>
public sealed record SearchTransfersRequest
{
    /// <summary>
    /// Gets the source warehouse ID filter. Optional.
    /// </summary>
    public int? SourceWarehouseId { get; init; }

    /// <summary>
    /// Gets the destination warehouse ID filter. Optional.
    /// </summary>
    public int? DestinationWarehouseId { get; init; }

    /// <summary>
    /// Gets the status filter. Optional.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets the start date filter (inclusive). Optional.
    /// </summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>
    /// Gets the end date filter (inclusive). Optional.
    /// </summary>
    public DateTime? DateTo { get; init; }

    /// <summary>
    /// Gets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the number of items per page. Defaults to 20.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Gets the generic filter expression string. Optional.
    /// </summary>
    public string? Filter { get; init; }
}
