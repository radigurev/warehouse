namespace Warehouse.Infrastructure.Messaging;

/// <summary>
/// Publishes domain events via the message bus with resilient fire-and-forget
/// semantics. Failures are logged but never propagated to the caller.
/// </summary>
public interface IResilientPublisher
{
    /// <summary>
    /// Publishes the specified event to the message bus.
    /// Silently catches and logs any failure so that message bus
    /// unavailability does not break the calling service's main operation.
    /// </summary>
    /// <typeparam name="TEvent">The event contract type.</typeparam>
    /// <param name="event">The event instance to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : class;
}
