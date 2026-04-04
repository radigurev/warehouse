namespace Warehouse.ServiceModel.Requests.Customers;

/// <summary>
/// Request payload for updating an existing customer phone entry.
/// </summary>
public sealed record UpdatePhoneRequest
{
    /// <summary>
    /// Gets the phone type. Required. One of: Mobile, Landline, Fax.
    /// </summary>
    public required string PhoneType { get; init; }

    /// <summary>
    /// Gets the phone number. Required, 5-20 characters.
    /// </summary>
    public required string PhoneNumber { get; init; }

    /// <summary>
    /// Gets the phone extension. Optional, max 10 characters, digits only.
    /// </summary>
    public string? Extension { get; init; }

    /// <summary>
    /// Gets whether this phone should be marked as the primary phone for the customer.
    /// </summary>
    public required bool IsPrimary { get; init; }
}
