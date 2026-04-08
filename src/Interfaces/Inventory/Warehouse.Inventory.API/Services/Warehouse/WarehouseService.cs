using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Interfaces.Warehouse;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Services.Warehouse;

/// <summary>
/// Implements warehouse management operations: CRUD and soft-delete.
/// <para>See <see cref="IWarehouseService"/>.</para>
/// </summary>
public sealed class WarehouseService : BaseInventoryEntityService, IWarehouseService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public WarehouseService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<WarehouseDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        WarehouseEntity? warehouse = await Context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (warehouse is null)
            return Result<WarehouseDto>.Failure("WAREHOUSE_NOT_FOUND", "Warehouse not found.", 404);

        WarehouseDto dto = Mapper.Map<WarehouseDto>(warehouse);
        return Result<WarehouseDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<WarehouseDto>>> SearchAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<WarehouseEntity> query = Context.Warehouses
            .AsNoTracking()
            .Where(w => !w.IsDeleted)
            .OrderBy(w => w.Name);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<WarehouseEntity> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<WarehouseDto> dtos = Mapper.Map<IReadOnlyList<WarehouseDto>>(items);

        PaginatedResponse<WarehouseDto> response = new()
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<WarehouseDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<WarehouseDto>> CreateAsync(
        CreateWarehouseRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        Result? codeValidation = await ValidateUniqueCodeAsync(request.Code, cancellationToken).ConfigureAwait(false);
        if (codeValidation is not null)
            return Result<WarehouseDto>.Failure(codeValidation.ErrorCode!, codeValidation.ErrorMessage!, codeValidation.StatusCode!.Value);

        WarehouseEntity warehouse = new()
        {
            Code = request.Code,
            Name = request.Name,
            Address = request.Address,
            Notes = request.Notes,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        Context.Warehouses.Add(warehouse);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        WarehouseDto dto = Mapper.Map<WarehouseDto>(warehouse);
        return Result<WarehouseDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<WarehouseDto>> UpdateAsync(
        int id,
        UpdateWarehouseRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        WarehouseEntity? warehouse = await Context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (warehouse is null)
            return Result<WarehouseDto>.Failure("WAREHOUSE_NOT_FOUND", "Warehouse not found.", 404);

        warehouse.Name = request.Name;
        warehouse.Address = request.Address;
        warehouse.Notes = request.Notes;
        warehouse.ModifiedAtUtc = DateTime.UtcNow;
        warehouse.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        WarehouseDto dto = Mapper.Map<WarehouseDto>(warehouse);
        return Result<WarehouseDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        WarehouseEntity? warehouse = await Context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (warehouse is null || warehouse.IsDeleted)
            return Result.Failure("WAREHOUSE_NOT_FOUND", "Warehouse not found.", 404);

        warehouse.IsDeleted = true;
        warehouse.DeletedAtUtc = DateTime.UtcNow;
        warehouse.IsActive = false;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<WarehouseDto>> ReactivateAsync(int id, int userId, CancellationToken cancellationToken)
    {
        WarehouseEntity? warehouse = await Context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (warehouse is null)
            return Result<WarehouseDto>.Failure("WAREHOUSE_NOT_FOUND", "Warehouse not found.", 404);

        if (!warehouse.IsDeleted)
            return Result<WarehouseDto>.Failure("WAREHOUSE_ALREADY_ACTIVE", "Warehouse is already active.", 400);

        warehouse.IsDeleted = false;
        warehouse.DeletedAtUtc = null;
        warehouse.IsActive = true;
        warehouse.ModifiedAtUtc = DateTime.UtcNow;
        warehouse.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        WarehouseDto dto = Mapper.Map<WarehouseDto>(warehouse);
        return Result<WarehouseDto>.Success(dto);
    }

    /// <summary>
    /// Validates warehouse code uniqueness.
    /// </summary>
    private async Task<Result?> ValidateUniqueCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        bool exists = await Context.Warehouses
            .AnyAsync(w => w.Code == code, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_WAREHOUSE_CODE", "A warehouse with this code already exists.", 409)
            : null;
    }
}
