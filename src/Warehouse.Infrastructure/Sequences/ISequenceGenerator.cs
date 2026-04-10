namespace Warehouse.Infrastructure.Sequences;

/// <summary>
/// Generates sequential, formatted, unique identifiers using database-level
/// row locking to guarantee uniqueness under concurrent access.
/// </summary>
public interface ISequenceGenerator
{
    /// <summary>
    /// Returns the next formatted identifier for the given sequence key.
    /// </summary>
    /// <param name="sequenceKey">
    /// A registered sequence key (e.g., <c>PO</c>, <c>CUST</c>, <c>SO</c>).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A formatted, unique, sequential identifier string.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sequenceKey"/> is null, empty, or not registered.
    /// </exception>
    Task<string> NextAsync(string sequenceKey, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the next formatted batch number for the given product code.
    /// The batch number counter is independent per product per month.
    /// </summary>
    /// <param name="productCode">The product code (e.g., <c>PROD-000001</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A formatted batch number string (e.g., <c>PROD-001-202604-001</c>).</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="productCode"/> is null or empty.
    /// </exception>
    Task<string> NextBatchNumberAsync(string productCode, CancellationToken cancellationToken);
}
