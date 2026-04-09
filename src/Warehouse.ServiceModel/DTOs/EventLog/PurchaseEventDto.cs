namespace Warehouse.ServiceModel.DTOs.EventLog;

/// <summary>
/// DTO for Purchasing domain operations events with procurement-specific fields.
/// </summary>
public sealed record PurchaseEventDto : OperationsEventDto
{
    /// <summary>
    /// Gets the denormalized supplier name for display.
    /// </summary>
    public string? SupplierName { get; init; }

    /// <summary>
    /// Gets the document reference (PO number, GR number, SR number).
    /// </summary>
    public string? DocumentNumber { get; init; }
}
