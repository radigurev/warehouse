namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Read-only projection of inventory.Products for cross-schema product name resolution.
/// Mapped via ToView — no migrations are generated.
/// </summary>
public sealed class ProductLookup
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
