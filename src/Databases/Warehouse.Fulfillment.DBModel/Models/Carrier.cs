using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a shipping carrier (logistics partner).
/// <para>Extends ISA-95 Equipment Model for logistics partners.</para>
/// <para>See <see cref="CarrierServiceLevel"/>.</para>
/// </summary>
public sealed class Carrier : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique carrier code (max 20 characters).
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the carrier name (max 200 characters).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional contact phone number (max 20 characters).
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Gets or sets the optional contact email address (max 256 characters).
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets or sets the optional website URL (max 500 characters).
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the optional tracking URL template (max 500 characters). SHOULD contain {trackingNumber} placeholder.
    /// </summary>
    public string? TrackingUrlTemplate { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether the carrier is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this carrier (cross-schema ref to auth.Users).
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional ID of the user who last modified this carrier.
    /// </summary>
    public int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of carrier service levels.
    /// </summary>
    public ICollection<CarrierServiceLevel> ServiceLevels { get; set; } = [];
}
