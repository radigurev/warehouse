using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Immutable tracking entry recording a shipment status update.
/// <para>Conforms to ISA-95 Operations Event Model.</para>
/// <para>See <see cref="Shipment"/>.</para>
/// </summary>
public sealed class ShipmentTracking : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the shipment.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the shipment status at this tracking point (stored as nvarchar(30)).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the optional tracking notes (max 2000 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this status was recorded.
    /// </summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who recorded this status (cross-schema ref to auth.Users).
    /// </summary>
    public int RecordedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent shipment.
    /// </summary>
    public Shipment Shipment { get; set; } = null!;
}
