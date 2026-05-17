namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Compact parcel representation embedded in the SalesOrderDetailDto response.
/// Uses unit-suffixed dimension field names (<c>WeightKg</c>, <c>LengthCm</c>, etc.) to match
/// the frontend contract and avoid ambiguity.
/// </summary>
public sealed record SalesOrderParcelSummaryDto
{
    /// <summary>Gets the parcel ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the unique parcel number.</summary>
    public required string ParcelNumber { get; init; }

    /// <summary>Gets the optional weight in kilograms.</summary>
    public decimal? WeightKg { get; init; }

    /// <summary>Gets the optional length in centimeters.</summary>
    public decimal? LengthCm { get; init; }

    /// <summary>Gets the optional width in centimeters.</summary>
    public decimal? WidthCm { get; init; }

    /// <summary>Gets the optional height in centimeters.</summary>
    public decimal? HeightCm { get; init; }

    /// <summary>Gets the optional tracking number.</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets the collection of packed items.</summary>
    public required IReadOnlyList<SalesOrderParcelItemSummaryDto> Items { get; init; }
}
