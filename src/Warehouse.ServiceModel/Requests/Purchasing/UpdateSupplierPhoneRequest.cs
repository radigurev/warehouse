namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for updating an existing supplier phone entry.
/// </summary>
public sealed record UpdateSupplierPhoneRequest
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
    /// Gets whether this should be the primary phone.
    /// </summary>
    public bool IsPrimary { get; init; }
}
