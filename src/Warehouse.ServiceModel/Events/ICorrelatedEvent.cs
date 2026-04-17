namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Marker interface for MassTransit event contracts that carry a correlation ID
/// from the originating HTTP request for end-to-end traceability.
/// </summary>
public interface ICorrelatedEvent
{
    /// <summary>
    /// Gets or sets the correlation ID from the originating HTTP request.
    /// <c>null</c> when the event originates from a non-HTTP context (background tasks, consumers).
    /// </summary>
    string? CorrelationId { get; set; }
}
