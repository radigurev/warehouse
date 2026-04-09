using MassTransit;
using Microsoft.EntityFrameworkCore;
using Warehouse.EventLog.DBModel;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.Events;

namespace Warehouse.EventLog.API.Consumers;

/// <summary>
/// Consumes Customer Management domain operations events and persists them as CustomerEvent records.
/// </summary>
public sealed class CustomerEventOccurredEventConsumer : IConsumer<CustomerEventOccurredEvent>
{
    private readonly EventLogDbContext _context;
    private readonly ILogger<CustomerEventOccurredEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CustomerEventOccurredEventConsumer(EventLogDbContext context, ILogger<CustomerEventOccurredEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<CustomerEventOccurredEvent> context)
    {
        CustomerEventOccurredEvent message = context.Message;

        try
        {
            bool isDuplicate = await _context.CustomerEvents.AnyAsync(e =>
                e.Domain == "Customers" &&
                e.EventType == message.EventType &&
                e.EntityType == message.EntityType &&
                e.EntityId == message.EntityId &&
                e.OccurredAtUtc == message.OccurredAtUtc).ConfigureAwait(false);

            if (isDuplicate)
            {
                _logger.LogDebug("Duplicate CustomerEventOccurredEvent skipped: {EventType} {EntityType}:{EntityId} at {OccurredAtUtc}",
                    message.EventType, message.EntityType, message.EntityId, message.OccurredAtUtc);
                return;
            }

            CustomerEvent customerEvent = new()
            {
                Domain = "Customers",
                EventType = message.EventType,
                EntityType = message.EntityType,
                EntityId = message.EntityId,
                UserId = message.UserId,
                OccurredAtUtc = message.OccurredAtUtc,
                ReceivedAtUtc = DateTime.UtcNow,
                Payload = message.Payload,
                CorrelationId = message.CorrelationId,
                CustomerName = message.CustomerName,
                CustomerCode = message.CustomerCode
            };

            _context.CustomerEvents.Add(customerEvent);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Persisted CustomerEvent: {EventType} {EntityType}:{EntityId}",
                message.EventType, message.EntityType, message.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist CustomerEventOccurredEvent: {EventType} {EntityType}:{EntityId}. Payload: {Payload}",
                message.EventType, message.EntityType, message.EntityId, message.Payload);
        }
    }
}
