namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Compact pick-list representation embedded in the SalesOrderDetailDto response.
/// </summary>
public sealed record SalesOrderPickListSummaryDto
{
    /// <summary>Gets the pick list ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the unique pick list number.</summary>
    public required string PickListNumber { get; init; }

    /// <summary>Gets the current status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }
}
