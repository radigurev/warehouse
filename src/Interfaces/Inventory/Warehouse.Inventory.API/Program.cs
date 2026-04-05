using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using Warehouse.Infrastructure.Extensions;
using Warehouse.Inventory.API.Interfaces.Products;
using Warehouse.Inventory.API.Interfaces.Stock;
using Warehouse.Inventory.API.Interfaces.Stocktake;
using Warehouse.Inventory.API.Interfaces.Warehouse;
using Warehouse.Inventory.API.Services.Products;
using Warehouse.Inventory.API.Services.Stock;
using Warehouse.Inventory.API.Services.Stocktake;
using Warehouse.Inventory.API.Services.Warehouse;
using Warehouse.Inventory.DBModel;
using Warehouse.Mapping.Profiles.Inventory;

NLog.Logger logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting Warehouse.Inventory.API");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    ConfigureServices(builder);

    WebApplication app = builder.Build();

    app.UseWarehousePipeline("Warehouse Inventory API v1");

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application startup failed");
    throw;
}
finally
{
    LogManager.Shutdown();
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    IServiceCollection services = builder.Services;
    IConfiguration configuration = builder.Configuration;

    ConfigureDatabase(services, configuration);
    services.AddWarehouseAuthentication(configuration);
    services.AddWarehouseApiVersioning();
    services.AddWarehouseSwagger("Warehouse Inventory API", "Inventory management service for the Warehouse system.");
    ConfigureFluentValidation(services);
    ConfigureAutoMapper(services);
    services.AddWarehouseHealthChecks(configuration);
    ConfigureApplicationServices(services);

    services.AddControllers();
    services.AddEndpointsApiExplorer();
}

static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
{
    string connectionString = configuration.GetConnectionString("WarehouseDb")!;

    services.AddDbContext<InventoryDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
            sql.MigrationsAssembly(typeof(InventoryDbContext).Assembly.GetName().Name)));
}

static void ConfigureFluentValidation(IServiceCollection services)
{
    services.AddFluentValidationAutoValidation();
    services.AddValidatorsFromAssemblyContaining<Program>();
}

static void ConfigureAutoMapper(IServiceCollection services)
{
    services.AddAutoMapper(typeof(InventoryMappingProfile).Assembly);
}

static void ConfigureApplicationServices(IServiceCollection services)
{
    services.AddScoped<IProductService, ProductService>();
    services.AddScoped<IProductCategoryService, ProductCategoryService>();
    services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();
    services.AddScoped<IWarehouseService, WarehouseService>();
    services.AddScoped<IZoneService, ZoneService>();
    services.AddScoped<IStorageLocationService, StorageLocationService>();
    services.AddScoped<IBomService, BomService>();
    services.AddScoped<IProductAccessoryService, ProductAccessoryService>();
    services.AddScoped<IProductSubstituteService, ProductSubstituteService>();
    services.AddScoped<IStockLevelService, StockLevelService>();
    services.AddScoped<IStockMovementService, StockMovementService>();
    services.AddScoped<IBatchService, BatchService>();
    services.AddScoped<IInventoryAdjustmentService, InventoryAdjustmentService>();
    services.AddScoped<IWarehouseTransferService, WarehouseTransferService>();
    services.AddScoped<IStocktakeSessionService, StocktakeSessionService>();
    services.AddScoped<IStocktakeCountService, StocktakeCountService>();
}
