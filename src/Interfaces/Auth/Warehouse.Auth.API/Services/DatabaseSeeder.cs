using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Auth.DBModel;
using Warehouse.Auth.DBModel.Models;
using Warehouse.Infrastructure.Configuration;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// Seeds required initial data into the database on application startup.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly AuthDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IFeatureManager _featureManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    private static readonly string[][] DefaultPermissions =
    [
        ["users", "read"], ["users", "write"], ["users", "update"], ["users", "delete"],
        ["roles", "read"], ["roles", "write"], ["roles", "update"], ["roles", "delete"],
        ["audit", "read"],
        ["customers", "read"], ["customers", "write"], ["customers", "update"], ["customers", "delete"],
        ["customer-categories", "read"], ["customer-categories", "write"], ["customer-categories", "update"], ["customer-categories", "delete"],
        ["products", "read"], ["products", "create"], ["products", "update"], ["products", "delete"],
        ["product-categories", "read"], ["product-categories", "create"], ["product-categories", "update"], ["product-categories", "delete"],
        ["units-of-measure", "read"], ["units-of-measure", "create"], ["units-of-measure", "update"], ["units-of-measure", "delete"],
        ["bom", "read"], ["bom", "create"], ["bom", "update"], ["bom", "delete"],
        ["product-accessories", "read"], ["product-accessories", "create"], ["product-accessories", "delete"],
        ["product-substitutes", "read"], ["product-substitutes", "create"], ["product-substitutes", "delete"],
        ["warehouses", "read"], ["warehouses", "create"], ["warehouses", "update"], ["warehouses", "delete"],
        ["zones", "read"], ["zones", "create"], ["zones", "update"], ["zones", "delete"],
        ["storage-locations", "read"], ["storage-locations", "create"], ["storage-locations", "update"], ["storage-locations", "delete"],
        ["stock-levels", "read"],
        ["stock-movements", "read"], ["stock-movements", "create"],
        ["batches", "read"], ["batches", "create"], ["batches", "update"], ["batches", "delete"],
        ["inventory-adjustments", "read"], ["inventory-adjustments", "create"], ["inventory-adjustments", "approve"], ["inventory-adjustments", "apply"],
        ["warehouse-transfers", "read"], ["warehouse-transfers", "create"], ["warehouse-transfers", "update"],
        ["stocktake", "read"], ["stocktake", "create"], ["stocktake", "update"], ["stocktake", "finalize"]
    ];

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public DatabaseSeeder(
        AuthDbContext context,
        IPasswordHasher passwordHasher,
        IFeatureManager featureManager,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _featureManager = featureManager;
        _logger = logger;
    }

    /// <summary>
    /// Ensures the Admin role, default permissions, and admin user exist in the database.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        bool seedingEnabled = await _featureManager.IsEnabledAsync(FeatureFlags.EnableDatabaseSeeding).ConfigureAwait(false);
        if (!seedingEnabled)
        {
            _logger.LogInformation("Database seeding is disabled by feature flag {Flag}", FeatureFlags.EnableDatabaseSeeding);
            return;
        }

        Role adminRole = await SeedAdminRoleAsync(cancellationToken).ConfigureAwait(false);
        await SeedPermissionsAsync(adminRole, cancellationToken).ConfigureAwait(false);
        await SeedAdminUserAsync(adminRole, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Role> SeedAdminRoleAsync(CancellationToken cancellationToken)
    {
        Role? adminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Admin" && r.IsSystem, cancellationToken)
            .ConfigureAwait(false);

        if (adminRole is not null)
            return adminRole;

        adminRole = new Role
        {
            Name = "Admin",
            Description = "System administrator with full permissions.",
            IsSystem = true
        };

        _context.Roles.Add(adminRole);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Seeded Admin role with ID {RoleId}", adminRole.Id);

        return adminRole;
    }

    private async Task SeedPermissionsAsync(Role adminRole, CancellationToken cancellationToken)
    {
        foreach (string[] pair in DefaultPermissions)
        {
            string resource = pair[0];
            string action = pair[1];

            bool exists = await _context.Permissions
                .AnyAsync(p => p.Resource == resource && p.Action == action, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
                continue;

            Permission permission = new() { Resource = resource, Action = action };
            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            bool alreadyAssigned = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id, cancellationToken)
                .ConfigureAwait(false);

            if (!alreadyAssigned)
                _context.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = permission.Id });
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Seeded {Count} default permissions for Admin role", DefaultPermissions.Length);
    }

    private async Task SeedAdminUserAsync(Role adminRole, CancellationToken cancellationToken)
    {
        bool adminSeedEnabled = await _featureManager.IsEnabledAsync(FeatureFlags.EnableSeedDefaultAdmin).ConfigureAwait(false);
        if (!adminSeedEnabled)
        {
            _logger.LogInformation("Default admin user seeding is disabled by feature flag {Flag}", FeatureFlags.EnableSeedDefaultAdmin);
            return;
        }

        bool adminUserExists = await _context.Users
            .AnyAsync(u => u.Username == "admin", cancellationToken)
            .ConfigureAwait(false);

        if (adminUserExists)
            return;

        User adminUser = new()
        {
            Username = "admin",
            Email = "admin@warehouse.local",
            PasswordHash = _passwordHasher.Hash("Admin123!"),
            FirstName = "System",
            LastName = "Administrator"
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _context.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Seeded admin user with ID {UserId}", adminUser.Id);
    }
}
