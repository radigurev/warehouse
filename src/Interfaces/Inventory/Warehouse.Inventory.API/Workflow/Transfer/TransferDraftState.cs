using Warehouse.Common.Workflow;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Workflow.Transfer;

/// <summary>
/// Represents the Draft state for a warehouse transfer.
/// Allows transitions to Completed and Cancelled.
/// </summary>
public sealed class TransferDraftState : IWorkflowState<WarehouseTransfer>
{
    /// <inheritdoc />
    public string StatusName => "Draft";

    /// <inheritdoc />
    public IReadOnlySet<string> AllowedTransitions { get; } = new HashSet<string> { "Completed", "Cancelled" };

    /// <inheritdoc />
    public bool CanTransitionTo(string targetStatus) => AllowedTransitions.Contains(targetStatus);

    /// <inheritdoc />
    public Task OnEnterAsync(WarehouseTransfer entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        entity.CreatedAtUtc = context.TimestampUtc;
        entity.CreatedByUserId = context.UserId;
        return Task.CompletedTask;
    }
}
