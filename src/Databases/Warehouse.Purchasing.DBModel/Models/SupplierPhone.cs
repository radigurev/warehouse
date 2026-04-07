using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a phone number belonging to a supplier.
/// <para>See <see cref="Supplier"/>.</para>
/// </summary>
[Table("SupplierPhones", Schema = "purchasing")]
[Index(nameof(SupplierId), Name = "IX_SupplierPhones_SupplierId")]
public sealed class SupplierPhone : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the owning supplier.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Supplier))]
    public int SupplierId { get; set; }

    /// <summary>
    /// Gets or sets the phone type (e.g., Mobile, Landline, Fax).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string PhoneType { get; set; }

    /// <summary>
    /// Gets or sets the phone number (max 20 characters).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the optional phone extension (max 10 characters).
    /// </summary>
    [MaxLength(10)]
    [Column(TypeName = "nvarchar(10)")]
    public string? Extension { get; set; }

    /// <summary>
    /// Gets or sets whether this is the primary phone for the supplier.
    /// </summary>
    [Required]
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the owning supplier.
    /// </summary>
    public Supplier Supplier { get; set; } = null!;
}
