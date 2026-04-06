namespace Warehouse.Infrastructure.Configuration;

/// <summary>
/// Centralized feature flag name constants. Add new flags here as needed.
/// Flags are defined in <c>appsettings.json</c> under the <c>FeatureManagement</c> section.
/// </summary>
public static class FeatureFlags
{
    /// <summary>
    /// Gates the entire database seeding process on application startup.
    /// </summary>
    public const string EnableDatabaseSeeding = "EnableDatabaseSeeding";

    /// <summary>
    /// Gates the creation of the default admin user during database seeding.
    /// </summary>
    public const string EnableSeedDefaultAdmin = "EnableSeedDefaultAdmin";
}
