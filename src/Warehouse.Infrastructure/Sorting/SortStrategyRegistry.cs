namespace Warehouse.Infrastructure.Sorting;

/// <summary>
/// Maps sort field names to sort strategies for an entity type.
/// Falls back to a default strategy when the requested field is not registered.
/// </summary>
/// <typeparam name="TEntity">The entity type to sort.</typeparam>
public sealed class SortStrategyRegistry<TEntity>
{
    private readonly Dictionary<string, ISortStrategy<TEntity>> _strategies;
    private readonly ISortStrategy<TEntity> _defaultStrategy;

    /// <summary>
    /// Initializes a new instance with the specified default strategy.
    /// </summary>
    public SortStrategyRegistry(ISortStrategy<TEntity> defaultStrategy)
    {
        _strategies = new Dictionary<string, ISortStrategy<TEntity>>(StringComparer.OrdinalIgnoreCase);
        _defaultStrategy = defaultStrategy;
    }

    /// <summary>
    /// Registers a sort strategy for the given field name.
    /// </summary>
    public SortStrategyRegistry<TEntity> Register(string fieldName, ISortStrategy<TEntity> strategy)
    {
        _strategies[fieldName] = strategy;
        return this;
    }

    /// <summary>
    /// Applies sorting to the query using the strategy for the given field, or the default.
    /// </summary>
    public IOrderedQueryable<TEntity> Apply(IQueryable<TEntity> query, string? field, bool descending)
    {
        ISortStrategy<TEntity> strategy = field is not null
            && _strategies.TryGetValue(field, out ISortStrategy<TEntity>? s)
            ? s : _defaultStrategy;

        return strategy.Apply(query, descending);
    }
}
