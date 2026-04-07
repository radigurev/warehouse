namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for creating a customer return (RMA) with lines.
/// </summary>
public sealed record CreateCustomerReturnRequest
{
    /// <summary>Gets the customer ID. Required.</summary>
    public required int CustomerId { get; init; }

    /// <summary>Gets the optional sales order ID (reference to original SO).</summary>
    public int? SalesOrderId { get; init; }

    /// <summary>Gets the return reason. Required, 1-500 characters.</summary>
    public required string Reason { get; init; }

    /// <summary>Gets the optional notes. Max 2000 characters.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets the collection of return lines. At least one required.</summary>
    public required IReadOnlyList<CreateCustomerReturnLineRequest> Lines { get; init; }
}
