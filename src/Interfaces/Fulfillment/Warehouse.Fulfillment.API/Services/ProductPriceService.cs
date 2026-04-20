using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
/// Implements the CRUD and diagnostic resolver operations for the Product Price Catalog.
/// <para>See <see cref="IProductPriceService"/>, <see cref="IProductPriceResolver"/>.</para>
/// <para>Conforms to CHG-FEAT-007 §2.2 and §2.5.</para>
/// </summary>
public sealed class ProductPriceService : BaseFulfillmentEntityService, IProductPriceService
{
    private readonly IProductPriceResolver _resolver;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public ProductPriceService(
        FulfillmentDbContext context,
        IMapper mapper,
        IProductPriceResolver resolver)
        : base(context, mapper)
    {
        _resolver = resolver;
    }

    /// <inheritdoc />
    public async Task<Result<ProductPriceDto>> CreateAsync(
        CreateProductPriceRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        bool duplicate = await Context.ProductPrices
            .AnyAsync(p => p.ProductId == request.ProductId
                        && p.CurrencyCode == request.CurrencyCode
                        && p.ValidFrom == request.ValidFrom, cancellationToken)
            .ConfigureAwait(false);

        if (duplicate)
            return DuplicateFailure(request);

        ProductPrice entity = Mapper.Map<ProductPrice>(request);
        entity.CreatedAtUtc = DateTime.UtcNow;
        entity.CreatedByUserId = userId;

        Context.ProductPrices.Add(entity);

        try
        {
            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniqueIndexViolation(ex))
        {
            return DuplicateFailure(request);
        }

        ProductPriceDto dto = Mapper.Map<ProductPriceDto>(entity);
        return Result<ProductPriceDto>.Success(dto);
    }

    private static Result<ProductPriceDto> DuplicateFailure(CreateProductPriceRequest request)
        => Result<ProductPriceDto>.Failure(
            "FULF_PRICE_DUPLICATE",
            $"A price for product {request.ProductId} in currency {request.CurrencyCode} with ValidFrom {request.ValidFrom?.ToString("O") ?? "null"} already exists.",
            409,
            new Dictionary<string, object?>
            {
                ["productId"] = request.ProductId,
                ["currencyCode"] = request.CurrencyCode,
                ["validFrom"] = request.ValidFrom
            });

    private static bool IsUniqueIndexViolation(DbUpdateException ex)
    {
        string? message = ex.InnerException?.Message ?? ex.Message;
        if (message is null) return false;
        return message.Contains("UX_ProductPrices", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<Result<ProductPriceDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        ProductPrice? entity = await Context.ProductPrices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return Result<ProductPriceDto>.Failure(
                "FULF_PRICE_NOT_FOUND_BY_ID",
                $"Product price with ID {id} does not exist.",
                404);

        ProductPriceDto dto = Mapper.Map<ProductPriceDto>(entity);
        return Result<ProductPriceDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<ProductPriceDto>>> SearchAsync(
        SearchProductPricesRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<ProductPrice> query = Context.ProductPrices.AsNoTracking();

        if (request.ProductId.HasValue)
            query = query.Where(p => p.ProductId == request.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
            query = query.Where(p => p.CurrencyCode == request.CurrencyCode);

        if (request.ActiveOnDate.HasValue)
        {
            DateTime onDate = request.ActiveOnDate.Value;
            query = query
                .Where(p => p.ValidFrom == null || p.ValidFrom <= onDate)
                .Where(p => p.ValidTo == null || p.ValidTo > onDate);
        }

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        query = ApplySorting(query, request.SortBy, request.SortDescending);

        int effectivePageSize = Math.Clamp(request.PageSize, 1, PaginationParams.MaxPageSize);
        int skip = (Math.Max(request.Page, 1) - 1) * effectivePageSize;

        List<ProductPrice> items = await query
            .Skip(skip)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ProductPriceDto> dtos = Mapper.Map<IReadOnlyList<ProductPriceDto>>(items);
        PaginatedResponse<ProductPriceDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = effectivePageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<ProductPriceDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<ProductPriceDto>> UpdateAsync(
        int id,
        UpdateProductPriceRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        ProductPrice? entity = await Context.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return Result<ProductPriceDto>.Failure(
                "FULF_PRICE_NOT_FOUND_BY_ID",
                $"Product price with ID {id} does not exist.",
                404);

        entity.UnitPrice = request.UnitPrice;
        entity.ValidFrom = request.ValidFrom;
        entity.ValidTo = request.ValidTo;
        entity.ModifiedAtUtc = DateTime.UtcNow;
        entity.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        ProductPriceDto dto = Mapper.Map<ProductPriceDto>(entity);
        return Result<ProductPriceDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        ProductPrice? entity = await Context.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return Result.Failure(
                "FULF_PRICE_NOT_FOUND_BY_ID",
                $"Product price with ID {id} does not exist.",
                404);

        Context.ProductPrices.Remove(entity);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<ProductPriceDto>> ResolveAsync(
        int productId,
        string currencyCode,
        DateTime? onDateUtc,
        CancellationToken cancellationToken)
    {
        DateTime effectiveDate = onDateUtc ?? DateTime.UtcNow;

        ProductPrice? resolved = await _resolver
            .ResolveAsync(productId, currencyCode, effectiveDate, cancellationToken)
            .ConfigureAwait(false);

        if (resolved is null)
            return Result<ProductPriceDto>.Failure(
                "FULF_PRICE_NOT_FOUND",
                $"No active price found for product {productId} in currency {currencyCode} on {effectiveDate:O}.",
                404,
                new Dictionary<string, object?>
                {
                    ["productId"] = productId,
                    ["currencyCode"] = currencyCode,
                    ["onDate"] = effectiveDate
                });

        ProductPriceDto dto = Mapper.Map<ProductPriceDto>(resolved);
        return Result<ProductPriceDto>.Success(dto);
    }

    private static IQueryable<ProductPrice> ApplySorting(
        IQueryable<ProductPrice> query,
        string? sortBy,
        bool sortDescending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "productid" => sortDescending ? query.OrderByDescending(p => p.ProductId) : query.OrderBy(p => p.ProductId),
            "currencycode" => sortDescending ? query.OrderByDescending(p => p.CurrencyCode) : query.OrderBy(p => p.CurrencyCode),
            "unitprice" => sortDescending ? query.OrderByDescending(p => p.UnitPrice) : query.OrderBy(p => p.UnitPrice),
            "validfrom" => sortDescending ? query.OrderByDescending(p => p.ValidFrom) : query.OrderBy(p => p.ValidFrom),
            _ => sortDescending ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc)
        };
    }
}
