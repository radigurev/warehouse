namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for updating a carrier service level.
/// </summary>
public sealed record UpdateCarrierServiceLevelRequest
{
    /// <summary>Gets the service level name. Required, 1-100 characters.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the estimated delivery days. Optional, 1-365.</summary>
    public int? EstimatedDeliveryDays { get; init; }

    /// <summary>Gets the base rate. Optional, must be >= 0.</summary>
    public decimal? BaseRate { get; init; }

    /// <summary>Gets the per-kilogram rate. Optional, must be >= 0.</summary>
    public decimal? PerKgRate { get; init; }

    /// <summary>Gets the optional notes. Max 500 characters.</summary>
    public string? Notes { get; init; }
}
