using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Nomenclature.DBModel.Models;

/// <summary>
/// Represents a country with ISO 3166-1 codes for geographic reference.
/// <para>See <see cref="StateProvince"/>.</para>
/// </summary>
[Table("Countries", Schema = "nomenclature")]
[Index(nameof(Iso2Code), IsUnique = true, Name = "UQ_Countries_Iso2Code")]
[Index(nameof(Iso3Code), IsUnique = true, Name = "UQ_Countries_Iso3Code")]
[Index(nameof(Name), Name = "IX_Countries_Name")]
public sealed class Country : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 code (e.g., BG, US).
    /// </summary>
    [Required]
    [MaxLength(2)]
    [Column(TypeName = "nvarchar(2)")]
    public required string Iso2Code { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-3 code (e.g., BGR, USA).
    /// </summary>
    [Required]
    [MaxLength(3)]
    [Column(TypeName = "nvarchar(3)")]
    public required string Iso3Code { get; set; }

    /// <summary>
    /// Gets or sets the country name in English (max 100 characters).
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the international dialing code (e.g., +359, +1).
    /// </summary>
    [MaxLength(10)]
    [Column(TypeName = "nvarchar(10)")]
    public string? PhonePrefix { get; set; }

    /// <summary>
    /// Gets or sets whether the country is active (soft-delete flag).
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
    /// Gets or sets the navigation collection of state/provinces in this country.
    /// </summary>
    public ICollection<StateProvince> StateProvinces { get; set; } = [];
}
