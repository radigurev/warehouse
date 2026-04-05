namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Product category representation with optional parent information.
/// </summary>
public sealed record ProductCategoryDto
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
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the optional parent category ID.
    /// </summary>
    public int? ParentCategoryId { get; init; }

    /// <summary>
    /// Gets the optional parent category name.
    /// </summary>
    public string? ParentCategoryName { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
