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

    /// <summary>
    /// Gates the entire nomenclature database seeding process on application startup.
    /// </summary>
    public const string EnableNomenclatureSeeding = "EnableNomenclatureSeeding";

    /// <summary>
    /// Gates country (and state/province, city) seeding within nomenclature.
    /// </summary>
    public const string EnableSeedCountries = "EnableSeedCountries";

    /// <summary>
    /// Gates currency seeding within nomenclature.
    /// </summary>
    public const string EnableSeedCurrencies = "EnableSeedCurrencies";

    /// <summary>
    /// Gates state/province seeding within nomenclature.
    /// </summary>
    public const string EnableSeedStateProvinces = "EnableSeedStateProvinces";

    /// <summary>
    /// Gates city seeding within nomenclature (Bulgaria only).
    /// </summary>
    public const string EnableSeedCities = "EnableSeedCities";

    /// <summary>
    /// Gates backend validation of CountryCode and CurrencyCode against the
    /// Nomenclature Redis cache in consumer services (Customers, Purchasing, Fulfillment).
    /// When disabled, only format-level validation runs.
    /// </summary>
    public const string EnableNomenclatureValidation = "EnableNomenclatureValidation";
}
