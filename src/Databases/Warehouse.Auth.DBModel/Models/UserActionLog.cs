using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Warehouse.Auth.DBModel.Models;

/// <summary>
/// Represents an immutable audit log entry for user and authentication events.
/// <para>See <see cref="User"/>.</para>
/// </summary>
[Table("UserActionLogs", Schema = "auth")]
[Index(nameof(UserId), nameof(CreatedAt), Name = "IX_UserActionLogs_UserId")]
[Index(nameof(Action), nameof(CreatedAt), Name = "IX_UserActionLogs_Action")]
public sealed class UserActionLog
{
    /// <summary>
    /// Gets or sets the auto-incrementing BIGINT primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the optional foreign key to the acting user.
    /// </summary>
    [ForeignKey(nameof(User))]
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the action performed (e.g., "Login", "CreateUser").
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public required string Action { get; set; }

    /// <summary>
    /// Gets or sets the resource affected (e.g., "auth", "users").
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string Resource { get; set; }

    /// <summary>
    /// Gets or sets optional JSON details about the action.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the request originator.
    /// </summary>
    [MaxLength(45)]
    [Column(TypeName = "nvarchar(45)")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the acting user.
    /// </summary>
    public User? User { get; set; }
}
