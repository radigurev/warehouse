namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for updating a parcel (before dispatch only).
/// </summary>
public sealed record UpdateParcelRequest
{
    /// <summary>Gets the optional weight in kilograms.</summary>
    public decimal? Weight { get; init; }

    /// <summary>Gets the optional length in centimeters.</summary>
    public decimal? Length { get; init; }

    /// <summary>Gets the optional width in centimeters.</summary>
    public decimal? Width { get; init; }

    /// <summary>Gets the optional height in centimeters.</summary>
    public decimal? Height { get; init; }

    /// <summary>Gets the optional tracking number. Max 100 characters.</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>Gets the optional notes. Max 2000 characters.</summary>
    public string? Notes { get; init; }
}
