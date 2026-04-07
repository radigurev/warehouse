namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Represents a supplier return line with product and location details.
/// </summary>
public sealed record SupplierReturnLineDto
{
    /// <summary>
    /// Gets the line ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the warehouse ID.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the return quantity.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the optional batch ID.
    /// </summary>
    public int? BatchId { get; init; }

    /// <summary>
    /// Gets the optional goods receipt line ID.
    /// </summary>
    public int? GoodsReceiptLineId { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }
}
