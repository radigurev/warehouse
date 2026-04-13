using Warehouse.EventLog.DBModel.Models;

namespace Warehouse.EventLog.API.Factories;

/// <summary>
/// Maps domain strings to the correct <see cref="OperationsEvent"/> subclass.
/// <para>Returns null and logs a warning when the domain is not recognized.</para>
/// <para>See also: <see cref="IOperationsEventFactory"/>, <see cref="AuthEvent"/>,
/// <see cref="CustomerEvent"/>, <see cref="InventoryEvent"/>,
/// <see cref="PurchaseEvent"/>, <see cref="FulfillmentEvent"/>.</para>
/// </summary>
public sealed class OperationsEventFactory : IOperationsEventFactory
{
    private readonly ILogger<OperationsEventFactory> _logger;

    /// <summary>
    /// Initializes a new instance with the specified logger.
    /// </summary>
    public OperationsEventFactory(ILogger<OperationsEventFactory> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public OperationsEvent? CreateFrom(
        string domain,
        string eventType,
        string entityType,
        int entityId,
        DateTime occurredAtUtc,
        string? payload)
    {
        OperationsEvent? operationsEvent = CreateSubclass(domain, eventType, entityType);

        if (operationsEvent is null)
        {
            _logger.LogWarning(
                "Unknown domain '{Domain}' received — event skipped. EventType={EventType}, EntityType={EntityType}, EntityId={EntityId}",
                domain, eventType, entityType, entityId);
            return null;
        }

        operationsEvent.EntityId = entityId;
        operationsEvent.OccurredAtUtc = occurredAtUtc;
        operationsEvent.ReceivedAtUtc = DateTime.UtcNow;
        operationsEvent.Payload = payload;

        return operationsEvent;
    }

    /// <summary>
    /// Creates a subclass instance with base required fields populated, or null if the domain is unrecognized.
    /// </summary>
    private static OperationsEvent? CreateSubclass(string domain, string eventType, string entityType)
    {
        return domain switch
        {
            "Auth" => new AuthEvent
            {
                Domain = domain,
                EventType = eventType,
                EntityType = entityType,
                Action = string.Empty,
                Resource = string.Empty
            },
            "Customer" => new CustomerEvent
            {
                Domain = domain,
                EventType = eventType,
                EntityType = entityType
            },
            "Inventory" => new InventoryEvent
            {
                Domain = domain,
                EventType = eventType,
                EntityType = entityType
            },
            "Purchasing" => new PurchaseEvent
            {
                Domain = domain,
                EventType = eventType,
                EntityType = entityType
            },
            "Fulfillment" => new FulfillmentEvent
            {
                Domain = domain,
                EventType = eventType,
                EntityType = entityType
            },
            _ => null
        };
    }
}
