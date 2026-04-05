using System.Linq.Expressions;

namespace Warehouse.GenericFiltering;

/// <summary>
/// Replaces parameter references in expression trees to enable combining multiple expressions.
/// </summary>
internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly ParameterExpression _newParameter;

    /// <summary>
    /// Initializes a new instance that replaces references from old to new parameter.
    /// </summary>
    internal ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        _oldParameter = oldParameter;
        _newParameter = newParameter;
    }

    /// <summary>
    /// Visits a parameter node, replacing it if it matches the old parameter.
    /// </summary>
    protected override Expression VisitParameter(ParameterExpression node)
        => node == _oldParameter ? _newParameter : base.VisitParameter(node);
}
