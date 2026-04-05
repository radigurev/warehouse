namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Bill of materials component line representation.
/// </summary>
public sealed record BomLineDto
{
    /// <summary>
    /// Gets the BOM line ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the parent BOM ID.
    /// </summary>
    public required int BillOfMaterialsId { get; init; }

    /// <summary>
    /// Gets the child product ID.
    /// </summary>
    public required int ChildProductId { get; init; }

    /// <summary>
    /// Gets the child product name.
    /// </summary>
    public required string ChildProductName { get; init; }

    /// <summary>
    /// Gets the required quantity.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }
}
