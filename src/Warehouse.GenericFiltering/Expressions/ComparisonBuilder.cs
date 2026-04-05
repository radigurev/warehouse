using System.Linq.Expressions;

namespace Warehouse.GenericFiltering;

/// <summary>
/// Builds comparison <see cref="Expression"/> nodes from selector bodies and constants.
/// <para>Supports binary comparisons, string/collection contains, IN-semantics, and collection Any projections.</para>
/// </summary>
internal static class ComparisonBuilder
{
    /// <summary>
    /// Builds the comparison expression for the given selector body, constant, and operator.
    /// </summary>
    internal static Expression BuildComparison(
        Expression selectorBody,
        Expression constantExpression,
        FilterOperator op)
    {
        Type? constSeq = PropertyPathResolver.FindGenericEnumerableInterface(constantExpression.Type);
        if (constSeq != null
            && selectorBody.Type == constSeq.GetGenericArguments()[0])
        {
            if (op == FilterOperator.Equals)
            {
                return Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.Contains),
                    [selectorBody.Type],
                    constantExpression,
                    selectorBody);
            }

            if (op == FilterOperator.NotEquals)
            {
                MethodCallExpression containsExpr = Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.Contains),
                    [selectorBody.Type],
                    constantExpression,
                    selectorBody);

                return Expression.Not(containsExpr);
            }
        }

        if (op == FilterOperator.Contains)
            return BuildContainsExpression(selectorBody, constantExpression);

        if (op == FilterOperator.NotContains)
        {
            Expression containsExpr = BuildContainsExpression(selectorBody, constantExpression);
            return Expression.Not(containsExpr);
        }

        if (op == FilterOperator.StartsWith)
            return BuildStringMethodExpression(selectorBody, constantExpression, nameof(string.StartsWith));

        if (op == FilterOperator.EndsWith)
            return BuildStringMethodExpression(selectorBody, constantExpression, nameof(string.EndsWith));

        Type? seqInterface = PropertyPathResolver.FindGenericEnumerableInterface(selectorBody.Type);
        if (seqInterface != null && selectorBody.Type != typeof(string))
            return BuildCollectionAnyComparison(selectorBody, constantExpression, seqInterface, op);

        return BuildBinaryComparison(selectorBody, constantExpression, op);
    }

    /// <summary>
    /// Builds a binary comparison with null-safety for value types compared against null.
    /// </summary>
    private static Expression BuildBinaryComparison(
        Expression left,
        Expression right,
        FilterOperator op)
    {
        if (right is ConstantExpression { Value: null } &&
            left.Type.IsValueType &&
            Nullable.GetUnderlyingType(left.Type) == null)
        {
            Type nullableType = typeof(Nullable<>).MakeGenericType(left.Type);
            left = Expression.Convert(left, nullableType);
            right = Expression.Constant(null, nullableType);
        }

        return op switch
        {
            FilterOperator.Equals => Expression.Equal(left, right),
            FilterOperator.NotEquals => Expression.NotEqual(left, right),
            FilterOperator.GreaterThan => Expression.GreaterThan(left, right),
            FilterOperator.GreaterOrEqual => Expression.GreaterThanOrEqual(left, right),
            FilterOperator.LessThan => Expression.LessThan(left, right),
            FilterOperator.LessOrEqual => Expression.LessThanOrEqual(left, right),
            _ => throw new FilterException($"Operator '{op}' is not supported for binary comparison.")
        };
    }

    /// <summary>
    /// Builds a string method call expression (StartsWith, EndsWith) for string properties.
    /// </summary>
    private static Expression BuildStringMethodExpression(
        Expression visitorResult,
        Expression constant,
        string methodName)
    {
        if (visitorResult.Type != typeof(string))
            throw new FilterException($"Operator '{methodName}' is only supported for string properties.");

        return Expression.Call(visitorResult, methodName, Type.EmptyTypes, constant);
    }

    /// <summary>
    /// Builds a Contains expression for string (string.Contains) or collection (Enumerable.Contains).
    /// </summary>
    private static Expression BuildContainsExpression(Expression visitorResult, Expression constant)
    {
        if (visitorResult.Type == typeof(string))
            return Expression.Call(visitorResult, nameof(string.Contains), Type.EmptyTypes, constant);

        Type elementType = visitorResult.Type.GetGenericArguments().First();
        return Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [elementType],
            visitorResult,
            constant);
    }

    /// <summary>
    /// Builds an Enumerable.Any(e => e op constant) expression for collection properties.
    /// </summary>
    private static Expression BuildCollectionAnyComparison(
        Expression selectorBody,
        Expression constantExpression,
        Type seqInterface,
        FilterOperator op)
    {
        Type elementType = seqInterface.GetGenericArguments()[0];
        ParameterExpression itemParam = Expression.Parameter(elementType, "e");
        Expression innerComparison = BuildBinaryComparison(itemParam, constantExpression, op);
        LambdaExpression predicate = Expression.Lambda(innerComparison, itemParam);
        return Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Any),
            [elementType],
            selectorBody,
            predicate);
    }
}
