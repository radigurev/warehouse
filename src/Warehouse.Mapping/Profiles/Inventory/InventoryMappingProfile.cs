using AutoMapper;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;

namespace Warehouse.Mapping.Profiles.Inventory;

/// <summary>
/// AutoMapper profile for mapping inventory domain entities to DTOs.
/// <para>See <see cref="Product"/>, <see cref="ProductCategory"/>, <see cref="UnitOfMeasure"/>,
/// <see cref="WarehouseEntity"/>, <see cref="Zone"/>, <see cref="StorageLocation"/>,
/// <see cref="BillOfMaterials"/>, <see cref="BomLine"/>, <see cref="ProductAccessory"/>,
/// <see cref="ProductSubstitute"/>, <see cref="StockLevel"/>, <see cref="StockMovement"/>,
/// <see cref="Batch"/>, <see cref="InventoryAdjustment"/>, <see cref="InventoryAdjustmentLine"/>,
/// <see cref="WarehouseTransfer"/>, <see cref="WarehouseTransferLine"/>,
/// <see cref="StocktakeSession"/>, <see cref="StocktakeCount"/>.</para>
/// </summary>
public sealed class InventoryMappingProfile : Profile
{
    /// <summary>
    /// Initializes mapping configurations for all inventory entity-to-DTO pairs.
    /// </summary>
    public InventoryMappingProfile()
    {
        ConfigureProductMappings();
        ConfigureWarehouseMappings();
        ConfigureZoneMappings();
        ConfigureStorageLocationMappings();
        ConfigureBomMappings();
        ConfigureProductRelationMappings();
        ConfigureStockMappings();
        ConfigureBatchMappings();
        ConfigureAdjustmentMappings();
        ConfigureTransferMappings();
        ConfigureStocktakeMappings();
    }

    /// <summary>
    /// Configures product and category mappings.
    /// </summary>
    private void ConfigureProductMappings()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(
                dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(
                dest => dest.UnitOfMeasureName,
                opt => opt.MapFrom(src => src.UnitOfMeasure.Name));

        CreateMap<Product, ProductDetailDto>()
            .ForMember(
                dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(
                dest => dest.UnitOfMeasureName,
                opt => opt.MapFrom(src => src.UnitOfMeasure.Name));

        CreateMap<ProductCategory, ProductCategoryDto>()
            .ForMember(
                dest => dest.ParentCategoryName,
                opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null));

        CreateMap<UnitOfMeasure, UnitOfMeasureDto>();
    }

    /// <summary>
    /// Configures warehouse mappings.
    /// </summary>
    private void ConfigureWarehouseMappings()
    {
        CreateMap<WarehouseEntity, WarehouseDto>()
            .ForMember(
                dest => dest.IsActive,
                opt => opt.MapFrom(src => !src.IsDeleted));

        CreateMap<WarehouseEntity, WarehouseDetailDto>()
            .ForMember(
                dest => dest.IsActive,
                opt => opt.MapFrom(src => !src.IsDeleted))
            .ForMember(
                dest => dest.Zones,
                opt => opt.MapFrom(src => src.Zones));
    }

    /// <summary>
    /// Configures zone mappings.
    /// </summary>
    private void ConfigureZoneMappings()
    {
        CreateMap<Zone, ZoneDto>()
            .ForMember(
                dest => dest.WarehouseName,
                opt => opt.MapFrom(src => src.Warehouse.Name));

        CreateMap<Zone, ZoneDetailDto>()
            .ForMember(
                dest => dest.WarehouseName,
                opt => opt.MapFrom(src => src.Warehouse.Name))
            .ForMember(
                dest => dest.StorageLocations,
                opt => opt.MapFrom(src => src.Locations));
    }

    /// <summary>
    /// Configures storage location mappings.
    /// </summary>
    private void ConfigureStorageLocationMappings()
    {
        CreateMap<StorageLocation, StorageLocationDto>()
            .ForMember(
                dest => dest.WarehouseName,
                opt => opt.MapFrom(src => src.Warehouse.Name))
            .ForMember(
                dest => dest.ZoneName,
                opt => opt.MapFrom(src => src.Zone != null ? src.Zone.Name : null));
    }

    /// <summary>
    /// Configures bill of materials mappings.
    /// </summary>
    private void ConfigureBomMappings()
    {
        CreateMap<BillOfMaterials, BomDto>()
            .ForMember(
                dest => dest.ParentProductName,
                opt => opt.MapFrom(src => src.ParentProduct.Name))
            .ForMember(
                dest => dest.Lines,
                opt => opt.MapFrom(src => src.Lines));

        CreateMap<BomLine, BomLineDto>()
            .ForMember(
                dest => dest.ChildProductName,
                opt => opt.MapFrom(src => src.ChildProduct.Name));
    }

