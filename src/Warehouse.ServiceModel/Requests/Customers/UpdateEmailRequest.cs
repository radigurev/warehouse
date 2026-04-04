namespace Warehouse.ServiceModel.Requests.Customers;

/// <summary>
/// Request payload for updating an existing customer email entry.
/// </summary>
public sealed record UpdateEmailRequest
{
    /// <summary>
    /// Gets the email type. Required. One of: General, Billing, Support.
    /// </summary>
    public required string EmailType { get; init; }

    /// <summary>
    /// Gets the email address. Required, valid RFC 5322 format, max 256 characters.
    /// </summary>
    public required string EmailAddress { get; init; }

    /// <summary>
    /// Gets whether this email should be marked as the primary email for the customer.
    /// </summary>
    public required bool IsPrimary { get; init; }
}
