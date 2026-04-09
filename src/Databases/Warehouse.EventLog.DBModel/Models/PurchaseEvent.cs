namespace Warehouse.EventLog.DBModel.Models;

/// <summary>
/// Operations event for the Procurement domain.
/// Extends the base with purchasing-specific fields.
/// </summary>
public class PurchaseEvent : OperationsEvent
{
    /// <summary>
    /// Gets or sets the denormalized supplier name for display.
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// Gets or sets the document reference (PO number, GR number, SR number).
    /// </summary>
    public string? DocumentNumber { get; set; }
}
