using AutoMapper;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;

namespace Warehouse.Mapping.Profiles.Purchasing;

/// <summary>
/// AutoMapper profile for mapping purchasing domain entities to DTOs.
/// <para>See <see cref="Supplier"/>, <see cref="SupplierCategory"/>, <see cref="PurchaseOrder"/>,
/// <see cref="GoodsReceipt"/>, <see cref="SupplierReturn"/>, <see cref="PurchaseEvent"/>.</para>
/// </summary>
public sealed class PurchasingMappingProfile : Profile
{
    /// <summary>
    /// Initializes mapping configurations for all purchasing entity-to-DTO pairs.
    /// </summary>
    public PurchasingMappingProfile()
    {
        CreateMap<Supplier, SupplierDto>()
            .ForMember(
                dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));

        CreateMap<Supplier, SupplierDetailDto>()
            .ForMember(
                dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(
                dest => dest.Addresses,
                opt => opt.MapFrom(src => src.Addresses))
            .ForMember(
                dest => dest.Phones,
                opt => opt.MapFrom(src => src.Phones))
            .ForMember(
                dest => dest.Emails,
                opt => opt.MapFrom(src => src.Emails));

        CreateMap<SupplierCategory, SupplierCategoryDto>();

        CreateMap<SupplierAddress, SupplierAddressDto>();

        CreateMap<SupplierPhone, SupplierPhoneDto>();

        CreateMap<SupplierEmail, SupplierEmailDto>();

        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(
                dest => dest.SupplierName,
                opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : string.Empty));

        CreateMap<PurchaseOrder, PurchaseOrderDetailDto>()
            .ForMember(
                dest => dest.SupplierName,
                opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : string.Empty))
            .ForMember(
                dest => dest.Lines,
                opt => opt.MapFrom(src => src.Lines));

        CreateMap<PurchaseOrderLine, PurchaseOrderLineDto>();

        CreateMap<GoodsReceipt, GoodsReceiptDto>()
            .ForMember(
                dest => dest.PurchaseOrderNumber,
                opt => opt.MapFrom(src => src.PurchaseOrder != null ? src.PurchaseOrder.OrderNumber : string.Empty));

        CreateMap<GoodsReceipt, GoodsReceiptDetailDto>()
            .ForMember(
                dest => dest.PurchaseOrderNumber,
                opt => opt.MapFrom(src => src.PurchaseOrder != null ? src.PurchaseOrder.OrderNumber : string.Empty))
            .ForMember(
                dest => dest.Lines,
                opt => opt.MapFrom(src => src.Lines));

        CreateMap<GoodsReceiptLine, GoodsReceiptLineDto>();

        CreateMap<SupplierReturn, SupplierReturnDto>()
            .ForMember(
                dest => dest.SupplierName,
                opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : string.Empty));

        CreateMap<SupplierReturn, SupplierReturnDetailDto>()
            .ForMember(
                dest => dest.SupplierName,
                opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : string.Empty))
            .ForMember(
                dest => dest.Lines,
                opt => opt.MapFrom(src => src.Lines));

        CreateMap<SupplierReturnLine, SupplierReturnLineDto>();

        CreateMap<PurchaseEvent, PurchaseEventDto>();
    }
}
