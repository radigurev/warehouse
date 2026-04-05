using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a bill of materials header linking a parent product to its component lines.
/// <para>See <see cref="BomLine"/>, <see cref="Product"/>.</para>
/// </summary>
[Table("BillOfMaterials", Schema = "inventory")]
[Index(nameof(ParentProductId), Name = "IX_BillOfMaterials_ParentProductId")]
public sealed class BillOfMaterials : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the parent product.
    /// </summary>
    [ForeignKey(nameof(ParentProduct))]
    public int ParentProductId { get; set; }

    /// <summary>
    /// Gets or sets the optional BOM name (max 100 characters).
    /// </summary>
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the optional BOM version (max 20 characters).
    /// </summary>
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets whether this BOM is the active version.
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this BOM.
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional ID of the user who last modified this BOM.
    /// </summary>
    public int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent product.
    /// </summary>
    public Product ParentProduct { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation collection of BOM component lines.
    /// </summary>
    public ICollection<BomLine> Lines { get; set; } = [];
}
