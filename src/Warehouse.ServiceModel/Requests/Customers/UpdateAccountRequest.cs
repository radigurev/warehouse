namespace Warehouse.ServiceModel.Requests.Customers;

/// <summary>
/// Request payload for updating an existing customer account.
/// </summary>
public sealed record UpdateAccountRequest
{
    /// <summary>
    /// Gets the account description. Optional, max 500 characters.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether this account should be marked as the primary account for the customer.
    /// </summary>
    public required bool IsPrimary { get; init; }
}
