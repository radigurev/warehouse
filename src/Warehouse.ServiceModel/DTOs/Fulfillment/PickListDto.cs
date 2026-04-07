namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Lightweight pick list representation for list views.
/// </summary>
public sealed record PickListDto
{
    /// <summary>Gets the pick list ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the unique pick list number.</summary>
    public required string PickListNumber { get; init; }

    /// <summary>Gets the sales order ID.</summary>
    public required int SalesOrderId { get; init; }

    /// <summary>Gets the current status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Gets the UTC completion timestamp.</summary>
    public DateTime? CompletedAtUtc { get; init; }
}
