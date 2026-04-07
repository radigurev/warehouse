namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Full supplier representation including contacts and category details.
/// </summary>
public sealed record SupplierDetailDto
{
    /// <summary>
    /// Gets the supplier ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the unique supplier code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the supplier name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the tax identification number.
    /// </summary>
    public string? TaxId { get; init; }

    /// <summary>
    /// Gets the assigned supplier category ID.
    /// </summary>
    public int? CategoryId { get; init; }

    /// <summary>
    /// Gets the name of the assigned supplier category.
    /// </summary>
    public string? CategoryName { get; init; }

    /// <summary>
    /// Gets the payment term in days.
    /// </summary>
    public int? PaymentTermDays { get; init; }

    /// <summary>
    /// Gets the supplier notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets whether the supplier is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of supplier addresses.
    /// </summary>
    public required IReadOnlyList<SupplierAddressDto> Addresses { get; init; }

    /// <summary>
    /// Gets the collection of supplier phone numbers.
    /// </summary>
    public required IReadOnlyList<SupplierPhoneDto> Phones { get; init; }

    /// <summary>
    /// Gets the collection of supplier email addresses.
    /// </summary>
    public required IReadOnlyList<SupplierEmailDto> Emails { get; init; }
}
