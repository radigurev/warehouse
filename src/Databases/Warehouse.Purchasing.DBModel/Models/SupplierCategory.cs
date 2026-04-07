using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a classification category for suppliers.
/// <para>See <see cref="Supplier"/>.</para>
/// </summary>
public sealed class SupplierCategory : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique category name (max 100 characters).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional description (max 500 characters).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of suppliers in this category.
    /// </summary>
    public ICollection<Supplier> Suppliers { get; set; } = [];
}
