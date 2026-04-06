using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Auth.API.Services;
using Warehouse.Auth.DBModel;
using Warehouse.Infrastructure.Extensions;
using Warehouse.Mapping.Profiles.Auth;

NLog.Logger logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting Warehouse.Auth.API");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    ConfigureServices(builder);

    WebApplication app = builder.Build();

    app.UseWarehousePipeline("Warehouse Auth API v1");

    await SeedDatabaseAsync(app);

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
    services.AddWarehouseApiVersioning();
    services.AddWarehouseSwagger("Warehouse Auth API", "Authentication and authorization service for the Warehouse system.");
    services.AddWarehouseHealthChecks(configuration);

    ConfigureFluentValidation(services);
    ConfigureAutoMapper(services);
    ConfigureApplicationServices(services);

    services.AddControllers();
    services.AddEndpointsApiExplorer();
}

static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
{
    string connectionString = configuration.GetConnectionString("WarehouseDb")!;

    services.AddDbContext<AuthDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
            sql.MigrationsAssembly(typeof(AuthDbContext).Assembly.GetName().Name)));
}

static void ConfigureFluentValidation(IServiceCollection services)
{
    services.AddFluentValidationAutoValidation();
    services.AddValidatorsFromAssemblyContaining<Program>();
}

static void ConfigureAutoMapper(IServiceCollection services)
{
    services.AddAutoMapper(typeof(AuthMappingProfile).Assembly);
}

static void ConfigureApplicationServices(IServiceCollection services)
{
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IRoleService, RoleService>();
    services.AddScoped<IPermissionService, PermissionService>();
    services.AddScoped<IAuditService, AuditService>();
    services.AddScoped<IJwtTokenService, JwtTokenService>();
    services.AddSingleton<IPasswordHasher, PasswordHasher>();

    services.AddScoped<DatabaseSeeder>();
}

static async Task SeedDatabaseAsync(WebApplication app)
{
    using IServiceScope scope = app.Services.CreateScope();
    DatabaseSeeder seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
}
