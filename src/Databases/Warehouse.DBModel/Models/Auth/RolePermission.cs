using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Warehouse.DBModel.Models.Auth;

/// <summary>
/// Represents the many-to-many join between roles and permissions.
/// <para>See <see cref="Role"/>, <see cref="Permission"/>.</para>
/// </summary>
[Table("RolePermissions", Schema = "auth")]
public sealed class RolePermission
{
    /// <summary>
    /// Gets or sets the foreign key to the role.
    /// </summary>
    [ForeignKey(nameof(Role))]
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the permission.
    /// </summary>
    [ForeignKey(nameof(Permission))]
    public int PermissionId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the permission was assigned.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the role.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the permission.
    /// </summary>
    public Permission Permission { get; set; } = null!;
}
