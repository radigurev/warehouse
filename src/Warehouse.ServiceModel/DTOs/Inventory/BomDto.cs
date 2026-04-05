namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Bill of materials header representation for list views.
/// </summary>
public sealed record BomDto
{
    /// <summary>
    /// Gets the BOM ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the parent product ID.
    /// </summary>
    public required int ParentProductId { get; init; }

    /// <summary>
    /// Gets the parent product name.
    /// </summary>
    public required string ParentProductName { get; init; }

    /// <summary>
    /// Gets the optional BOM name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets whether this BOM is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of BOM component lines.
    /// </summary>
    public required IReadOnlyList<BomLineDto> Lines { get; init; }
}
