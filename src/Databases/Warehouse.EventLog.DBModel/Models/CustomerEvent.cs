namespace Warehouse.EventLog.DBModel.Models;

/// <summary>
/// Operations event for the Customer Management domain.
/// Extends the base with customer-specific fields.
/// </summary>
public class CustomerEvent : OperationsEvent
{
    /// <summary>
    /// Gets or sets the denormalized customer name for display.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the denormalized customer code for display.
    /// </summary>
    public string? CustomerCode { get; set; }
}
