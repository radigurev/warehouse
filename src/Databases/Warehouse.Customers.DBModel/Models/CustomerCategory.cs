using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Customers.DBModel.Models;

/// <summary>
/// Represents a classification category for customers.
/// <para>See <see cref="Customer"/>.</para>
/// </summary>
[Table("CustomerCategories", Schema = "customers")]
[Index(nameof(Name), IsUnique = true, Name = "IX_CustomerCategories_Name")]
public sealed class CustomerCategory : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique category name (max 100 characters).
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional description (max 500 characters).
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
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
    /// Gets or sets the navigation collection of customers in this category.
    /// </summary>
    public ICollection<Customer> Customers { get; set; } = [];
}
