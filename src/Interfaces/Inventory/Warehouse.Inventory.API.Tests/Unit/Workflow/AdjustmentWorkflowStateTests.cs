using FluentAssertions;
using Warehouse.Common.Workflow;
using Warehouse.Inventory.API.Workflow.Adjustment;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Tests.Unit.Workflow;

/// <summary>
/// Unit tests for inventory adjustment workflow state transitions and OnEnter side effects.
/// <para>Links to specification CHG-REFAC-003, SDD-INV-002.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-002")]
[Category("CHG-REFAC-003")]
public sealed class AdjustmentWorkflowStateTests
{
    private IWorkflowEngine<InventoryAdjustment> _engine = null!;

    [SetUp]
    public void SetUp()
    {
        List<IWorkflowState<InventoryAdjustment>> states =
        [
            new AdjustmentPendingState(),
            new AdjustmentApprovedState(),
            new AdjustmentRejectedState(),
            new AdjustmentAppliedState()
        ];
        _engine = new WorkflowEngine<InventoryAdjustment>(states);
    }

    [Test]
    public void PendingState_AllowsTransitionToApproved()
    {
        // Arrange
        AdjustmentPendingState state = new();

        // Act
        bool canTransition = state.CanTransitionTo("Approved");

        // Assert
        canTransition.Should().BeTrue();
    }

    [Test]
    public void PendingState_AllowsTransitionToRejected()
    {
        // Arrange
        AdjustmentPendingState state = new();

        // Act
        bool canTransition = state.CanTransitionTo("Rejected");

        // Assert
        canTransition.Should().BeTrue();
    }

    [Test]
    public void PendingState_DoesNotAllowTransitionToApplied()
    {
        // Arrange
        AdjustmentPendingState state = new();

        // Act
        bool canTransition = state.CanTransitionTo("Applied");

        // Assert
        canTransition.Should().BeFalse();
    }

    [Test]
    public void ApprovedState_AllowsTransitionToApplied()
    {
        // Arrange
        AdjustmentApprovedState state = new();

        // Act
        bool canTransition = state.CanTransitionTo("Applied");

        // Assert
        canTransition.Should().BeTrue();
    }

    [Test]
    public void ApprovedState_DoesNotAllowTransitionToRejected()
    {
        // Arrange
        AdjustmentApprovedState state = new();

        // Act
        bool canTransition = state.CanTransitionTo("Rejected");

        // Assert
        canTransition.Should().BeFalse();
    }

    [Test]
    public void AppliedState_IsTerminal()
    {
        // Arrange
        AdjustmentAppliedState state = new();

        // Act & Assert
        state.AllowedTransitions.Should().BeEmpty();
        state.CanTransitionTo("Pending").Should().BeFalse();
        state.CanTransitionTo("Approved").Should().BeFalse();
    }

    [Test]
    public void RejectedState_IsTerminal()
    {
        // Arrange
        AdjustmentRejectedState state = new();

        // Act & Assert
        state.AllowedTransitions.Should().BeEmpty();
        state.CanTransitionTo("Pending").Should().BeFalse();
        state.CanTransitionTo("Approved").Should().BeFalse();
    }

    [Test]
    public async Task ApprovedState_OnEnter_SetsApprovalFields()
    {
        // Arrange
        InventoryAdjustment entity = new() { Reason = "Test", Status = "Pending" };
        WorkflowContext context = new() { UserId = 42, TimestampUtc = new DateTime(2026, 4, 13, 10, 0, 0, DateTimeKind.Utc) };

        // Act
        AdjustmentApprovedState state = new();
        await state.OnEnterAsync(entity, context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        entity.Status.Should().Be("Approved");
        entity.ApprovedAtUtc.Should().Be(context.TimestampUtc);
        entity.ApprovedByUserId.Should().Be(42);
    }

    [Test]
    public async Task RejectedState_OnEnter_SetsRejectionFields()
    {
        // Arrange
        InventoryAdjustment entity = new() { Reason = "Test", Status = "Pending" };
        WorkflowContext context = new() { UserId = 7, TimestampUtc = new DateTime(2026, 4, 13, 11, 0, 0, DateTimeKind.Utc) };

        // Act
        AdjustmentRejectedState state = new();
        await state.OnEnterAsync(entity, context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        entity.Status.Should().Be("Rejected");
        entity.RejectedAtUtc.Should().Be(context.TimestampUtc);
        entity.RejectedByUserId.Should().Be(7);
    }

    [Test]
    public async Task AppliedState_OnEnter_SetsApplicationFields()
    {
        // Arrange
        InventoryAdjustment entity = new() { Reason = "Test", Status = "Approved" };
        WorkflowContext context = new() { UserId = 99, TimestampUtc = new DateTime(2026, 4, 13, 12, 0, 0, DateTimeKind.Utc) };

        // Act
        AdjustmentAppliedState state = new();
        await state.OnEnterAsync(entity, context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        entity.Status.Should().Be("Applied");
        entity.AppliedAtUtc.Should().Be(context.TimestampUtc);
        entity.AppliedByUserId.Should().Be(99);
    }

    [Test]
    public void Engine_InvalidTransition_ThrowsInvalidOperationException()
    {
        // Arrange
        InventoryAdjustment entity = new() { Reason = "Test", Status = "Pending" };
        WorkflowContext context = new() { UserId = 1, TimestampUtc = DateTime.UtcNow };

        // Act
        Func<Task> act = () => _engine.TransitionAsync(entity, "Pending", "Applied", context, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot transition*");
    }

    [Test]
    public void Engine_TransitionFromTerminalState_ThrowsInvalidOperationException()
    {
        // Arrange
        InventoryAdjustment entity = new() { Reason = "Test", Status = "Applied" };
        WorkflowContext context = new() { UserId = 1, TimestampUtc = DateTime.UtcNow };

        // Act
        Func<Task> act = () => _engine.TransitionAsync(entity, "Applied", "Pending", context, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been*");
    }
}
