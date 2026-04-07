using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a phone number belonging to a supplier.
/// <para>See <see cref="Supplier"/>.</para>
/// </summary>
public sealed class SupplierPhone : IEntity
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
    /// Gets or sets the phone type (e.g., Mobile, Landline, Fax).
    /// </summary>
    public required string PhoneType { get; set; }

    /// <summary>
    /// Gets or sets the phone number (max 20 characters).
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the optional phone extension (max 10 characters).
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// Gets or sets whether this is the primary phone for the supplier.
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
