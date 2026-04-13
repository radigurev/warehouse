namespace Warehouse.Infrastructure.Sorting;

/// <summary>
/// Defines a sorting strategy for a single field on an entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type to sort.</typeparam>
public interface ISortStrategy<TEntity>
{
    /// <summary>
    /// Applies the sort to the query.
    /// </summary>
    IOrderedQueryable<TEntity> Apply(IQueryable<TEntity> query, bool descending);
}
