using System.Text;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using Microsoft.AspNetCore.Authorization;
using Warehouse.Auth.API.Authorization;
using Warehouse.Auth.API.Configuration;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Auth.API.Middleware;
using Warehouse.Auth.API.Services;
using Warehouse.Auth.DBModel;
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

    ConfigurePipeline(app);

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
    ConfigureAuthentication(services, configuration);
    ConfigureApiVersioning(services);
    ConfigureSwagger(services);
    ConfigureFluentValidation(services);
    ConfigureAutoMapper(services);
    ConfigureApplicationServices(services);
    ConfigureHealthChecks(services, configuration);

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

static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
{
    JwtSettings jwtSettings = new()
    {
        SecretKey = configuration["Jwt:SecretKey"]!,
        Issuer = configuration["Jwt:Issuer"]!,
        Audience = configuration["Jwt:Audience"]!,
        AccessTokenExpirationMinutes = configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes"),
        RefreshTokenExpirationDays = configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays")
    };

    services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

    services.AddAuthorization();

    services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
}

static void ConfigureApiVersioning(IServiceCollection services)
{
    services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
}

static void ConfigureSwagger(IServiceCollection services)
{
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Warehouse Auth API",
            Version = "v1",
            Description = "Authentication and authorization service for the Warehouse system."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter the JWT access token."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
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

static void ConfigureHealthChecks(IServiceCollection services, IConfiguration configuration)
{
    services.AddHealthChecks()
        .AddSqlServer(
            configuration.GetConnectionString("WarehouseDb")!,
            name: "database",
            tags: ["ready"]);
}

static void ConfigurePipeline(WebApplication app)
{
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Warehouse Auth API v1");
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });

    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
}

static async Task SeedDatabaseAsync(WebApplication app)
{
    using IServiceScope scope = app.Services.CreateScope();
    DatabaseSeeder seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
}
