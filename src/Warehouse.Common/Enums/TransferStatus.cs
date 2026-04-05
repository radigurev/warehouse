namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the lifecycle statuses for a warehouse transfer.
/// </summary>
public enum TransferStatus
{
    /// <summary>
    /// Transfer created but not yet executed.
    /// </summary>
    Draft,

    /// <summary>
    /// Transfer has been executed and stock moved.
    /// </summary>
    Completed,

    /// <summary>
    /// Transfer was cancelled before execution.
    /// </summary>
    Cancelled
}
