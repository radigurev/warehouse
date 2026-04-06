using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Warehouse.Infrastructure.Middleware;

namespace Warehouse.Infrastructure.Extensions;

/// <summary>
/// Extension methods for <see cref="WebApplication"/> to configure the shared HTTP pipeline.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the shared middleware pipeline: correlation ID, exception handler, Swagger (in dev),
    /// authentication, authorization, controllers, and health check endpoints.
    /// </summary>
    public static WebApplication UseWarehousePipeline(
        this WebApplication app,
        string swaggerEndpointTitle)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", swaggerEndpointTitle);
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }
}
