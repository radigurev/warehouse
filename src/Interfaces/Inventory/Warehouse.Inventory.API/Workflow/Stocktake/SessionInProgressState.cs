using Warehouse.Common.Workflow;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Workflow.Stocktake;

/// <summary>
/// Represents the InProgress state for a stocktake session.
/// Sets StartedAtUtc. Allows transitions to Completed and Cancelled.
/// Stock level snapshot is handled by the service method.
/// </summary>
public sealed class SessionInProgressState : IWorkflowState<StocktakeSession>
{
    /// <inheritdoc />
    public string StatusName => "InProgress";

    /// <inheritdoc />
    public IReadOnlySet<string> AllowedTransitions { get; } = new HashSet<string> { "Completed", "Cancelled" };

    /// <inheritdoc />
    public bool CanTransitionTo(string targetStatus) => AllowedTransitions.Contains(targetStatus);

    /// <inheritdoc />
    public Task OnEnterAsync(StocktakeSession entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        entity.StartedAtUtc = context.TimestampUtc;
        return Task.CompletedTask;
    }
}
