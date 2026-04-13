using FluentAssertions;
using Warehouse.Common.Workflow;
using Warehouse.Inventory.API.Workflow.Stocktake;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Tests.Unit.Workflow;

/// <summary>
/// Unit tests for stocktake session workflow state transitions and OnEnter side effects.
/// <para>Links to specification CHG-REFAC-003, SDD-INV-004.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-004")]
[Category("CHG-REFAC-003")]
public sealed class StocktakeWorkflowStateTests
{
    private IWorkflowEngine<StocktakeSession> _engine = null!;

    [SetUp]
    public void SetUp()
    {
        List<IWorkflowState<StocktakeSession>> states =
        [
            new SessionDraftState(),
            new SessionInProgressState(),
            new SessionCompletedState(),
            new SessionCancelledState()
        ];
        _engine = new WorkflowEngine<StocktakeSession>(states);
    }

    [Test]
    public void DraftState_AllowsTransitionToInProgress()
    {
        // Arrange
        SessionDraftState state = new();

        // Act & Assert
        state.CanTransitionTo("InProgress").Should().BeTrue();
    }

    [Test]
    public void DraftState_AllowsTransitionToCancelled()
    {
        // Arrange
        SessionDraftState state = new();

        // Act & Assert
        state.CanTransitionTo("Cancelled").Should().BeTrue();
    }

    [Test]
    public void DraftState_DoesNotAllowTransitionToCompleted()
    {
        // Arrange
        SessionDraftState state = new();

        // Act & Assert
        state.CanTransitionTo("Completed").Should().BeFalse();
    }

    [Test]
    public void InProgressState_AllowsTransitionToCompleted()
    {
        // Arrange
        SessionInProgressState state = new();

        // Act & Assert
        state.CanTransitionTo("Completed").Should().BeTrue();
    }

    [Test]
    public void InProgressState_AllowsTransitionToCancelled()
    {
        // Arrange
        SessionInProgressState state = new();

        // Act & Assert
        state.CanTransitionTo("Cancelled").Should().BeTrue();
    }

    [Test]
    public void CompletedState_IsTerminal()
    {
        // Arrange
        SessionCompletedState state = new();

        // Act & Assert
        state.AllowedTransitions.Should().BeEmpty();
        state.CanTransitionTo("InProgress").Should().BeFalse();
        state.CanTransitionTo("Cancelled").Should().BeFalse();
    }

    [Test]
    public void CancelledState_IsTerminal()
    {
        // Arrange
        SessionCancelledState state = new();

        // Act & Assert
        state.AllowedTransitions.Should().BeEmpty();
        state.CanTransitionTo("Draft").Should().BeFalse();
    }

    [Test]
    public async Task InProgressState_OnEnter_SetsStartedAtUtc()
    {
        // Arrange
        StocktakeSession entity = new() { WarehouseId = 1, Name = "Test", Status = "Draft" };
        WorkflowContext context = new() { UserId = 0, TimestampUtc = new DateTime(2026, 4, 13, 9, 0, 0, DateTimeKind.Utc) };

        // Act
        SessionInProgressState state = new();
        await state.OnEnterAsync(entity, context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        entity.Status.Should().Be("InProgress");
        entity.StartedAtUtc.Should().Be(context.TimestampUtc);
    }

    [Test]
    public async Task CompletedState_OnEnter_SetsCompletionFields()
    {
        // Arrange
        StocktakeSession entity = new() { WarehouseId = 1, Name = "Test", Status = "InProgress" };
        WorkflowContext context = new() { UserId = 15, TimestampUtc = new DateTime(2026, 4, 13, 16, 0, 0, DateTimeKind.Utc) };

        // Act
        SessionCompletedState state = new();
        await state.OnEnterAsync(entity, context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        entity.Status.Should().Be("Completed");
        entity.CompletedAtUtc.Should().Be(context.TimestampUtc);
        entity.CompletedByUserId.Should().Be(15);
    }

    [Test]
    public void Engine_InvalidTransition_DraftToCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        WorkflowContext context = new() { UserId = 1, TimestampUtc = DateTime.UtcNow };

        // Act
        Func<Task> act = () => _engine.TransitionAsync(
            new StocktakeSession { WarehouseId = 1, Name = "Test", Status = "Draft" },
            "Draft", "Completed", context, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public void Engine_CanTransition_ReturnsCorrectResults()
    {
        // Act & Assert
        _engine.CanTransition("Draft", "InProgress").Should().BeTrue();
        _engine.CanTransition("Draft", "Cancelled").Should().BeTrue();
        _engine.CanTransition("Draft", "Completed").Should().BeFalse();
        _engine.CanTransition("InProgress", "Completed").Should().BeTrue();
        _engine.CanTransition("InProgress", "Cancelled").Should().BeTrue();
        _engine.CanTransition("Completed", "Draft").Should().BeFalse();
        _engine.CanTransition("Cancelled", "Draft").Should().BeFalse();
    }
}
