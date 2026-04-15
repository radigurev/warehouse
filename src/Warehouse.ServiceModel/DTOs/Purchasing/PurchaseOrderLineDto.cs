namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Represents a purchase order line with receiving progress.
/// </summary>
public sealed record PurchaseOrderLineDto
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
    /// Gets or sets the product name (resolved from inventory lookup).
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product code (resolved from inventory lookup, used for batch number generation).
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets the ordered quantity.
    /// </summary>
    public required decimal OrderedQuantity { get; init; }

    /// <summary>
    /// Gets the unit price.
    /// </summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>
    /// Gets the line total.
    /// </summary>
    public required decimal LineTotal { get; init; }

    /// <summary>
    /// Gets the total received quantity (sum of accepted goods receipt quantities).
    /// </summary>
    public required decimal ReceivedQuantity { get; init; }

    /// <summary>
    /// Gets the remaining quantity to receive.
    /// </summary>
    public decimal RemainingQuantity => OrderedQuantity - ReceivedQuantity;

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }
}
