using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Auth.API.Controllers;

/// <summary>
/// Handles audit log query operations.
/// <para>See <see cref="IAuditService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit")]
[RequirePermission("audit:read")]
public sealed class AuditController : BaseApiController
{
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance with the specified audit service.
    /// </summary>
    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Queries audit logs with optional filters.
    /// </summary>
    [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogsAsync(
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Result<PaginatedResponse<AuditLogDto>> result = await _auditService
            .GetLogsAsync(userId, action, fromDate, toDate, page, pageSize, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Queries audit logs for a specific user.
    /// </summary>
    [HttpGet("users/{userId:int}")]
        [ProducesResponseType(typeof(PaginatedResponse<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAuditLogsAsync(
        int userId,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Result<PaginatedResponse<AuditLogDto>> result = await _auditService
            .GetLogsAsync(userId, action, fromDate, toDate, page, pageSize, cancellationToken);

        return ToActionResult(result);
    }
}
