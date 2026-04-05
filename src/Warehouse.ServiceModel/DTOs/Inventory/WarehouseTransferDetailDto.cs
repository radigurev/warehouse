namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Full warehouse transfer representation including lines.
/// </summary>
public sealed record WarehouseTransferDetailDto
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

    /// <summary>
    /// Gets the optional UTC completion timestamp.
    /// </summary>
    public DateTime? CompletedAtUtc { get; init; }

    /// <summary>
    /// Gets the optional ID of the completing user.
    /// </summary>
    public int? CompletedByUserId { get; init; }

    /// <summary>
    /// Gets the collection of transfer lines.
    /// </summary>
    public required IReadOnlyList<WarehouseTransferLineDto> Lines { get; init; }
}
