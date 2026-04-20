using Microsoft.AspNetCore.Http;

namespace Warehouse.Infrastructure.Authorization;

/// <summary>
/// Forwards the inbound request's <c>Authorization</c> header onto outbound calls so downstream
/// microservices (notably Auth.API) can authenticate the original caller. Used by the
/// <see cref="UserPermissionService"/> typed <see cref="HttpClient"/> when fetching permissions
/// from Auth.API on a Redis cache miss.
/// <para>
/// Without this handler the outbound call goes anonymous, Auth.API returns 401, and the
/// permission resolver fails closed (CHG-FIX-001).
/// </para>
/// </summary>
public sealed class BearerTokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance with the ASP.NET Core HTTP context accessor.
    /// </summary>
    public BearerTokenForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Copies the inbound <c>Authorization</c> header (if present) onto the outbound request before sending.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            string? authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorization))
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
