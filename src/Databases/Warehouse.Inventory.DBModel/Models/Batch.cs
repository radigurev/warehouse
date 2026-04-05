using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a production batch or lot for a product with optional expiry tracking.
/// <para>See <see cref="Product"/>.</para>
/// </summary>
[Table("Batches", Schema = "inventory")]
public sealed class Batch : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the product.
    /// </summary>
    [ForeignKey(nameof(Product))]
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the batch number (max 50 characters), unique per product.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public required string BatchNumber { get; set; }

    /// <summary>
    /// Gets or sets the optional manufacturing date.
    /// </summary>
    [Column(TypeName = "date")]
    public DateOnly? ManufacturingDate { get; set; }

    /// <summary>
    /// Gets or sets the optional expiry date.
    /// </summary>
    [Column(TypeName = "date")]
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets optional notes (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether the batch is active.
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
    /// Gets or sets the ID of the user who created this batch.
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional ID of the user who last modified this batch.
    /// </summary>
    public int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the product.
    /// </summary>
    public Product Product { get; set; } = null!;
}
