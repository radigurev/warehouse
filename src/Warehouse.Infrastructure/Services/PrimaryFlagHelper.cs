using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Infrastructure.Services;

/// <summary>
/// Static utility for managing primary/default flags on entity collections.
/// <para>See <see cref="IEntity"/>.</para>
/// </summary>
public static class PrimaryFlagHelper
{
    /// <summary>
    /// Unsets a flag on all entities matching the filter, excluding the entity with the specified ID.
    /// </summary>
    public static async Task UnsetOthersAsync<TEntity>(
        DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>> filter,
        int excludeId,
        Action<TEntity> clearFlag,
        CancellationToken cancellationToken) where TEntity : class, IEntity
    {
        List<TEntity> others = await dbSet
            .Where(filter)
            .Where(e => e.Id != excludeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (TEntity entity in others)
            clearFlag(entity);
    }

    /// <summary>
    /// Promotes the next entity by creation order to the flag, matching the filter.
    /// </summary>
    public static async Task PromoteNextAsync<TEntity>(
        DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>> filter,
        Expression<Func<TEntity, DateTime>> orderBy,
        Action<TEntity> setFlag,
        CancellationToken cancellationToken) where TEntity : class, IEntity
    {
        TEntity? next = await dbSet
            .Where(filter)
            .OrderBy(orderBy)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (next is not null)
            setFlag(next);
    }
}
