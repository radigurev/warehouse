namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Product accessory link representation.
/// </summary>
public sealed record ProductAccessoryDto
{
    /// <summary>
    /// Gets the accessory link ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the source product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the accessory product ID.
    /// </summary>
    public required int AccessoryProductId { get; init; }

    /// <summary>
    /// Gets the accessory product name.
    /// </summary>
    public required string AccessoryProductName { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
