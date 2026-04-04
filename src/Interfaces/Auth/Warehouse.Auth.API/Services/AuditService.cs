using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.Auth.DBModel;
using Warehouse.Auth.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// Logs and queries user action audit entries.
/// <para>See <see cref="IAuditService"/>.</para>
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly AuthDbContext _context;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public AuditService(AuthDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task LogAsync(
        int? userId,
        string action,
        string resource,
        string? details,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        UserActionLog entry = new()
        {
            UserId = userId,
            Action = action,
            Resource = resource,
            Details = details,
            IpAddress = ipAddress
        };

        _context.UserActionLogs.Add(entry);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<AuditLogDto>>> GetLogsAsync(
        int? userId,
        string? action,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<UserActionLog> query = _context.UserActionLogs.AsNoTracking();

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);
        if (fromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(l => l.CreatedAt <= toDate.Value);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<UserActionLog> items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<AuditLogDto> dtos = _mapper.Map<IReadOnlyList<AuditLogDto>>(items);

        PaginatedResponse<AuditLogDto> response = new()
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<AuditLogDto>>.Success(response);
    }
}
