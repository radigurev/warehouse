namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Represents a supplier category for classification purposes.
/// </summary>
public sealed record SupplierCategoryDto
{
    /// <summary>
    /// Gets the category ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the category description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
