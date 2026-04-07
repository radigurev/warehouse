namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for creating a parcel for a sales order.
/// </summary>
public sealed record CreateParcelRequest
{
    /// <summary>Gets the optional weight in kilograms. Must be greater than 0 when provided.</summary>
    public decimal? Weight { get; init; }

    /// <summary>Gets the optional length in centimeters. Must be greater than 0 when provided.</summary>
    public decimal? Length { get; init; }

    /// <summary>Gets the optional width in centimeters. Must be greater than 0 when provided.</summary>
    public decimal? Width { get; init; }

    /// <summary>Gets the optional height in centimeters. Must be greater than 0 when provided.</summary>
    public decimal? Height { get; init; }

    /// <summary>Gets the optional tracking number. Max 100 characters.</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>Gets the optional notes. Max 2000 characters.</summary>
    public string? Notes { get; init; }
}
