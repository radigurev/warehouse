# CHG-{TYPE}-{NNN} — {Title}

> Status: Draft
> Last updated: {YYYY-MM-DD}
> Owner: TBD
> Priority: {P1|P2|P3}

## 1. Context & Scope

**Why this change is needed:**
<!-- 2-3 sentences explaining the motivation -->

**Scope:**
- [ ] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

<!-- Use MUST / SHOULD / MAY terminology -->
<!-- Each rule should be independently testable -->

- The system MUST ...
- The system SHOULD ...
- The system MAY ...

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | | | |

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | | | | |

## 5. Versioning Notes

**API version impact:** None | New version | Breaking change

**Database migration required:** Yes | No

**Backwards compatibility:** Fully compatible | Requires migration | Breaking

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] {TestName}` — {what it verifies}

### Integration Tests

- [ ] `[Integration] {TestName}` — {what it verifies}

## 7. Detailed Design

### API Changes

<!-- New or modified endpoints, request/response contracts -->

### Data Model Changes

<!-- New entities, modified columns, migration scripts -->

### Service Layer Changes

<!-- New or modified services, business logic -->

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-XXX-NNN | {describe how this spec is affected} |

## Migration Plan

1. **Pre-deployment:** ...
2. **Deployment:** ...
3. **Post-deployment:** ...
4. **Rollback:** ...

## Open Questions

- [ ] {Question that needs resolution before implementation}
