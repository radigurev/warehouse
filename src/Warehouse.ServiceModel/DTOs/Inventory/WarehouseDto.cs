namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Warehouse representation for list and detail views.
/// </summary>
public sealed record WarehouseDto
{
    /// <summary>
    /// Gets the warehouse ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the unique warehouse code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the warehouse name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional address.
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets whether the warehouse is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
