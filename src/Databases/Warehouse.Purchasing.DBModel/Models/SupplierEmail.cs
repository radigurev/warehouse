using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents an email address belonging to a supplier.
/// <para>See <see cref="Supplier"/>.</para>
/// </summary>
public sealed class SupplierEmail : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the owning supplier.
    /// </summary>
    public int SupplierId { get; set; }

    /// <summary>
    /// Gets or sets the email type (e.g., General, Billing, Support).
    /// </summary>
    public required string EmailType { get; set; }

    /// <summary>
    /// Gets or sets the email address (max 256 characters).
    /// </summary>
    public required string EmailAddress { get; set; }

    /// <summary>
    /// Gets or sets whether this is the primary email for the supplier.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the owning supplier.
    /// </summary>
    public Supplier Supplier { get; set; } = null!;
}
