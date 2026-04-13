namespace Warehouse.Common.ErrorCodes;

/// <summary>
/// Common infrastructure and validation error codes shared across all domains.
/// </summary>
public static class CommonErrorCodes
{
    /// <summary>Generic not-found error.</summary>
    public const string NotFound = "NOT_FOUND";

    /// <summary>Generic validation error (FluentValidation aggregate).</summary>
    public const string ValidationError = "VALIDATION_ERROR";

    /// <summary>Generic conflict error.</summary>
    public const string Conflict = "CONFLICT";

    /// <summary>Access denied — insufficient permissions.</summary>
    public const string Forbidden = "FORBIDDEN";

    /// <summary>Insufficient stock for the requested operation.</summary>
    public const string InsufficientStock = "INSUFFICIENT_STOCK";

    /// <summary>Product requires batch tracking but no batch was provided.</summary>
    public const string BatchRequired = "BATCH_REQUIRED";

    /// <summary>Entity references itself (e.g., category parent = self).</summary>
    public const string SelfReference = "SELF_REFERENCE";

    /// <summary>No variance found to create adjustment.</summary>
    public const string NoVariance = "NO_VARIANCE";
}
