using AutoMapper;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Mapping.Profiles.Fulfillment;

/// <summary>
/// AutoMapper profile for the Fulfillment Product Price Catalog entity family.
/// <para>Conforms to CHG-FEAT-007 §7 AutoMapper Profile requirement.</para>
/// <para>See <see cref="ProductPrice"/>, <see cref="ProductPriceDto"/>.</para>
/// </summary>
public sealed class ProductPriceMappingProfile : Profile
{
    /// <summary>
    /// Initializes the mapping configuration for Product Price entities, DTOs, and request models.
    /// </summary>
    public ProductPriceMappingProfile()
    {
        CreateMap<ProductPrice, ProductPriceDto>();

        CreateMap<CreateProductPriceRequest, ProductPrice>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedByUserId, opt => opt.Ignore());

        CreateMap<UpdateProductPriceRequest, ProductPrice>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.CurrencyCode, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedByUserId, opt => opt.Ignore());
    }
}
