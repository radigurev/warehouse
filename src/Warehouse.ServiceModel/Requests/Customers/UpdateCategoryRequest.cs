namespace Warehouse.ServiceModel.Requests.Customers;

/// <summary>
/// Request payload for updating an existing customer category.
/// </summary>
public sealed record UpdateCategoryRequest
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
