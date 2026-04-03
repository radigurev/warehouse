using Microsoft.EntityFrameworkCore;
using Warehouse.Auth.API.Interfaces;
using Warehouse.DBModel;
using Warehouse.DBModel.Models.Auth;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// Seeds required initial data into the database on application startup.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly WarehouseDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public DatabaseSeeder(WarehouseDbContext context, IPasswordHasher passwordHasher, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Ensures the Admin role and default admin user exist in the database.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        Role adminRole = await SeedAdminRoleAsync(cancellationToken).ConfigureAwait(false);
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

    private async Task SeedAdminUserAsync(Role adminRole, CancellationToken cancellationToken)
    {
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
