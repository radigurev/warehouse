namespace Warehouse.ServiceModel.DTOs.Nomenclature;

/// <summary>
/// Represents a currency for financial reference (ISO 4217).
/// </summary>
public sealed record CurrencyDto
{
    /// <summary>
    /// Gets the currency ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the ISO 4217 currency code (e.g., USD, EUR, BGN).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the currency name (e.g., US Dollar).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the currency symbol (e.g., $, EUR).
    /// </summary>
    public string? Symbol { get; init; }

    /// <summary>
    /// Gets whether the currency is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; init; }
}
