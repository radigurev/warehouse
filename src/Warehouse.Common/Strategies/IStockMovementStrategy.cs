using Warehouse.Common.Enums;

namespace Warehouse.Common.Strategies;

/// <summary>
/// Defines a strategy for creating stock movement entities.
/// Each implementation encapsulates reason code, reference type, and metadata logic
/// for a specific movement type.
/// </summary>
public interface IStockMovementStrategy
{
    /// <summary>
    /// Gets the movement reason this strategy handles.
    /// </summary>
    StockMovementReason Reason { get; }
}
