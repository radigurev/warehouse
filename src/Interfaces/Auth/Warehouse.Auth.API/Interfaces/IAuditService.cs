using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Auth.API.Interfaces;

/// <summary>
/// Defines operations for logging and querying user action audit entries.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an audit entry for a user action.
    /// </summary>
    Task LogAsync(int? userId, string action, string resource, string? details, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Gets paginated audit log entries with optional filters.
    /// </summary>
    Task<Result<PaginatedResponse<AuditLogDto>>> GetLogsAsync(
        int? userId,
        string? action,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
