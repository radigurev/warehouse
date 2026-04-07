namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Full customer return representation including lines.
/// </summary>
public sealed record CustomerReturnDetailDto
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

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Gets the UTC confirmation timestamp.</summary>
    public DateTime? ConfirmedAtUtc { get; init; }

    /// <summary>Gets the UTC received timestamp.</summary>
    public DateTime? ReceivedAtUtc { get; init; }

    /// <summary>Gets the UTC closed timestamp.</summary>
    public DateTime? ClosedAtUtc { get; init; }

    /// <summary>Gets the collection of return lines.</summary>
    public required IReadOnlyList<CustomerReturnLineDto> Lines { get; init; }
}
