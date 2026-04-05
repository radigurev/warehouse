namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Warehouse transfer header representation for list views.
/// </summary>
public sealed record WarehouseTransferDto
{
    /// <summary>
    /// Gets the transfer ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the transfer status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the source warehouse ID.
    /// </summary>
    public required int SourceWarehouseId { get; init; }

    /// <summary>
    /// Gets the source warehouse name.
    /// </summary>
    public required string SourceWarehouseName { get; init; }

    /// <summary>
    /// Gets the destination warehouse ID.
    /// </summary>
    public required int DestinationWarehouseId { get; init; }

    /// <summary>
    /// Gets the destination warehouse name.
    /// </summary>
    public required string DestinationWarehouseName { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the ID of the user who created this transfer.
    /// </summary>
    public required int CreatedByUserId { get; init; }
}
