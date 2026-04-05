namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Product substitute link representation.
/// </summary>
public sealed record ProductSubstituteDto
{
    /// <summary>
    /// Gets the substitute link ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the source product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the substitute product ID.
    /// </summary>
    public required int SubstituteProductId { get; init; }

    /// <summary>
    /// Gets the substitute product name.
    /// </summary>
    public required string SubstituteProductName { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
