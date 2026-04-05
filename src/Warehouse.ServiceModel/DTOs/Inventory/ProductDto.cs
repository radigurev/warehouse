namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Lightweight product representation for list views.
/// </summary>
public sealed record ProductDto
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
    /// Gets the name of the assigned product category.
    /// </summary>
    public string? CategoryName { get; init; }

    /// <summary>
    /// Gets the name of the unit of measure.
    /// </summary>
    public required string UnitOfMeasureName { get; init; }

    /// <summary>
    /// Gets whether the product is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
