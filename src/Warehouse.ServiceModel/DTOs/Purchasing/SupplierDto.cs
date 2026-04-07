namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Lightweight supplier representation for list views.
/// </summary>
public sealed record SupplierDto
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
    /// Gets the name of the assigned supplier category.
    /// </summary>
    public string? CategoryName { get; init; }

    /// <summary>
    /// Gets the payment term in days.
    /// </summary>
    public int? PaymentTermDays { get; init; }

    /// <summary>
    /// Gets whether the supplier is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
