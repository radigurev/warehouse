using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Warehouse.DBModel.Models.Auth;

/// <summary>
/// Represents an application user in the auth schema.
/// <para>See <see cref="Role"/>, <see cref="UserRole"/>, <see cref="RefreshToken"/>.</para>
/// </summary>
[Table("Users", Schema = "auth")]
[Index(nameof(Username), IsUnique = true, Name = "IX_Users_Username")]
[Index(nameof(Email), IsUnique = true, Name = "IX_Users_Email")]
public sealed class User
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique login name (3-50 characters, alphanumeric + underscores).
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the unique email address (max 256 characters).
    /// </summary>
    [Required]
    [MaxLength(256)]
    [Column(TypeName = "nvarchar(256)")]
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the BCrypt password hash.
    /// </summary>
    [Required]
    [MaxLength(512)]
    [Column(TypeName = "nvarchar(512)")]
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Gets or sets the first name of the user.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the user.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string LastName { get; set; }

    /// <summary>
    /// Gets or sets whether the user account is active.
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-update timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of user-role assignments.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of refresh tokens.
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of audit log entries.
    /// </summary>
    public ICollection<UserActionLog> UserActionLogs { get; set; } = [];
}
