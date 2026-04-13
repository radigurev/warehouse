using AutoMapper;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.DTOs.EventLog;

namespace Warehouse.EventLog.API.Strategies;

/// <summary>
/// Defines a strategy for mapping an operations event entity to its domain-specific DTO.
/// </summary>
public interface IEventMappingStrategy
{
    /// <summary>
    /// Returns whether this strategy can map the given entity.
    /// </summary>
    bool CanMap(OperationsEvent entity);

    /// <summary>
    /// Maps the entity to its domain-specific DTO.
    /// </summary>
    OperationsEventDto Map(OperationsEvent entity, IMapper mapper);
}
