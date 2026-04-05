namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for updating an existing product category.
/// </summary>
public sealed record UpdateProductCategoryRequest
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
