namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Immutable tracking entry DTO for a shipment status update.
/// </summary>
public sealed record ShipmentTrackingDto
{
    /// <summary>Gets the tracking entry ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the shipment ID.</summary>
    public required int ShipmentId { get; init; }

    /// <summary>Gets the status at this tracking point.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the optional tracking notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets the UTC timestamp when this status was recorded (mapped from entity OccurredAtUtc).</summary>
    public required DateTime UpdatedAtUtc { get; init; }

    /// <summary>Gets the user who recorded this status (mapped from entity RecordedByUserId).</summary>
    public required int UpdatedByUserId { get; init; }
}
