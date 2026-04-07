namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Represents a supplier phone number with type and contact details.
/// </summary>
public sealed record SupplierPhoneDto
{
    /// <summary>
    /// Gets the phone entry ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the phone type (Mobile, Landline, or Fax).
    /// </summary>
    public required string PhoneType { get; init; }

    /// <summary>
    /// Gets the phone number.
    /// </summary>
    public required string PhoneNumber { get; init; }

    /// <summary>
    /// Gets the phone extension.
    /// </summary>
    public string? Extension { get; init; }

    /// <summary>
    /// Gets whether this is the primary phone for the supplier.
    /// </summary>
    public required bool IsPrimary { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
