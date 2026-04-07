using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services.Base;
using Warehouse.Fulfillment.DBModel;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Services;

/// <summary>
/// Implements carrier management operations: CRUD, service levels, deactivation.
/// <para>See <see cref="ICarrierService"/>.</para>
/// </summary>
public sealed class CarrierService : BaseFulfillmentEntityService, ICarrierService
{
    private const string CacheKey = "fulfillment:carriers:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CarrierService(FulfillmentDbContext context, IMapper mapper, IDistributedCache cache)
        : base(context, mapper)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<CarrierDetailDto>> CreateAsync(CreateCarrierRequest request, int userId, CancellationToken cancellationToken)
    {
        bool codeExists = await Context.Carriers.AnyAsync(c => c.Code == request.Code, cancellationToken).ConfigureAwait(false);
        if (codeExists) return Result<CarrierDetailDto>.Failure("DUPLICATE_CARRIER_CODE", "A carrier with this code already exists.", 409);

        Carrier carrier = new()
        {
            Code = request.Code, Name = request.Name, ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail, WebsiteUrl = request.WebsiteUrl,
            TrackingUrlTemplate = request.TrackingUrlTemplate, Notes = request.Notes,
            IsActive = true, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = userId
        };

        Context.Carriers.Add(carrier);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        return await GetByIdAsync(carrier.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<CarrierDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Carrier? carrier = await Context.Carriers.Include(c => c.ServiceLevels).FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
        if (carrier is null) return Result<CarrierDetailDto>.Failure("CARRIER_NOT_FOUND", "Carrier not found.", 404);
        CarrierDetailDto dto = Mapper.Map<CarrierDetailDto>(carrier);
        return Result<CarrierDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<CarrierDto>>> SearchAsync(SearchCarriersRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Carrier> query = Context.Carriers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Name)) query = query.Where(c => c.Name.Contains(request.Name));
        if (!string.IsNullOrWhiteSpace(request.Code)) query = query.Where(c => c.Code.StartsWith(request.Code));
        if (request.IsActive.HasValue) query = query.Where(c => c.IsActive == request.IsActive.Value);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        List<Carrier> items = await query.OrderBy(c => c.Name).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<CarrierDto> dtos = Mapper.Map<IReadOnlyList<CarrierDto>>(items);
        PaginatedResponse<CarrierDto> response = new() { Items = dtos, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
        return Result<PaginatedResponse<CarrierDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<CarrierDetailDto>> UpdateAsync(int id, UpdateCarrierRequest request, int userId, CancellationToken cancellationToken)
    {
        Carrier? carrier = await Context.Carriers.Include(c => c.ServiceLevels).FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
        if (carrier is null) return Result<CarrierDetailDto>.Failure("CARRIER_NOT_FOUND", "Carrier not found.", 404);

        carrier.Name = request.Name; carrier.ContactPhone = request.ContactPhone; carrier.ContactEmail = request.ContactEmail;
        carrier.WebsiteUrl = request.WebsiteUrl; carrier.TrackingUrlTemplate = request.TrackingUrlTemplate;
        carrier.Notes = request.Notes; carrier.IsActive = request.IsActive;
        carrier.ModifiedAtUtc = DateTime.UtcNow; carrier.ModifiedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        CarrierDetailDto dto = Mapper.Map<CarrierDetailDto>(carrier);
        return Result<CarrierDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CarrierDetailDto>> DeactivateAsync(int id, int userId, CancellationToken cancellationToken)
    {
        Carrier? carrier = await Context.Carriers.Include(c => c.ServiceLevels).FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
        if (carrier is null) return Result<CarrierDetailDto>.Failure("CARRIER_NOT_FOUND", "Carrier not found.", 404);

        bool hasActiveShipments = await Context.Shipments.AnyAsync(s => s.CarrierId == id && (s.Status == nameof(ShipmentStatus.Dispatched) || s.Status == nameof(ShipmentStatus.InTransit)), cancellationToken).ConfigureAwait(false);
        if (hasActiveShipments) return Result<CarrierDetailDto>.Failure("CARRIER_HAS_ACTIVE_SHIPMENTS", "Cannot deactivate carrier -- active shipments exist.", 409);

        carrier.IsActive = false; carrier.ModifiedAtUtc = DateTime.UtcNow; carrier.ModifiedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        CarrierDetailDto dto = Mapper.Map<CarrierDetailDto>(carrier);
        return Result<CarrierDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CarrierServiceLevelDto>> CreateServiceLevelAsync(int carrierId, CreateCarrierServiceLevelRequest request, CancellationToken cancellationToken)
    {
        bool carrierExists = await Context.Carriers.AnyAsync(c => c.Id == carrierId, cancellationToken).ConfigureAwait(false);
        if (!carrierExists) return Result<CarrierServiceLevelDto>.Failure("CARRIER_NOT_FOUND", "Carrier not found.", 404);

        bool codeExists = await Context.CarrierServiceLevels.AnyAsync(sl => sl.CarrierId == carrierId && sl.Code == request.Code, cancellationToken).ConfigureAwait(false);
        if (codeExists) return Result<CarrierServiceLevelDto>.Failure("DUPLICATE_SERVICE_LEVEL_CODE", "A service level with this code already exists for this carrier.", 409);

        CarrierServiceLevel level = new()
        {
            CarrierId = carrierId, Code = request.Code, Name = request.Name,
            EstimatedDeliveryDays = request.EstimatedDeliveryDays, BaseRate = request.BaseRate,
            PerKgRate = request.PerKgRate, Notes = request.Notes, CreatedAtUtc = DateTime.UtcNow
        };

        Context.CarrierServiceLevels.Add(level);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CarrierServiceLevelDto dto = Mapper.Map<CarrierServiceLevelDto>(level);
        return Result<CarrierServiceLevelDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CarrierServiceLevelDto>>> ListServiceLevelsAsync(int carrierId, CancellationToken cancellationToken)
    {
        bool carrierExists = await Context.Carriers.AnyAsync(c => c.Id == carrierId, cancellationToken).ConfigureAwait(false);
        if (!carrierExists) return Result<IReadOnlyList<CarrierServiceLevelDto>>.Failure("CARRIER_NOT_FOUND", "Carrier not found.", 404);

        List<CarrierServiceLevel> levels = await Context.CarrierServiceLevels.Where(sl => sl.CarrierId == carrierId).AsNoTracking().OrderBy(sl => sl.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<CarrierServiceLevelDto> dtos = Mapper.Map<IReadOnlyList<CarrierServiceLevelDto>>(levels);
        return Result<IReadOnlyList<CarrierServiceLevelDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<CarrierServiceLevelDto>> UpdateServiceLevelAsync(int carrierId, int levelId, UpdateCarrierServiceLevelRequest request, CancellationToken cancellationToken)
    {
        CarrierServiceLevel? level = await Context.CarrierServiceLevels.FirstOrDefaultAsync(sl => sl.Id == levelId && sl.CarrierId == carrierId, cancellationToken).ConfigureAwait(false);
        if (level is null) return Result<CarrierServiceLevelDto>.Failure("SERVICE_LEVEL_NOT_FOUND", "Carrier service level not found.", 404);

        level.Name = request.Name; level.EstimatedDeliveryDays = request.EstimatedDeliveryDays;
        level.BaseRate = request.BaseRate; level.PerKgRate = request.PerKgRate;
        level.Notes = request.Notes; level.ModifiedAtUtc = DateTime.UtcNow;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CarrierServiceLevelDto dto = Mapper.Map<CarrierServiceLevelDto>(level);
        return Result<CarrierServiceLevelDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteServiceLevelAsync(int carrierId, int levelId, CancellationToken cancellationToken)
    {
        CarrierServiceLevel? level = await Context.CarrierServiceLevels.FirstOrDefaultAsync(sl => sl.Id == levelId && sl.CarrierId == carrierId, cancellationToken).ConfigureAwait(false);
        if (level is null) return Result.Failure("SERVICE_LEVEL_NOT_FOUND", "Carrier service level not found.", 404);

        bool inUse = await Context.Shipments.AnyAsync(s => s.CarrierServiceLevelId == levelId, cancellationToken).ConfigureAwait(false);
        if (inUse) return Result.Failure("SERVICE_LEVEL_IN_USE", "Cannot delete service level -- it is referenced by shipments.", 409);

        Context.CarrierServiceLevels.Remove(level);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task InvalidateCacheAsync(CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(CacheKey, cancellationToken).ConfigureAwait(false);
    }
}
