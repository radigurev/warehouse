# CHG-REFAC-003 — State Pattern for Workflow State Machines

> Status: Implemented
> Last updated: 2026-04-13
> Owner: TBD
> Priority: P1

## 1. Context & Scope

**Why this change is needed:**
Three inventory workflows (InventoryAdjustment, WarehouseTransfer, StocktakeSession) use string-based status fields with transition logic scattered across service methods. Each method re-validates the current status with `if (entity.Status != "Pending")` before proceeding. State-entry side effects (timestamps, userId, event publishing, stock snapshots) are embedded in service methods rather than encapsulated by the state itself. Adding a new state or transition requires modifying existing service methods, violating the Open/Closed Principle. As new workflows are introduced (Production Orders, Quality Holds, Fulfillment Orders), this pattern will not scale.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### State Infrastructure

- The system MUST define an `IWorkflowState<TEntity>` interface with methods: `CanTransitionTo(string targetStatus)`, `OnEnter(TEntity entity, WorkflowContext context)`, `GetAllowedTransitions()`.
- The system MUST provide concrete state classes for each workflow status value.
- The system MUST enforce that all state transitions go through the state machine — direct string assignment to `Status` properties MUST be prohibited in service code.
- The system SHOULD register state classes in DI so they can access services (e.g., event publishing, stock adjustment).
- The system MUST throw `InvalidOperationException` when an invalid transition is attempted.

### Inventory Adjustment Workflow

- States: `PendingState`, `ApprovedState`, `RejectedState`, `AppliedState`.
- `PendingState` MUST allow transitions to `Approved` and `Rejected`.
- `ApprovedState` MUST allow transition to `Applied` only.
- `RejectedState` and `AppliedState` MUST be terminal — no outbound transitions.
- `ApprovedState.OnEnter` MUST set `ApprovedAtUtc` and `ApprovedByUserId`.
- `AppliedState.OnEnter` MUST execute stock adjustments and publish `InventoryAdjustmentAppliedEvent`.
- `RejectedState.OnEnter` MUST set `RejectedAtUtc` and `RejectedByUserId`.

### Warehouse Transfer Workflow

- States: `DraftState`, `CompletedState`, `CancelledState`.
- `DraftState` MUST allow transitions to `Completed` and `Cancelled`.
- `CompletedState` and `CancelledState` MUST be terminal.
- `CompletedState.OnEnter` MUST execute stock adjustments for all transfer lines (debit source, credit destination) and publish `WarehouseTransferCompletedEvent`.
- `CompletedState.OnEnter` MUST set `CompletedAtUtc` and `CompletedByUserId`.

### Stocktake Session Workflow

- States: `DraftState`, `InProgressState`, `CompletedState`, `CancelledState`.
- `DraftState` MUST allow transition to `InProgress` only.
- `InProgressState` MUST allow transitions to `Completed` and `Cancelled`.
- `DraftState` and `InProgressState` MUST allow transition to `Cancelled`.
- `CompletedState` and `CancelledState` MUST be terminal.
- `InProgressState.OnEnter` MUST trigger `SnapshotStockLevelsAsync` and set `StartedAtUtc`.
- `CompletedState.OnEnter` MUST set `CompletedAtUtc` and `CompletedByUserId`.

### Extensibility

- Adding a new workflow (e.g., `ProductionOrder`) MUST require only: defining new state classes and registering them in DI.
- Adding a new state to an existing workflow MUST require only: creating the new state class and updating the originating state's `GetAllowedTransitions()`.
- The state machine infrastructure MUST be generic and reusable across all workflow types.

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | Transition request | Target state MUST be in the current state's allowed transitions | INVALID_STATE_TRANSITION |
| V2 | Terminal state | No transitions MUST be allowed from terminal states | WORKFLOW_COMPLETED |

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Transition not allowed from current state | 409 | INVALID_STATE_TRANSITION | Cannot transition from {current} to {target}. |
| E2 | Entity already in terminal state | 409 | WORKFLOW_COMPLETED | This {entity} has already been {status} and cannot be modified. |

## 5. Versioning Notes

**API version impact:** None — external API contracts remain identical.

**Database migration required:** No — `Status` column continues to store string values.

