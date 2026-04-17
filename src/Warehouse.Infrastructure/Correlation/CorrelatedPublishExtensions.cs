using MassTransit;
using Warehouse.ServiceModel.Events;

namespace Warehouse.Infrastructure.Correlation;

/// <summary>
/// Extension methods for publishing <see cref="ICorrelatedEvent"/> instances with automatic
/// correlation ID population from the current HTTP request context.
/// </summary>
public static class CorrelatedPublishExtensions
{
    /// <summary>
    /// Sets the <see cref="ICorrelatedEvent.CorrelationId"/> from the accessor and publishes the event.
    /// </summary>
    /// <typeparam name="T">The correlated event contract type.</typeparam>
    /// <param name="endpoint">The MassTransit publish endpoint.</param>
    /// <param name="event">The event instance to publish.</param>
    /// <param name="accessor">The correlation ID accessor for the current request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task PublishWithCorrelationAsync<T>(
        this IPublishEndpoint endpoint,
        T @event,
        ICorrelationIdAccessor accessor,
        CancellationToken cancellationToken)
        where T : class, ICorrelatedEvent
    {
        @event.CorrelationId = accessor.CorrelationId;
        return endpoint.Publish(@event, cancellationToken);
    }
}
