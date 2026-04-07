using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents an email address belonging to a supplier.
/// <para>See <see cref="Supplier"/>.</para>
/// </summary>
[Table("SupplierEmails", Schema = "purchasing")]
[Index(nameof(SupplierId), Name = "IX_SupplierEmails_SupplierId")]
[Index(nameof(SupplierId), nameof(EmailAddress), IsUnique = true, Name = "IX_SupplierEmails_SupplierId_EmailAddress")]
public sealed class SupplierEmail : IEntity
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
    /// Gets or sets the email type (e.g., General, Billing, Support).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string EmailType { get; set; }

    /// <summary>
    /// Gets or sets the email address (max 256 characters).
    /// </summary>
    [Required]
    [MaxLength(256)]
    [Column(TypeName = "nvarchar(256)")]
    public required string EmailAddress { get; set; }

    /// <summary>
    /// Gets or sets whether this is the primary email for the supplier.
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
