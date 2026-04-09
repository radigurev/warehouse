# CHG-REFAC-008 — Factory Pattern for Generic EventLog Consumer

> Status: Draft
> Last updated: 2026-04-09
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**
The EventLog service has 5 nearly identical MassTransit consumers (`AuthAuditLoggedEventConsumer`, `CustomerEventOccurredEventConsumer`, `InventoryEventOccurredEventConsumer`, `FulfillmentEventOccurredEventConsumer`, `PurchaseEventOccurredEventConsumer`). Each duplicates the same logic: deduplication check, entity creation, persistence, and error handling — differing only in the domain name and the concrete `OperationsEvent` subclass they create. When a new domain is added (e.g., Production, Quality), a new consumer class must be manually created with copied boilerplate. The Factory pattern replaces this with a single generic consumer that delegates entity creation to a factory.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### Generic Consumer

- The system MUST provide a single `GenericDomainEventConsumer<TEvent>` that handles all domain event types inheriting from a common base contract.
- The consumer MUST perform deduplication using the existing check: domain + eventType + entityType + entityId + occurredAtUtc.
- The consumer MUST delegate entity creation to an `IOperationsEventFactory` that maps event contracts to `OperationsEvent` subclass instances.
- The consumer MUST wrap persistence in try-catch with structured logging — identical to current behavior.
- The consumer MUST call `SaveChangesAsync` after adding the entity.

### Event Factory

- The system MUST define an `IOperationsEventFactory` interface with method: `OperationsEvent CreateFrom<TEvent>(TEvent @event)`.
- The factory MUST map each event contract type to the correct `OperationsEvent` subclass (`AuthEvent`, `CustomerEvent`, `InventoryEvent`, `PurchaseEvent`, `FulfillmentEvent`).
- Adding a new domain event MUST require only: creating a new `OperationsEvent` subclass (if not already present) and registering the mapping in the factory — no new consumer class needed.
- The factory MUST extract common fields (Domain, EventType, EntityType, EntityId, OccurredAtUtc, Payload) from the event contract.

### Consumer Registration

- MassTransit MUST register the generic consumer for each event contract type.
- The 5 existing consumer classes MUST be deleted after migration.
- The existing queue/exchange topology MUST remain unchanged to avoid message loss during deployment.

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | Event contract | Factory MUST resolve a mapping for the event type | *(logged warning, not user-facing)* |
| V2 | Deduplication | Duplicate events MUST be silently skipped | *(no error — idempotent)* |

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Unknown event type | N/A (consumer) | *(logged warning)* | Unknown event type {type} — skipped. |
| E2 | Persistence failure | N/A (consumer) | *(logged error)* | Failed to persist event {type} for {entityId}. |

## 5. Versioning Notes

**API version impact:** None — consumers are internal.

**Database migration required:** No

**Backwards compatibility:** Fully compatible — event contracts and queue topology unchanged.

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] OperationsEventFactory_CreatesAuthEvent_FromAuthAuditLoggedEvent` — verify mapping
- [ ] `[Unit] OperationsEventFactory_CreatesInventoryEvent_FromInventoryEventOccurredEvent` — verify mapping
- [ ] `[Unit] OperationsEventFactory_CreatesCustomerEvent_FromCustomerEventOccurredEvent` — verify mapping
- [ ] `[Unit] OperationsEventFactory_CreatesPurchaseEvent_FromPurchaseEventOccurredEvent` — verify mapping
- [ ] `[Unit] OperationsEventFactory_CreatesFulfillmentEvent_FromFulfillmentEventOccurredEvent` — verify mapping
- [ ] `[Unit] GenericConsumer_SkipsDuplicateEvent` — verify deduplication
- [ ] `[Unit] GenericConsumer_PersistsNewEvent` — verify persistence
- [ ] `[Unit] GenericConsumer_LogsWarning_OnPersistenceFailure` — verify error handling

### Integration Tests

- [ ] `[Integration] GenericConsumer_ReceivesAndPersists_InventoryEvent` — end-to-end with MassTransit test harness
- [ ] `[Integration] GenericConsumer_ReceivesAndPersists_AuthEvent` — end-to-end
- [ ] `[Integration] GenericConsumer_Deduplicates_OnRetry` — verify idempotency

## 7. Detailed Design

### Factory Interface

```csharp
// src/Infrastructure/EventLog/Warehouse.EventLog.API/Factories/IOperationsEventFactory.cs
public interface IOperationsEventFactory
{
    OperationsEvent Create<TEvent>(TEvent @event) where TEvent : class;
}
```

### Generic Consumer

```csharp
// src/Infrastructure/EventLog/Warehouse.EventLog.API/Consumers/GenericDomainEventConsumer.cs
public sealed class GenericDomainEventConsumer<TEvent> : IConsumer<TEvent>
    where TEvent : class
{
    private readonly EventLogDbContext _context;
    private readonly IOperationsEventFactory _factory;
    private readonly ILogger<GenericDomainEventConsumer<TEvent>> _logger;

    public async Task Consume(ConsumeContext<TEvent> context)
    {
        try
        {
            OperationsEvent entity = _factory.Create(context.Message);

            bool isDuplicate = await _context.OperationsEvents.AnyAsync(e =>
                e.Domain == entity.Domain &&
                e.EventType == entity.EventType &&
                e.EntityType == entity.EntityType &&
                e.EntityId == entity.EntityId &&
                e.OccurredAtUtc == entity.OccurredAtUtc);

            if (isDuplicate) return;

            _context.OperationsEvents.Add(entity);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist {EventType}", typeof(TEvent).Name);
        }
    }
}
```

### Files Deleted

- `Consumers/AuthAuditLoggedEventConsumer.cs`
- `Consumers/CustomerEventOccurredEventConsumer.cs`
- `Consumers/InventoryEventOccurredEventConsumer.cs`
- `Consumers/FulfillmentEventOccurredEventConsumer.cs`
- `Consumers/PurchaseEventOccurredEventConsumer.cs`

### Files Created

- `Consumers/GenericDomainEventConsumer.cs`
- `Factories/IOperationsEventFactory.cs`
- `Factories/OperationsEventFactory.cs`

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-INFRA-002 (EventLog) | Consumer architecture changed from 5 specific to 1 generic — behavior identical |

## Migration Plan

1. **Pre-deployment:** Implement factory and generic consumer. Register alongside existing consumers. Verify both paths process events correctly. Then remove old consumers.
2. **Deployment:** Deploy updated assemblies. Ensure RabbitMQ queue bindings match new consumer registrations.
3. **Post-deployment:** Verify events from all 5 domains appear in the OperationsEvents table.
4. **Rollback:** Revert to previous commit — restore 5 individual consumers.

## Open Questions

- [ ] Should the factory use a dictionary-based registry or pattern matching for event type resolution?
- [ ] Should the generic consumer share queues with the old consumers during migration, or use new queues?
