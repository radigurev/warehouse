using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a service level (shipping option) offered by a carrier.
/// <para>See <see cref="Carrier"/>.</para>
/// </summary>
public sealed class CarrierServiceLevel : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the carrier.
    /// </summary>
    public int CarrierId { get; set; }

    /// <summary>
    /// Gets or sets the unique code within the carrier (max 20 characters).
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the service level name (max 100 characters).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the estimated delivery time in days.
    /// </summary>
    public int? EstimatedDeliveryDays { get; set; }

    /// <summary>
    /// Gets or sets the base rate for this service level.
    /// </summary>
    public decimal? BaseRate { get; set; }

    /// <summary>
    /// Gets or sets the per-kilogram rate for this service level.
    /// </summary>
    public decimal? PerKgRate { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 500 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent carrier.
    /// </summary>
    public Carrier Carrier { get; set; } = null!;
}
