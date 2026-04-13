namespace Warehouse.Common.Workflow;

/// <summary>
/// Orchestrates state transitions for a workflow entity.
/// Validates the transition is allowed and delegates to the target state's OnEnterAsync.
/// </summary>
/// <typeparam name="TEntity">The workflow entity type.</typeparam>
public interface IWorkflowEngine<in TEntity> where TEntity : class
{
    /// <summary>
    /// Transitions the entity from its current status to the target status.
    /// Throws <see cref="InvalidOperationException"/> if the transition is not allowed.
    /// </summary>
    Task TransitionAsync(TEntity entity, string currentStatus, string targetStatus, WorkflowContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Returns whether a transition from the current status to the target status is valid.
    /// </summary>
    bool CanTransition(string currentStatus, string targetStatus);
}
