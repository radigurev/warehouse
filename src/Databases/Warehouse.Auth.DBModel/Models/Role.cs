using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Warehouse.Auth.DBModel.Models;

/// <summary>
/// Represents an authorization role that groups permissions.
/// <para>See <see cref="Permission"/>, <see cref="UserRole"/>, <see cref="RolePermission"/>.</para>
/// </summary>
[Table("Roles", Schema = "auth")]
[Index(nameof(Name), IsUnique = true, Name = "IX_Roles_Name")]
public sealed class Role
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique role name (2-50 characters).
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional description (max 500 characters).
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this is a system-protected role that cannot be deleted.
    /// </summary>
    [Required]
    public bool IsSystem { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of user-role assignments.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of role-permission assignments.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
