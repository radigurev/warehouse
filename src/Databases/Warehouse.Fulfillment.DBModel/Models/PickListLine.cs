using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a single line item on a pick list with pick execution tracking.
/// <para>See <see cref="PickList"/>, <see cref="SalesOrderLine"/>.</para>
/// </summary>
public sealed class PickListLine : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the pick list.
    /// </summary>
    public int PickListId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the sales order line.
    /// </summary>
    public int SalesOrderLineId { get; set; }

    /// <summary>
    /// Gets or sets the product ID (cross-schema ref to inventory.Products).
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the warehouse ID (cross-schema ref to inventory.Warehouses).
    /// </summary>
    public int WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the optional location ID (cross-schema ref to inventory.StorageLocations).
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Gets or sets the requested pick quantity.
    /// </summary>
    public decimal RequestedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the actual picked quantity (null if not yet picked).
    /// </summary>
    public decimal? ActualQuantity { get; set; }

    /// <summary>
    /// Gets or sets the pick line status (stored as nvarchar(20)).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this line was picked.
    /// </summary>
    public DateTime? PickedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who picked this line (cross-schema ref to auth.Users).
    /// </summary>
    public int? PickedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent pick list.
    /// </summary>
    public PickList PickList { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the sales order line.
    /// </summary>
    public SalesOrderLine SalesOrderLine { get; set; } = null!;
}
