namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Warehouse transfer line representation.
/// </summary>
public sealed record WarehouseTransferLineDto
{
    /// <summary>
    /// Gets the transfer line ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the parent transfer ID.
    /// </summary>
    public required int TransferId { get; init; }

    /// <summary>
    /// Gets the product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public required string ProductName { get; init; }

    /// <summary>
    /// Gets the transfer quantity.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the optional source location ID.
    /// </summary>
    public int? SourceLocationId { get; init; }

    /// <summary>
    /// Gets the optional destination location ID.
    /// </summary>
    public int? DestinationLocationId { get; init; }
}
