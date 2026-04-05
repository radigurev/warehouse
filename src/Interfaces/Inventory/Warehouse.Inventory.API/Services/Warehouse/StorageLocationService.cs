using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.GenericFiltering;
using Warehouse.Inventory.API.Interfaces.Warehouse;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Services.Warehouse;

/// <summary>
/// Implements storage location lifecycle operations: CRUD and search.
/// <para>See <see cref="IStorageLocationService"/>.</para>
/// </summary>
public sealed class StorageLocationService : BaseInventoryEntityService, IStorageLocationService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public StorageLocationService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<StorageLocationDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        StorageLocation? location = await GetLocationWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (location is null)
            return Result<StorageLocationDto>.Failure("LOCATION_NOT_FOUND", "Storage location not found.", 404);

        StorageLocationDto dto = Mapper.Map<StorageLocationDto>(location);
        return Result<StorageLocationDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<StorageLocationDto>>> SearchAsync(
        SearchStorageLocationsRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<StorageLocation> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<StorageLocation> items = await query
            .OrderBy(l => l.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<StorageLocationDto> dtos = Mapper.Map<IReadOnlyList<StorageLocationDto>>(items);

        PaginatedResponse<StorageLocationDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<StorageLocationDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<StorageLocationDto>> CreateAsync(
        CreateStorageLocationRequest request,
        CancellationToken cancellationToken)
    {
        Zone? zone = await Context.Zones
            .Include(z => z.Warehouse)
            .FirstOrDefaultAsync(z => z.Id == request.ZoneId, cancellationToken)
            .ConfigureAwait(false);

        if (zone is null)
            return Result<StorageLocationDto>.Failure("INVALID_ZONE", "The specified zone does not exist.", 400);

        Result? codeValidation = await ValidateUniqueCodeAsync(zone.WarehouseId, request.Code, null, cancellationToken).ConfigureAwait(false);
        if (codeValidation is not null)
            return Result<StorageLocationDto>.Failure(codeValidation.ErrorCode!, codeValidation.ErrorMessage!, codeValidation.StatusCode!.Value);

        StorageLocation location = new()
        {
            WarehouseId = zone.WarehouseId,
            ZoneId = request.ZoneId,
            Code = request.Code,
            Name = request.Name,
            LocationType = request.LocationType,
            Capacity = request.Capacity,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.StorageLocations.Add(location);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        StorageLocation? created = await GetLocationWithDetailsAsync(location.Id, cancellationToken).ConfigureAwait(false);
        StorageLocationDto dto = Mapper.Map<StorageLocationDto>(created!);
        return Result<StorageLocationDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<StorageLocationDto>> UpdateAsync(
        int id,
        UpdateStorageLocationRequest request,
        CancellationToken cancellationToken)
    {
        StorageLocation? location = await GetLocationWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (location is null)
            return Result<StorageLocationDto>.Failure("LOCATION_NOT_FOUND", "Storage location not found.", 404);

        location.Name = request.Name;
        location.LocationType = request.LocationType;
        location.Capacity = request.Capacity;
        location.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        StorageLocation? updated = await GetLocationWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        StorageLocationDto dto = Mapper.Map<StorageLocationDto>(updated!);
        return Result<StorageLocationDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        StorageLocation? location = await Context.StorageLocations
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (location is null)
            return Result.Failure("LOCATION_NOT_FOUND", "Storage location not found.", 404);

        bool hasStock = await Context.StockLevels
            .AnyAsync(s => s.LocationId == id && s.QuantityOnHand > 0, cancellationToken)
            .ConfigureAwait(false);

        if (hasStock)
            return Result.Failure("LOCATION_HAS_STOCK", "Cannot delete location that has stock.", 409);

        Context.StorageLocations.Remove(location);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Loads a storage location with warehouse and zone details.
    /// </summary>
    private async Task<StorageLocation?> GetLocationWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.StorageLocations
            .Include(l => l.Warehouse)
            .Include(l => l.Zone)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<StorageLocation> BuildSearchQuery(SearchStorageLocationsRequest request)
    {
        IQueryable<StorageLocation> query = Context.StorageLocations
            .AsNoTracking()
            .Include(l => l.Warehouse)
            .Include(l => l.Zone);

        if (request.ZoneId.HasValue)
            query = query.Where(l => l.ZoneId == request.ZoneId.Value);

        if (request.WarehouseId.HasValue)
            query = query.Where(l => l.WarehouseId == request.WarehouseId.Value);

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(l => l.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Code))
            query = query.Where(l => l.Code.StartsWith(request.Code));

        query = query.ApplyFilter(request.Filter);

        return query;
    }

    /// <summary>
    /// Validates location code uniqueness within a warehouse.
    /// </summary>
    private async Task<Result?> ValidateUniqueCodeAsync(
        int warehouseId,
        string code,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<StorageLocation> query = Context.StorageLocations
            .Where(l => l.WarehouseId == warehouseId && l.Code == code);

        if (excludeId.HasValue)
            query = query.Where(l => l.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_LOCATION_CODE", "A storage location with this code already exists in the warehouse.", 409)
            : null;
    }
}
