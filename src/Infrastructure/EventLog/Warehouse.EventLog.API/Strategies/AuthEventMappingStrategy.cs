using AutoMapper;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.DTOs.EventLog;

namespace Warehouse.EventLog.API.Strategies;

/// <summary>
/// Maps <see cref="AuthEvent"/> entities to <see cref="AuthEventDto"/>.
/// </summary>
public sealed class AuthEventMappingStrategy : IEventMappingStrategy
{
    /// <inheritdoc />
    public bool CanMap(OperationsEvent entity) => entity is AuthEvent;

    /// <inheritdoc />
    public OperationsEventDto Map(OperationsEvent entity, IMapper mapper) => mapper.Map<AuthEventDto>(entity);
}
