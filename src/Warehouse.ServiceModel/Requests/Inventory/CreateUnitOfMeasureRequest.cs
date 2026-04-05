namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for creating a new unit of measure.
/// </summary>
public sealed record CreateUnitOfMeasureRequest
{
    /// <summary>
    /// Gets the unit code. Required, 1-10 characters, uppercase alphanumeric.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the unit name. Required, 1-50 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description. Max 200 characters.
    /// </summary>
    public string? Description { get; init; }
}
