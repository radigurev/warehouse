namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for searching and filtering warehouses with pagination.
/// </summary>
public sealed record SearchWarehousesRequest
{
    /// <summary>
    /// Gets the name filter (contains match). Optional.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the code filter (starts-with match). Optional.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets whether to include soft-deleted warehouses. Defaults to false.
    /// </summary>
    public bool IncludeDeleted { get; init; }

    /// <summary>
    /// Gets the field name to sort by. Supports: name, code, createdAtUtc. Optional.
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
