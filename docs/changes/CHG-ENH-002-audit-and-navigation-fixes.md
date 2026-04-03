# CHG-ENH-002 — Audit UX and Navigation Improvements

> Status: Implemented
> Last updated: 2026-04-03
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**
During UI testing of the CHG-ENH-001 redesign, several usability issues were identified: audit logs displayed raw user IDs instead of usernames, the action filter was a free-text input instead of a dropdown, date filters used the browser's native date picker which is inconsistent across browsers, the Apply Filters button didn't work due to parameter name mismatches, and the collapsed sidebar navigation needed visual improvements for grouped menu items.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [x] Frontend changes
- [ ] Configuration changes

---

## 2. Behavior (RFC 2119)

### 2.1 Audit Log Username Resolution

- The audit log User column MUST display usernames instead of numeric user IDs.
- The SPA MUST load all users (including inactive) on the audit page to build a userId-to-username map.
- If a user ID cannot be resolved (e.g., hard-deleted), the display MUST fall back to `#<id>`.
- Null user IDs MUST display as a dash (`—`).

### 2.2 Include Inactive Users API Parameter

- The `GET /api/v1/users` endpoint MUST accept an optional `includeInactive` query parameter (default: `false`).
- When `includeInactive` is `true`, the endpoint MUST return both active and deactivated users.
- When `includeInactive` is `false` (default), existing behavior is preserved (active users only).

### 2.3 Audit Log User Filter

- The user filter MUST be an autocomplete dropdown populated with usernames (not a numeric input).
- The autocomplete MUST require at least 2 characters before showing suggestions.
- The `GET /api/v1/audit` endpoint MUST accept an optional `userId` query parameter to filter by user.

### 2.4 Audit Log Action Filter

- The action filter MUST be an autocomplete dropdown populated with distinct action values from the loaded audit data.
- The autocomplete MUST require at least 2 characters before showing suggestions.

### 2.5 Date Picker

- Date From and Date To filters MUST use a Vuetify `v-date-picker` inside a menu popup instead of the browser's native date input.
- Selecting a date MUST close the picker and populate the field.
- The field MUST be clearable.

### 2.6 Audit Filter Parameter Alignment

- The frontend MUST send `fromDate` and `toDate` parameters matching the API's expected names (not `dateFrom`/`dateTo`).

### 2.7 Collapsed Sidebar Admin Group

- In rail (collapsed) mode, the Admin group MUST show a toggle with a chevron arrow.
- The arrow MUST animate (rotate 180°) when toggling open/closed.
- Sub-items MUST animate with an expand/collapse transition.
- When expanded, the Admin parent item MUST have a subtle indigo background tint.
- Sub-items MUST have a slightly stronger indigo background tint to visually group them.
- There MUST be a small gap between the parent toggle and the children container.
- Sub-item icons MUST remain centered and aligned with top-level navigation icons.

---

## 3. Validation Rules

No new validation rules.

---

## 4. Error Rules

No new error rules.

---

## 5. Versioning Notes

**API version impact:** Non-breaking — new optional query parameters only.

**Database migration required:** No

**Backwards compatibility:** Fully compatible.

---

## 6. Test Plan

### Unit Tests

- `[Unit] auditView_ResolvesUserIdToUsername` — User ID displays as username
- `[Unit] auditView_UnknownUserId_ShowsFallback` — Unknown ID shows #id format
- `[Unit] auditView_NullUserId_ShowsDash` — Null userId shows dash
- `[Unit] userFilter_RequiresMinTwoChars` — Autocomplete hides items until 2 chars typed
- `[Unit] actionFilter_PopulatesFromAuditData` — Dropdown contains distinct action values

### Integration Tests

- `[Integration] getUsersWithInactiveFlag_ReturnsAllUsers` — includeInactive=true returns deactivated users
- `[Integration] auditEndpoint_FilterByUserId_ReturnsFiltered` — userId param filters results
- `[Integration] auditEndpoint_FilterByDateRange_ReturnsFiltered` — fromDate/toDate params work

---

## 7. Detailed Design

### API Changes

**`GET /api/v1/users`** — Added `includeInactive` query parameter (bool, default false).

**`GET /api/v1/audit`** — Added `userId` query parameter (int?, default null).

### Modified Files

| File | Changes |
|---|---|
| `src/Interfaces/Warehouse.Auth.API/Interfaces/IUserService.cs` | Added `bool includeInactive` parameter |
| `src/Interfaces/Warehouse.Auth.API/Services/UserService.cs` | Conditional `IsActive` filter |
| `src/Interfaces/Warehouse.Auth.API/Controllers/UsersController.cs` | Added `includeInactive` query param |
| `src/Interfaces/Warehouse.Auth.API/Controllers/AuditController.cs` | Added `userId` query param |
| `frontend/src/api/users.ts` | Added `includeInactive` parameter |
| `frontend/src/api/audit.ts` | Fixed param names: `fromDate`/`toDate` |
| `frontend/src/views/audit/AuditView.vue` | Username resolution, autocomplete filters, date picker, min 2 char search |
| `frontend/src/components/templates/DefaultLayout.vue` | Rail mode admin group styling and animations |

---

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-UI-001 | Audit view enhanced with username display, dropdown filters, date picker |
| SDD-AUTH-001 | GET /users now accepts includeInactive param; GET /audit now accepts userId param |

---

## Migration Plan

1. **Pre-deployment:** No preparation needed.
2. **Deployment:** Deploy backend first (new optional params are backwards compatible), then frontend.
3. **Post-deployment:** Verify audit log shows usernames, filters work, sidebar rail mode visuals.
4. **Rollback:** Revert both frontend and backend builds.

---

## Open Questions

None.
