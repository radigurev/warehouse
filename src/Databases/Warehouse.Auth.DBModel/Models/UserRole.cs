using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Warehouse.Auth.DBModel.Models;

/// <summary>
/// Represents the many-to-many join between users and roles.
/// <para>See <see cref="User"/>, <see cref="Role"/>.</para>
/// </summary>
[Table("UserRoles", Schema = "auth")]
public sealed class UserRole
{
    /// <summary>
    /// Gets or sets the foreign key to the user.
    /// </summary>
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the role.
    /// </summary>
    [ForeignKey(nameof(Role))]
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the role was assigned.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the user.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the role.
    /// </summary>
    public Role Role { get; set; } = null!;
}
