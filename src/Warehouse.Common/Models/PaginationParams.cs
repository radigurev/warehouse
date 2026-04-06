namespace Warehouse.Common.Models;

/// <summary>
/// Shared pagination parameters for list endpoints.
/// </summary>
public sealed record PaginationParams
{
    /// <summary>
    /// Default page size used across all paginated endpoints.
    /// </summary>
    public const int DefaultPageSize = 25;

    /// <summary>
    /// Maximum allowed page size.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Gets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the number of items per page. Defaults to 25, clamped to 100.
    /// </summary>
    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>
    /// Gets the effective page size, clamped between 1 and the maximum.
    /// </summary>
    public int EffectivePageSize => Math.Clamp(PageSize, 1, MaxPageSize);

    /// <summary>
    /// Gets the number of items to skip based on page and page size.
    /// </summary>
    public int Skip => (Math.Max(Page, 1) - 1) * EffectivePageSize;
}
