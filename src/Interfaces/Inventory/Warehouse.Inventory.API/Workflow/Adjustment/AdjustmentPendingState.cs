using Warehouse.Common.Workflow;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Workflow.Adjustment;

/// <summary>
/// Represents the Pending state for an inventory adjustment.
/// Allows transitions to Approved and Rejected.
/// </summary>
public sealed class AdjustmentPendingState : IWorkflowState<InventoryAdjustment>
{
    /// <inheritdoc />
    public string StatusName => "Pending";

    /// <inheritdoc />
    public IReadOnlySet<string> AllowedTransitions { get; } = new HashSet<string> { "Approved", "Rejected" };

    /// <inheritdoc />
    public bool CanTransitionTo(string targetStatus) => AllowedTransitions.Contains(targetStatus);

    /// <inheritdoc />
    public Task OnEnterAsync(InventoryAdjustment entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        entity.CreatedAtUtc = context.TimestampUtc;
        entity.CreatedByUserId = context.UserId;
        return Task.CompletedTask;
    }
}
