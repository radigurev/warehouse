namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for updating a bill of materials header.
/// </summary>
public sealed record UpdateBomRequest
{
    /// <summary>
    /// Gets the optional BOM name. Max 100 characters.
    /// </summary>
    public string? Name { get; init; }
}
