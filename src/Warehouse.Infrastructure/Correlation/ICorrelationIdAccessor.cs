namespace Warehouse.Infrastructure.Correlation;

/// <summary>
/// Provides access to the current request's correlation ID, decoupled from <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// Gets the correlation ID from the current HTTP request context, or <c>null</c> if unavailable.
    /// </summary>
    string? CorrelationId { get; }
}
