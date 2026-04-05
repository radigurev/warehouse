namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Aggregated stock summary for a product across all warehouses.
/// </summary>
public sealed record StockSummaryDto
{
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
    /// Gets the total quantity on hand across all warehouses.
    /// </summary>
    public required decimal TotalOnHand { get; init; }

    /// <summary>
    /// Gets the total reserved quantity across all warehouses.
    /// </summary>
    public required decimal TotalReserved { get; init; }

    /// <summary>
    /// Gets the total available quantity across all warehouses.
    /// </summary>
    public required decimal TotalAvailable { get; init; }

    /// <summary>
    /// Gets the per-warehouse stock breakdown.
    /// </summary>
    public required IReadOnlyList<StockLevelDto> WarehouseBreakdown { get; init; }
}
