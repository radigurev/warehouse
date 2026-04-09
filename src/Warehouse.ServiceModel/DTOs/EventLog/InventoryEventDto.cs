namespace Warehouse.ServiceModel.DTOs.EventLog;

/// <summary>
/// DTO for Inventory domain operations events with inventory-specific fields.
/// </summary>
public sealed record InventoryEventDto : OperationsEventDto
{
    /// <summary>
    /// Gets the denormalized warehouse name for display.
    /// </summary>
    public string? WarehouseName { get; init; }

    /// <summary>
    /// Gets the denormalized product code and name for display.
    /// </summary>
    public string? ProductInfo { get; init; }
}
