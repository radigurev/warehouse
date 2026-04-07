using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a single line item on a sales order with quantity tracking across pick/pack/ship stages.
/// <para>See <see cref="SalesOrder"/>.</para>
/// </summary>
public sealed class SalesOrderLine : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the sales order.
    /// </summary>
    public int SalesOrderId { get; set; }

    /// <summary>
    /// Gets or sets the product ID (cross-schema ref to inventory.Products -- no EF navigation).
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the ordered quantity.
    /// </summary>
    public decimal OrderedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the line total (OrderedQuantity * UnitPrice).
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Gets or sets the total picked quantity across all pick lists.
    /// </summary>
    public decimal PickedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the total packed quantity across all parcels.
    /// </summary>
    public decimal PackedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the total shipped quantity.
    /// </summary>
    public decimal ShippedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 500 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent sales order.
    /// </summary>
    public SalesOrder SalesOrder { get; set; } = null!;
}
