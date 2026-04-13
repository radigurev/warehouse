namespace Warehouse.ServiceModel.Requests;

/// <summary>
/// Defines common pagination parameters for search requests.
/// All search request records SHOULD implement this interface.
/// </summary>
public interface IPaginationParams
{
    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    int Page { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    int PageSize { get; }
}
