using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;
using Warehouse.Common.Models;

namespace Warehouse.Infrastructure.Services;

/// <summary>
/// Generic base service providing common utility methods for EF Core entity services.
/// <para>See <see cref="IEntity"/>, <see cref="Result{T}"/>.</para>
/// </summary>
public abstract class BaseEntityService<TContext> where TContext : DbContext
{
    /// <summary>
    /// Gets the database context for the current domain.
    /// </summary>
    protected TContext Context { get; }

    /// <summary>
    /// Gets the AutoMapper instance for entity-to-DTO mapping.
    /// </summary>
    protected IMapper Mapper { get; }

    /// <summary>
    /// Initializes a new instance with the specified context and mapper.
    /// </summary>
    protected BaseEntityService(TContext context, IMapper mapper)
    {
        Context = context;
        Mapper = mapper;
    }

    /// <summary>
    /// Finds an entity by ID or returns a not-found failure result.
    /// </summary>
    protected async Task<Result<TDto>> FindOrNotFoundAsync<TEntity, TDto>(
        DbSet<TEntity> dbSet,
        int id,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken) where TEntity : class, IEntity
    {
        TEntity? entity = await dbSet
            .FindAsync([id], cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return Result<TDto>.Failure(errorCode, errorMessage, 404);

        return Result<TDto>.Success(Mapper.Map<TDto>(entity));
    }

    /// <summary>
    /// Maps an entity to a DTO wrapped in a success result.
    /// </summary>
    protected Result<TDto> MapToResult<TEntity, TDto>(TEntity entity)
    {
        return Result<TDto>.Success(Mapper.Map<TDto>(entity));
    }

    /// <summary>
    /// Maps an entity collection to a DTO list wrapped in a success result.
    /// </summary>
    protected Result<IReadOnlyList<TDto>> MapListToResult<TEntity, TDto>(IEnumerable<TEntity> entities)
    {
        IReadOnlyList<TDto> dtos = Mapper.Map<IReadOnlyList<TDto>>(entities);
        return Result<IReadOnlyList<TDto>>.Success(dtos);
    }

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    protected async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
