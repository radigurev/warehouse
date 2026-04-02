using Microsoft.EntityFrameworkCore;
using Warehouse.DBModel;
using Warehouse.DBModel.Models.Auth;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// Seeds required initial data into the database on application startup.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly WarehouseDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public DatabaseSeeder(WarehouseDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ensures the Admin role exists in the database.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        bool adminExists = await _context.Roles
            .AnyAsync(r => r.Name == "Admin" && r.IsSystem, cancellationToken)
            .ConfigureAwait(false);

        if (!adminExists)
        {
            Role adminRole = new()
            {
                Name = "Admin",
                Description = "System administrator with full permissions.",
                IsSystem = true
            };

            _context.Roles.Add(adminRole);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Seeded Admin role with ID {RoleId}", adminRole.Id);
        }
    }
}
