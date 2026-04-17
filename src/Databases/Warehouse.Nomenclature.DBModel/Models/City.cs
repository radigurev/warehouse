using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Nomenclature.DBModel.Models;

/// <summary>
/// Represents a city within a state/province for geographic reference.
/// <para>See <see cref="StateProvince"/>.</para>
/// </summary>
[Table("Cities", Schema = "nomenclature")]
[Index(nameof(StateProvinceId), nameof(Name), IsUnique = true, Name = "UQ_Cities_StateProvinceId_Name")]
[Index(nameof(StateProvinceId), Name = "IX_Cities_StateProvinceId")]
public sealed class City : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the parent state/province ID.
    /// </summary>
    [Required]
    [ForeignKey(nameof(StateProvince))]
    public int StateProvinceId { get; set; }

    /// <summary>
    /// Gets or sets the city name (max 100 characters).
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional postal/ZIP code (max 20 characters).
    /// </summary>
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Gets or sets whether the city is active (soft-delete flag).
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

    /// <summary>
    /// Gets or sets the navigation property to the parent state/province.
    /// </summary>
    public StateProvince StateProvince { get; set; } = null!;
}
