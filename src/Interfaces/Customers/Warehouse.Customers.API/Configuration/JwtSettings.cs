namespace Warehouse.Customers.API.Configuration;

/// <summary>
/// Strongly-typed JWT configuration bound from appsettings.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// Gets or sets the configuration section name.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets or sets the HMAC-SHA256 secret key.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Gets or sets the token issuer.
    /// </summary>
    public required string Issuer { get; set; }

    /// <summary>
    /// Gets or sets the token audience.
    /// </summary>
    public required string Audience { get; set; }

    /// <summary>
    /// Gets or sets the access token expiration in minutes.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the refresh token expiration in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
