using System.Linq.Expressions;

namespace Warehouse.Infrastructure.Sorting;

/// <summary>
/// Generic sort strategy that sorts by a property expression.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The property type to sort by.</typeparam>
public sealed class PropertySortStrategy<TEntity, TKey> : ISortStrategy<TEntity>
{
    private readonly Expression<Func<TEntity, TKey>> _keySelector;

    /// <summary>
    /// Initializes a new instance with the property selector expression.
    /// </summary>
    public PropertySortStrategy(Expression<Func<TEntity, TKey>> keySelector)
    {
        _keySelector = keySelector;
    }

    /// <inheritdoc />
    public IOrderedQueryable<TEntity> Apply(IQueryable<TEntity> query, bool descending)
    {
        return descending
            ? query.OrderByDescending(_keySelector)
            : query.OrderBy(_keySelector);
    }
}
