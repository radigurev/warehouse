using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Configuration;
using Warehouse.Infrastructure.Http;

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
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

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
}
