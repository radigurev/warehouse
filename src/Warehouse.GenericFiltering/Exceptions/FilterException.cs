namespace Warehouse.GenericFiltering;

/// <summary>
/// Exception thrown when filter parsing or expression building fails.
/// </summary>
public sealed class FilterException : Exception
{
    /// <summary>
    /// Initializes a new instance with the specified error message.
    /// </summary>
    public FilterException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with the specified error message and inner exception.
    /// </summary>
    public FilterException(string message, Exception innerException) : base(message, innerException) { }
}
