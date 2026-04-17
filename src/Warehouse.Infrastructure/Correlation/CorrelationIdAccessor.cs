using Microsoft.AspNetCore.Http;
using Warehouse.Infrastructure.Middleware;

namespace Warehouse.Infrastructure.Correlation;

/// <summary>
/// Reads the correlation ID from <see cref="HttpContext.Items"/> where it was stored by <see cref="CorrelationIdMiddleware"/>.
/// Returns <c>null</c> when no HTTP context is available (background tasks, MassTransit consumers).
/// </summary>
public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance with the HTTP context accessor.
    /// </summary>
    public CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string? CorrelationId
    {
        get
        {
            HttpContext? httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is null)
                return null;

            if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out object? value)
                && value is string correlationId)
            {
                return correlationId;
            }

            return null;
        }
    }
}
