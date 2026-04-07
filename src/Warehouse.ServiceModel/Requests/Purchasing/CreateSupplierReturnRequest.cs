namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for creating a new supplier return with lines.
/// </summary>
public sealed record CreateSupplierReturnRequest
{
    /// <summary>
    /// Gets the supplier ID. Required.
    /// </summary>
    public required int SupplierId { get; init; }

    /// <summary>
    /// Gets the return reason. Required, 1-500 characters.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the return notes. Optional, max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the collection of return lines. At least one required.
    /// </summary>
    public required IReadOnlyList<CreateSupplierReturnLineRequest> Lines { get; init; }
}
