using AutoMapper;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.DTOs.EventLog;

namespace Warehouse.EventLog.API.Strategies;

/// <summary>
/// Maps <see cref="CustomerEvent"/> entities to <see cref="CustomerEventDto"/>.
/// </summary>
public sealed class CustomerEventMappingStrategy : IEventMappingStrategy
{
    /// <inheritdoc />
    public bool CanMap(OperationsEvent entity) => entity is CustomerEvent;

    /// <inheritdoc />
    public OperationsEventDto Map(OperationsEvent entity, IMapper mapper) => mapper.Map<CustomerEventDto>(entity);
}