**Backwards compatibility:** Fully compatible — internal refactor only.

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] AdjustmentPendingState_AllowsTransitionToApproved` — verify allowed transitions
- [ ] `[Unit] AdjustmentPendingState_AllowsTransitionToRejected` — verify allowed transitions
- [ ] `[Unit] AdjustmentApprovedState_AllowsTransitionToApplied` — verify allowed transitions
- [ ] `[Unit] AdjustmentAppliedState_IsTerminal` — verify no outbound transitions
- [ ] `[Unit] AdjustmentRejectedState_IsTerminal` — verify no outbound transitions
- [ ] `[Unit] AppliedState_OnEnter_ExecutesStockAdjustments` — verify side effects
- [ ] `[Unit] AppliedState_OnEnter_PublishesEvent` — verify event publishing
- [ ] `[Unit] TransferDraftState_AllowsCompletedAndCancelled` — verify transitions
- [ ] `[Unit] TransferCompletedState_OnEnter_AdjustsStock` — verify stock debit/credit
- [ ] `[Unit] SessionDraftState_AllowsInProgress` — verify transitions
- [ ] `[Unit] SessionInProgressState_OnEnter_SnapshotsStock` — verify snapshot trigger
- [ ] `[Unit] InvalidTransition_ThrowsInvalidOperationException` — verify guard

### Integration Tests

- [ ] `[Integration] AdjustmentWorkflow_FullLifecycle_PendingToApplied` — end-to-end workflow
- [ ] `[Integration] TransferWorkflow_FullLifecycle_DraftToCompleted` — end-to-end workflow
- [ ] `[Integration] StocktakeWorkflow_FullLifecycle_DraftToCompleted` — end-to-end workflow

## 7. Detailed Design

### New Interfaces

```csharp
// src/Warehouse.Common/Workflow/IWorkflowState.cs
public interface IWorkflowState<TEntity> where TEntity : class
{
    string StatusName { get; }
    IReadOnlySet<string> AllowedTransitions { get; }
    bool CanTransitionTo(string targetStatus);
    Task OnEnterAsync(TEntity entity, WorkflowContext context, CancellationToken cancellationToken);
}

// src/Warehouse.Common/Workflow/WorkflowContext.cs
public sealed class WorkflowContext
{
    public required int UserId { get; init; }
    public required DateTime TimestampUtc { get; init; }
}

// src/Warehouse.Common/Workflow/IWorkflowEngine.cs
public interface IWorkflowEngine<TEntity> where TEntity : class
{
    Task TransitionAsync(TEntity entity, string targetStatus, WorkflowContext context, CancellationToken cancellationToken);
}
```

### State Class Example

```csharp
// src/Interfaces/Inventory/Warehouse.Inventory.API/Workflow/Adjustment/PendingState.cs
public sealed class AdjustmentPendingState : IWorkflowState<InventoryAdjustment>
{
    public string StatusName => "Pending";
    public IReadOnlySet<string> AllowedTransitions => new HashSet<string> { "Approved", "Rejected" };
    public bool CanTransitionTo(string targetStatus) => AllowedTransitions.Contains(targetStatus);
    
    public Task OnEnterAsync(InventoryAdjustment entity, WorkflowContext context, CancellationToken cancellationToken)
    {
        entity.Status = StatusName;
        entity.CreatedAtUtc = context.TimestampUtc;
        entity.CreatedByUserId = context.UserId;
        return Task.CompletedTask;
    }
}
```

### Service Layer Changes

- `InventoryAdjustmentService.ApproveAsync` — replace inline status check and field assignment with `_workflowEngine.TransitionAsync(adjustment, "Approved", context, ct)`.
- `InventoryAdjustmentService.RejectAsync` — same pattern.
- `InventoryAdjustmentService.ApplyAsync` — same pattern. `AppliedState.OnEnterAsync` handles stock adjustments and event publishing.
- `WarehouseTransferService.CompleteAsync` — same pattern.
- `WarehouseTransferService.CancelAsync` — same pattern.
- `StocktakeSessionService.StartAsync` — same pattern. `InProgressState.OnEnterAsync` handles snapshot.
- `StocktakeSessionService.CompleteAsync` — same pattern.
- `StocktakeSessionService.CancelAsync` — same pattern.

### File Structure

```
src/Warehouse.Common/Workflow/
├── IWorkflowState.cs
├── IWorkflowEngine.cs
├── WorkflowContext.cs
└── WorkflowEngine.cs

src/Interfaces/Inventory/Warehouse.Inventory.API/Workflow/
├── Adjustment/
│   ├── AdjustmentPendingState.cs
│   ├── AdjustmentApprovedState.cs
│   ├── AdjustmentAppliedState.cs
│   └── AdjustmentRejectedState.cs
├── Transfer/
│   ├── TransferDraftState.cs
│   ├── TransferCompletedState.cs
│   └── TransferCancelledState.cs
└── Stocktake/
    ├── SessionDraftState.cs
    ├── SessionInProgressState.cs
    ├── SessionCompletedState.cs
    └── SessionCancelledState.cs
```

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-INV-002 | Adjustment workflow transitions now enforced by state machine — behavior identical |
| SDD-INV-003 | Transfer workflow transitions now enforced by state machine — behavior identical |
| SDD-INV-004 | Stocktake session workflow transitions now enforced by state machine — behavior identical |

## Migration Plan

1. **Pre-deployment:** Implement state interfaces in Warehouse.Common, then state classes in Inventory.API. Refactor services to delegate to workflow engine. Run all existing tests.
2. **Deployment:** Deploy updated assemblies — no schema changes.
3. **Post-deployment:** Verify workflows via API (create adjustment → approve → apply; create transfer → complete; create session → start → complete).
4. **Rollback:** Revert to previous commit — no data migration needed.

## Open Questions

- [ ] Should state classes have access to DbContext directly, or should side effects be delegated via callbacks/events passed through WorkflowContext?
- [ ] Should the workflow engine log transitions to an audit trail separate from domain events?
