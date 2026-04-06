using Microsoft.AspNetCore.Http;
using Warehouse.Infrastructure.Middleware;

namespace Warehouse.Infrastructure.Http;

/// <summary>
/// Propagates the X-Correlation-ID header from the current request context to outbound HTTP calls.
/// <para>Register as a transient delegating handler via <see cref="IHttpClientBuilder"/>.</para>
/// </summary>
public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance with the HTTP context accessor for reading the correlation ID.
    /// </summary>
    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Adds the correlation ID header to the outbound request if available.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.ItemKey] is string correlationId)
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
