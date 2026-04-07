namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when a supplier return is confirmed.
/// Event naming: Purchasing.SupplierReturn.Completed.
/// </summary>
public sealed record SupplierReturnCompletedEvent
{
    /// <summary>
    /// Gets the supplier return ID.
    /// </summary>
    public required int SupplierReturnId { get; init; }

    /// <summary>
    /// Gets the supplier return number.
    /// </summary>
    public required string SupplierReturnNumber { get; init; }

    /// <summary>
    /// Gets the supplier ID.
    /// </summary>
    public required int SupplierId { get; init; }

    /// <summary>
    /// Gets the user who confirmed the return.
    /// </summary>
    public required int ConfirmedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the return was confirmed.
    /// </summary>
    public required DateTime ConfirmedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of return lines.
    /// </summary>
    public required IReadOnlyList<SupplierReturnCompletedLine> Lines { get; init; }
}

/// <summary>
/// Represents a single line in a supplier return completed event.
/// </summary>
public sealed record SupplierReturnCompletedLine
{
    /// <summary>
    /// Gets the supplier return line ID.
    /// </summary>
    public required int SupplierReturnLineId { get; init; }

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
}
