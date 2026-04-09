using MassTransit;
using Microsoft.EntityFrameworkCore;
using Warehouse.EventLog.DBModel;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.Events;

namespace Warehouse.EventLog.API.Consumers;

/// <summary>
/// Consumes Inventory domain operations events and persists them as InventoryEvent records.
/// </summary>
public sealed class InventoryEventOccurredEventConsumer : IConsumer<InventoryEventOccurredEvent>
{
    private readonly EventLogDbContext _context;
    private readonly ILogger<InventoryEventOccurredEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public InventoryEventOccurredEventConsumer(EventLogDbContext context, ILogger<InventoryEventOccurredEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<InventoryEventOccurredEvent> context)
    {
        InventoryEventOccurredEvent message = context.Message;

        try
        {
            bool isDuplicate = await _context.InventoryEvents.AnyAsync(e =>
                e.Domain == "Inventory" &&
                e.EventType == message.EventType &&
                e.EntityType == message.EntityType &&
                e.EntityId == message.EntityId &&
                e.OccurredAtUtc == message.OccurredAtUtc).ConfigureAwait(false);

            if (isDuplicate)
            {
                _logger.LogDebug("Duplicate InventoryEventOccurredEvent skipped: {EventType} {EntityType}:{EntityId} at {OccurredAtUtc}",
                    message.EventType, message.EntityType, message.EntityId, message.OccurredAtUtc);
                return;
            }

            InventoryEvent inventoryEvent = new()
            {
                Domain = "Inventory",
                EventType = message.EventType,
                EntityType = message.EntityType,
                EntityId = message.EntityId,
                UserId = message.UserId,
                OccurredAtUtc = message.OccurredAtUtc,
                ReceivedAtUtc = DateTime.UtcNow,
                Payload = message.Payload,
                CorrelationId = message.CorrelationId,
                WarehouseName = message.WarehouseName,
                ProductInfo = message.ProductInfo
            };

            _context.InventoryEvents.Add(inventoryEvent);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Persisted InventoryEvent: {EventType} {EntityType}:{EntityId}",
                message.EventType, message.EntityType, message.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist InventoryEventOccurredEvent: {EventType} {EntityType}:{EntityId}. Payload: {Payload}",
                message.EventType, message.EntityType, message.EntityId, message.Payload);
        }
    }
}
