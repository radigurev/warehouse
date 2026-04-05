namespace Warehouse.Common.Interfaces;

/// <summary>
/// Marker interface for entities with an integer primary key.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    int Id { get; set; }
}
