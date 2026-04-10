namespace Warehouse.Inventory.API.Interfaces;

/// <summary>
/// Defines the receipt stock intake operation: batch resolution, stock movement creation,
/// and stock level upsert triggered by goods receipt events.
/// <para>Specification: SDD-INV-005.</para>
/// </summary>
public interface IReceiptStockIntakeService
{
    /// <summary>
    /// Processes a single goods receipt line: resolves or creates a batch, creates an immutable
    /// stock movement, and upserts the stock level. Idempotent — duplicate lines are skipped.
    /// </summary>
    /// <param name="context">All data needed to process the receipt line.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the line was processed; <c>false</c> if it was skipped (duplicate or validation failure).</returns>
    Task<bool> ProcessLineAsync(ReceiptLineContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Contains all data needed to process a single goods receipt line for stock intake.
/// <para>Specification: SDD-INV-005, Section 2.3.</para>
/// </summary>
public sealed class ReceiptLineContext
{
    /// <summary>
    /// Gets or sets the goods receipt line ID (used for idempotency and reference).
    /// </summary>
    public required int GoodsReceiptLineId { get; init; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets or sets the warehouse ID.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets or sets the optional storage location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets or sets the received quantity (must be positive).
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets or sets the optional batch number from the receipt line.
    /// </summary>
    public string? BatchNumber { get; init; }

    /// <summary>
    /// Gets or sets the optional manufacturing date.
    /// </summary>
    public DateOnly? ManufacturingDate { get; init; }

    /// <summary>
    /// Gets or sets the optional expiry date.
    /// </summary>
    public DateOnly? ExpiryDate { get; init; }

    /// <summary>
    /// Gets or sets the purchase order number for movement reference traceability.
    /// </summary>
    public required string PurchaseOrderNumber { get; init; }

    /// <summary>
    /// Gets or sets the goods receipt number for movement notes.
    /// </summary>
    public required string GoodsReceiptNumber { get; init; }

    /// <summary>
    /// Gets or sets the user ID who triggered the stock intake.
    /// </summary>
    public required int CreatedByUserId { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp for the stock movement.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
