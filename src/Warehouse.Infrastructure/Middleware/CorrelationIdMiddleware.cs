using Microsoft.AspNetCore.Http;

namespace Warehouse.Infrastructure.Middleware;

/// <summary>
/// Reads or generates an X-Correlation-ID header, stores it in HttpContext.Items,
/// sets the NLog mapped diagnostic context, and returns it in the response header.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>
    /// The HTTP header name used for correlation ID propagation.
    /// </summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// The key used to store the correlation ID in HttpContext.Items.
    /// </summary>
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance with the next middleware delegate.
    /// </summary>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Reads the correlation ID from the request header or generates a new one,
    /// stores it in the diagnostic context, and adds it to the response header.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("D");

        context.Items[ItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        IDisposable scopeProperty = NLog.ScopeContext.PushProperty(ItemKey, correlationId);

        try
        {
            await _next(context);
        }
        finally
        {
            scopeProperty.Dispose();
        }
    }
}
