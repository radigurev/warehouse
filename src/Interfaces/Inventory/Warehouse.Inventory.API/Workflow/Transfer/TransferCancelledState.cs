using Warehouse.Common.Workflow;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Workflow.Transfer;

/// <summary>
/// Represents the Cancelled state for a warehouse transfer.
/// Terminal state — no outbound transitions.
/// </summary>
public sealed class TransferCancelledState : IWorkflowState<WarehouseTransfer>
{
    /// <inheritdoc />
    public string StatusName => "Cancelled";

    /// <inheritdoc />
    public IReadOnlySet<string> AllowedTransitions { get; } = new HashSet<string>();

    /// <inheritdoc />
    public bool CanTransitionTo(string targetStatus) => false;

    /// <inheritdoc />
    public Task OnEnterAsync(WarehouseTransfer entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        return Task.CompletedTask;
    }
}
