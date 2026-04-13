using MassTransit;
using Microsoft.Extensions.Logging;

namespace Warehouse.Infrastructure.Messaging;

/// <summary>
/// Wraps <see cref="IPublishEndpoint"/> with try-catch resilience.
/// RabbitMQ unavailability is logged as a warning and never propagated
/// to the caller (fire-and-forget semantics).
/// <para>See <see cref="IResilientPublisher"/>.</para>
/// </summary>
public sealed class ResilientPublisher : IResilientPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ResilientPublisher> _logger;

    /// <summary>
    /// Initializes a new instance with the MassTransit publish endpoint and logger.
    /// </summary>
    public ResilientPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<ResilientPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : class
    {
        try
        {
            await _publishEndpoint.Publish(@event, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            string eventTypeName = typeof(TEvent).Name;
            _logger.LogWarning(ex, "Failed to publish event {EventType}", eventTypeName);
        }
    }
}
