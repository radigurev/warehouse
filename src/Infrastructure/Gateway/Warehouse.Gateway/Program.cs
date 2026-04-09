using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NLog;
using NLog.Web;
using Warehouse.Infrastructure.Extensions;
using Warehouse.Infrastructure.Middleware;

Logger logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting Warehouse.Gateway");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    builder.Services.AddWarehouseTracing(builder.Configuration, "warehouse-gateway");

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("fixed", limiter =>
        {
            limiter.PermitLimit = 100;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 10;
        });

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20
                }));
    });

    string authUrl = builder.Configuration["HealthChecks:AuthApi"] ?? "http://localhost:5001";
    string customersUrl = builder.Configuration["HealthChecks:CustomersApi"] ?? "http://localhost:5002";
    string inventoryUrl = builder.Configuration["HealthChecks:InventoryApi"] ?? "http://localhost:5003";
    string purchasingUrl = builder.Configuration["HealthChecks:PurchasingApi"] ?? "http://localhost:5004";
    string fulfillmentUrl = builder.Configuration["HealthChecks:FulfillmentApi"] ?? "http://localhost:5005";

    builder.Services.AddHealthChecks()
        .AddUrlGroup(new Uri($"{authUrl}/health/ready"), "auth-api", tags: ["ready"])
        .AddUrlGroup(new Uri($"{customersUrl}/health/ready"), "customers-api", tags: ["ready"])
        .AddUrlGroup(new Uri($"{inventoryUrl}/health/ready"), "inventory-api", tags: ["ready"])
        .AddUrlGroup(new Uri($"{purchasingUrl}/health/ready"), "purchasing-api", tags: ["ready"])
        .AddUrlGroup(new Uri($"{fulfillmentUrl}/health/ready"), "fulfillment-api", tags: ["ready"]);

    WebApplication app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseRateLimiter();

    app.MapHealthChecks("/health");
    app.MapReverseProxy();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Gateway startup failed");
    throw;
}
finally
{
    LogManager.Shutdown();
}
