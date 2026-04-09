using MassTransit;
using Microsoft.EntityFrameworkCore;
using Warehouse.EventLog.DBModel;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.Events;

namespace Warehouse.EventLog.API.Consumers;

/// <summary>
/// Consumes Auth domain audit events and persists them as AuthEvent records.
/// </summary>
public sealed class AuthAuditLoggedEventConsumer : IConsumer<AuthAuditLoggedEvent>
{
    private readonly EventLogDbContext _context;
    private readonly ILogger<AuthAuditLoggedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public AuthAuditLoggedEventConsumer(EventLogDbContext context, ILogger<AuthAuditLoggedEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AuthAuditLoggedEvent> context)
    {
        AuthAuditLoggedEvent message = context.Message;

        try
        {
            string entityType = message.Resource ?? "Unknown";
            string eventType = message.Action;
            int entityId = 0;
            int userId = message.UserId ?? 0;

            bool isDuplicate = await _context.AuthEvents.AnyAsync(e =>
                e.Domain == "Auth" &&
                e.EventType == eventType &&
                e.EntityType == entityType &&
                e.EntityId == entityId &&
                e.OccurredAtUtc == message.OccurredAtUtc).ConfigureAwait(false);

            if (isDuplicate)
            {
                _logger.LogDebug("Duplicate AuthAuditLoggedEvent skipped: {Action} {Resource} at {OccurredAtUtc}",
                    message.Action, message.Resource, message.OccurredAtUtc);
                return;
            }

            AuthEvent authEvent = new()
            {
                Domain = "Auth",
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                OccurredAtUtc = message.OccurredAtUtc,
                ReceivedAtUtc = DateTime.UtcNow,
                Payload = message.Details,
                CorrelationId = message.CorrelationId,
                Action = message.Action,
                Resource = message.Resource,
                IpAddress = message.IpAddress,
                Username = message.Username
            };

            _context.AuthEvents.Add(authEvent);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Persisted AuthEvent: {Action} on {Resource} by user {UserId}",
                message.Action, message.Resource, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist AuthAuditLoggedEvent: {Action} {Resource}. Payload: {Details}",
                message.Action, message.Resource, message.Details);
        }
    }
}
