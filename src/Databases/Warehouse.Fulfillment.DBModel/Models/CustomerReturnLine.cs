using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a single line item on a customer return specifying the product and destination.
/// <para>See <see cref="CustomerReturn"/>.</para>
/// </summary>
public sealed class CustomerReturnLine : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the customer return.
    /// </summary>
    public int CustomerReturnId { get; set; }

    /// <summary>
    /// Gets or sets the product ID (cross-schema ref to inventory.Products).
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the destination warehouse ID (cross-schema ref to inventory.Warehouses).
    /// </summary>
    public int WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the optional destination location ID (cross-schema ref to inventory.StorageLocations).
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Gets or sets the return quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the optional batch ID (cross-schema ref to inventory.Batches).
    /// </summary>
    public int? BatchId { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 500 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent customer return.
    /// </summary>
    public CustomerReturn CustomerReturn { get; set; } = null!;
}
