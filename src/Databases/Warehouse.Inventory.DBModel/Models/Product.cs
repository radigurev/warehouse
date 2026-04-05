using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a product in the inventory catalog with category, unit of measure, and related associations.
/// <para>See <see cref="ProductCategory"/>, <see cref="UnitOfMeasure"/>, <see cref="BillOfMaterials"/>.</para>
/// </summary>
[Table("Products", Schema = "inventory")]
[Index(nameof(Code), IsUnique = true, Name = "IX_Products_Code")]
[Index(nameof(CategoryId), Name = "IX_Products_CategoryId")]
[Index(nameof(UnitOfMeasureId), Name = "IX_Products_UnitOfMeasureId")]
[Index(nameof(Name), Name = "IX_Products_Name")]
public sealed class Product : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique product code (max 50 characters).
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the product name (max 200 characters).
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional product description (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the optional SKU (max 100 characters).
    /// </summary>
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string? Sku { get; set; }

    /// <summary>
    /// Gets or sets the optional barcode (max 100 characters).
    /// </summary>
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string? Barcode { get; set; }

    /// <summary>
    /// Gets or sets the optional foreign key to the product category.
    /// </summary>
    [ForeignKey(nameof(Category))]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the unit of measure.
    /// </summary>
    [ForeignKey(nameof(UnitOfMeasure))]
    public int UnitOfMeasureId { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether the product is active.
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the product is soft-deleted.
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the product was soft-deleted.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this product.
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional ID of the user who last modified this product.
    /// </summary>
    public int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the product category.
    /// </summary>
    public ProductCategory? Category { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the unit of measure.
    /// </summary>
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation collection of product accessories.
    /// </summary>
    public ICollection<ProductAccessory> Accessories { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of product substitutes.
    /// </summary>
    public ICollection<ProductSubstitute> Substitutes { get; set; } = [];
}
