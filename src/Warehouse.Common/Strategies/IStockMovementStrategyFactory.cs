using Warehouse.Common.Enums;

namespace Warehouse.Common.Strategies;

/// <summary>
/// Resolves the correct stock movement strategy by reason code.
/// </summary>
public interface IStockMovementStrategyFactory
{
    /// <summary>
    /// Gets the strategy for the specified movement reason.
    /// Throws <see cref="InvalidOperationException"/> if no strategy is registered.
    /// </summary>
    IStockMovementStrategy GetStrategy(StockMovementReason reason);

    /// <summary>
    /// Returns whether a strategy is registered for the given reason.
    /// </summary>
    bool HasStrategy(StockMovementReason reason);
}
