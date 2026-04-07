using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a supplier return document for returning goods to a supplier.
/// <para>Conforms to ISA-95 Part 3 -- Material Shipment (return) activity.</para>
/// <para>See <see cref="Supplier"/>, <see cref="SupplierReturnLine"/>.</para>
/// </summary>
[Table("SupplierReturns", Schema = "purchasing")]
[Index(nameof(ReturnNumber), IsUnique = true, Name = "IX_SupplierReturns_ReturnNumber")]
[Index(nameof(SupplierId), Name = "IX_SupplierReturns_SupplierId")]
[Index(nameof(Status), Name = "IX_SupplierReturns_Status")]
public sealed class SupplierReturn : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique return number (format: SR-YYYYMMDD-NNNN).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string ReturnNumber { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the supplier.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Supplier))]
    public int SupplierId { get; set; }

    /// <summary>
    /// Gets or sets the return status (stored as nvarchar(20)).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the return reason (max 500 characters).
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public required string Reason { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this return (cross-schema ref to auth.Users).
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the return was confirmed.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
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
