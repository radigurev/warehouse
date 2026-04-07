using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services.Base;
using Warehouse.Fulfillment.DBModel;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Services;

/// <summary>
/// Implements packing operations: create/update/remove parcels, add/remove items.
/// <para>See <see cref="IPackingService"/>.</para>
/// </summary>
public sealed class PackingService : BaseFulfillmentEntityService, IPackingService
{
    private readonly IFulfillmentEventService _eventService;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public PackingService(FulfillmentDbContext context, IMapper mapper, IFulfillmentEventService eventService)
        : base(context, mapper)
    {
        _eventService = eventService;
    }

    /// <inheritdoc />
    public async Task<Result<ParcelDto>> CreateParcelAsync(int soId, CreateParcelRequest request, int userId, CancellationToken cancellationToken)
    {
        SalesOrder? so = await Context.SalesOrders.FirstOrDefaultAsync(s => s.Id == soId, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result<ParcelDto>.Failure("SO_NOT_FOUND", "Sales order not found.", 404);

        bool hasCompletedPicks = await Context.PickLists.AnyAsync(pl => pl.SalesOrderId == soId && pl.Status == nameof(PickListStatus.Completed), cancellationToken).ConfigureAwait(false);
        if (so.Status != nameof(SalesOrderStatus.Picking) && so.Status != nameof(SalesOrderStatus.Packed))
            return Result<ParcelDto>.Failure("SO_NOT_PACKABLE", "Sales order is not in a packable status.", 409);
        if (so.Status == nameof(SalesOrderStatus.Picking) && !hasCompletedPicks)
            return Result<ParcelDto>.Failure("SO_NOT_PACKABLE", "Sales order is not in a packable status.", 409);

        string parcelNumber = await GenerateParcelNumberAsync(cancellationToken).ConfigureAwait(false);

        Parcel parcel = new()
        {
            ParcelNumber = parcelNumber, SalesOrderId = soId, Weight = request.Weight,
            Length = request.Length, Width = request.Width, Height = request.Height,
            TrackingNumber = request.TrackingNumber, Notes = request.Notes,
            CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = userId
        };

        Context.Parcels.Add(parcel);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("ParcelCreated", "Parcel", parcel.Id, userId, null, cancellationToken).ConfigureAwait(false);

        ParcelDto dto = Mapper.Map<ParcelDto>(parcel);
        return Result<ParcelDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<ParcelDto>> GetParcelByIdAsync(int soId, int parcelId, CancellationToken cancellationToken)
    {
        Parcel? parcel = await Context.Parcels.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == parcelId && p.SalesOrderId == soId, cancellationToken).ConfigureAwait(false);
        if (parcel is null) return Result<ParcelDto>.Failure("PARCEL_NOT_FOUND", "Parcel not found.", 404);
        ParcelDto dto = Mapper.Map<ParcelDto>(parcel);
        return Result<ParcelDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ParcelDto>>> ListParcelsAsync(int soId, CancellationToken cancellationToken)
    {
        List<Parcel> parcels = await Context.Parcels.Include(p => p.Items).Where(p => p.SalesOrderId == soId).AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<ParcelDto> dtos = Mapper.Map<IReadOnlyList<ParcelDto>>(parcels);
        return Result<IReadOnlyList<ParcelDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<ParcelDto>> UpdateParcelAsync(int soId, int parcelId, UpdateParcelRequest request, CancellationToken cancellationToken)
    {
        SalesOrder? so = await Context.SalesOrders.FirstOrDefaultAsync(s => s.Id == soId, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result<ParcelDto>.Failure("SO_NOT_FOUND", "Sales order not found.", 404);
        if (so.Status == nameof(SalesOrderStatus.Shipped) || so.Status == nameof(SalesOrderStatus.Completed))
            return Result<ParcelDto>.Failure("PARCEL_NOT_EDITABLE", "Parcel cannot be edited after shipment dispatch.", 409);

        Parcel? parcel = await Context.Parcels.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == parcelId && p.SalesOrderId == soId, cancellationToken).ConfigureAwait(false);
        if (parcel is null) return Result<ParcelDto>.Failure("PARCEL_NOT_FOUND", "Parcel not found.", 404);

        parcel.Weight = request.Weight; parcel.Length = request.Length; parcel.Width = request.Width; parcel.Height = request.Height;
        parcel.TrackingNumber = request.TrackingNumber; parcel.Notes = request.Notes; parcel.ModifiedAtUtc = DateTime.UtcNow;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        ParcelDto dto = Mapper.Map<ParcelDto>(parcel);
        return Result<ParcelDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> RemoveParcelAsync(int soId, int parcelId, CancellationToken cancellationToken)
    {
        SalesOrder? so = await Context.SalesOrders.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == soId, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result.Failure("SO_NOT_FOUND", "Sales order not found.", 404);
        if (so.Status == nameof(SalesOrderStatus.Shipped) || so.Status == nameof(SalesOrderStatus.Completed))
            return Result.Failure("PARCEL_NOT_EDITABLE", "Parcel cannot be edited after shipment dispatch.", 409);

        Parcel? parcel = await Context.Parcels.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == parcelId && p.SalesOrderId == soId, cancellationToken).ConfigureAwait(false);
        if (parcel is null) return Result.Failure("PARCEL_NOT_FOUND", "Parcel not found.", 404);

        foreach (ParcelItem item in parcel.Items)
        {
            SalesOrderLine? soLine = so.Lines.FirstOrDefault(l => l.ProductId == item.ProductId);
            if (soLine is not null) soLine.PackedQuantity -= item.Quantity;
        }

        Context.Parcels.Remove(parcel);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<ParcelItemDto>> AddItemAsync(int soId, int parcelId, AddParcelItemRequest request, CancellationToken cancellationToken)
    {
        Parcel? parcel = await Context.Parcels.FirstOrDefaultAsync(p => p.Id == parcelId && p.SalesOrderId == soId, cancellationToken).ConfigureAwait(false);
        if (parcel is null) return Result<ParcelItemDto>.Failure("PARCEL_NOT_FOUND", "Parcel not found.", 404);

        PickListLine? pickLine = await Context.PickListLines.FirstOrDefaultAsync(l => l.Id == request.PickListLineId, cancellationToken).ConfigureAwait(false);
        if (pickLine is null || pickLine.Status != nameof(PickListStatus.Completed))
            return Result<ParcelItemDto>.Failure("INVALID_PICK_LINE", "The specified pick list line has not been picked or does not exist.", 400);

        decimal totalPickedForProduct = await Context.PickListLines.Where(l => l.PickList.SalesOrderId == soId && l.ProductId == request.ProductId && l.Status == nameof(PickListStatus.Completed)).SumAsync(l => l.ActualQuantity ?? 0, cancellationToken).ConfigureAwait(false);
        decimal totalPackedForProduct = await Context.ParcelItems.Where(pi => pi.Parcel.SalesOrderId == soId && pi.ProductId == request.ProductId).SumAsync(pi => pi.Quantity, cancellationToken).ConfigureAwait(false);

        if (totalPackedForProduct + request.Quantity > totalPickedForProduct)
            return Result<ParcelItemDto>.Failure("OVER_PACK", "Packed quantity exceeds the picked quantity for this product.", 409);

        ParcelItem item = new() { ParcelId = parcelId, PickListLineId = request.PickListLineId, ProductId = request.ProductId, Quantity = request.Quantity };
        Context.ParcelItems.Add(item);

        SalesOrderLine? soLine = await Context.SalesOrderLines.FirstOrDefaultAsync(l => l.SalesOrderId == soId && l.ProductId == request.ProductId, cancellationToken).ConfigureAwait(false);
        if (soLine is not null) soLine.PackedQuantity += request.Quantity;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await CheckAndTransitionToPackedAsync(soId, cancellationToken).ConfigureAwait(false);

        ParcelItemDto dto = Mapper.Map<ParcelItemDto>(item);
        return Result<ParcelItemDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> RemoveItemAsync(int soId, int parcelId, int itemId, CancellationToken cancellationToken)
    {
        ParcelItem? item = await Context.ParcelItems.FirstOrDefaultAsync(pi => pi.Id == itemId && pi.ParcelId == parcelId && pi.Parcel.SalesOrderId == soId, cancellationToken).ConfigureAwait(false);
        if (item is null) return Result.Failure("PARCEL_NOT_FOUND", "Parcel item not found.", 404);

        SalesOrderLine? soLine = await Context.SalesOrderLines.FirstOrDefaultAsync(l => l.SalesOrderId == soId && l.ProductId == item.ProductId, cancellationToken).ConfigureAwait(false);
        if (soLine is not null) soLine.PackedQuantity -= item.Quantity;

        Context.ParcelItems.Remove(item);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task CheckAndTransitionToPackedAsync(int soId, CancellationToken cancellationToken)
    {
        SalesOrder? so = await Context.SalesOrders.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == soId, cancellationToken).ConfigureAwait(false);
        if (so is null) return;

        bool allPacked = so.Lines.All(l => l.PackedQuantity >= l.OrderedQuantity);
        if (allPacked && so.Status == nameof(SalesOrderStatus.Picking))
        {
            so.Status = nameof(SalesOrderStatus.Packed);
            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string> GenerateParcelNumberAsync(CancellationToken cancellationToken)
    {
        string datePrefix = $"PKG-{DateTime.UtcNow:yyyyMMdd}-";
        int count = await Context.Parcels.CountAsync(p => p.ParcelNumber.StartsWith(datePrefix), cancellationToken).ConfigureAwait(false);
        return $"{datePrefix}{(count + 1):D4}";
    }
}
