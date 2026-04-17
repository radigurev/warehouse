using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Nomenclature.DBModel.Models;

/// <summary>
/// Represents a currency with ISO 4217 code for financial reference.
/// </summary>
[Table("Currencies", Schema = "nomenclature")]
[Index(nameof(Code), IsUnique = true, Name = "UQ_Currencies_Code")]
public sealed class Currency : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ISO 4217 currency code (e.g., USD, EUR, BGN).
    /// </summary>
    [Required]
    [MaxLength(3)]
    [Column(TypeName = "nvarchar(3)")]
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the currency name (e.g., US Dollar). Max 100 characters.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the currency symbol (e.g., $, EUR). Max 5 characters.
    /// </summary>
    [MaxLength(5)]
    [Column(TypeName = "nvarchar(5)")]
    public string? Symbol { get; set; }

    /// <summary>
    /// Gets or sets whether the currency is active (soft-delete flag).
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
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ModifiedAtUtc { get; set; }
}
