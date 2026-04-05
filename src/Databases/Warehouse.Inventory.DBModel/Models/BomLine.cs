using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a component line within a bill of materials.
/// <para>See <see cref="BillOfMaterials"/>, <see cref="Product"/>.</para>
/// </summary>
[Table("BomLines", Schema = "inventory")]
[Index(nameof(BillOfMaterialsId), Name = "IX_BomLines_BillOfMaterialsId")]
[Index(nameof(ChildProductId), Name = "IX_BomLines_ChildProductId")]
public sealed class BomLine : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the parent BOM.
    /// </summary>
    [ForeignKey(nameof(BillOfMaterials))]
    public int BillOfMaterialsId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the child (component) product.
    /// </summary>
    [ForeignKey(nameof(ChildProduct))]
    public int ChildProductId { get; set; }

    /// <summary>
    /// Gets or sets the quantity of the component required.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets optional notes for the BOM line (max 500 characters).
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent BOM.
    /// </summary>
    public BillOfMaterials BillOfMaterials { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the child product.
    /// </summary>
    public Product ChildProduct { get; set; } = null!;
}
