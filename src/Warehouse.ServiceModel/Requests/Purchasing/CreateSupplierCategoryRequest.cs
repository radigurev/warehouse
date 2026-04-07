namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for creating a new supplier category.
/// </summary>
public sealed record CreateSupplierCategoryRequest
{
    /// <summary>
    /// Gets the category name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the category description. Optional, max 500 characters.
    /// </summary>
    public string? Description { get; init; }
}
