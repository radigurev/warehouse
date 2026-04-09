namespace Warehouse.ServiceModel.DTOs.EventLog;

/// <summary>
/// DTO for Customer Management domain operations events with customer-specific fields.
/// </summary>
public sealed record CustomerEventDto : OperationsEventDto
{
    /// <summary>
    /// Gets the denormalized customer name for display.
    /// </summary>
    public string? CustomerName { get; init; }

    /// <summary>
    /// Gets the denormalized customer code for display.
    /// </summary>
    public string? CustomerCode { get; init; }
}
