namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for creating a new product category.
/// </summary>
public sealed record CreateProductCategoryRequest
{
    /// <summary>
    /// Gets the category name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description. Max 500 characters.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the optional parent category ID for hierarchy.
    /// </summary>
    public int? ParentCategoryId { get; init; }
}
