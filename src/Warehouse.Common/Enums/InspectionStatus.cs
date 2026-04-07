namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the inspection status values for goods receipt lines.
/// </summary>
public enum InspectionStatus
{
    /// <summary>
    /// Line has not yet been inspected.
    /// </summary>
    Pending,

    /// <summary>
    /// Line has been accepted and counts toward received totals.
    /// </summary>
    Accepted,

    /// <summary>
    /// Line has been rejected and does not count toward received totals.
    /// </summary>
    Rejected,

    /// <summary>
    /// Line has been quarantined pending further review.
    /// </summary>
    Quarantined
}
