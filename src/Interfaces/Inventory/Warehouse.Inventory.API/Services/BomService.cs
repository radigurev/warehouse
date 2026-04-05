using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Interfaces;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Services;

/// <summary>
/// Implements bill of materials operations: CRUD and line management.
/// <para>See <see cref="IBomService"/>.</para>
/// </summary>
public sealed class BomService : BaseInventoryEntityService, IBomService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public BomService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<BomDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        BillOfMaterials? bom = await GetBomWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (bom is null)
            return Result<BomDto>.Failure("BOM_NOT_FOUND", "Bill of materials not found.", 404);

        BomDto dto = Mapper.Map<BomDto>(bom);
        return Result<BomDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BomDto>>> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
    {
        List<BillOfMaterials> boms = await Context.BillOfMaterials
            .AsNoTracking()
            .Include(b => b.ParentProduct)
            .Include(b => b.Lines).ThenInclude(l => l.ChildProduct)
            .Where(b => b.ParentProductId == productId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<BomDto> dtos = Mapper.Map<IReadOnlyList<BomDto>>(boms);
        return Result<IReadOnlyList<BomDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<BomDto>> CreateAsync(
        CreateBomRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        Result? productValidation = await ValidateProductExistsAsync(request.ParentProductId, cancellationToken).ConfigureAwait(false);
        if (productValidation is not null)
            return Result<BomDto>.Failure(productValidation.ErrorCode!, productValidation.ErrorMessage!, productValidation.StatusCode!.Value);

        BillOfMaterials bom = new()
        {
            ParentProductId = request.ParentProductId,
            Name = request.Name,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        foreach (CreateBomLineRequest lineReq in request.Lines)
        {
            bom.Lines.Add(new BomLine
            {
                ChildProductId = lineReq.ChildProductId,
                Quantity = lineReq.Quantity
            });
        }

        Context.BillOfMaterials.Add(bom);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        BillOfMaterials? created = await GetBomWithDetailsAsync(bom.Id, cancellationToken).ConfigureAwait(false);
        BomDto dto = Mapper.Map<BomDto>(created!);
        return Result<BomDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<BomDto>> UpdateAsync(
        int id,
        UpdateBomRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        BillOfMaterials? bom = await GetBomWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (bom is null)
            return Result<BomDto>.Failure("BOM_NOT_FOUND", "Bill of materials not found.", 404);

        bom.Name = request.Name;
        bom.ModifiedAtUtc = DateTime.UtcNow;
        bom.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        BillOfMaterials? updated = await GetBomWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        BomDto dto = Mapper.Map<BomDto>(updated!);
        return Result<BomDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<BomDto>> AddLineAsync(
        int bomId,
        AddBomLineRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        BillOfMaterials? bom = await GetBomWithDetailsAsync(bomId, cancellationToken).ConfigureAwait(false);

        if (bom is null)
            return Result<BomDto>.Failure("BOM_NOT_FOUND", "Bill of materials not found.", 404);

        bool duplicate = bom.Lines.Any(l => l.ChildProductId == request.ChildProductId);
        if (duplicate)
            return Result<BomDto>.Failure("DUPLICATE_BOM_LINE", "This product is already a component in this BOM.", 409);

        BomLine line = new()
        {
            BillOfMaterialsId = bomId,
            ChildProductId = request.ChildProductId,
            Quantity = request.Quantity
        };

        Context.BomLines.Add(line);
        bom.ModifiedAtUtc = DateTime.UtcNow;
        bom.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        BillOfMaterials? updated = await GetBomWithDetailsAsync(bomId, cancellationToken).ConfigureAwait(false);
        BomDto dto = Mapper.Map<BomDto>(updated!);
        return Result<BomDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> RemoveLineAsync(int bomId, int lineId, CancellationToken cancellationToken)
    {
        BomLine? line = await Context.BomLines
            .FirstOrDefaultAsync(l => l.Id == lineId && l.BillOfMaterialsId == bomId, cancellationToken)
            .ConfigureAwait(false);

        if (line is null)
            return Result.Failure("BOM_LINE_NOT_FOUND", "BOM line not found.", 404);

        Context.BomLines.Remove(line);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        BillOfMaterials? bom = await Context.BillOfMaterials
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (bom is null)
            return Result.Failure("BOM_NOT_FOUND", "Bill of materials not found.", 404);

        Context.BillOfMaterials.Remove(bom);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Loads a BOM with parent product and lines including child products.
    /// </summary>
    private async Task<BillOfMaterials?> GetBomWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.BillOfMaterials
            .Include(b => b.ParentProduct)
            .Include(b => b.Lines).ThenInclude(l => l.ChildProduct)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Validates that a product exists and is not deleted.
    /// </summary>
    private async Task<Result?> ValidateProductExistsAsync(int productId, CancellationToken cancellationToken)
    {
        bool exists = await Context.Products
            .AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : Result.Failure("INVALID_PRODUCT", "The specified product does not exist.", 400);
    }
}
