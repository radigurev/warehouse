namespace Warehouse.EventLog.DBModel.Models;

/// <summary>
/// Operations event for the Fulfillment domain.
/// Extends the base with fulfillment-specific fields.
/// </summary>
public class FulfillmentEvent : OperationsEvent
{
    /// <summary>
    /// Gets or sets the denormalized customer name for display.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the document reference (SO number, PL number, SH number, RMA number).
    /// </summary>
    public string? DocumentNumber { get; set; }
}
