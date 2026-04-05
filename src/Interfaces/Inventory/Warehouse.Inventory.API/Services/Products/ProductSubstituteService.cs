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
/// Implements product substitute link operations: create, delete, and list.
/// <para>See <see cref="IProductSubstituteService"/>.</para>
/// </summary>
public sealed class ProductSubstituteService : BaseInventoryEntityService, IProductSubstituteService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public ProductSubstituteService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductSubstituteDto>>> GetByProductIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        List<ProductSubstitute> substitutes = await Context.ProductSubstitutes
            .AsNoTracking()
            .Include(s => s.SubstituteProduct)
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.SubstituteProduct.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ProductSubstituteDto> dtos = Mapper.Map<IReadOnlyList<ProductSubstituteDto>>(substitutes);
        return Result<IReadOnlyList<ProductSubstituteDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<ProductSubstituteDto>> CreateAsync(
        CreateProductSubstituteRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProductId == request.SubstituteProductId)
            return Result<ProductSubstituteDto>.Failure("SELF_REFERENCE", "A product cannot be its own substitute.", 400);

        bool duplicate = await Context.ProductSubstitutes
            .AnyAsync(s => s.ProductId == request.ProductId && s.SubstituteProductId == request.SubstituteProductId, cancellationToken)
            .ConfigureAwait(false);

        if (duplicate)
            return Result<ProductSubstituteDto>.Failure("DUPLICATE_SUBSTITUTE", "This substitute link already exists.", 409);

        ProductSubstitute substitute = new()
        {
            ProductId = request.ProductId,
            SubstituteProductId = request.SubstituteProductId,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.ProductSubstitutes.Add(substitute);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        ProductSubstitute? created = await Context.ProductSubstitutes
            .Include(s => s.SubstituteProduct)
            .FirstOrDefaultAsync(s => s.Id == substitute.Id, cancellationToken)
            .ConfigureAwait(false);

        ProductSubstituteDto dto = Mapper.Map<ProductSubstituteDto>(created!);
        return Result<ProductSubstituteDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        ProductSubstitute? substitute = await Context.ProductSubstitutes
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (substitute is null)
            return Result.Failure("SUBSTITUTE_NOT_FOUND", "Product substitute link not found.", 404);

        Context.ProductSubstitutes.Remove(substitute);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
