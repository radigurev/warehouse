namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Unit of measure representation.
/// </summary>
public sealed record UnitOfMeasureDto
{
    /// <summary>
    /// Gets the unit ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the unit code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the unit name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
