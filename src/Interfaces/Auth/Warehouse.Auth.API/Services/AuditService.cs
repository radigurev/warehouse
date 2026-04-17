using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.Auth.DBModel;
using Warehouse.Auth.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.Infrastructure.Correlation;
using Warehouse.ServiceModel.Events;
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
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<AuditService> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public AuditService(
        AuthDbContext context,
        IMapper mapper,
        IPublishEndpoint publishEndpoint,
        ICorrelationIdAccessor correlationIdAccessor,
        ILogger<AuditService> logger)
    {
        _context = context;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAsync(
        int? userId,
        string action,
        string resource,
        string? details,
        string? ipAddress,
        CancellationToken cancellationToken,
        string? username = null)
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

        try
        {
            await _publishEndpoint.PublishWithCorrelationAsync(new AuthAuditLoggedEvent
            {
                UserId = userId,
                Action = action,
                Resource = resource,
                Details = details,
                IpAddress = ipAddress,
                Username = username,
                OccurredAtUtc = entry.CreatedAt
            }, _correlationIdAccessor, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish AuthAuditLoggedEvent for {Action} on {Resource}", action, resource);
        }
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
