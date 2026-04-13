using Warehouse.Common.Workflow;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Workflow.Adjustment;

/// <summary>
/// Represents the Applied state for an inventory adjustment.
/// Terminal state — no outbound transitions. Sets application timestamps and userId.
/// Stock adjustments and event publishing are handled by the service method.
/// </summary>
public sealed class AdjustmentAppliedState : IWorkflowState<InventoryAdjustment>
{
    /// <inheritdoc />
    public string StatusName => "Applied";

    /// <inheritdoc />
    public IReadOnlySet<string> AllowedTransitions { get; } = new HashSet<string>();

    /// <inheritdoc />
    public bool CanTransitionTo(string targetStatus) => false;

    /// <inheritdoc />
    public Task OnEnterAsync(InventoryAdjustment entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        entity.AppliedAtUtc = context.TimestampUtc;
        entity.AppliedByUserId = context.UserId;
        return Task.CompletedTask;
    }
}
