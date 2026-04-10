using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using Warehouse.Infrastructure.Extensions;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services;
using Warehouse.Purchasing.DBModel;
using Warehouse.Mapping.Profiles.Purchasing;

NLog.Logger logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting Warehouse.Purchasing.API");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    ConfigureServices(builder);

    WebApplication app = builder.Build();

    app.UseWarehousePipeline("Warehouse Purchasing API v1");

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
    services.AddCorrelationId();
    services.AddWarehouseAuthentication(configuration);
    services.AddWarehousePermissionValidation(configuration);
    services.AddWarehouseApiVersioning();
    services.AddWarehouseSwagger("Warehouse Purchasing API", "Procurement operations service for the Warehouse system.");
    ConfigureFluentValidation(services);
    ConfigureAutoMapper(services);
    services.AddWarehouseHealthChecks(configuration);
    services.AddWarehouseRedisCache(configuration);
    services.AddWarehouseMessageBus(configuration);
    services.AddSequenceGenerator<PurchasingDbContext>();
    services.AddWarehouseTracing(configuration, "warehouse-purchasing-api");
    ConfigureApplicationServices(services);

    services.AddControllers();
    services.AddEndpointsApiExplorer();
}

static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
{
    string connectionString = configuration.GetConnectionString("WarehouseDb")!;

    services.AddDbContext<PurchasingDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
            sql.MigrationsAssembly(typeof(PurchasingDbContext).Assembly.GetName().Name)));
}

static void ConfigureFluentValidation(IServiceCollection services)
{
    services.AddFluentValidationAutoValidation();
    services.AddValidatorsFromAssemblyContaining<Program>();
}

static void ConfigureAutoMapper(IServiceCollection services)
{
    services.AddAutoMapper(typeof(PurchasingMappingProfile).Assembly);
}

static void ConfigureApplicationServices(IServiceCollection services)
{
    services.AddScoped<IPurchaseEventService, PurchaseEventService>();
    services.AddScoped<ISupplierService, SupplierService>();
    services.AddScoped<ISupplierCategoryService, SupplierCategoryService>();
    services.AddScoped<ISupplierAddressService, SupplierAddressService>();
    services.AddScoped<ISupplierPhoneService, SupplierPhoneService>();
    services.AddScoped<ISupplierEmailService, SupplierEmailService>();
    services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
    services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
    services.AddScoped<IReceivingInspectionService, ReceivingInspectionService>();
    services.AddScoped<ISupplierReturnService, SupplierReturnService>();
}
