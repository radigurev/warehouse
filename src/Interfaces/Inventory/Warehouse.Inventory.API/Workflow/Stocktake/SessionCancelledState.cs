using Warehouse.Common.Workflow;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Workflow.Stocktake;

/// <summary>
/// Represents the Cancelled state for a stocktake session.
/// Terminal state — no outbound transitions.
/// </summary>
public sealed class SessionCancelledState : IWorkflowState<StocktakeSession>
{
    /// <inheritdoc />
    public string StatusName => "Cancelled";

    /// <inheritdoc />
    public IReadOnlySet<string> AllowedTransitions { get; } = new HashSet<string>();

    /// <inheritdoc />
    public bool CanTransitionTo(string targetStatus) => false;

    /// <inheritdoc />
    public Task OnEnterAsync(StocktakeSession entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        return Task.CompletedTask;
    }
}
