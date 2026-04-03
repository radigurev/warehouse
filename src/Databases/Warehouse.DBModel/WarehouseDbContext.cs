using Microsoft.EntityFrameworkCore;
using Warehouse.DBModel.Models.Auth;

namespace Warehouse.DBModel;

/// <summary>
/// EF Core database context for the Warehouse application.
/// </summary>
public sealed class WarehouseDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance using the specified options.
    /// </summary>
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Roles DbSet.
    /// </summary>
    public DbSet<Role> Roles { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Permissions DbSet.
    /// </summary>
    public DbSet<Permission> Permissions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserRoles DbSet.
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; } = null!;

    /// <summary>
    /// Gets or sets the RolePermissions DbSet.
    /// </summary>
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the RefreshTokens DbSet.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserActionLogs DbSet.
    /// </summary>
    public DbSet<UserActionLog> UserActionLogs { get; set; } = null!;

    /// <summary>
    /// Configures composite keys and default values that cannot be expressed via Data Annotations.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId })
            .HasName("PK_UserRoles");

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId })
            .HasName("PK_RolePermissions");

        modelBuilder.Entity<User>(u =>
        {
            u.Property(e => e.IsActive).HasDefaultValue(true);
            u.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            u.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            u.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Users_IsActive")
                .HasFilter("[IsActive] = 1");
        });

        modelBuilder.Entity<Role>(r =>
        {
            r.Property(e => e.IsSystem).HasDefaultValue(false);
            r.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Permission>(p =>
        {
            p.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<UserRole>(ur =>
        {
            ur.Property(e => e.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<RolePermission>(rp =>
        {
            rp.Property(e => e.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<RefreshToken>(rt =>
        {
            rt.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<UserActionLog>(l =>
        {
            l.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
