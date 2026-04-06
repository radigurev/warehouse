namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for searching batches with pagination.
/// </summary>
public sealed record SearchBatchesRequest
{
    /// <summary>
    /// Gets the product ID filter. Optional.
    /// </summary>
    public int? ProductId { get; init; }

    /// <summary>
    /// Gets the batch number filter (contains match). Optional.
    /// </summary>
    public string? BatchNumber { get; init; }

    /// <summary>
    /// Gets whether to include expired batches. Defaults to false.
    /// </summary>
    public bool IncludeExpired { get; init; }

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
