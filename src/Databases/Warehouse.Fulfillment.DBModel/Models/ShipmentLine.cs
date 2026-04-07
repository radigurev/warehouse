using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a single shipped product line within a shipment.
/// <para>See <see cref="Shipment"/>, <see cref="SalesOrderLine"/>.</para>
/// </summary>
public sealed class ShipmentLine : IEntity
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
    /// Gets or sets the foreign key to the sales order line.
    /// </summary>
    public int SalesOrderLineId { get; set; }

    /// <summary>
    /// Gets or sets the product ID (cross-schema ref to inventory.Products).
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the shipped quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the optional location ID (cross-schema ref to inventory.StorageLocations).
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Gets or sets the optional batch ID (cross-schema ref to inventory.Batches).
    /// </summary>
    public int? BatchId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent shipment.
    /// </summary>
    public Shipment Shipment { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the sales order line.
    /// </summary>
    public SalesOrderLine SalesOrderLine { get; set; } = null!;
}
