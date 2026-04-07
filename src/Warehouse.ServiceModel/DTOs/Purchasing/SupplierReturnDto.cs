namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Lightweight supplier return representation for list views.
/// </summary>
public sealed record SupplierReturnDto
{
    /// <summary>
    /// Gets the return ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the unique return number.
    /// </summary>
    public required string ReturnNumber { get; init; }

    /// <summary>
    /// Gets the supplier ID.
    /// </summary>
    public required int SupplierId { get; init; }

    /// <summary>
    /// Gets the supplier name.
    /// </summary>
    public required string SupplierName { get; init; }

    /// <summary>
    /// Gets the return status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the return reason.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC confirmation timestamp.
    /// </summary>
    public DateTime? ConfirmedAtUtc { get; init; }
}
