namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Carrier DTO for list views.
/// </summary>
public sealed record CarrierDto
{
    /// <summary>Gets the carrier ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the carrier code.</summary>
    public required string Code { get; init; }

    /// <summary>Gets the carrier name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets whether the carrier is active.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }
}
