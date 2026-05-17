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
        CreateMap<SalesOrder, SalesOrderDto>()
            .ForMember(dest => dest.CustomerName, opt => opt.Ignore())
            .ForMember(dest => dest.WarehouseName, opt => opt.Ignore());

        CreateMap<SalesOrder, SalesOrderDetailDto>()
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines))
            .ForMember(dest => dest.PickLists, opt => opt.MapFrom(src => src.PickLists))
            .ForMember(dest => dest.Parcels, opt => opt.MapFrom(src => src.Parcels))
            .ForMember(dest => dest.CarrierName, opt => opt.MapFrom(src => src.Carrier != null ? src.Carrier.Name : null))
            .ForMember(dest => dest.CarrierServiceLevelName, opt => opt.MapFrom(src => src.CarrierServiceLevel != null ? src.CarrierServiceLevel.Name : null))
            .ForMember(dest => dest.CustomerName, opt => opt.Ignore())
            .ForMember(dest => dest.WarehouseName, opt => opt.Ignore())
            .ForMember(dest => dest.ShippingCountryName, opt => opt.Ignore())
            .ForMember(dest => dest.BillingCountryName, opt => opt.Ignore())
            .ForMember(dest => dest.Shipment, opt => opt.Ignore());

        CreateMap<SalesOrderLine, SalesOrderLineDto>()
            .ForMember(dest => dest.ProductCode, opt => opt.Ignore())
            .ForMember(dest => dest.ProductName, opt => opt.Ignore());

        CreateMap<PickList, SalesOrderPickListSummaryDto>();

        CreateMap<Parcel, SalesOrderParcelSummaryDto>()
            .ForMember(dest => dest.WeightKg, opt => opt.MapFrom(src => src.Weight))
            .ForMember(dest => dest.LengthCm, opt => opt.MapFrom(src => src.Length))
            .ForMember(dest => dest.WidthCm, opt => opt.MapFrom(src => src.Width))
            .ForMember(dest => dest.HeightCm, opt => opt.MapFrom(src => src.Height))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        CreateMap<ParcelItem, SalesOrderParcelItemSummaryDto>()
            .ForMember(dest => dest.ProductCode, opt => opt.Ignore())
            .ForMember(dest => dest.ProductName, opt => opt.Ignore());

        CreateMap<Shipment, SalesOrderShipmentSummaryDto>()
            .ForMember(dest => dest.CarrierName, opt => opt.MapFrom(src => src.Carrier != null ? src.Carrier.Name : null))
            .ForMember(dest => dest.CarrierServiceLevelName, opt => opt.MapFrom(src => src.CarrierServiceLevel != null ? src.CarrierServiceLevel.Name : null))
            .ForMember(dest => dest.LineCount, opt => opt.MapFrom(src => src.Lines.Count))
            .ForMember(dest => dest.TotalQuantity, opt => opt.MapFrom(src => src.Lines.Sum(l => l.Quantity)));

        CreateMap<PickList, PickListDto>()
            .ForMember(dest => dest.SalesOrderNumber, opt => opt.MapFrom(src => src.SalesOrder.OrderNumber))
            .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.SalesOrder.WarehouseId))
            .ForMember(dest => dest.WarehouseName, opt => opt.Ignore());

        CreateMap<PickList, PickListDetailDto>()
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines))
            .ForMember(dest => dest.SalesOrderNumber, opt => opt.MapFrom(src => src.SalesOrder.OrderNumber))
            .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.SalesOrder.WarehouseId))
            .ForMember(dest => dest.WarehouseName, opt => opt.Ignore());

        CreateMap<PickListLine, PickListLineDto>()
            .ForMember(dest => dest.SourceLocationId, opt => opt.MapFrom(src => src.LocationId))
            .ForMember(dest => dest.ActualPickedQuantity, opt => opt.MapFrom(src => src.ActualQuantity))
            .ForMember(dest => dest.ProductCode, opt => opt.Ignore())
            .ForMember(dest => dest.ProductName, opt => opt.Ignore())
            .ForMember(dest => dest.SourceLocationCode, opt => opt.Ignore());

        CreateMap<Parcel, ParcelDto>()
            .ForMember(dest => dest.WeightKg, opt => opt.MapFrom(src => src.Weight))
            .ForMember(dest => dest.LengthCm, opt => opt.MapFrom(src => src.Length))
            .ForMember(dest => dest.WidthCm, opt => opt.MapFrom(src => src.Width))
            .ForMember(dest => dest.HeightCm, opt => opt.MapFrom(src => src.Height))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        CreateMap<ParcelItem, ParcelItemDto>()
            .ForMember(dest => dest.ProductCode, opt => opt.Ignore())
            .ForMember(dest => dest.ProductName, opt => opt.Ignore());

        CreateMap<Shipment, ShipmentDto>()
            .ForMember(dest => dest.SalesOrderNumber, opt => opt.MapFrom(src => src.SalesOrder.OrderNumber))
            .ForMember(dest => dest.CarrierName, opt => opt.MapFrom(src => src.Carrier != null ? src.Carrier.Name : null));

        CreateMap<Shipment, ShipmentDetailDto>()
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines))
            .ForMember(dest => dest.TrackingHistory, opt => opt.MapFrom(src => src.TrackingEntries))
            .ForMember(dest => dest.SalesOrderNumber, opt => opt.MapFrom(src => src.SalesOrder.OrderNumber))
            .ForMember(dest => dest.CarrierName, opt => opt.MapFrom(src => src.Carrier != null ? src.Carrier.Name : null))
            .ForMember(dest => dest.CarrierServiceLevelName, opt => opt.MapFrom(src => src.CarrierServiceLevel != null ? src.CarrierServiceLevel.Name : null))
            .ForMember(dest => dest.ShippingCountryName, opt => opt.Ignore())
            .ForMember(dest => dest.Parcels, opt => opt.Ignore());

        CreateMap<ShipmentLine, ShipmentLineDto>()
            .ForMember(dest => dest.ShippedQuantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.ProductCode, opt => opt.Ignore())
            .ForMember(dest => dest.ProductName, opt => opt.Ignore());

        CreateMap<ShipmentTracking, ShipmentTrackingDto>()
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.MapFrom(src => src.OccurredAtUtc))
            .ForMember(dest => dest.UpdatedByUserId, opt => opt.MapFrom(src => src.RecordedByUserId));

        CreateMap<Carrier, CarrierDto>();

        CreateMap<Carrier, CarrierDetailDto>()
            .ForMember(
                dest => dest.ServiceLevels,
                opt => opt.MapFrom(src => src.ServiceLevels));

        CreateMap<CarrierServiceLevel, CarrierServiceLevelDto>();

        CreateMap<CustomerReturn, CustomerReturnDto>()
            .ForMember(dest => dest.CustomerName, opt => opt.Ignore())
            .ForMember(dest => dest.SalesOrderNumber, opt => opt.Ignore());

        CreateMap<CustomerReturn, CustomerReturnDetailDto>()
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines))
            .ForMember(dest => dest.CustomerName, opt => opt.Ignore())
            .ForMember(dest => dest.SalesOrderNumber, opt => opt.Ignore());

        CreateMap<CustomerReturnLine, CustomerReturnLineDto>()
            .ForMember(dest => dest.ProductCode, opt => opt.Ignore())
            .ForMember(dest => dest.ProductName, opt => opt.Ignore())
            .ForMember(dest => dest.WarehouseName, opt => opt.Ignore())
            .ForMember(dest => dest.LocationCode, opt => opt.Ignore());

        CreateMap<FulfillmentEvent, FulfillmentEventDto>();
    }
}
