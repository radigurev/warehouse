namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for updating a carrier.
/// </summary>
public sealed record UpdateCarrierRequest
{
    /// <summary>Gets the carrier name. Required, 1-200 characters.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional contact phone. Max 20 characters.</summary>
    public string? ContactPhone { get; init; }

    /// <summary>Gets the optional contact email. Max 256 characters.</summary>
    public string? ContactEmail { get; init; }

    /// <summary>Gets the optional website URL. Max 500 characters.</summary>
    public string? WebsiteUrl { get; init; }

    /// <summary>Gets the optional tracking URL template. Max 500 characters.</summary>
    public string? TrackingUrlTemplate { get; init; }

    /// <summary>Gets the optional notes. Max 2000 characters.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets whether the carrier is active.</summary>
    public required bool IsActive { get; init; }
}
