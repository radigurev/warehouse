using Warehouse.Common.Models;

namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Query payload for listing product prices with optional filters.
/// <para>Conforms to CHG-FEAT-007 §2.2 (GET /api/v1/product-prices).</para>
/// </summary>
public sealed record SearchProductPricesRequest
{
    /// <summary>Gets the optional product ID filter.</summary>
    public int? ProductId { get; init; }

    /// <summary>Gets the optional currency code filter (exact match).</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>
    /// Gets the optional "active on date" filter. When supplied, returns only rows
    /// whose validity window contains this UTC date.
    /// </summary>
    public DateTime? ActiveOnDate { get; init; }

    /// <summary>Gets the 1-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Gets the page size.</summary>
    public int PageSize { get; init; } = PaginationParams.DefaultPageSize;

    /// <summary>Gets the optional sort key.</summary>
    public string? SortBy { get; init; }

    /// <summary>Gets whether sort should be descending.</summary>
    public bool SortDescending { get; init; }
}
