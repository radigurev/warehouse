using AutoMapper;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;

namespace Warehouse.Mapping.Profiles.Fulfillment;

/// <summary>
/// AutoMapper profile for mapping fulfillment domain entities to DTOs.
/// <para>See <see cref="SalesOrder"/>, <see cref="PickList"/>, <see cref="Parcel"/>,
/// <see cref="Shipment"/>, <see cref="Carrier"/>, <see cref="CustomerReturn"/>, <see cref="FulfillmentEvent"/>.</para>
/// </summary>
public sealed class FulfillmentMappingProfile : Profile
{
    /// <summary>
    /// Initializes mapping configurations for all fulfillment entity-to-DTO pairs.
    /// </summary>
    public FulfillmentMappingProfile()
    {
        CreateMap<SalesOrder, SalesOrderDto>();

        CreateMap<SalesOrder, SalesOrderDetailDto>()
            .ForMember(
                dest => dest.Lines,
                opt => opt.MapFrom(src => src.Lines));

        CreateMap<SalesOrderLine, SalesOrderLineDto>();

        CreateMap<PickList, PickListDto>();

        CreateMap<PickList, PickListDetailDto>()
            .ForMember(
                dest => dest.Lines,
                opt => opt.MapFrom(src => src.Lines));

        CreateMap<PickListLine, PickListLineDto>();

        CreateMap<Parcel, ParcelDto>()
            .ForMember(
                dest => dest.Items,
                opt => opt.MapFrom(src => src.Items));

        CreateMap<ParcelItem, ParcelItemDto>();

        CreateMap<Shipment, ShipmentDto>();

        CreateMap<Shipment, ShipmentDetailDto>()
            .ForMember(
                dest => dest.Lines,
                opt => opt.MapFrom(src => src.Lines))
            .ForMember(
                dest => dest.TrackingEntries,
                opt => opt.MapFrom(src => src.TrackingEntries));

        CreateMap<ShipmentLine, ShipmentLineDto>();

        CreateMap<ShipmentTracking, ShipmentTrackingDto>();

        CreateMap<Carrier, CarrierDto>();

        CreateMap<Carrier, CarrierDetailDto>()
            .ForMember(
                dest => dest.ServiceLevels,
                opt => opt.MapFrom(src => src.ServiceLevels));

        CreateMap<CarrierServiceLevel, CarrierServiceLevelDto>();

        CreateMap<CustomerReturn, CustomerReturnDto>();

        CreateMap<CustomerReturn, CustomerReturnDetailDto>()
            .ForMember(
                dest => dest.Lines,
                opt => opt.MapFrom(src => src.Lines));

        CreateMap<CustomerReturnLine, CustomerReturnLineDto>();

        CreateMap<FulfillmentEvent, FulfillmentEventDto>();
    }
}
