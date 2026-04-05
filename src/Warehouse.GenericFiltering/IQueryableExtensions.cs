using System.Linq.Expressions;

namespace Warehouse.GenericFiltering;

/// <summary>
/// Extension methods for applying dynamic filters to <see cref="IQueryable{T}"/> sources.
/// </summary>
public static class IQueryableExtensions
{
    /// <summary>
    /// Applies a raw filter string to the queryable source.
    /// </summary>
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, string? rawFilter) where T : class
    {
        if (string.IsNullOrWhiteSpace(rawFilter))
            return query;

        FilterGroup filterGroup = FilterParser.Parse(rawFilter);
        return ApplyFilter(query, filterGroup);
    }

    /// <summary>
    /// Applies a <see cref="FilterGroup"/> to the queryable source.
    /// </summary>
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, FilterGroup filterGroup) where T : class
    {
        if (filterGroup.Descriptors.Count == 0)
            return query;

        Expression<Func<T, bool>> predicate = FilterExpressionBuilder.Build<T>(filterGroup);
        return query.Where(predicate);
    }
}
