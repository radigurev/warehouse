using Warehouse.EventLog.DBModel.Models;

namespace Warehouse.EventLog.API.Factories;

/// <summary>
/// Creates domain-specific <see cref="OperationsEvent"/> subclass instances from raw event data.
/// <para>Used by the generic event consumer to delegate entity creation without domain coupling.</para>
/// </summary>
public interface IOperationsEventFactory
{
    /// <summary>
    /// Creates an <see cref="OperationsEvent"/> subclass for the specified domain, or null if the domain is unknown.
    /// </summary>
    OperationsEvent? CreateFrom(string domain, string eventType, string entityType, int entityId, DateTime occurredAtUtc, string? payload);
}
