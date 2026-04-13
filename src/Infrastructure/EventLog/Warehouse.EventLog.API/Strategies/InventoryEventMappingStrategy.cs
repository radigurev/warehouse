using AutoMapper;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.DTOs.EventLog;

namespace Warehouse.EventLog.API.Strategies;

/// <summary>
/// Maps <see cref="InventoryEvent"/> entities to <see cref="InventoryEventDto"/>.
/// </summary>
public sealed class InventoryEventMappingStrategy : IEventMappingStrategy
{
    /// <inheritdoc />
    public bool CanMap(OperationsEvent entity) => entity is InventoryEvent;

    /// <inheritdoc />
    public OperationsEventDto Map(OperationsEvent entity, IMapper mapper) => mapper.Map<InventoryEventDto>(entity);
}
