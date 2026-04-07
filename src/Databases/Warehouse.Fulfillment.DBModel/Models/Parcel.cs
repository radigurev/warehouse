using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a parcel (package) associated with a sales order for packing operations.
/// <para>See <see cref="SalesOrder"/>, <see cref="ParcelItem"/>.</para>
/// </summary>
public sealed class Parcel : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique parcel number (format: PKG-YYYYMMDD-NNNN).
    /// </summary>
    public required string ParcelNumber { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the sales order.
    /// </summary>
    public int SalesOrderId { get; set; }

    /// <summary>
    /// Gets or sets the optional weight in kilograms.
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// Gets or sets the optional length in centimeters.
    /// </summary>
    public decimal? Length { get; set; }

    /// <summary>
    /// Gets or sets the optional width in centimeters.
    /// </summary>
    public decimal? Width { get; set; }

    /// <summary>
    /// Gets or sets the optional height in centimeters.
    /// </summary>
    public decimal? Height { get; set; }

    /// <summary>
    /// Gets or sets the optional tracking number (max 100 characters).
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this parcel (cross-schema ref to auth.Users).
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent sales order.
    /// </summary>
    public SalesOrder SalesOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation collection of parcel items.
    /// </summary>
    public ICollection<ParcelItem> Items { get; set; } = [];
}
