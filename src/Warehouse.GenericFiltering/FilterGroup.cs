namespace Warehouse.GenericFiltering;

/// <summary>
/// Represents an ordered collection of filter descriptors with logical operators.
/// <para>Use the fluent builder methods or <see cref="FilterParser"/> to construct.</para>
/// </summary>
public sealed class FilterGroup
{
    private readonly List<FilterDescriptor> _descriptors = [];

    /// <summary>
    /// Gets the ordered list of filter descriptors.
    /// </summary>
    public IReadOnlyList<FilterDescriptor> Descriptors => _descriptors;

    /// <summary>
    /// Adds the first filter descriptor to the group.
    /// </summary>
    public FilterGroup Where(string propertyPath, FilterOperator op, string value)
    {
        _descriptors.Add(new FilterDescriptor
        {
            PropertyPath = propertyPath,
            Operator = op,
            RawValue = value
        });
        return this;
    }

    /// <summary>
    /// Adds a filter descriptor joined by AND to the previous.
    /// </summary>
    public FilterGroup And(string propertyPath, FilterOperator op, string value)
    {
        SetLastLogical(LogicalOperator.And);
        _descriptors.Add(new FilterDescriptor
        {
            PropertyPath = propertyPath,
            Operator = op,
            RawValue = value
        });
        return this;
    }

    /// <summary>
    /// Adds a filter descriptor joined by OR to the previous.
    /// </summary>
    public FilterGroup Or(string propertyPath, FilterOperator op, string value)
    {
        SetLastLogical(LogicalOperator.Or);
        _descriptors.Add(new FilterDescriptor
        {
            PropertyPath = propertyPath,
            Operator = op,
            RawValue = value
        });
        return this;
    }

    /// <summary>
    /// Adds a pre-built <see cref="FilterDescriptor"/> directly.
    /// </summary>
    internal void AddDescriptor(FilterDescriptor descriptor)
    {
        _descriptors.Add(descriptor);
    }

    /// <summary>
    /// Updates the <see cref="FilterDescriptor.NextLogical"/> on the last descriptor.
    /// </summary>
    internal void SetLastLogical(LogicalOperator logical)
    {
        if (_descriptors.Count == 0) return;
        FilterDescriptor last = _descriptors[^1];
        _descriptors[^1] = last with { NextLogical = logical };
    }
}
