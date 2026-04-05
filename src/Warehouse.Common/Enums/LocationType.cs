namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the type of a storage location within a warehouse.
/// </summary>
public enum LocationType
{
    /// <summary>
    /// A row within a zone.
    /// </summary>
    Row,

    /// <summary>
    /// A shelf within a row.
    /// </summary>
    Shelf,

    /// <summary>
    /// A bin within a shelf.
    /// </summary>
    Bin,

    /// <summary>
    /// A bulk storage area without shelving.
    /// </summary>
    Bulk
}
