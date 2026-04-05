using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a single count entry within a stocktake session for a product at a location.
/// <para>See <see cref="StocktakeSession"/>, <see cref="Product"/>, <see cref="StorageLocation"/>.</para>
/// </summary>
[Table("StocktakeCounts", Schema = "inventory")]
public sealed class StocktakeCount : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the stocktake session.
    /// </summary>
    [ForeignKey(nameof(Session))]
    public int SessionId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the product.
    /// </summary>
    [ForeignKey(nameof(Product))]
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the optional foreign key to the storage location.
    /// </summary>
    [ForeignKey(nameof(Location))]
    public int? LocationId { get; set; }

    /// <summary>
    /// Gets or sets the expected quantity from system records.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal ExpectedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the actual counted quantity.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal ActualQuantity { get; set; }

    /// <summary>
    /// Gets or sets the calculated variance (actual minus expected).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Variance { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the count was recorded.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? CountedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who recorded the count.
    /// </summary>
    public int? CountedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the stocktake session.
    /// </summary>
    public StocktakeSession Session { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the product.
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the storage location.
    /// </summary>
    public StorageLocation? Location { get; set; }
}
