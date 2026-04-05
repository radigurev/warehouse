namespace Warehouse.GenericFiltering;

/// <summary>
/// Constants used by the filter parsing engine.
/// </summary>
internal static class FilterConstants
{
    /// <summary>
    /// Regex group name for the dot-separated property path.
    /// </summary>
    internal const string PATH_GROUP = "PropertyPath";

    /// <summary>
    /// Regex group name for the comparison operator token.
    /// </summary>
    internal const string OPERATOR_GROUP = "ComparisonOperator";

    /// <summary>
    /// Regex group name for the target value.
    /// </summary>
    internal const string VALUE_GROUP = "TargetGroup";

    /// <summary>
    /// Regex group name for the first single filter clause.
    /// </summary>
    internal const string FIRST_SINGLE_FILTER_GROUP_NAME = "FirstGroupName";

    /// <summary>
    /// Regex group name for the logical operator between clauses.
    /// </summary>
    internal const string LOGICAL_OPERATOR_GROUP = "LogicalOperator";

    /// <summary>
    /// Lambda parameter name used in expression building.
    /// </summary>
    internal const string PARAMETER_NAME = "p";

    /// <summary>
    /// Separator character for nested property paths.
    /// </summary>
    internal const string PATH_SEPARATOR = ".";

    /// <summary>
    /// Regex pattern for a single filter clause: (path,operator,value).
    /// </summary>
    internal const string SINGLE_FILTER_REGEX =
        $"\\((?<{PATH_GROUP}>[A-Za-z0-9_.]+),(?<{OPERATOR_GROUP}>eq|gt|ge|lt|nq|le|cn|ncn|sw|ew),(?<{VALUE_GROUP}>\\[[^\\]]*\\]|'[^']*'|\"[^\"]*\"|[^)]+)\\)";

    /// <summary>
    /// Regex pattern for multiple filter clauses joined by and/or.
    /// </summary>
    internal const string MULTIPLE_FILTERS_REGEX =
        $"(?<{FIRST_SINGLE_FILTER_GROUP_NAME}>{SINGLE_FILTER_REGEX})(?:(?:(?<{LOGICAL_OPERATOR_GROUP}>and|or){SINGLE_FILTER_REGEX}))*$";
}
