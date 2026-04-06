using NLog;
using NLog.Web;
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

    builder.Services.AddHealthChecks()
        .AddUrlGroup(new Uri("http://localhost:5001/health/ready"), "auth-api", tags: ["ready"])
        .AddUrlGroup(new Uri("http://localhost:5002/health/ready"), "customers-api", tags: ["ready"])
        .AddUrlGroup(new Uri("http://localhost:5003/health/ready"), "inventory-api", tags: ["ready"]);

    WebApplication app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();

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
