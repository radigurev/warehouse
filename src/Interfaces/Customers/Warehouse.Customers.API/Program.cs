using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using Warehouse.Customers.API.Interfaces;
using Warehouse.Customers.API.Services;
using Warehouse.Customers.DBModel;
using Warehouse.Infrastructure.Extensions;
using Warehouse.Mapping.Profiles.Customers;

NLog.Logger logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting Warehouse.Customers.API");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    ConfigureServices(builder);

    WebApplication app = builder.Build();

    app.UseWarehousePipeline("Warehouse Customers API v1");

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
    services.AddWarehouseSwagger("Warehouse Customers API", "Customer management service for the Warehouse system.");
    ConfigureFluentValidation(services);
    ConfigureAutoMapper(services);
    services.AddWarehouseHealthChecks(configuration);
    services.AddWarehouseRedisCache(configuration);
    services.AddWarehouseMessageBus(configuration);
    services.AddSequenceGenerator<CustomersDbContext>();
    services.AddWarehouseTracing(configuration, "warehouse-customers-api");
    ConfigureApplicationServices(services);

    services.AddControllers();
    services.AddEndpointsApiExplorer();
}

static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
{
    string connectionString = configuration.GetConnectionString("WarehouseDb")!;

    services.AddDbContext<CustomersDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
            sql.MigrationsAssembly(typeof(CustomersDbContext).Assembly.GetName().Name)));
}

static void ConfigureFluentValidation(IServiceCollection services)
{
    services.AddFluentValidationAutoValidation();
    services.AddValidatorsFromAssemblyContaining<Program>();
}

static void ConfigureAutoMapper(IServiceCollection services)
{
    services.AddAutoMapper(typeof(CustomerMappingProfile).Assembly);
}

static void ConfigureApplicationServices(IServiceCollection services)
{
    services.AddScoped<ICustomerService, CustomerService>();
    services.AddScoped<ICustomerAccountService, CustomerAccountService>();
    services.AddScoped<ICustomerAddressService, CustomerAddressService>();
    services.AddScoped<ICustomerPhoneService, CustomerPhoneService>();
    services.AddScoped<ICustomerEmailService, CustomerEmailService>();
    services.AddScoped<ICustomerCategoryService, CustomerCategoryService>();
}
