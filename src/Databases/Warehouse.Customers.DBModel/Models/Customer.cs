using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Warehouse.Customers.DBModel.Models;

/// <summary>
/// Represents a customer entity with contact information, accounts, and category assignment.
/// <para>See <see cref="CustomerCategory"/>, <see cref="CustomerAccount"/>, <see cref="CustomerAddress"/>, <see cref="CustomerPhone"/>, <see cref="CustomerEmail"/>.</para>
/// </summary>
[Table("Customers", Schema = "customers")]
[Index(nameof(Code), IsUnique = true, Name = "IX_Customers_Code")]
[Index(nameof(CategoryId), Name = "IX_Customers_CategoryId")]
[Index(nameof(Name), Name = "IX_Customers_Name")]
public sealed class Customer
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique customer code (max 20 characters).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the customer name (max 200 characters).
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional tax identification number (max 50 characters).
    /// </summary>
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string? TaxId { get; set; }

    /// <summary>
    /// Gets or sets the optional foreign key to the customer category.
    /// </summary>
    [ForeignKey(nameof(Category))]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether the customer is active.
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the customer is soft-deleted.
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the customer was soft-deleted.
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
    /// Gets or sets the ID of the user who created this customer.
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional ID of the user who last modified this customer.
    /// </summary>
    public int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the customer category.
    /// </summary>
    public CustomerCategory? Category { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of customer accounts.
    /// </summary>
    public ICollection<CustomerAccount> Accounts { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of customer addresses.
    /// </summary>
    public ICollection<CustomerAddress> Addresses { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of customer phones.
    /// </summary>
    public ICollection<CustomerPhone> Phones { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of customer emails.
    /// </summary>
    public ICollection<CustomerEmail> Emails { get; set; } = [];
}
