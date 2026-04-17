using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using NLog;
using NLog.Web;
using Warehouse.Infrastructure.Extensions;
using Warehouse.Mapping.Profiles.Nomenclature;
using Warehouse.Nomenclature.API.Interfaces;
using Warehouse.Nomenclature.API.Seeding;
using Warehouse.Nomenclature.API.Services;
using Warehouse.Nomenclature.DBModel;

NLog.Logger logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting Warehouse.Nomenclature.API");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    ConfigureServices(builder);

    WebApplication app = builder.Build();

    await SeedDatabaseAsync(app);

    app.UseWarehousePipeline("Warehouse Nomenclature API v1");

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
    services.AddWarehouseSwagger("Warehouse Nomenclature API", "Geographic and financial reference data service for the Warehouse system.");
    ConfigureFluentValidation(services);
    ConfigureAutoMapper(services);
    services.AddWarehouseHealthChecks(configuration);
    services.AddWarehouseRedisCache(configuration);
    services.AddWarehouseTracing(configuration, "warehouse-nomenclature-api");
    services.AddFeatureManagement();
    ConfigureApplicationServices(services);

    services.AddControllers();
    services.AddEndpointsApiExplorer();
}

static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
{
    string connectionString = configuration.GetConnectionString("WarehouseDb")!;

    services.AddDbContext<NomenclatureDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
            sql.MigrationsAssembly(typeof(NomenclatureDbContext).Assembly.GetName().Name)));
}

static void ConfigureFluentValidation(IServiceCollection services)
{
    services.AddFluentValidationAutoValidation();
    services.AddValidatorsFromAssemblyContaining<Program>();
}

static void ConfigureAutoMapper(IServiceCollection services)
{
    services.AddAutoMapper(typeof(NomenclatureMappingProfile).Assembly);
}

static void ConfigureApplicationServices(IServiceCollection services)
{
    services.AddScoped<ICountryService, CountryService>();
    services.AddScoped<IStateProvinceService, StateProvinceService>();
    services.AddScoped<ICityService, CityService>();
    services.AddScoped<ICurrencyService, CurrencyService>();
    services.AddScoped<NomenclatureSeeder>();
}

static async Task SeedDatabaseAsync(WebApplication app)
{
    using IServiceScope scope = app.Services.CreateScope();
    NomenclatureSeeder seeder = scope.ServiceProvider.GetRequiredService<NomenclatureSeeder>();
    await seeder.SeedAsync(CancellationToken.None).ConfigureAwait(false);
}
