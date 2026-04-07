namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for searching and filtering carriers with pagination.
/// </summary>
public sealed record SearchCarriersRequest
{
    /// <summary>Gets the name filter (contains). Optional.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the code filter (exact or starts-with). Optional.</summary>
    public string? Code { get; init; }

    /// <summary>Gets the active status filter. Optional.</summary>
    public bool? IsActive { get; init; }

    /// <summary>Gets the page number (1-based). Defaults to 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Gets the number of items per page. Defaults to 25.</summary>
    public int PageSize { get; init; } = 25;
}
