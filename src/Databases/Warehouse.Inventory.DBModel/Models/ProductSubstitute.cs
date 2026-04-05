using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a link between a product and a substitute (interchangeable) product.
/// <para>See <see cref="Product"/>.</para>
/// </summary>
[Table("ProductSubstitutes", Schema = "inventory")]
public sealed class ProductSubstitute : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the source product.
    /// </summary>
    [ForeignKey(nameof(Product))]
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the substitute product.
    /// </summary>
    [ForeignKey(nameof(SubstituteProduct))]
    public int SubstituteProductId { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the source product.
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the substitute product.
    /// </summary>
    public Product SubstituteProduct { get; set; } = null!;
}
