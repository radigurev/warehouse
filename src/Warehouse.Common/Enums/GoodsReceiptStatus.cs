namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the status values for goods receipt documents.
/// </summary>
public enum GoodsReceiptStatus
{
    /// <summary>
    /// Receipt is open and lines can be added or inspected.
    /// </summary>
    Open,

    /// <summary>
    /// Receipt has been completed with all lines finalized.
    /// </summary>
    Completed
}
