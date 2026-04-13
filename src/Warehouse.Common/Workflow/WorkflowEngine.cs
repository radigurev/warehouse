namespace Warehouse.Common.Workflow;

/// <summary>
/// Generic workflow engine that resolves states from a registered collection
/// and orchestrates validated transitions.
/// </summary>
/// <typeparam name="TEntity">The workflow entity type.</typeparam>
public sealed class WorkflowEngine<TEntity> : IWorkflowEngine<TEntity> where TEntity : class
{
    private readonly IReadOnlyDictionary<string, IWorkflowState<TEntity>> _states;

    /// <summary>
    /// Initializes a new instance with the registered workflow states.
    /// </summary>
    public WorkflowEngine(IEnumerable<IWorkflowState<TEntity>> states)
    {
        _states = states.ToDictionary(s => s.StatusName, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task TransitionAsync(
        TEntity entity,
        string currentStatus,
        string targetStatus,
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        if (!_states.TryGetValue(currentStatus, out IWorkflowState<TEntity>? currentState))
            throw new InvalidOperationException($"Unknown workflow state: {currentStatus}.");

        if (!currentState.CanTransitionTo(targetStatus))
        {
            bool isTerminal = currentState.AllowedTransitions.Count == 0;
            string message = isTerminal
                ? $"This entity has already been {currentStatus} and cannot be modified."
                : $"Cannot transition from {currentStatus} to {targetStatus}.";

            throw new InvalidOperationException(message);
        }

        if (!_states.TryGetValue(targetStatus, out IWorkflowState<TEntity>? targetState))
            throw new InvalidOperationException($"Unknown workflow state: {targetStatus}.");

        await targetState.OnEnterAsync(entity, context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool CanTransition(string currentStatus, string targetStatus)
    {
        if (!_states.TryGetValue(currentStatus, out IWorkflowState<TEntity>? currentState))
            return false;

        return currentState.CanTransitionTo(targetStatus);
    }
}
