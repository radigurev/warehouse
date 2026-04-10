using Warehouse.Common.Enums;

namespace Warehouse.Infrastructure.Sequences;

/// <summary>
/// Provides all built-in <see cref="SequenceDefinition"/> instances for the warehouse system.
/// </summary>
public static class SequenceDefinitions
{
    /// <summary>
    /// Returns the complete set of built-in sequence definitions keyed by <see cref="SequenceDefinition.SequenceKey"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, SequenceDefinition> GetBuiltInDefinitions()
    {
        List<SequenceDefinition> definitions =
        [
            new SequenceDefinition
            {
                SequenceKey = "PO",
                Prefix = "PO-",
                ResetPolicy = SequenceResetPolicy.Daily,
                Padding = 4,
                IncludesDateSegment = true,
                DateFormat = "yyyyMMdd"
            },
            new SequenceDefinition
            {
                SequenceKey = "GR",
                Prefix = "GR-",
                ResetPolicy = SequenceResetPolicy.Daily,
                Padding = 4,
                IncludesDateSegment = true,
                DateFormat = "yyyyMMdd"
            },
            new SequenceDefinition
            {
                SequenceKey = "SR",
                Prefix = "SR-",
                ResetPolicy = SequenceResetPolicy.Daily,
                Padding = 4,
                IncludesDateSegment = true,
                DateFormat = "yyyyMMdd"
            },
            new SequenceDefinition
            {
                SequenceKey = "SO",
                Prefix = "SO-",
                ResetPolicy = SequenceResetPolicy.Daily,
                Padding = 4,
                IncludesDateSegment = true,
                DateFormat = "yyyyMMdd"
            },
            new SequenceDefinition
            {
                SequenceKey = "PL",
                Prefix = "PL-",
                ResetPolicy = SequenceResetPolicy.Daily,
                Padding = 4,
                IncludesDateSegment = true,
                DateFormat = "yyyyMMdd"
            },
            new SequenceDefinition
            {
                SequenceKey = "PKG",
                Prefix = "PKG-",
                ResetPolicy = SequenceResetPolicy.Daily,
                Padding = 4,
                IncludesDateSegment = true,
                DateFormat = "yyyyMMdd"
            },
            new SequenceDefinition
            {
                SequenceKey = "SHP",
                Prefix = "SHP-",
                ResetPolicy = SequenceResetPolicy.Daily,
                Padding = 4,
                IncludesDateSegment = true,
                DateFormat = "yyyyMMdd"
            },
            new SequenceDefinition
            {
                SequenceKey = "CR",
                Prefix = "CR-",
                ResetPolicy = SequenceResetPolicy.Daily,
                Padding = 4,
                IncludesDateSegment = true,
                DateFormat = "yyyyMMdd"
            },
            new SequenceDefinition
            {
                SequenceKey = "SUPP",
                Prefix = "SUPP-",
                ResetPolicy = SequenceResetPolicy.Never,
                Padding = 6,
                IncludesDateSegment = false,
                DateFormat = null
            },
            new SequenceDefinition
            {
                SequenceKey = "PROD",
                Prefix = "PROD-",
                ResetPolicy = SequenceResetPolicy.Never,
                Padding = 6,
                IncludesDateSegment = false,
                DateFormat = null
            },
            new SequenceDefinition
            {
                SequenceKey = "CUST",
                Prefix = "CUST-",
                ResetPolicy = SequenceResetPolicy.Never,
                Padding = 6,
                IncludesDateSegment = false,
                DateFormat = null
            },
            new SequenceDefinition
            {
                SequenceKey = "BATCH",
                Prefix = "",
                ResetPolicy = SequenceResetPolicy.Monthly,
                Padding = 3,
                IncludesDateSegment = true,
                DateFormat = "yyyyMM"
            }
        ];

        Dictionary<string, SequenceDefinition> dictionary = new(StringComparer.OrdinalIgnoreCase);

        foreach (SequenceDefinition definition in definitions)
        {
            definition.Validate();

            if (!dictionary.TryAdd(definition.SequenceKey, definition))
            {
                throw new InvalidOperationException(
                    $"Duplicate sequence key: '{definition.SequenceKey}'.");
            }
        }

        return dictionary;
    }
}
