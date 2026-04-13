using Warehouse.Common.Models;

namespace Warehouse.Common.Validation;

/// <summary>
/// Executes a chain of validators in order and returns the first failure (fail-fast mode).
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
public sealed class ValidationChain<TRequest>
{
    private readonly IReadOnlyList<IChainValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance with validators sorted by Order.
    /// </summary>
    public ValidationChain(IEnumerable<IChainValidator<TRequest>> validators)
    {
        _validators = validators.OrderBy(v => v.Order).ToList();
    }

    /// <summary>
    /// Executes all validators in order. Returns null if all pass, or the first failure.
    /// </summary>
    public async Task<Result?> ExecuteAsync(TRequest request, CancellationToken cancellationToken)
    {
        foreach (IChainValidator<TRequest> validator in _validators)
        {
            Result? result = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            if (result is not null)
                return result;
        }

        return null;
    }
}
