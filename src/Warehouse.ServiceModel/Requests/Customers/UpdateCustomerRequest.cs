namespace Warehouse.ServiceModel.Requests.Customers;

/// <summary>
/// Request payload for updating an existing customer.
/// </summary>
public sealed record UpdateCustomerRequest
{
    /// <summary>
    /// Gets the customer name. Required, 1-200 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the customer name in native language. Optional, max 200 characters.
    /// </summary>
    public string? NativeLanguageName { get; init; }

    /// <summary>
    /// Gets the tax identification number. Optional, 1-50 characters when provided.
    /// </summary>
    public string? TaxId { get; init; }

    /// <summary>
    /// Gets the customer category ID. Optional; must reference an existing category when provided.
    /// </summary>
    public int? CategoryId { get; init; }

    /// <summary>
    /// Gets the customer notes. Optional, max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }
}
