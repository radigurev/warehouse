using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Auth.DBModel.Models;

/// <summary>
/// Represents an opaque refresh token stored in the database for token rotation.
/// <para>See <see cref="User"/>.</para>
/// </summary>
[Table("RefreshTokens", Schema = "auth")]
[Index(nameof(Token), IsUnique = true, Name = "IX_RefreshTokens_Token")]
[Index(nameof(UserId), Name = "IX_RefreshTokens_UserId")]
public sealed class RefreshToken : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the owning user.
    /// </summary>
    [Required]
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the opaque token string.
    /// </summary>
    [Required]
    [MaxLength(512)]
    [Column(TypeName = "nvarchar(512)")]
    public required string Token { get; set; }

    /// <summary>
    /// Gets or sets the UTC expiration timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the token was revoked, or null if still active.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the owning user.
    /// </summary>
    public User User { get; set; } = null!;
}
