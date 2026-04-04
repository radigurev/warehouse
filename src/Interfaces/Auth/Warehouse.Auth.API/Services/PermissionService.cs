using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.Auth.DBModel;
using Warehouse.Auth.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// Implements permission management operations.
/// <para>See <see cref="IPermissionService"/>.</para>
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private readonly AuthDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public PermissionService(
        AuthDbContext context,
        IAuditService auditService,
        IMapper mapper)
    {
        _context = context;
        _auditService = auditService;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PermissionDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<Permission> permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Resource).ThenBy(p => p.Action)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<PermissionDto> dtos = _mapper.Map<IReadOnlyList<PermissionDto>>(permissions);
        return Result<IReadOnlyList<PermissionDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<PermissionDto>> CreateAsync(
        CreatePermissionRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        bool exists = await _context.Permissions
            .AnyAsync(p => p.Resource == request.Resource && p.Action == request.Action, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
            return Result<PermissionDto>.Failure("DUPLICATE_PERMISSION", "A permission with this resource and action already exists.", 409);

        Permission permission = new()
        {
            Resource = request.Resource,
            Action = request.Action,
            Description = request.Description
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditService.LogAsync(null, "CreatePermission", "permissions", null, ipAddress, cancellationToken).ConfigureAwait(false);

        PermissionDto dto = _mapper.Map<PermissionDto>(permission);
        return Result<PermissionDto>.Success(dto);
    }
}
