using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Warehouse.Customers.DBModel.Models;

/// <summary>
/// Represents an email address belonging to a customer.
/// <para>See <see cref="Customer"/>.</para>
/// </summary>
[Table("CustomerEmails", Schema = "customers")]
[Index(nameof(CustomerId), Name = "IX_CustomerEmails_CustomerId")]
[Index(nameof(CustomerId), nameof(EmailAddress), IsUnique = true, Name = "IX_CustomerEmails_CustomerId_EmailAddress")]
public sealed class CustomerEmail
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
    /// Gets or sets the email type (e.g., General, Billing, Support).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string EmailType { get; set; }

    /// <summary>
    /// Gets or sets the email address (max 256 characters).
    /// </summary>
    [Required]
    [MaxLength(256)]
    [Column(TypeName = "nvarchar(256)")]
    public required string EmailAddress { get; set; }

    /// <summary>
    /// Gets or sets whether this is the primary email for the customer.
    /// </summary>
    [Required]
    public bool IsPrimary { get; set; }

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
