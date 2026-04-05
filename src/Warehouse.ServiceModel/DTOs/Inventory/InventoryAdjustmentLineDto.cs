namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Inventory adjustment line representation.
/// </summary>
public sealed record InventoryAdjustmentLineDto
{
    /// <summary>
    /// Gets the adjustment line ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the parent adjustment ID.
    /// </summary>
    public required int AdjustmentId { get; init; }

    /// <summary>
    /// Gets the product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public required string ProductName { get; init; }

    /// <summary>
    /// Gets the warehouse ID.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the optional location name.
    /// </summary>
    public string? LocationName { get; init; }

    /// <summary>
    /// Gets the expected quantity from system records.
    /// </summary>
    public required decimal ExpectedQuantity { get; init; }

    /// <summary>
    /// Gets the actual quantity from physical count.
    /// </summary>
    public required decimal ActualQuantity { get; init; }

    /// <summary>
    /// Gets the variance (actual minus expected).
    /// </summary>
    public decimal Variance => ActualQuantity - ExpectedQuantity;
}
