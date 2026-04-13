using AutoMapper;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.DTOs.EventLog;

namespace Warehouse.EventLog.API.Strategies;

/// <summary>
/// Maps <see cref="PurchaseEvent"/> entities to <see cref="PurchaseEventDto"/>.
/// </summary>
public sealed class PurchaseEventMappingStrategy : IEventMappingStrategy
{
    /// <inheritdoc />
    public bool CanMap(OperationsEvent entity) => entity is PurchaseEvent;

    /// <inheritdoc />
    public OperationsEventDto Map(OperationsEvent entity, IMapper mapper) => mapper.Map<PurchaseEventDto>(entity);
}
