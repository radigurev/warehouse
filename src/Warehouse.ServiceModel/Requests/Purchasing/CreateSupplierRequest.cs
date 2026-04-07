namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for creating a new supplier.
/// </summary>
public sealed record CreateSupplierRequest
{
    /// <summary>
    /// Gets the supplier name. Required, 1-200 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the supplier code. Optional; auto-generated if omitted. 1-20 characters, alphanumeric and hyphens.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets the tax identification number. Optional, 1-50 characters when provided.
    /// </summary>
    public string? TaxId { get; init; }

    /// <summary>
    /// Gets the supplier category ID. Optional; must reference an existing category when provided.
    /// </summary>
    public int? CategoryId { get; init; }

    /// <summary>
    /// Gets the payment term in days. Optional, 0-365 when provided.
    /// </summary>
    public int? PaymentTermDays { get; init; }

    /// <summary>
    /// Gets the supplier notes. Optional, max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }
}
