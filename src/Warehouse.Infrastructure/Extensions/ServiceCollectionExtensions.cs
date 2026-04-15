using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MassTransit;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.FeatureManagement;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Caching;
using Warehouse.Infrastructure.Configuration;
using Warehouse.Infrastructure.Http;
using Warehouse.Infrastructure.Messaging;
using Warehouse.Infrastructure.Sequences;

namespace Warehouse.Infrastructure.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register shared infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers JWT Bearer authentication and permission-based authorization.
    /// </summary>
    public static IServiceCollection AddWarehouseAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
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
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

    /// <summary>
    /// Registers the real-time permission validation service that resolves user permissions
    /// via Redis cache with HTTP fallback to Auth.API.
    /// Must be called after <see cref="AddWarehouseAuthentication"/> and <see cref="AddWarehouseRedisCache"/>.
    /// </summary>
    public static IServiceCollection AddWarehousePermissionValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string authApiBaseAddress = configuration["PermissionValidation:AuthApiBaseAddress"]
            ?? "http://localhost:5001";

        services.AddCorrelationId();

        services.AddHttpClient<IUserPermissionService, UserPermissionService>(client =>
            {
                client.BaseAddress = new Uri(authApiBaseAddress);
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromMilliseconds(200);
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;

                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
            });

        return services;
    }

    /// <summary>
    /// Registers URL-based API versioning with default version 1.0.
    /// </summary>
    public static IServiceCollection AddWarehouseApiVersioning(this IServiceCollection services)
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

        return services;
    }

    /// <summary>
    /// Registers Swagger/OpenAPI with JWT Bearer security definition.
    /// </summary>
    public static IServiceCollection AddWarehouseSwagger(
        this IServiceCollection services,
        string title,
        string description)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
                Version = "v1",
                Description = description
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

        return services;
    }

    /// <summary>
    /// Registers health checks with a SQL Server readiness probe.
    /// </summary>
    public static IServiceCollection AddWarehouseHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                configuration.GetConnectionString("WarehouseDb")!,
                name: "database",
                tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Registers correlation ID infrastructure: IHttpContextAccessor and the delegating handler.
    /// </summary>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdDelegatingHandler>();

        return services;
    }

    /// <summary>
    /// Registers Redis as the distributed cache (IDistributedCache) and adds a Redis readiness health check.
    /// Reads the connection string from <c>ConnectionStrings:Redis</c> (defaults to localhost:6379).
    /// </summary>
    public static IServiceCollection AddWarehouseRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "warehouse:";
        });

        services.AddHealthChecks()
            .AddRedis(redisConnection, name: "redis", tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Registers the open-generic <see cref="ICacheService{T}"/> backed by
    /// <see cref="DistributedCacheService{T}"/> as a singleton.
    /// Must be called after <see cref="AddWarehouseRedisCache"/>.
    /// </summary>
    public static IServiceCollection AddWarehouseCacheService(this IServiceCollection services)
    {
        services.AddSingleton(typeof(ICacheService<>), typeof(DistributedCacheService<>));

        return services;
    }

    /// <summary>
    /// Registers <see cref="IResilientPublisher"/> backed by <see cref="ResilientPublisher"/>
    /// as a scoped service. Must be called after <see cref="AddWarehouseMessageBus"/>.
    /// </summary>
    public static IServiceCollection AddWarehouseResilientPublisher(this IServiceCollection services)
    {
        services.AddScoped<IResilientPublisher, ResilientPublisher>();

        return services;
    }

    /// <summary>
    /// Registers a typed HttpClient with standard Polly resilience policies:
    /// retry (exponential backoff + jitter, 3 attempts), circuit breaker (5 failures, 30s break),
    /// and total request timeout (30s). Includes correlation ID propagation.
    /// </summary>
    public static IServiceCollection AddWarehouseHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        string baseAddress)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>(client =>
            {
                client.BaseAddress = new Uri(baseAddress);
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;

                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            });

        return services;
    }

    /// <summary>
    /// Registers OpenTelemetry distributed tracing with ASP.NET Core, HttpClient,
    /// and SQL Client auto-instrumentation, exporting via OTLP to Jaeger.
    /// </summary>
    public static IServiceCollection AddWarehouseTracing(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        string otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                })
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                }));

        return services;
    }

    /// <summary>
    /// Registers MassTransit with RabbitMQ transport. Consumers are registered via the
    /// <paramref name="configureConsumers"/> callback. Reads RabbitMQ connection settings
    /// from <c>RabbitMQ:Host</c>, <c>RabbitMQ:Username</c>, and <c>RabbitMQ:Password</c>.
    /// </summary>
    public static IServiceCollection AddWarehouseMessageBus(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        string host = configuration["RabbitMQ:Host"] ?? "localhost";
        string username = configuration["RabbitMQ:Username"] ?? "warehouse";
        string password = configuration["RabbitMQ:Password"] ?? "warehouse";

        services.AddMassTransit(bus =>
        {
            configureConsumers?.Invoke(bus);

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Registers Microsoft Feature Management, reading feature flag definitions
    /// from the <c>FeatureManagement</c> section of application configuration.
    /// </summary>
    public static IServiceCollection AddWarehouseFeatureFlags(this IServiceCollection services)
    {
        services.AddFeatureManagement();

        return services;
    }

    /// <summary>
    /// Registers the centralized <see cref="ISequenceGenerator"/> service (scoped)
    /// with all built-in sequence definitions. The generator executes raw SQL against
    /// the <typeparamref name="TContext"/> database connection.
    /// </summary>
    /// <typeparam name="TContext">
    /// The calling service's <see cref="DbContext"/> type (e.g., <c>InventoryDbContext</c>).
    /// </typeparam>
    public static IServiceCollection AddSequenceGenerator<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        IReadOnlyDictionary<string, SequenceDefinition> definitions =
            SequenceDefinitions.GetBuiltInDefinitions();

        services.AddSingleton(definitions);

        services.AddScoped<ISequenceGenerator>(sp =>
        {
            TContext context = sp.GetRequiredService<TContext>();
            IReadOnlyDictionary<string, SequenceDefinition> defs =
                sp.GetRequiredService<IReadOnlyDictionary<string, SequenceDefinition>>();
            return new SequenceGenerator(context, defs);
        });

        return services;
    }
}
