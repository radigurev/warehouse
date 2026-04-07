namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for updating an existing supplier email entry.
/// </summary>
public sealed record UpdateSupplierEmailRequest
{
    /// <summary>
    /// Gets the email type. Required. One of: General, Billing, Support.
    /// </summary>
    public required string EmailType { get; init; }

    /// <summary>
    /// Gets the email address. Required, valid email format, max 256 characters.
    /// </summary>
    public required string EmailAddress { get; init; }

    /// <summary>
    /// Gets whether this should be the primary email.
    /// </summary>
    public bool IsPrimary { get; init; }
}
