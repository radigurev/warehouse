namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Lightweight customer return representation for list views.
/// </summary>
public sealed record CustomerReturnDto
{
    /// <summary>Gets the return ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the unique return number.</summary>
    public required string ReturnNumber { get; init; }

    /// <summary>Gets the customer ID.</summary>
    public required int CustomerId { get; init; }

    /// <summary>Gets the optional sales order ID.</summary>
    public int? SalesOrderId { get; init; }

    /// <summary>Gets the current status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the return reason.</summary>
    public required string Reason { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }
}
