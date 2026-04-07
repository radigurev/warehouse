namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Represents a supplier email address with type and contact details.
/// </summary>
public sealed record SupplierEmailDto
{
    /// <summary>
    /// Gets the email entry ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the email type (General, Billing, or Support).
    /// </summary>
    public required string EmailType { get; init; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public required string EmailAddress { get; init; }

    /// <summary>
    /// Gets whether this is the primary email for the supplier.
    /// </summary>
    public required bool IsPrimary { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
