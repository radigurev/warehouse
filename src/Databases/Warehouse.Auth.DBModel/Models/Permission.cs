using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Warehouse.Auth.DBModel.Models;

/// <summary>
/// Represents a granular permission defined as a resource-action pair.
/// <para>See <see cref="RolePermission"/>, <see cref="Role"/>.</para>
/// </summary>
[Table("Permissions", Schema = "auth")]
[Index(nameof(Resource), nameof(Action), IsUnique = true, Name = "IX_Permissions_Resource_Action")]
public sealed class Permission
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the resource identifier (e.g., "users", "inventory.products").
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string Resource { get; set; }

    /// <summary>
    /// Gets or sets the action type (e.g., "read", "write", "update", "delete", "all").
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string Action { get; set; }

    /// <summary>
    /// Gets or sets the optional description (max 500 characters).
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of role-permission assignments.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
