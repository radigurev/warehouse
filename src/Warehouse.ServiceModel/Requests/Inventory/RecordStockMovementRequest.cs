using Warehouse.Common.Enums;

namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for recording a stock movement.
/// </summary>
public sealed record RecordStockMovementRequest
{
    /// <summary>
    /// Gets the product ID. Required.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the warehouse ID. Required.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the movement quantity. Required, must be greater than zero.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the reason code per ISA-95 terminology. Required.
    /// </summary>
    public required StockMovementReason ReasonCode { get; init; }

    /// <summary>
    /// Gets the optional batch ID.
    /// </summary>
    public int? BatchId { get; init; }

    /// <summary>
    /// Gets the optional reference number. Max 100 characters.
    /// </summary>
    public string? ReferenceNumber { get; init; }

    /// <summary>
    /// Gets the optional notes. Max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }
}
