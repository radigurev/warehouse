namespace Warehouse.Common.Models;

/// <summary>
/// Represents the outcome of a service operation that returns a value of type T.
/// </summary>
public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? errorCode, string? errorMessage, int? statusCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the result value, or default on failure.
    /// </summary>
    public T? Value { get; }

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
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, null, null, null);

    /// <summary>
    /// Creates a failed result with the specified error details.
    /// </summary>
    public static Result<T> Failure(string errorCode, string errorMessage, int statusCode)
        => new(false, default, errorCode, errorMessage, statusCode);
}
