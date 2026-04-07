namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Carrier service level DTO.
/// </summary>
public sealed record CarrierServiceLevelDto
{
    /// <summary>Gets the service level ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the carrier ID.</summary>
    public required int CarrierId { get; init; }

    /// <summary>Gets the service level code.</summary>
    public required string Code { get; init; }

    /// <summary>Gets the service level name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the estimated delivery days.</summary>
    public int? EstimatedDeliveryDays { get; init; }

    /// <summary>Gets the base rate.</summary>
    public decimal? BaseRate { get; init; }

    /// <summary>Gets the per-kilogram rate.</summary>
    public decimal? PerKgRate { get; init; }

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; init; }
}
