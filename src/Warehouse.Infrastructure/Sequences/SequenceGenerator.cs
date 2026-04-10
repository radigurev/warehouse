using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Warehouse.Common.Enums;

namespace Warehouse.Infrastructure.Sequences;

/// <summary>
/// Generates sequential, formatted, unique identifiers by atomically incrementing
/// a counter in the <c>infrastructure.Sequences</c> table using row-level locking.
/// </summary>
public sealed class SequenceGenerator : ISequenceGenerator
{
    private readonly DbContext _context;
    private readonly IReadOnlyDictionary<string, SequenceDefinition> _definitions;

    private const string MergeSql = @"
MERGE [infrastructure].[Sequences] WITH (HOLDLOCK) AS target
USING (SELECT @CompositeKey AS CompositeKey) AS source
ON target.CompositeKey = source.CompositeKey
WHEN MATCHED THEN
    UPDATE SET CurrentValue = CurrentValue + 1, ModifiedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (CompositeKey, SequenceKey, CurrentValue, ResetPolicy, CreatedAtUtc, ModifiedAtUtc)
    VALUES (@CompositeKey, @SequenceKey, 1, @ResetPolicy, SYSUTCDATETIME(), SYSUTCDATETIME())
OUTPUT inserted.CurrentValue;";

    /// <summary>
    /// Initializes a new instance with the calling service's <see cref="DbContext"/>
    /// and the registered sequence definitions.
    /// </summary>
    public SequenceGenerator(
        DbContext context,
        IReadOnlyDictionary<string, SequenceDefinition> definitions)
    {
        _context = context;
        _definitions = definitions;
    }

    /// <inheritdoc />
    public async Task<string> NextAsync(string sequenceKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sequenceKey))
        {
            throw new ArgumentException("Sequence key must not be null or empty.", nameof(sequenceKey));
        }

        if (!_definitions.TryGetValue(sequenceKey, out SequenceDefinition? definition))
        {
            throw new ArgumentException($"Unknown sequence key: '{sequenceKey}'.", nameof(sequenceKey));
        }

        DateTime utcNow = DateTime.UtcNow;
        string compositeKey = BuildCompositeKey(definition, utcNow);
        int nextValue = await IncrementAsync(compositeKey, definition, cancellationToken)
            .ConfigureAwait(false);

        return FormatOutput(definition, utcNow, nextValue);
    }

    /// <inheritdoc />
    public async Task<string> NextBatchNumberAsync(string productCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new ArgumentException("Product code must not be null or empty.", nameof(productCode));
        }

        if (!_definitions.TryGetValue("BATCH", out SequenceDefinition? definition))
        {
            throw new InvalidOperationException("BATCH sequence definition is not registered.");
        }

        DateTime utcNow = DateTime.UtcNow;
        string dateSegment = utcNow.ToString("yyyyMM", CultureInfo.InvariantCulture);
        string compositeKey = $"BATCH:{productCode}:{dateSegment}";

        int nextValue = await IncrementAsync(compositeKey, definition, cancellationToken)
            .ConfigureAwait(false);

        string paddedCounter = nextValue.ToString(CultureInfo.InvariantCulture)
            .PadLeft(definition.Padding, '0');

        return $"{productCode}-{dateSegment}-{paddedCounter}";
    }

    /// <summary>
    /// Atomically reads and increments the counter for the given composite key
    /// using a serializable transaction with row-level locking.
    /// Uses raw ADO.NET via the DbContext connection because the MERGE OUTPUT
    /// SQL is non-composable and cannot be used with EF Core's SqlQueryRaw.
    /// </summary>
    private async Task<int> IncrementAsync(
        string compositeKey,
        SequenceDefinition definition,
        CancellationToken cancellationToken)
    {
        System.Data.Common.DbConnection connection = _context.Database.GetDbConnection();
        bool connectionOpened = false;

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            connectionOpened = true;
        }

        System.Data.Common.DbTransaction? transaction = null;
        bool ownTransaction = _context.Database.CurrentTransaction is null;

        try
        {
            if (ownTransaction)
            {
                transaction = await connection
                    .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                    .ConfigureAwait(false);
            }

            await using System.Data.Common.DbCommand command = connection.CreateCommand();
            command.CommandText = MergeSql;
            command.Transaction = transaction ?? _context.Database.CurrentTransaction?.GetDbTransaction();

            SqlParameter compositeKeyParam = new("@CompositeKey", SqlDbType.NVarChar, 200)
            {
                Value = compositeKey
            };
            SqlParameter sequenceKeyParam = new("@SequenceKey", SqlDbType.NVarChar, 50)
            {
                Value = definition.SequenceKey
            };
            SqlParameter resetPolicyParam = new("@ResetPolicy", SqlDbType.NVarChar, 20)
            {
                Value = definition.ResetPolicy.ToString()
            };

            command.Parameters.Add(compositeKeyParam);
            command.Parameters.Add(sequenceKeyParam);
            command.Parameters.Add(resetPolicyParam);

            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            int nextValue = Convert.ToInt32(result);

            if (ownTransaction && transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            return nextValue;
        }
        catch
        {
            if (ownTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }

            throw;
        }
        finally
        {
            if (ownTransaction && transaction is not null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }

            if (connectionOpened)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Builds the composite key for the given definition and current UTC time.
    /// </summary>
    private static string BuildCompositeKey(SequenceDefinition definition, DateTime utcNow)
    {
        return definition.ResetPolicy switch
        {
            SequenceResetPolicy.Daily =>
                $"{definition.SequenceKey}:{utcNow.ToString(definition.DateFormat!, CultureInfo.InvariantCulture)}",
            SequenceResetPolicy.Monthly =>
                $"{definition.SequenceKey}:{utcNow.ToString(definition.DateFormat!, CultureInfo.InvariantCulture)}",
            SequenceResetPolicy.Never =>
                definition.SequenceKey,
            _ => throw new InvalidOperationException($"Unknown reset policy: {definition.ResetPolicy}")
        };
    }

    /// <summary>
    /// Formats the output string using the definition's pattern and the counter value.
    /// </summary>
    private static string FormatOutput(SequenceDefinition definition, DateTime utcNow, int counterValue)
    {
        string paddedCounter = counterValue.ToString(CultureInfo.InvariantCulture)
            .PadLeft(definition.Padding, '0');

        if (definition.IncludesDateSegment)
        {
            string dateSegment = utcNow.ToString(definition.DateFormat!, CultureInfo.InvariantCulture);
            return $"{definition.Prefix}{dateSegment}-{paddedCounter}";
        }

        return $"{definition.Prefix}{paddedCounter}";
    }
}
