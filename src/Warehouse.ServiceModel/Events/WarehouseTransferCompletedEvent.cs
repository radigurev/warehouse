namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when an inter-warehouse transfer is completed.
/// </summary>
public sealed record WarehouseTransferCompletedEvent
{
    /// <summary>
    /// Gets the transfer identifier.
    /// </summary>
    public required int TransferId { get; init; }

    /// <summary>
    /// Gets the source warehouse identifier.
    /// </summary>
    public required int SourceWarehouseId { get; init; }

    /// <summary>
    /// Gets the destination warehouse identifier.
    /// </summary>
    public required int DestinationWarehouseId { get; init; }

    /// <summary>
    /// Gets the user who completed the transfer.
    /// </summary>
    public required int CompletedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the transfer was completed.
    /// </summary>
    public required DateTime CompletedAt { get; init; }
}
