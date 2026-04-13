using Warehouse.Common.Workflow;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Workflow.Transfer;

/// <summary>
/// Represents the Completed state for a warehouse transfer.
/// Terminal state — no outbound transitions. Sets completion timestamps and userId.
/// Stock movements and event publishing are handled by the service method.
/// </summary>
public sealed class TransferCompletedState : IWorkflowState<WarehouseTransfer>
{
    /// <inheritdoc />
    public string StatusName => "Completed";

    /// <inheritdoc />
    public IReadOnlySet<string> AllowedTransitions { get; } = new HashSet<string>();

    /// <inheritdoc />
    public bool CanTransitionTo(string targetStatus) => false;

    /// <inheritdoc />
    public Task OnEnterAsync(WarehouseTransfer entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        entity.CompletedAtUtc = context.TimestampUtc;
        entity.CompletedByUserId = context.UserId;
        return Task.CompletedTask;
    }
}
