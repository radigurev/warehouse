using FluentAssertions;
using Warehouse.Common.Workflow;
using Warehouse.Inventory.API.Workflow.Transfer;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Tests.Unit.Workflow;

/// <summary>
/// Unit tests for warehouse transfer workflow state transitions and OnEnter side effects.
/// <para>Links to specification CHG-REFAC-003, SDD-INV-003.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-003")]
[Category("CHG-REFAC-003")]
public sealed class TransferWorkflowStateTests
{
    private IWorkflowEngine<WarehouseTransfer> _engine = null!;

    [SetUp]
    public void SetUp()
    {
        List<IWorkflowState<WarehouseTransfer>> states =
        [
            new TransferDraftState(),
            new TransferCompletedState(),
            new TransferCancelledState()
        ];
        _engine = new WorkflowEngine<WarehouseTransfer>(states);
    }

    [Test]
    public void DraftState_AllowsTransitionToCompleted()
    {
        // Arrange
        TransferDraftState state = new();

        // Act & Assert
        state.CanTransitionTo("Completed").Should().BeTrue();
    }

    [Test]
    public void DraftState_AllowsTransitionToCancelled()
    {
        // Arrange
        TransferDraftState state = new();

        // Act & Assert
        state.CanTransitionTo("Cancelled").Should().BeTrue();
    }

    [Test]
    public void CompletedState_IsTerminal()
    {
        // Arrange
        TransferCompletedState state = new();

        // Act & Assert
        state.AllowedTransitions.Should().BeEmpty();
        state.CanTransitionTo("Draft").Should().BeFalse();
        state.CanTransitionTo("Cancelled").Should().BeFalse();
    }

    [Test]
    public void CancelledState_IsTerminal()
    {
        // Arrange
        TransferCancelledState state = new();

        // Act & Assert
        state.AllowedTransitions.Should().BeEmpty();
        state.CanTransitionTo("Draft").Should().BeFalse();
    }

    [Test]
    public async Task CompletedState_OnEnter_SetsCompletionFields()
    {
        // Arrange
        WarehouseTransfer entity = new() { SourceWarehouseId = 1, DestinationWarehouseId = 2, Status = "Draft" };
        WorkflowContext context = new() { UserId = 10, TimestampUtc = new DateTime(2026, 4, 13, 14, 0, 0, DateTimeKind.Utc) };

        // Act
        TransferCompletedState state = new();
        await state.OnEnterAsync(entity, context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        entity.Status.Should().Be("Completed");
        entity.CompletedAtUtc.Should().Be(context.TimestampUtc);
        entity.CompletedByUserId.Should().Be(10);
    }

    [Test]
    public async Task CancelledState_OnEnter_SetsStatusOnly()
    {
        // Arrange
        WarehouseTransfer entity = new() { SourceWarehouseId = 1, DestinationWarehouseId = 2, Status = "Draft" };
        WorkflowContext context = new() { UserId = 5, TimestampUtc = DateTime.UtcNow };

        // Act
        TransferCancelledState state = new();
        await state.OnEnterAsync(entity, context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        entity.Status.Should().Be("Cancelled");
        entity.CompletedAtUtc.Should().BeNull();
        entity.CompletedByUserId.Should().BeNull();
    }

    [Test]
    public void Engine_InvalidTransition_CompletedToDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        WorkflowContext context = new() { UserId = 1, TimestampUtc = DateTime.UtcNow };

        // Act
        Func<Task> act = () => _engine.TransitionAsync(
            new WarehouseTransfer { SourceWarehouseId = 1, DestinationWarehouseId = 2, Status = "Completed" },
            "Completed", "Draft", context, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>();
    }
}
