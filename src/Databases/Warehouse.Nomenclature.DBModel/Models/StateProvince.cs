using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Nomenclature.DBModel.Models;

/// <summary>
/// Represents an administrative division (state, province, region) within a country.
/// <para>See <see cref="Country"/>, <see cref="City"/>.</para>
/// </summary>
[Table("StateProvinces", Schema = "nomenclature")]
[Index(nameof(CountryId), nameof(Code), IsUnique = true, Name = "UQ_StateProvinces_CountryId_Code")]
[Index(nameof(CountryId), Name = "IX_StateProvinces_CountryId")]
public sealed class StateProvince : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the parent country ID.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Country))]
    public int CountryId { get; set; }

    /// <summary>
    /// Gets or sets the state/province code (e.g., CA, SOF). Max 10 characters.
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column(TypeName = "nvarchar(10)")]
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the state/province name (max 100 characters).
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets whether the state/province is active (soft-delete flag).
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
    /// Gets or sets the navigation property to the parent country.
    /// </summary>
    public Country Country { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation collection of cities in this state/province.
    /// </summary>
    public ICollection<City> Cities { get; set; } = [];
}
