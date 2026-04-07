namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Parcel DTO including packed items.
/// </summary>
public sealed record ParcelDto
{
    /// <summary>Gets the parcel ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the unique parcel number.</summary>
    public required string ParcelNumber { get; init; }

    /// <summary>Gets the sales order ID.</summary>
    public required int SalesOrderId { get; init; }

    /// <summary>Gets the optional weight (kg).</summary>
    public decimal? Weight { get; init; }

    /// <summary>Gets the optional length (cm).</summary>
    public decimal? Length { get; init; }

    /// <summary>Gets the optional width (cm).</summary>
    public decimal? Width { get; init; }

    /// <summary>Gets the optional height (cm).</summary>
    public decimal? Height { get; init; }

    /// <summary>Gets the optional tracking number.</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Gets the collection of packed items.</summary>
    public required IReadOnlyList<ParcelItemDto> Items { get; init; }
}
