namespace Warehouse.Common.Workflow;

/// <summary>
/// Defines a single state within a workflow state machine.
/// Each implementation encapsulates transition rules and state-entry side effects.
/// </summary>
/// <typeparam name="TEntity">The workflow entity type.</typeparam>
public interface IWorkflowState<in TEntity> where TEntity : class
{
    /// <summary>
    /// Gets the status name this state represents.
    /// </summary>
    string StatusName { get; }

    /// <summary>
    /// Gets the set of status names this state can transition to.
    /// </summary>
    IReadOnlySet<string> AllowedTransitions { get; }

    /// <summary>
    /// Determines whether a transition to the specified target status is valid.
    /// </summary>
    bool CanTransitionTo(string targetStatus);

    /// <summary>
    /// Executes state-entry side effects on the entity (status assignment, timestamps, userIds).
    /// Called by the workflow engine after transition validation passes.
    /// </summary>
    Task OnEnterAsync(TEntity entity, WorkflowContext context, CancellationToken cancellationToken);
}
