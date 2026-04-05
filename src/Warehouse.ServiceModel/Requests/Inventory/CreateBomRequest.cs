namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for creating a new bill of materials with lines.
/// </summary>
public sealed record CreateBomRequest
{
    /// <summary>
    /// Gets the parent product ID. Required.
    /// </summary>
    public required int ParentProductId { get; init; }

    /// <summary>
    /// Gets the optional BOM name. Max 100 characters.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the component lines. At least one line is required.
    /// </summary>
    public required IReadOnlyList<CreateBomLineRequest> Lines { get; init; }
}
