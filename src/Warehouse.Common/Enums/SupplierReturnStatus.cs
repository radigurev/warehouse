namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the status values for supplier return documents.
/// </summary>
public enum SupplierReturnStatus
{
    /// <summary>
    /// Return is being drafted and can be edited.
    /// </summary>
    Draft,

    /// <summary>
    /// Return has been confirmed and stock movements have been generated.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Return has been cancelled.
    /// </summary>
    Cancelled
}
