namespace Warehouse.Common.Models;

/// <summary>
/// Represents the outcome of a service operation that does not return a value.
/// </summary>
public sealed class Result
{
    private Result(
        bool isSuccess,
        string? errorCode,
        string? errorMessage,
        int? statusCode,
        IReadOnlyDictionary<string, object?>? extensions)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
        Extensions = extensions;
    }

    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the machine-readable error code, or null on success.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the human-readable error message, or null on success.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the suggested HTTP status code for the error, or null on success.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets optional structured context carried onto ProblemDetails.Extensions, or null when none.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, null, null, null, null);

    /// <summary>
    /// Creates a failed result with the specified error details.
    /// </summary>
    public static Result Failure(string errorCode, string errorMessage, int statusCode)
        => new(false, errorCode, errorMessage, statusCode, null);

    /// <summary>
    /// Creates a failed result with the specified error details and structured extensions (propagated to ProblemDetails.Extensions).
    /// </summary>
    public static Result Failure(
        string errorCode,
        string errorMessage,
        int statusCode,
        IReadOnlyDictionary<string, object?> extensions)
        => new(false, errorCode, errorMessage, statusCode, extensions);
}
