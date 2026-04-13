using Warehouse.Common.Enums;

namespace Warehouse.Common.Strategies;

/// <summary>
/// Resolves stock movement strategies from a registered collection.
/// </summary>
public sealed class StockMovementStrategyFactory : IStockMovementStrategyFactory
{
    private readonly IReadOnlyDictionary<StockMovementReason, IStockMovementStrategy> _strategies;

    /// <summary>
    /// Initializes a new instance with the registered strategies.
    /// </summary>
    public StockMovementStrategyFactory(IEnumerable<IStockMovementStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Reason);
    }

    /// <inheritdoc />
    public IStockMovementStrategy GetStrategy(StockMovementReason reason)
    {
        if (!_strategies.TryGetValue(reason, out IStockMovementStrategy? strategy))
            throw new InvalidOperationException($"No stock movement strategy registered for reason '{reason}'.");

        return strategy;
    }

    /// <inheritdoc />
    public bool HasStrategy(StockMovementReason reason) => _strategies.ContainsKey(reason);
}
