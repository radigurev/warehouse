namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Full product representation including category, unit of measure, and optional extended fields.
/// </summary>
public sealed record ProductDetailDto
{
    /// <summary>
    /// Gets the product ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the unique product code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional product description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the optional SKU.
    /// </summary>
    public string? Sku { get; init; }

    /// <summary>
    /// Gets the optional barcode.
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// Gets the assigned category ID.
    /// </summary>
    public int? CategoryId { get; init; }

    /// <summary>
    /// Gets the name of the assigned category.
    /// </summary>
    public string? CategoryName { get; init; }

    /// <summary>
    /// Gets the unit of measure ID.
    /// </summary>
    public required int UnitOfMeasureId { get; init; }

    /// <summary>
    /// Gets the name of the unit of measure.
    /// </summary>
    public required string UnitOfMeasureName { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets whether this product requires batch/lot tracking.
    /// </summary>
    public required bool RequiresBatchTracking { get; init; }

    /// <summary>
    /// Gets whether the product is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
