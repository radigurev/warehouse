namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when a customer return is physically received at the warehouse.
/// Event naming: Fulfillment.CustomerReturn.Received.
/// </summary>
public sealed record CustomerReturnReceivedEvent
{
    /// <summary>
    /// Gets the customer return ID.
    /// </summary>
    public required int CustomerReturnId { get; init; }

    /// <summary>
    /// Gets the customer return number.
    /// </summary>
    public required string CustomerReturnNumber { get; init; }

    /// <summary>
    /// Gets the customer ID.
    /// </summary>
    public required int CustomerId { get; init; }

    /// <summary>
    /// Gets the optional sales order ID (if return references an SO).
    /// </summary>
    public int? SalesOrderId { get; init; }

    /// <summary>
    /// Gets the user who received the return.
    /// </summary>
    public required int ReceivedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the return was received.
    /// </summary>
    public required DateTime ReceivedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of received return lines.
    /// </summary>
    public required IReadOnlyList<CustomerReturnReceivedLine> Lines { get; init; }
}
