using AutoMapper;
using Warehouse.Nomenclature.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Nomenclature;

namespace Warehouse.Mapping.Profiles.Nomenclature;

/// <summary>
/// AutoMapper profile for nomenclature domain entity-to-DTO mappings.
/// </summary>
public sealed class NomenclatureMappingProfile : Profile
{
    /// <summary>
    /// Initializes nomenclature entity-to-DTO mappings.
    /// </summary>
    public NomenclatureMappingProfile()
    {
        ConfigureCountryMappings();
        ConfigureStateProvinceMappings();
        ConfigureCityMappings();
        ConfigureCurrencyMappings();
    }

    /// <summary>
    /// Configures Country entity mappings.
    /// </summary>
    private void ConfigureCountryMappings()
    {
        CreateMap<Country, CountryDto>();

        CreateMap<Country, CountryDetailDto>()
            .ForMember(
                dest => dest.StateProvinces,
                opt => opt.MapFrom(src => src.StateProvinces));
    }

    /// <summary>
    /// Configures StateProvince entity mappings.
    /// </summary>
    private void ConfigureStateProvinceMappings()
    {
        CreateMap<StateProvince, StateProvinceDto>();
    }

    /// <summary>
    /// Configures City entity mappings.
    /// </summary>
    private void ConfigureCityMappings()
    {
        CreateMap<City, CityDto>();
    }

    /// <summary>
    /// Configures Currency entity mappings.
    /// </summary>
    private void ConfigureCurrencyMappings()
    {
        CreateMap<Currency, CurrencyDto>();
    }
}
