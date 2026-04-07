namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for a customer return line.
/// </summary>
public sealed record CreateCustomerReturnLineRequest
{
    /// <summary>Gets the product ID. Required.</summary>
    public required int ProductId { get; init; }

    /// <summary>Gets the destination warehouse ID. Required.</summary>
    public required int WarehouseId { get; init; }

    /// <summary>Gets the optional destination location ID.</summary>
    public int? LocationId { get; init; }

    /// <summary>Gets the return quantity. Required, must be greater than 0.</summary>
    public required decimal Quantity { get; init; }

    /// <summary>Gets the optional batch ID.</summary>
    public int? BatchId { get; init; }

    /// <summary>Gets the optional notes. Max 500 characters.</summary>
    public string? Notes { get; init; }
}
