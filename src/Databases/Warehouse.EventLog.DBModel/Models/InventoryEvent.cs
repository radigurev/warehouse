namespace Warehouse.EventLog.DBModel.Models;

/// <summary>
/// Operations event for the Inventory domain.
/// Extends the base with inventory-specific fields.
/// </summary>
public class InventoryEvent : OperationsEvent
{
    /// <summary>
    /// Gets or sets the denormalized warehouse name for display.
    /// </summary>
    public string? WarehouseName { get; set; }

    /// <summary>
    /// Gets or sets the denormalized product code and name for display.
    /// </summary>
    public string? ProductInfo { get; set; }
}
