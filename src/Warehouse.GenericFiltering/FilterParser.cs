using System.Text.RegularExpressions;

namespace Warehouse.GenericFiltering;

/// <summary>
/// Parses raw filter strings into <see cref="FilterGroup"/> instances.
/// </summary>
public static class FilterParser
{
    private static readonly Regex MultipleFilterRegex = new(FilterConstants.MULTIPLE_FILTERS_REGEX, RegexOptions.Compiled);
    private static readonly Regex SingleFilterRegex = new(FilterConstants.SINGLE_FILTER_REGEX, RegexOptions.Compiled);

    /// <summary>
    /// Parses a raw filter string (e.g. "(name,cn,'John')and(isActive,eq,true)") into a <see cref="FilterGroup"/>.
    /// </summary>
    public static FilterGroup Parse(string rawFilter)
    {
        if (string.IsNullOrWhiteSpace(rawFilter))
            return new FilterGroup();

        if (!MultipleFilterRegex.IsMatch(rawFilter))
            throw new FilterException("Incorrect filter syntax.");

        FilterGroup group = new();
        MatchCollection matches = MultipleFilterRegex.Matches(rawFilter);

        while (true)
        {
            string singleFilterString = matches[0].Groups[FilterConstants.FIRST_SINGLE_FILTER_GROUP_NAME].Value;
            FilterDescriptor descriptor = ParseSingleFilter(singleFilterString);
            group.AddDescriptor(descriptor);

            rawFilter = rawFilter.Substring(singleFilterString.Length);

            if (string.IsNullOrWhiteSpace(rawFilter)) break;

            if (rawFilter.StartsWith("and", StringComparison.OrdinalIgnoreCase))
            {
                group.SetLastLogical(LogicalOperator.And);
                rawFilter = rawFilter.Substring(3);
            }
            else if (rawFilter.StartsWith("or", StringComparison.OrdinalIgnoreCase))
            {
                group.SetLastLogical(LogicalOperator.Or);
                rawFilter = rawFilter.Substring(2);
            }

            if (string.IsNullOrWhiteSpace(rawFilter)) break;
            matches = MultipleFilterRegex.Matches(rawFilter);
        }

        return group;
    }

    /// <summary>
    /// Parses a single filter clause string into a <see cref="FilterDescriptor"/>.
    /// </summary>
    private static FilterDescriptor ParseSingleFilter(string singleFilterString)
    {
        MatchCollection singleMatches = SingleFilterRegex.Matches(singleFilterString);
        string path = singleMatches[0].Groups[FilterConstants.PATH_GROUP].Value;
        string operatorToken = singleMatches[0].Groups[FilterConstants.OPERATOR_GROUP].Value;
        string value = RemoveQuotesIfNeeded(singleMatches[0].Groups[FilterConstants.VALUE_GROUP].Value);

        return new FilterDescriptor
        {
            PropertyPath = path,
            Operator = FilterOperatorParser.Parse(operatorToken),
            RawValue = value
        };
    }

    /// <summary>
    /// Strips surrounding single or double quotes from a value string.
    /// </summary>
    private static string RemoveQuotesIfNeeded(string value)
    {
        if ((value.StartsWith("'") && value.EndsWith("'")) ||
            (value.StartsWith("\"") && value.EndsWith("\"")))
            return value.Substring(1, value.Length - 2);

        return value;
    }
}
