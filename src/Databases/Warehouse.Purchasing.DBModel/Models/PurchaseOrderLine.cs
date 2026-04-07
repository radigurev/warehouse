using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a line item on a purchase order referencing a product from the Inventory domain.
/// <para>See <see cref="PurchaseOrder"/>.</para>
/// </summary>
public sealed class PurchaseOrderLine : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the purchase order.
    /// </summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Gets or sets the product ID (cross-schema ref to inventory.Products).
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
    /// Gets or sets the total received quantity across all goods receipts.
    /// </summary>
    public decimal ReceivedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 500 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the purchase order.
    /// </summary>
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation collection of goods receipt lines for this PO line.
    /// </summary>
    public ICollection<GoodsReceiptLine> GoodsReceiptLines { get; set; } = [];
}
