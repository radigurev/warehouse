using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.ServiceModel.Requests;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Infrastructure.Services;

/// <summary>
/// Abstract base class implementing the search/paginate template method.
/// Subclasses provide entity-specific query building and sorting.
/// </summary>
/// <typeparam name="TContext">The EF Core DbContext type.</typeparam>
/// <typeparam name="TEntity">The entity type to search.</typeparam>
/// <typeparam name="TDto">The DTO type returned in the paginated response.</typeparam>
/// <typeparam name="TSearchRequest">The search request type with pagination parameters.</typeparam>
public abstract class SearchableServiceBase<TContext, TEntity, TDto, TSearchRequest>
    : BaseEntityService<TContext>
    where TContext : DbContext
    where TEntity : class
    where TSearchRequest : IPaginationParams
{
    /// <summary>
    /// Initializes a new instance with the specified context and mapper.
    /// </summary>
    protected SearchableServiceBase(TContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <summary>
    /// Executes the search/paginate template: BuildSearchQuery → Count → ApplySorting → Skip/Take → Map → Wrap.
    /// </summary>
    protected async Task<Result<PaginatedResponse<TDto>>> SearchEntitiesAsync(
        TSearchRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<TEntity> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        query = ApplySorting(query, request);

        List<TEntity> items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<TDto> dtos = Mapper.Map<IReadOnlyList<TDto>>(items);

        PaginatedResponse<TDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<TDto>>.Success(response);
    }

    /// <summary>
    /// Builds the base query with entity-specific filters. Must be overridden by subclasses.
    /// </summary>
    protected abstract IQueryable<TEntity> BuildSearchQuery(TSearchRequest request);

    /// <summary>
    /// Applies sorting to the query. Subclasses SHOULD override for entity-specific sorting.
    /// Default implementation orders by CreatedAtUtc descending.
    /// </summary>
    protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, TSearchRequest request)
    {
        return query.OrderByDescending(e => EF.Property<DateTime>(e, "CreatedAtUtc"));
    }
}
