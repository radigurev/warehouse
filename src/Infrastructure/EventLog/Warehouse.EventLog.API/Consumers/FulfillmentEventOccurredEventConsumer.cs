using MassTransit;
using Microsoft.EntityFrameworkCore;
using Warehouse.EventLog.DBModel;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.Events;

namespace Warehouse.EventLog.API.Consumers;

/// <summary>
/// Consumes Fulfillment domain operations events and persists them as FulfillmentEvent records.
/// </summary>
public sealed class FulfillmentEventOccurredEventConsumer : IConsumer<FulfillmentEventOccurredEvent>
{
    private readonly EventLogDbContext _context;
    private readonly ILogger<FulfillmentEventOccurredEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public FulfillmentEventOccurredEventConsumer(EventLogDbContext context, ILogger<FulfillmentEventOccurredEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<FulfillmentEventOccurredEvent> context)
    {
        FulfillmentEventOccurredEvent message = context.Message;

        try
        {
            bool isDuplicate = await _context.FulfillmentEvents.AnyAsync(e =>
                e.Domain == "Fulfillment" &&
                e.EventType == message.EventType &&
                e.EntityType == message.EntityType &&
                e.EntityId == message.EntityId &&
                e.OccurredAtUtc == message.OccurredAtUtc).ConfigureAwait(false);

            if (isDuplicate)
            {
                _logger.LogDebug("Duplicate FulfillmentEventOccurredEvent skipped: {EventType} {EntityType}:{EntityId} at {OccurredAtUtc}",
                    message.EventType, message.EntityType, message.EntityId, message.OccurredAtUtc);
                return;
            }

            FulfillmentEvent fulfillmentEvent = new()
            {
                Domain = "Fulfillment",
                EventType = message.EventType,
                EntityType = message.EntityType,
                EntityId = message.EntityId,
                UserId = message.UserId,
                OccurredAtUtc = message.OccurredAtUtc,
                ReceivedAtUtc = DateTime.UtcNow,
                Payload = message.Payload,
                CorrelationId = message.CorrelationId,
                CustomerName = message.CustomerName,
                DocumentNumber = message.DocumentNumber
            };

            _context.FulfillmentEvents.Add(fulfillmentEvent);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Persisted FulfillmentEvent: {EventType} {EntityType}:{EntityId}",
                message.EventType, message.EntityType, message.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist FulfillmentEventOccurredEvent: {EventType} {EntityType}:{EntityId}. Payload: {Payload}",
                message.EventType, message.EntityType, message.EntityId, message.Payload);
        }
    }
}
