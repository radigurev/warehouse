using Warehouse.Common.Enums;

namespace Warehouse.Infrastructure.Sequences;

/// <summary>
/// Configuration for a named sequence pattern, defining its prefix, reset policy,
/// counter padding, and optional date segment format.
/// </summary>
public sealed class SequenceDefinition
{
    /// <summary>
    /// The base key identifying this sequence (e.g., <c>PO</c>, <c>CUST</c>, <c>BATCH</c>).
    /// </summary>
    public required string SequenceKey { get; init; }

    /// <summary>
    /// The prefix in the formatted output (e.g., <c>PO-</c>, <c>CUST-</c>).
    /// </summary>
    public required string Prefix { get; init; }

    /// <summary>
    /// Determines when the counter resets: <see cref="SequenceResetPolicy.Daily"/>,
    /// <see cref="SequenceResetPolicy.Monthly"/>, or <see cref="SequenceResetPolicy.Never"/>.
    /// </summary>
    public required SequenceResetPolicy ResetPolicy { get; init; }

    /// <summary>
    /// Zero-padding width for the counter portion (e.g., 4 produces <c>0001</c>).
    /// </summary>
    public required int Padding { get; init; }

    /// <summary>
    /// Whether the formatted output includes a date segment between prefix and counter.
    /// </summary>
    public required bool IncludesDateSegment { get; init; }

    /// <summary>
    /// Date format string for the date segment (e.g., <c>yyyyMMdd</c>, <c>yyyyMM</c>).
    /// Null when <see cref="IncludesDateSegment"/> is <c>false</c>.
    /// </summary>
    public string? DateFormat { get; init; }

    /// <summary>
    /// Validates this definition's configuration. Throws <see cref="InvalidOperationException"/>
    /// if the reset policy and date segment settings are inconsistent or padding is invalid.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SequenceKey))
        {
            throw new InvalidOperationException("SequenceKey must not be empty.");
        }

        if (Padding <= 0)
        {
            throw new InvalidOperationException(
                $"Sequence '{SequenceKey}': Padding must be greater than 0.");
        }

        if (ResetPolicy is SequenceResetPolicy.Daily or SequenceResetPolicy.Monthly)
        {
            if (!IncludesDateSegment)
            {
                throw new InvalidOperationException(
                    $"Sequence '{SequenceKey}': ResetPolicy '{ResetPolicy}' requires IncludesDateSegment = true.");
            }

            if (string.IsNullOrWhiteSpace(DateFormat))
            {
                throw new InvalidOperationException(
                    $"Sequence '{SequenceKey}': ResetPolicy '{ResetPolicy}' requires a non-null DateFormat.");
            }
        }

        if (ResetPolicy is SequenceResetPolicy.Never && IncludesDateSegment)
        {
            throw new InvalidOperationException(
                $"Sequence '{SequenceKey}': ResetPolicy 'Never' must not have IncludesDateSegment = true.");
        }
    }
}
