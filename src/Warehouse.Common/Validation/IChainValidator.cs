using Warehouse.Common.Models;

namespace Warehouse.Common.Validation;

/// <summary>
/// Defines a single validation step within a validation chain.
/// Each implementation performs one business rule check.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
public interface IChainValidator<in TRequest>
{
    /// <summary>
    /// Gets the execution order within the chain (lower runs first).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Validates the request. Returns null on success, or a failure Result on error.
    /// </summary>
    Task<Result?> ValidateAsync(TRequest request, CancellationToken cancellationToken);
}
