using AutoMapper;
using Warehouse.DBModel.Models.Customers;
using Warehouse.ServiceModel.DTOs.Customers;

namespace Warehouse.Mapping.Profiles.Customers;

/// <summary>
/// AutoMapper profile for mapping customer domain entities to DTOs.
/// <para>See <see cref="Customer"/>, <see cref="CustomerCategory"/>, <see cref="CustomerAccount"/>,
/// <see cref="CustomerAddress"/>, <see cref="CustomerPhone"/>, <see cref="CustomerEmail"/>.</para>
/// </summary>
public sealed class CustomerMappingProfile : Profile
{
    /// <summary>
    /// Initializes mapping configurations for all customer entity-to-DTO pairs.
    /// </summary>
    public CustomerMappingProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(
                dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));

        CreateMap<Customer, CustomerDetailDto>()
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
                opt => opt.MapFrom(src => src.Emails))
            .ForMember(
                dest => dest.Accounts,
                opt => opt.MapFrom(src => src.Accounts.Where(a => !a.IsDeleted)));

        CreateMap<CustomerAccount, CustomerAccountDto>();

        CreateMap<CustomerAddress, CustomerAddressDto>();

        CreateMap<CustomerPhone, CustomerPhoneDto>();

        CreateMap<CustomerEmail, CustomerEmailDto>();

        CreateMap<CustomerCategory, CustomerCategoryDto>();
    }
}
