using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a packed item within a parcel, referencing a confirmed pick list line.
/// <para>See <see cref="Parcel"/>, <see cref="PickListLine"/>.</para>
/// </summary>
public sealed class ParcelItem : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the parcel.
    /// </summary>
    public int ParcelId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the pick list line.
    /// </summary>
    public int PickListLineId { get; set; }

    /// <summary>
    /// Gets or sets the product ID (cross-schema ref to inventory.Products).
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the packed quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent parcel.
    /// </summary>
    public Parcel Parcel { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the pick list line.
    /// </summary>
    public PickListLine PickListLine { get; set; } = null!;
}
