namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Production batch representation for list and detail views.
/// </summary>
public sealed record BatchDto
{
    /// <summary>
    /// Gets the batch ID.
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
    /// Gets the batch number.
    /// </summary>
    public required string BatchNumber { get; init; }

    /// <summary>
    /// Gets the optional expiry date.
    /// </summary>
    public DateOnly? ExpiryDate { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the total quantity on hand across all stock levels for this batch.
    /// </summary>
    public decimal QuantityOnHand { get; set; }

    /// <summary>
    /// Gets whether the batch is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
