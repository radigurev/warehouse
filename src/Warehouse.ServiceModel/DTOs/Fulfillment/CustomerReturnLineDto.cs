namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Customer return line DTO.
/// </summary>
public sealed record CustomerReturnLineDto
{
    /// <summary>Gets the line ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the customer return ID.</summary>
    public required int CustomerReturnId { get; init; }

    /// <summary>Gets the product ID.</summary>
    public required int ProductId { get; init; }

    /// <summary>Gets the warehouse ID.</summary>
    public required int WarehouseId { get; init; }

    /// <summary>Gets the optional location ID.</summary>
    public int? LocationId { get; init; }

    /// <summary>Gets the return quantity.</summary>
    public required decimal Quantity { get; init; }

    /// <summary>Gets the optional batch ID.</summary>
    public int? BatchId { get; init; }

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; init; }
}
