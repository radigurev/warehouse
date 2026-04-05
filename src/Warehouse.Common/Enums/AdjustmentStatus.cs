namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the approval workflow statuses for an inventory adjustment.
/// </summary>
public enum AdjustmentStatus
{
    /// <summary>
    /// Adjustment created, awaiting approval.
    /// </summary>
    Pending,

    /// <summary>
    /// Adjustment approved and ready to apply.
    /// </summary>
    Approved,

    /// <summary>
    /// Adjustment rejected by an approver.
    /// </summary>
    Rejected,

    /// <summary>
    /// Adjustment applied to stock levels.
    /// </summary>
    Applied
}
