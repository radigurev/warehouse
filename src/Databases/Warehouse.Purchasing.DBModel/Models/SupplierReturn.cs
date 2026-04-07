using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a supplier return document for returning goods to a supplier.
/// <para>Conforms to ISA-95 Part 3 -- Material Shipment (return) activity.</para>
/// <para>See <see cref="Supplier"/>, <see cref="SupplierReturnLine"/>.</para>
/// </summary>
public sealed class SupplierReturn : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique return number (format: SR-YYYYMMDD-NNNN).
    /// </summary>
    public required string ReturnNumber { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the supplier.
    /// </summary>
    public int SupplierId { get; set; }

    /// <summary>
    /// Gets or sets the return status (stored as nvarchar(20)).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the return reason (max 500 characters).
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this return (cross-schema ref to auth.Users).
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the return was confirmed.
    /// </summary>
    public DateTime? ConfirmedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who confirmed this return.
    /// </summary>
    public int? ConfirmedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the supplier.
    /// </summary>
    public Supplier Supplier { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation collection of return lines.
    /// </summary>
    public ICollection<SupplierReturnLine> Lines { get; set; } = [];
}
