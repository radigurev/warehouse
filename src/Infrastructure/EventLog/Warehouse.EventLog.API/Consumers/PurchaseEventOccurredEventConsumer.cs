using MassTransit;
using Microsoft.EntityFrameworkCore;
using Warehouse.EventLog.DBModel;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.Events;

namespace Warehouse.EventLog.API.Consumers;

/// <summary>
/// Consumes Purchasing domain operations events and persists them as PurchaseEvent records.
/// </summary>
public sealed class PurchaseEventOccurredEventConsumer : IConsumer<PurchaseEventOccurredEvent>
{
    private readonly EventLogDbContext _context;
    private readonly ILogger<PurchaseEventOccurredEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public PurchaseEventOccurredEventConsumer(EventLogDbContext context, ILogger<PurchaseEventOccurredEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<PurchaseEventOccurredEvent> context)
    {
        PurchaseEventOccurredEvent message = context.Message;

        try
        {
            bool isDuplicate = await _context.PurchaseEvents.AnyAsync(e =>
                e.Domain == "Purchasing" &&
                e.EventType == message.EventType &&
                e.EntityType == message.EntityType &&
                e.EntityId == message.EntityId &&
                e.OccurredAtUtc == message.OccurredAtUtc).ConfigureAwait(false);

            if (isDuplicate)
            {
                _logger.LogDebug("Duplicate PurchaseEventOccurredEvent skipped: {EventType} {EntityType}:{EntityId} at {OccurredAtUtc}",
                    message.EventType, message.EntityType, message.EntityId, message.OccurredAtUtc);
                return;
            }

            PurchaseEvent purchaseEvent = new()
            {
                Domain = "Purchasing",
                EventType = message.EventType,
                EntityType = message.EntityType,
                EntityId = message.EntityId,
                UserId = message.UserId,
                OccurredAtUtc = message.OccurredAtUtc,
                ReceivedAtUtc = DateTime.UtcNow,
                Payload = message.Payload,
                CorrelationId = message.CorrelationId,
                SupplierName = message.SupplierName,
                DocumentNumber = message.DocumentNumber
            };

            _context.PurchaseEvents.Add(purchaseEvent);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Persisted PurchaseEvent: {EventType} {EntityType}:{EntityId}",
                message.EventType, message.EntityType, message.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist PurchaseEventOccurredEvent: {EventType} {EntityType}:{EntityId}. Payload: {Payload}",
                message.EventType, message.EntityType, message.EntityId, message.Payload);
        }
    }
}
