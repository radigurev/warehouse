using AutoMapper;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.DTOs.EventLog;

namespace Warehouse.EventLog.API.Strategies;

/// <summary>
/// Maps <see cref="FulfillmentEvent"/> entities to <see cref="FulfillmentEventDto"/>.
/// </summary>
public sealed class FulfillmentEventMappingStrategy : IEventMappingStrategy
{
    /// <inheritdoc />
    public bool CanMap(OperationsEvent entity) => entity is FulfillmentEvent;

    /// <inheritdoc />
    public OperationsEventDto Map(OperationsEvent entity, IMapper mapper) => mapper.Map<FulfillmentEventDto>(entity);
}
