using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a unit of measure for products (e.g., PCS, KG, M).
/// </summary>
[Table("UnitsOfMeasure", Schema = "inventory")]
[Index(nameof(Code), IsUnique = true, Name = "IX_UnitsOfMeasure_Code")]
public sealed class UnitOfMeasure : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique unit code (max 10 characters, e.g., PCS, KG).
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column(TypeName = "nvarchar(10)")]
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the unit name (max 50 characters).
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional description (max 200 characters).
    /// </summary>
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string? Description { get; set; }

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
    /// Gets or sets the navigation collection of products using this unit.
    /// </summary>
    public ICollection<Product> Products { get; set; } = [];
}
