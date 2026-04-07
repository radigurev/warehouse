using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a supplier entity with contact information and category assignment.
/// Maps to ISA-95 "Business Partner (supplier)" per Part 2.
/// <para>See <see cref="SupplierCategory"/>, <see cref="SupplierAddress"/>, <see cref="SupplierPhone"/>, <see cref="SupplierEmail"/>.</para>
/// </summary>
public sealed class Supplier : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique supplier code (max 20 characters).
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the supplier name (max 200 characters).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional tax identification number (max 50 characters).
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// Gets or sets the optional foreign key to the supplier category.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the optional payment term in days.
    /// </summary>
    public int? PaymentTermDays { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether the supplier is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the supplier is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the supplier was soft-deleted.
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this supplier (cross-schema ref to auth.Users).
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional ID of the user who last modified this supplier.
    /// </summary>
    public int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the supplier category.
    /// </summary>
    public SupplierCategory? Category { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of supplier addresses.
    /// </summary>
    public ICollection<SupplierAddress> Addresses { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of supplier phones.
    /// </summary>
    public ICollection<SupplierPhone> Phones { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of supplier emails.
    /// </summary>
    public ICollection<SupplierEmail> Emails { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of purchase orders for this supplier.
    /// </summary>
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
}
