namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Full carrier representation including contact info and service levels.
/// </summary>
public sealed record CarrierDetailDto
{
    /// <summary>Gets the carrier ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the carrier code.</summary>
    public required string Code { get; init; }

    /// <summary>Gets the carrier name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional contact phone.</summary>
    public string? ContactPhone { get; init; }

    /// <summary>Gets the optional contact email.</summary>
    public string? ContactEmail { get; init; }

    /// <summary>Gets the optional website URL.</summary>
    public string? WebsiteUrl { get; init; }

    /// <summary>Gets the optional tracking URL template.</summary>
    public string? TrackingUrlTemplate { get; init; }

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets whether the carrier is active.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Gets the collection of service levels.</summary>
    public required IReadOnlyList<CarrierServiceLevelDto> ServiceLevels { get; init; }
}
