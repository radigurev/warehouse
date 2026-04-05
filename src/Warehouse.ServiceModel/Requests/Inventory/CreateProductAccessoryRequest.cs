namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for linking an accessory product to a product.
/// </summary>
public sealed record CreateProductAccessoryRequest
{
    /// <summary>
    /// Gets the source product ID. Required.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the accessory product ID. Required.
    /// </summary>
    public required int AccessoryProductId { get; init; }
}
