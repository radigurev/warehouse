namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for creating a new supplier email entry.
/// </summary>
public sealed record CreateSupplierEmailRequest
{
    /// <summary>
    /// Gets the email type. Required. One of: General, Billing, Support.
    /// </summary>
    public required string EmailType { get; init; }

    /// <summary>
    /// Gets the email address. Required, valid email format, max 256 characters.
    /// </summary>
    public required string EmailAddress { get; init; }
}
