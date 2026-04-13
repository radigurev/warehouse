using Warehouse.Common.Workflow;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Workflow.Adjustment;

/// <summary>
/// Represents the Rejected state for an inventory adjustment.
/// Terminal state — no outbound transitions. Sets rejection timestamps and userId.
/// </summary>
public sealed class AdjustmentRejectedState : IWorkflowState<InventoryAdjustment>
{
    /// <inheritdoc />
    public string StatusName => "Rejected";

    /// <inheritdoc />
    public IReadOnlySet<string> AllowedTransitions { get; } = new HashSet<string>();

    /// <inheritdoc />
    public bool CanTransitionTo(string targetStatus) => false;

    /// <inheritdoc />
    public Task OnEnterAsync(InventoryAdjustment entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        entity.RejectedAtUtc = context.TimestampUtc;
        entity.RejectedByUserId = context.UserId;
        return Task.CompletedTask;
    }
}
