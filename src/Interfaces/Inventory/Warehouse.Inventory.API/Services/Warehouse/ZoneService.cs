using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.GenericFiltering;
using Warehouse.Inventory.API.Interfaces;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Services;

/// <summary>
/// Implements zone lifecycle operations: CRUD and search.
/// <para>See <see cref="IZoneService"/>.</para>
/// </summary>
public sealed class ZoneService : BaseInventoryEntityService, IZoneService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public ZoneService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<ZoneDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Zone? zone = await GetZoneWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (zone is null)
            return Result<ZoneDetailDto>.Failure("ZONE_NOT_FOUND", "Zone not found.", 404);

        ZoneDetailDto dto = Mapper.Map<ZoneDetailDto>(zone);
        return Result<ZoneDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<ZoneDto>>> SearchAsync(
        SearchZonesRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<Zone> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<Zone> items = await query
            .OrderBy(z => z.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ZoneDto> dtos = Mapper.Map<IReadOnlyList<ZoneDto>>(items);

        PaginatedResponse<ZoneDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<ZoneDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<ZoneDetailDto>> CreateAsync(
        CreateZoneRequest request,
        CancellationToken cancellationToken)
    {
        Result? warehouseValidation = await ValidateWarehouseExistsAsync(request.WarehouseId, cancellationToken).ConfigureAwait(false);
        if (warehouseValidation is not null)
            return Result<ZoneDetailDto>.Failure(warehouseValidation.ErrorCode!, warehouseValidation.ErrorMessage!, warehouseValidation.StatusCode!.Value);

        Result? codeValidation = await ValidateUniqueCodeAsync(request.WarehouseId, request.Code, null, cancellationToken).ConfigureAwait(false);
        if (codeValidation is not null)
            return Result<ZoneDetailDto>.Failure(codeValidation.ErrorCode!, codeValidation.ErrorMessage!, codeValidation.StatusCode!.Value);

        Zone zone = new()
        {
            WarehouseId = request.WarehouseId,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.Zones.Add(zone);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Zone? created = await GetZoneWithDetailsAsync(zone.Id, cancellationToken).ConfigureAwait(false);
        ZoneDetailDto dto = Mapper.Map<ZoneDetailDto>(created!);
        return Result<ZoneDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<ZoneDetailDto>> UpdateAsync(
        int id,
        UpdateZoneRequest request,
        CancellationToken cancellationToken)
    {
        Zone? zone = await GetZoneWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (zone is null)
            return Result<ZoneDetailDto>.Failure("ZONE_NOT_FOUND", "Zone not found.", 404);

        zone.Name = request.Name;
        zone.Description = request.Description;
        zone.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Zone? updated = await GetZoneWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        ZoneDetailDto dto = Mapper.Map<ZoneDetailDto>(updated!);
        return Result<ZoneDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        Zone? zone = await Context.Zones
            .FirstOrDefaultAsync(z => z.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (zone is null)
            return Result.Failure("ZONE_NOT_FOUND", "Zone not found.", 404);

        Context.Zones.Remove(zone);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Loads a zone with warehouse and storage location details.
    /// </summary>
    private async Task<Zone?> GetZoneWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.Zones
            .Include(z => z.Warehouse)
            .Include(z => z.Locations)
            .FirstOrDefaultAsync(z => z.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<Zone> BuildSearchQuery(SearchZonesRequest request)
    {
        IQueryable<Zone> query = Context.Zones
            .AsNoTracking()
            .Include(z => z.Warehouse);

        if (request.WarehouseId.HasValue)
            query = query.Where(z => z.WarehouseId == request.WarehouseId.Value);

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(z => z.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Code))
            query = query.Where(z => z.Code.StartsWith(request.Code));

        query = query.ApplyFilter(request.Filter);

        return query;
    }

    /// <summary>
    /// Validates that the warehouse exists and is not deleted.
    /// </summary>
    private async Task<Result?> ValidateWarehouseExistsAsync(int warehouseId, CancellationToken cancellationToken)
    {
        bool exists = await Context.Warehouses
            .AnyAsync(w => w.Id == warehouseId && !w.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : Result.Failure("INVALID_WAREHOUSE", "The specified warehouse does not exist.", 400);
    }

    /// <summary>
    /// Validates zone code uniqueness within a warehouse.
    /// </summary>
    private async Task<Result?> ValidateUniqueCodeAsync(
        int warehouseId,
        string code,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<Zone> query = Context.Zones
            .Where(z => z.WarehouseId == warehouseId && z.Code == code);

        if (excludeId.HasValue)
            query = query.Where(z => z.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_ZONE_CODE", "A zone with this code already exists in the warehouse.", 409)
            : null;
    }
}
