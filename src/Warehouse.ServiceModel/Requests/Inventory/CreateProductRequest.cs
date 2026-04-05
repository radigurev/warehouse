namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for creating a new product.
/// </summary>
public sealed record CreateProductRequest
{
    /// <summary>
    /// Gets the product code. Required, 1-50 characters, alphanumeric and hyphens/underscores.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the product name. Required, 1-200 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional product description. Max 2000 characters.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the optional SKU. Max 100 characters.
    /// </summary>
    public string? Sku { get; init; }

    /// <summary>
    /// Gets the optional barcode. Max 100 characters.
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// Gets the optional product category ID.
    /// </summary>
    public int? CategoryId { get; init; }

    /// <summary>
    /// Gets the unit of measure ID. Required.
    /// </summary>
    public required int UnitOfMeasureId { get; init; }

    /// <summary>
    /// Gets the optional notes. Max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }
}
