namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for updating an existing unit of measure.
/// </summary>
public sealed record UpdateUnitOfMeasureRequest
{
    /// <summary>
    /// Gets the unit name. Required, 1-50 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description. Max 200 characters.
    /// </summary>
    public string? Description { get; init; }
}
