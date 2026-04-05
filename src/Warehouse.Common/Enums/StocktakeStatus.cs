namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the lifecycle statuses for a stocktake session.
/// </summary>
public enum StocktakeStatus
{
    /// <summary>
    /// Session created but not yet started.
    /// </summary>
    Draft,

    /// <summary>
    /// Session is actively being counted.
    /// </summary>
    InProgress,

    /// <summary>
    /// Session counting is complete.
    /// </summary>
    Completed,

    /// <summary>
    /// Session was cancelled before completion.
    /// </summary>
    Cancelled
}
