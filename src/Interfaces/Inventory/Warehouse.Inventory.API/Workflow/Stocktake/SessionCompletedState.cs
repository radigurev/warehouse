using Warehouse.Common.Workflow;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Workflow.Stocktake;

/// <summary>
/// Represents the Completed state for a stocktake session.
/// Terminal state — no outbound transitions. Sets completion timestamps and userId.
/// </summary>
public sealed class SessionCompletedState : IWorkflowState<StocktakeSession>
{
    /// <inheritdoc />
    public string StatusName => "Completed";

    /// <inheritdoc />
    public IReadOnlySet<string> AllowedTransitions { get; } = new HashSet<string>();

    /// <inheritdoc />
    public bool CanTransitionTo(string targetStatus) => false;

    /// <inheritdoc />
    public Task OnEnterAsync(StocktakeSession entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        entity.CompletedAtUtc = context.TimestampUtc;
        entity.CompletedByUserId = context.UserId;
        return Task.CompletedTask;
    }
}
