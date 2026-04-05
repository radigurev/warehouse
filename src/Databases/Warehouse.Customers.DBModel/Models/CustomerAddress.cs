using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Customers.DBModel.Models;

/// <summary>
/// Represents a physical address belonging to a customer.
/// <para>See <see cref="Customer"/>.</para>
/// </summary>
[Table("CustomerAddresses", Schema = "customers")]
[Index(nameof(CustomerId), Name = "IX_CustomerAddresses_CustomerId")]
public sealed class CustomerAddress : ICustomerOwnedEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the owning customer.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Customer))]
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the address type (e.g., Billing, Shipping, Both).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string AddressType { get; set; }

    /// <summary>
    /// Gets or sets the first street line (max 200 characters).
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public required string StreetLine1 { get; set; }

    /// <summary>
    /// Gets or sets the optional second street line (max 200 characters).
    /// </summary>
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string? StreetLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city name (max 100 characters).
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string City { get; set; }

    /// <summary>
    /// Gets or sets the optional state or province (max 100 characters).
    /// </summary>
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string? StateProvince { get; set; }

    /// <summary>
    /// Gets or sets the postal code (max 20 characters).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 country code (2 characters).
    /// </summary>
    [Required]
    [MaxLength(2)]
    [Column(TypeName = "nvarchar(2)")]
    public required string CountryCode { get; set; }

    /// <summary>
    /// Gets or sets whether this is the default address for its type.
    /// </summary>
    [Required]
    public bool IsDefault { get; set; }

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
    /// Gets or sets the navigation property to the owning customer.
    /// </summary>
    public Customer Customer { get; set; } = null!;
}
