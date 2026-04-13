using Warehouse.Common.Workflow;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Workflow.Adjustment;

/// <summary>
/// Represents the Approved state for an inventory adjustment.
/// Sets approval timestamps and userId. Allows transition to Applied only.
/// </summary>
public sealed class AdjustmentApprovedState : IWorkflowState<InventoryAdjustment>
{
    /// <inheritdoc />
    public string StatusName => "Approved";

    /// <inheritdoc />
    public IReadOnlySet<string> AllowedTransitions { get; } = new HashSet<string> { "Applied" };

    /// <inheritdoc />
    public bool CanTransitionTo(string targetStatus) => AllowedTransitions.Contains(targetStatus);

    /// <inheritdoc />
    public Task OnEnterAsync(InventoryAdjustment entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        entity.ApprovedAtUtc = context.TimestampUtc;
        entity.ApprovedByUserId = context.UserId;
        return Task.CompletedTask;
    }
}
