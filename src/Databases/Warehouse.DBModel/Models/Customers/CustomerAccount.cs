using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Warehouse.DBModel.Models.Customers;

/// <summary>
/// Represents a multi-currency financial account belonging to a customer.
/// <para>See <see cref="Customer"/>.</para>
/// </summary>
[Table("CustomerAccounts", Schema = "customers")]
[Index(nameof(CustomerId), Name = "IX_CustomerAccounts_CustomerId")]
public sealed class CustomerAccount
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
    /// Gets or sets the ISO 4217 currency code (3 characters).
    /// </summary>
    [Required]
    [MaxLength(3)]
    [Column(TypeName = "nvarchar(3)")]
    public required string CurrencyCode { get; set; }

    /// <summary>
    /// Gets or sets the account balance with 4 decimal places.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Balance { get; set; }

    /// <summary>
    /// Gets or sets the optional account description (max 500 characters).
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this is the primary account for the customer.
    /// </summary>
    [Required]
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets whether the account is soft-deleted.
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the account was soft-deleted.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the owning customer.
    /// </summary>
    public Customer Customer { get; set; } = null!;
}
