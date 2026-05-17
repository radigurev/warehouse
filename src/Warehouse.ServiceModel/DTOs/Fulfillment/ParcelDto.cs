namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Parcel DTO including packed items. Uses unit-suffixed dimension field names
/// (<c>WeightKg</c>, <c>LengthCm</c>, etc.) to match the frontend contract.
/// </summary>
public sealed record ParcelDto
{
    /// <summary>Gets the parcel ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the unique parcel number.</summary>
    public required string ParcelNumber { get; init; }

    /// <summary>Gets the sales order ID.</summary>
    public required int SalesOrderId { get; init; }

    /// <summary>Gets the optional weight in kilograms (mapped from entity Weight).</summary>
    public decimal? WeightKg { get; init; }

    /// <summary>Gets the optional length in centimeters (mapped from entity Length).</summary>
    public decimal? LengthCm { get; init; }

    /// <summary>Gets the optional width in centimeters (mapped from entity Width).</summary>
    public decimal? WidthCm { get; init; }

    /// <summary>Gets the optional height in centimeters (mapped from entity Height).</summary>
    public decimal? HeightCm { get; init; }

    /// <summary>Gets the optional tracking number.</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Gets the collection of packed items.</summary>
    public required IReadOnlyList<ParcelItemDto> Items { get; init; }
}
