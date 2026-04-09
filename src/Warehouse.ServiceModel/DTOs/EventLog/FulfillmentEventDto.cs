namespace Warehouse.ServiceModel.DTOs.EventLog;

/// <summary>
/// DTO for Fulfillment domain operations events with fulfillment-specific fields.
/// </summary>
public sealed record FulfillmentEventDto : OperationsEventDto
{
    /// <summary>
    /// Gets the denormalized customer name for display.
    /// </summary>
    public string? CustomerName { get; init; }

    /// <summary>
    /// Gets the document reference (SO number, PL number, SH number, RMA number).
    /// </summary>
    public string? DocumentNumber { get; init; }
}