    /// <summary>
    /// Configures product accessory and substitute mappings.
    /// </summary>
    private void ConfigureProductRelationMappings()
    {
        CreateMap<ProductAccessory, ProductAccessoryDto>()
            .ForMember(
                dest => dest.AccessoryProductName,
                opt => opt.MapFrom(src => src.AccessoryProduct.Name));

        CreateMap<ProductSubstitute, ProductSubstituteDto>()
            .ForMember(
                dest => dest.SubstituteProductName,
                opt => opt.MapFrom(src => src.SubstituteProduct.Name));
    }

    /// <summary>
    /// Configures stock level and movement mappings.
    /// </summary>
    private void ConfigureStockMappings()
    {
        CreateMap<StockLevel, StockLevelDto>()
            .ForMember(
                dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(
                dest => dest.ProductCode,
                opt => opt.MapFrom(src => src.Product.Code))
            .ForMember(
                dest => dest.WarehouseName,
                opt => opt.MapFrom(src => src.Warehouse.Name))
            .ForMember(
                dest => dest.LocationName,
                opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null))
            .ForMember(
                dest => dest.ReservedQuantity,
                opt => opt.MapFrom(src => src.QuantityReserved))
            .ForMember(
                dest => dest.AvailableQuantity,
                opt => opt.MapFrom(src => src.QuantityOnHand - src.QuantityReserved));

        CreateMap<StockMovement, StockMovementDto>()
            .ForMember(
                dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(
                dest => dest.WarehouseName,
                opt => opt.MapFrom(src => src.Warehouse.Name))
            .ForMember(
                dest => dest.LocationName,
                opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null))
            .ForMember(
                dest => dest.ReasonCode,
                opt => opt.MapFrom(src => src.ReasonCode.ToString()))
            .ForMember(
                dest => dest.ReferenceType,
                opt => opt.MapFrom(src => src.ReferenceType != null ? src.ReferenceType.ToString() : null));
    }

    /// <summary>
    /// Configures batch mappings.
    /// </summary>
    private void ConfigureBatchMappings()
    {
        CreateMap<Batch, BatchDto>()
            .ForMember(
                dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name));
    }

    /// <summary>
    /// Configures inventory adjustment mappings.
    /// </summary>
    private void ConfigureAdjustmentMappings()
    {
        CreateMap<InventoryAdjustment, InventoryAdjustmentDto>();

        CreateMap<InventoryAdjustment, InventoryAdjustmentDetailDto>()
            .ForMember(
                dest => dest.Lines,
                opt => opt.MapFrom(src => src.Lines));

        CreateMap<InventoryAdjustmentLine, InventoryAdjustmentLineDto>()
            .ForMember(
                dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(
                dest => dest.LocationName,
                opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null));
    }

    /// <summary>
    /// Configures warehouse transfer mappings.
    /// </summary>
    private void ConfigureTransferMappings()
    {
        CreateMap<WarehouseTransfer, WarehouseTransferDto>()
            .ForMember(
                dest => dest.SourceWarehouseName,
                opt => opt.MapFrom(src => src.SourceWarehouse.Name))
            .ForMember(
                dest => dest.DestinationWarehouseName,
                opt => opt.MapFrom(src => src.DestinationWarehouse.Name));

        CreateMap<WarehouseTransfer, WarehouseTransferDetailDto>()
            .ForMember(
                dest => dest.SourceWarehouseName,
                opt => opt.MapFrom(src => src.SourceWarehouse.Name))
            .ForMember(
                dest => dest.DestinationWarehouseName,
                opt => opt.MapFrom(src => src.DestinationWarehouse.Name))
            .ForMember(
                dest => dest.Lines,
                opt => opt.MapFrom(src => src.Lines));

        CreateMap<WarehouseTransferLine, WarehouseTransferLineDto>()
            .ForMember(
                dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name));
    }

    /// <summary>
    /// Configures stocktake session and count mappings.
    /// </summary>
    private void ConfigureStocktakeMappings()
    {
        CreateMap<StocktakeSession, StocktakeSessionDto>()
            .ForMember(
                dest => dest.WarehouseName,
                opt => opt.MapFrom(src => src.Warehouse.Name))
            .ForMember(
                dest => dest.ZoneName,
                opt => opt.MapFrom(src => src.Zone != null ? src.Zone.Name : null));

        CreateMap<StocktakeSession, StocktakeSessionDetailDto>()
            .ForMember(
                dest => dest.WarehouseName,
                opt => opt.MapFrom(src => src.Warehouse.Name))
            .ForMember(
                dest => dest.ZoneName,
                opt => opt.MapFrom(src => src.Zone != null ? src.Zone.Name : null))
            .ForMember(
                dest => dest.Counts,
                opt => opt.MapFrom(src => src.Counts));

        CreateMap<StocktakeCount, StocktakeCountDto>()
            .ForMember(
                dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(
                dest => dest.LocationName,
                opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null));
    }
}
