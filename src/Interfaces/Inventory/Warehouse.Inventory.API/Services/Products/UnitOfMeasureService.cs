using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Interfaces.Products;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Services.Products;

/// <summary>
/// Implements unit of measure management operations.
/// <para>See <see cref="IUnitOfMeasureService"/>.</para>
/// </summary>
public sealed class UnitOfMeasureService : BaseInventoryEntityService, IUnitOfMeasureService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public UnitOfMeasureService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<UnitOfMeasureDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        UnitOfMeasure? unit = await Context.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (unit is null)
            return Result<UnitOfMeasureDto>.Failure("UNIT_NOT_FOUND", "Unit of measure not found.", 404);

        UnitOfMeasureDto dto = Mapper.Map<UnitOfMeasureDto>(unit);
        return Result<UnitOfMeasureDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<UnitOfMeasureDto>>> ListAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        IQueryable<UnitOfMeasure> query = Context.UnitsOfMeasure
            .AsNoTracking()
            .OrderBy(u => u.Name);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<UnitOfMeasure> units = await query
            .Skip(pagination.Skip)
            .Take(pagination.EffectivePageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<UnitOfMeasureDto> dtos = Mapper.Map<IReadOnlyList<UnitOfMeasureDto>>(units);

        PaginatedResponse<UnitOfMeasureDto> response = new()
        {
            Items = dtos,
            Page = pagination.Page,
            PageSize = pagination.EffectivePageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<UnitOfMeasureDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<UnitOfMeasureDto>> CreateAsync(
        CreateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        Result? codeValidation = await ValidateUniqueCodeAsync(request.Code, null, cancellationToken).ConfigureAwait(false);
        if (codeValidation is not null)
            return Result<UnitOfMeasureDto>.Failure(codeValidation.ErrorCode!, codeValidation.ErrorMessage!, codeValidation.StatusCode!.Value);

        UnitOfMeasure unit = new()
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.UnitsOfMeasure.Add(unit);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        UnitOfMeasureDto dto = Mapper.Map<UnitOfMeasureDto>(unit);
        return Result<UnitOfMeasureDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<UnitOfMeasureDto>> UpdateAsync(
        int id,
        UpdateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        UnitOfMeasure? unit = await Context.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (unit is null)
            return Result<UnitOfMeasureDto>.Failure("UNIT_NOT_FOUND", "Unit of measure not found.", 404);

        unit.Name = request.Name;
        unit.Description = request.Description;
        unit.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        UnitOfMeasureDto dto = Mapper.Map<UnitOfMeasureDto>(unit);
        return Result<UnitOfMeasureDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        UnitOfMeasure? unit = await Context.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (unit is null)
            return Result.Failure("UNIT_NOT_FOUND", "Unit of measure not found.", 404);

        int productCount = await Context.Products
            .CountAsync(p => p.UnitOfMeasureId == id && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (productCount > 0)
            return Result.Failure("UNIT_IN_USE", $"Cannot delete unit -- it is assigned to {productCount} product(s).", 409);

        Context.UnitsOfMeasure.Remove(unit);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Validates unit code uniqueness.
    /// </summary>
    private async Task<Result?> ValidateUniqueCodeAsync(
        string code,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<UnitOfMeasure> query = Context.UnitsOfMeasure.Where(u => u.Code == code);

        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_UNIT_CODE", "A unit of measure with this code already exists.", 409)
            : null;
    }
}
