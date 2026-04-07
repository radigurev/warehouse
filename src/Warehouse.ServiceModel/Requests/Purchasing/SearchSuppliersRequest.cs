namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for searching and filtering suppliers with pagination.
/// </summary>
public sealed record SearchSuppliersRequest
{
    /// <summary>
    /// Gets the name filter (contains match). Optional.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the code filter (exact or starts-with match). Optional.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets the tax ID filter (exact match). Optional.
    /// </summary>
    public string? TaxId { get; init; }

    /// <summary>
    /// Gets the category ID filter. Optional.
    /// </summary>
    public int? CategoryId { get; init; }

    /// <summary>
    /// Gets whether to include soft-deleted suppliers in results. Defaults to false.
    /// </summary>
    public bool IncludeDeleted { get; init; }

    /// <summary>
    /// Gets the field name to sort by. Supports: name, code, createdAtUtc. Optional.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Gets whether to sort in descending order. Defaults to false (ascending).
    /// </summary>
    public bool SortDescending { get; init; }

    /// <summary>
    /// Gets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the number of items per page. Defaults to 25.
    /// </summary>
    public int PageSize { get; init; } = 25;

    /// <summary>
    /// Gets the generic filter expression string. Optional.
    /// </summary>
    public string? Filter { get; init; }
}
