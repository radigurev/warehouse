using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a hierarchical product category for organizing the product catalog.
/// </summary>
[Table("ProductCategories", Schema = "inventory")]
[Index(nameof(Name), IsUnique = true, Name = "IX_ProductCategories_Name")]
[Index(nameof(ParentCategoryId), Name = "IX_ProductCategories_ParentCategoryId")]
public sealed class ProductCategory : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the category name (max 100 characters).
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional category description (max 500 characters).
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the optional parent category ID for hierarchy.
    /// </summary>
    [ForeignKey(nameof(ParentCategory))]
    public int? ParentCategoryId { get; set; }

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
    /// Gets or sets the navigation property to the parent category.
    /// </summary>
    public ProductCategory? ParentCategory { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of child categories.
    /// </summary>
    public ICollection<ProductCategory> ChildCategories { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of products in this category.
    /// </summary>
    public ICollection<Product> Products { get; set; } = [];
}
