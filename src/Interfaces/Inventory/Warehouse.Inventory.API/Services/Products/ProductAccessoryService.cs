using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Interfaces.Products;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Services.Products;

/// <summary>
/// Implements product accessory link operations: create, delete, and list.
/// <para>See <see cref="IProductAccessoryService"/>.</para>
/// </summary>
public sealed class ProductAccessoryService : BaseInventoryEntityService, IProductAccessoryService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public ProductAccessoryService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductAccessoryDto>>> GetByProductIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        List<ProductAccessory> accessories = await Context.ProductAccessories
            .AsNoTracking()
            .Include(a => a.AccessoryProduct)
            .Where(a => a.ProductId == productId)
            .OrderBy(a => a.AccessoryProduct.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ProductAccessoryDto> dtos = Mapper.Map<IReadOnlyList<ProductAccessoryDto>>(accessories);
        return Result<IReadOnlyList<ProductAccessoryDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<ProductAccessoryDto>> CreateAsync(
        CreateProductAccessoryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProductId == request.AccessoryProductId)
            return Result<ProductAccessoryDto>.Failure("SELF_REFERENCE", "A product cannot be its own accessory.", 400);

        bool duplicate = await Context.ProductAccessories
            .AnyAsync(a => a.ProductId == request.ProductId && a.AccessoryProductId == request.AccessoryProductId, cancellationToken)
            .ConfigureAwait(false);

        if (duplicate)
            return Result<ProductAccessoryDto>.Failure("DUPLICATE_ACCESSORY", "This accessory link already exists.", 409);

        ProductAccessory accessory = new()
        {
            ProductId = request.ProductId,
            AccessoryProductId = request.AccessoryProductId,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.ProductAccessories.Add(accessory);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        ProductAccessory? created = await Context.ProductAccessories
            .Include(a => a.AccessoryProduct)
            .FirstOrDefaultAsync(a => a.Id == accessory.Id, cancellationToken)
            .ConfigureAwait(false);

        ProductAccessoryDto dto = Mapper.Map<ProductAccessoryDto>(created!);
        return Result<ProductAccessoryDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        ProductAccessory? accessory = await Context.ProductAccessories
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (accessory is null)
            return Result.Failure("ACCESSORY_NOT_FOUND", "Product accessory link not found.", 404);

        Context.ProductAccessories.Remove(accessory);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
