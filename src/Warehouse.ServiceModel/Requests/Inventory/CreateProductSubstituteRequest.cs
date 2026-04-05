namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for linking a substitute product to a product.
/// </summary>
public sealed record CreateProductSubstituteRequest
{
    /// <summary>
    /// Gets the source product ID. Required.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the substitute product ID. Required.
    /// </summary>
    public required int SubstituteProductId { get; init; }
}
