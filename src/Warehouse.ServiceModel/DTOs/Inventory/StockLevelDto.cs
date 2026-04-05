namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Stock level representation for a product at a warehouse and location.
/// </summary>
public sealed record StockLevelDto
{
    /// <summary>
    /// Gets the stock level ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public required string ProductName { get; init; }

    /// <summary>
    /// Gets the product code.
    /// </summary>
    public required string ProductCode { get; init; }

    /// <summary>
    /// Gets the warehouse ID.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the warehouse name.
    /// </summary>
    public required string WarehouseName { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the optional location name.
    /// </summary>
    public string? LocationName { get; init; }

    /// <summary>
    /// Gets the quantity on hand.
    /// </summary>
    public required decimal QuantityOnHand { get; init; }

    /// <summary>
    /// Gets the reserved quantity.
    /// </summary>
    public required decimal ReservedQuantity { get; init; }

    /// <summary>
    /// Gets the available quantity (on hand minus reserved).
    /// </summary>
    public required decimal AvailableQuantity { get; init; }
}
