namespace Warehouse.ServiceModel.DTOs.Customers;

/// <summary>
/// Lightweight customer representation for list views.
/// </summary>
public sealed record CustomerDto
{
    /// <summary>
    /// Gets the customer ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the unique customer code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the customer name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the tax identification number.
    /// </summary>
    public string? TaxId { get; init; }

    /// <summary>
    /// Gets the name of the assigned customer category.
    /// </summary>
    public string? CategoryName { get; init; }

    /// <summary>
    /// Gets whether the customer is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
