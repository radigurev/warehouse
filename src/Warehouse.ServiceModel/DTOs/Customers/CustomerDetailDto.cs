namespace Warehouse.ServiceModel.DTOs.Customers;

/// <summary>
/// Full customer representation including contacts, accounts, and category details.
/// </summary>
public sealed record CustomerDetailDto
{
    /// <summary>
    /// Gets the customer ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the unique customer code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the customer name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the customer name in native language.
    /// </summary>
    public string? NativeLanguageName { get; init; }

    /// <summary>
    /// Gets the tax identification number.
    /// </summary>
    public string? TaxId { get; init; }

    /// <summary>
    /// Gets the name of the assigned customer category.
    /// </summary>
    public string? CategoryName { get; init; }

    /// <summary>
    /// Gets whether the customer is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the customer notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the assigned customer category ID.
    /// </summary>
    public int? CategoryId { get; init; }

    /// <summary>
    /// Gets the collection of customer addresses.
    /// </summary>
    public required IReadOnlyList<CustomerAddressDto> Addresses { get; init; }

    /// <summary>
    /// Gets the collection of customer phone numbers.
    /// </summary>
    public required IReadOnlyList<CustomerPhoneDto> Phones { get; init; }

    /// <summary>
    /// Gets the collection of customer email addresses.
    /// </summary>
    public required IReadOnlyList<CustomerEmailDto> Emails { get; init; }

    /// <summary>
    /// Gets the collection of customer accounts.
    /// </summary>
    public required IReadOnlyList<CustomerAccountDto> Accounts { get; init; }
}
