using AutoMapper;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.DTOs.EventLog;

namespace Warehouse.Mapping.Profiles.EventLog;

/// <summary>
/// AutoMapper profile for EventLog domain entity-to-DTO mappings.
/// </summary>
public sealed class EventLogMappingProfile : Profile
{
    /// <summary>
    /// Initializes the EventLog mapping configuration.
    /// </summary>
    public EventLogMappingProfile()
    {
        CreateMap<OperationsEvent, OperationsEventDto>();
        CreateMap<AuthEvent, AuthEventDto>();
        CreateMap<PurchaseEvent, PurchaseEventDto>();
        CreateMap<FulfillmentEvent, FulfillmentEventDto>();
        CreateMap<InventoryEvent, InventoryEventDto>();
        CreateMap<CustomerEvent, CustomerEventDto>();
    }
}
