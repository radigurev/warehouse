namespace Warehouse.Common.Workflow;

/// <summary>
/// Provides contextual data for a workflow state transition.
/// </summary>
public sealed class WorkflowContext
{
    /// <summary>
    /// Gets the ID of the user triggering the transition.
    /// </summary>
    public required int UserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp of the transition.
    /// </summary>
    public required DateTime TimestampUtc { get; init; }
}
