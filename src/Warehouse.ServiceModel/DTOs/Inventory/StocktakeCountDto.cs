namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Stocktake count entry representation.
/// </summary>
public sealed record StocktakeCountDto
{
    /// <summary>
    /// Gets the count entry ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the parent session ID.
    /// </summary>
    public required int SessionId { get; init; }

    /// <summary>
    /// Gets the product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public required string ProductName { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the optional location name.
    /// </summary>
    public string? LocationName { get; init; }

    /// <summary>
    /// Gets the expected quantity from system records.
    /// </summary>
    public required decimal ExpectedQuantity { get; init; }

    /// <summary>
    /// Gets the actual counted quantity.
    /// </summary>
    public required decimal ActualQuantity { get; init; }

    /// <summary>
    /// Gets the variance (actual minus expected).
    /// </summary>
    public required decimal Variance { get; init; }
}
