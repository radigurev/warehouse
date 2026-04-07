namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for updating a shipment's status with tracking information.
/// </summary>
public sealed record UpdateShipmentStatusRequest
{
    /// <summary>Gets the new shipment status. Required.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the optional tracking number. Max 100 characters.</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>Gets the optional tracking URL. Max 500 characters.</summary>
    public string? TrackingUrl { get; init; }

    /// <summary>Gets the optional tracking notes. Max 2000 characters.</summary>
    public string? Notes { get; init; }
}
