using Warehouse.Common.Workflow;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Workflow.Stocktake;

/// <summary>
/// Represents the Draft state for a stocktake session.
/// Allows transition to InProgress only.
/// </summary>
public sealed class SessionDraftState : IWorkflowState<StocktakeSession>
{
    /// <inheritdoc />
    public string StatusName => "Draft";

    /// <inheritdoc />
    public IReadOnlySet<string> AllowedTransitions { get; } = new HashSet<string> { "InProgress", "Cancelled" };

    /// <inheritdoc />
    public bool CanTransitionTo(string targetStatus) => AllowedTransitions.Contains(targetStatus);

    /// <inheritdoc />
    public Task OnEnterAsync(StocktakeSession entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        entity.CreatedAtUtc = context.TimestampUtc;
        entity.CreatedByUserId = context.UserId;
        return Task.CompletedTask;
    }
}
