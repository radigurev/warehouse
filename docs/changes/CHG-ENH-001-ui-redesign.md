# CHG-ENH-001 — Auth SPA UI Redesign

> Status: Implemented
> Last updated: 2026-04-03
> Owner: TBD
> Priority: P1

## 1. Context & Scope

**Why this change is needed:**
The initial SPA implementation (SDD-UI-001) is functional but uses a dated color scheme, plain page headers, and basic table interactions. The UI needs a modern, professional redesign before customer-facing testing. This includes visual polish, improved navigation structure, better notification UX, column-level filtering, and restructuring to Atomic Design for long-term maintainability.

**Scope:**
- [ ] Backend API changes
- [ ] Database schema changes
- [x] Frontend changes
- [ ] Configuration changes

---

## 2. Behavior (RFC 2119)

### 2.1 Color Palette

- The SPA MUST use a bright, vibrant color palette based on indigo/violet tones.
- The sidebar MUST use a dark slate background (`#334155` Slate-700) with light text.
- The app bar MUST use a white/surface background with a subtle bottom border (no elevation).
- All interactive elements (buttons, links, active states) MUST use the primary indigo color.

### 2.2 App Bar — Dynamic Page Title

- The app bar MUST display the current page title dynamically (based on the active route), replacing the static "Warehouse Management" text.
- Pages MUST NOT render their own `<h1>` heading — the app bar title is the sole page identifier.
- The page title MUST update reactively when the user navigates between routes.

### 2.3 Rounded UI Elements

- All buttons MUST render with pill-shaped rounding (`rounded="pill"`) via Vuetify global defaults.
- All text inputs, selects, and textareas MUST render with large rounding (`rounded="lg"`).
- All cards MUST render with large rounding (`rounded="lg"`).
- All chips MUST render with pill-shaped rounding.

### 2.4 Input Icons

- Every text field, select, and textarea across all forms MUST have a semantic `prepend-inner-icon`.
- Icons MUST be contextually appropriate (e.g., `mdi-account` for username, `mdi-email` for email, `mdi-lock` for password).

### 2.5 User Profile Dropdown Menu

- The app bar MUST display a user avatar (first letter of username) with the username text.
- Hovering over the user area MUST open a dropdown menu containing:
  - User identity header (avatar + username)
  - Language section: English and Bulgarian options (radio-style selection)
  - Display section: Density toggle (compact/comfortable)
  - Logout action (styled with error color)
- The separate density toggle button, language switcher dropdown, and logout button MUST be removed from the app bar.

### 2.6 Admin Submenu in Sidebar

- The sidebar MUST group Users, Roles, Permissions, and Audit Logs under an expandable "Admin" section.
- The Admin section MUST only be visible to users with the `Admin` role.
- Dashboard MUST remain a top-level navigation item (not inside Admin).
- The Admin group MUST use a `v-list-group` with an expand/collapse icon.
- Navigation items MUST be styled as pills with rounded active state.

### 2.7 Action Pills in Data Tables

- Row action buttons in data tables MUST be rendered as tonal pill chips (`v-chip` with `variant="tonal"`, `prepend-icon`, and translated label text).
- Each action MUST have a distinct color: primary for edit, info for secondary actions, error for destructive actions.
- Chip size MUST adapt to the active density mode (smaller in compact, standard in comfortable).

### 2.8 Column Filtering

- Data tables MUST support per-column filtering on text columns.
- Each filterable column header MUST display a filter icon button.
- Clicking the filter icon MUST open a popover with:
  - An operator select (Contains, Starts with, Ends with, Equals, Not equals)
  - A text input for the filter value
  - Apply and Clear buttons
- The filter icon MUST visually indicate when an active filter is applied (filled icon, primary color).
- Filters MUST apply client-side to the currently loaded data.
- Multiple column filters MUST combine with AND logic.
- Filterable columns per view:
  - Users: username, email, firstName, lastName
  - Roles: name
  - Permissions: resource, action
  - Audit: action, resource

### 2.9 Toast Notifications

- Non-critical messages (success, info, warning) MUST display as lightweight toast notifications positioned at the top-right of the screen.
- Toasts MUST auto-dismiss after a short timeout (4 seconds for success/info, 5 seconds for warning).
- Critical error messages MUST display as a more prominent snackbar notification.
- Toasts MUST stack vertically when multiple are shown simultaneously.

### 2.10 Atomic Design Pattern

- The component directory MUST be structured following the Atomic Design methodology:
  - `atoms/` — Smallest reusable UI elements (StatusChip, ActionChip)
  - `molecules/` — Composed groups of atoms (ColumnFilter, ConfirmDialog, ToastNotification)
  - `organisms/` — Complex feature components (UserFormDialog, RoleFormDialog, etc.)
  - `templates/` — Page-level layout shells (DefaultLayout)
- Views (`views/`) remain unchanged — they are Atomic Design "pages".

---

## 3. Validation Rules

No new validation rules. All existing validation from SDD-UI-001 Section 3 remains unchanged.

Column filter inputs do not require validation — empty filters are ignored, any text is accepted as a filter value.

---

## 4. Error Rules

No new error rules. All existing error handling from SDD-UI-001 Section 4 remains unchanged.

The toast notification system changes the presentation of errors, not the error logic itself.

---

## 5. Versioning Notes

**API version impact:** None — all changes are frontend-only.

**Database migration required:** No

**Backwards compatibility:** Fully compatible — no API contract changes.

---

## 6. Test Plan

### Unit Tests

- `[Unit] densityToggle_AffectsChipSize` — Compact mode uses x-small chips, comfortable uses small
- `[Unit] columnFilter_Contains_FiltersCorrectly` — Contains operator matches substring
- `[Unit] columnFilter_StartsWith_FiltersCorrectly` — Starts with operator matches prefix
- `[Unit] columnFilter_EndsWith_FiltersCorrectly` — Ends with operator matches suffix
- `[Unit] columnFilter_Equals_FiltersCorrectly` — Equals operator matches exact value
- `[Unit] columnFilter_NotEquals_FiltersCorrectly` — Not equals operator excludes exact match
- `[Unit] columnFilter_MultipleFilters_CombineWithAnd` — Two active filters both apply
- `[Unit] columnFilter_CaseInsensitive` — All operators ignore case
- `[Unit] columnFilter_EmptyValue_ShowsAllRows` — Empty filter value shows all data
- `[Unit] toastNotification_SuccessAutoDismisses` — Success toast disappears after 4 seconds
- `[Unit] toastNotification_ErrorStaysProminent` — Error notification uses prominent style
- `[Unit] adminSubmenu_VisibleForAdminRole` — Admin nav group renders for admin users
- `[Unit] adminSubmenu_HiddenForNonAdmin` — Admin nav group hidden for non-admin users
- `[Unit] appBar_ShowsDynamicPageTitle` — App bar title matches current route

### Integration Tests

- `[Integration] usersPage_ColumnFilter_FiltersByUsername` — Apply contains filter on username column
- `[Integration] rolesPage_ColumnFilter_FiltersByName` — Apply filter on role name column
- `[Integration] userMenu_LanguageSwitch_ChangesLocale` — Language switch in dropdown changes UI language
- `[Integration] userMenu_DensityToggle_ChangesLayout` — Density toggle in dropdown changes table density
- `[Integration] sidebar_AdminSubmenu_ExpandsAndNavigates` — Expand admin group, click Users, navigates correctly
- `[Integration] actionChips_UsersPage_TriggersDialogs` — Clicking edit chip opens edit dialog

---

## 7. Detailed Design

### Color Palette

| Token | Hex | Source |
|---|---|---|
| primary | `#6366F1` | Indigo-500 |
| secondary | `#94A3B8` | Slate-400 |
| accent | `#A78BFA` | Violet-400 |
| error | `#EF4444` | Red-500 |
| warning | `#FBBF24` | Amber-400 |
| info | `#60A5FA` | Blue-400 |
| success | `#34D399` | Emerald-400 |
| surface | `#F8FAFC` | Slate-50 |
| sidebar | `#334155` | Slate-700 |

### Atomic Design Directory Structure

```
frontend/src/components/
├── atoms/
│   ├── StatusChip.vue
│   └── ActionChip.vue
├── molecules/
│   ├── ColumnFilter.vue
│   ├── ConfirmDialog.vue
│   └── ToastNotification.vue
├── organisms/
│   ├── UserFormDialog.vue
│   ├── ChangePasswordDialog.vue
│   ├── UserRolesDialog.vue
│   ├── RoleFormDialog.vue
│   ├── RolePermissionsDialog.vue
│   └── PermissionFormDialog.vue
└── templates/
    └── DefaultLayout.vue
```

### Sidebar Navigation Structure

```
Dashboard           (top-level, icon: mdi-view-dashboard)
▼ Admin             (expandable group, icon: mdi-shield-crown)
  ├── Users         (icon: mdi-account-group)
  ├── Roles         (icon: mdi-shield-account)
  ├── Permissions   (icon: mdi-key)
  └── Audit Logs    (icon: mdi-clipboard-text-clock)
```

### New Files

| File | Purpose |
|---|---|
| `src/types/filter.ts` | FilterOperator, ColumnFilterState types |
| `src/composables/useColumnFilters.ts` | Reusable client-side column filter composable |
| `src/components/atoms/StatusChip.vue` | Active/Inactive status chip |
| `src/components/atoms/ActionChip.vue` | Tonal pill chip for table row actions |
| `src/components/molecules/ColumnFilter.vue` | Per-column filter popover |
| `src/components/molecules/ToastNotification.vue` | Lightweight top-right toast |

### Modified Files

| File | Changes |
|---|---|
| `src/main.ts` | New color palette, global rounded defaults |
| `src/components/templates/DefaultLayout.vue` | Sidebar color, admin submenu, light app bar, dynamic title, user dropdown, toast |
| `src/views/LoginView.vue` | Replace hardcoded blue with theme primary |
| `src/views/DashboardView.vue` | Remove h1 header |
| `src/views/users/UsersView.vue` | Remove h1, action chips, column filters, use atoms |
| `src/views/roles/RolesView.vue` | Remove h1, action chips, column filters |
| `src/views/permissions/PermissionsView.vue` | Remove h1, column filters |
| `src/views/audit/AuditView.vue` | Remove h1, input icons, column filters |
| `src/components/organisms/UserFormDialog.vue` | Input icons |
| `src/components/organisms/ChangePasswordDialog.vue` | Input icons |
| `src/components/organisms/RoleFormDialog.vue` | Input icons |
| `src/components/organisms/PermissionFormDialog.vue` | Input icons |
| `src/i18n/locales/en.ts` | Filter keys, admin nav label, user menu labels |
| `src/i18n/locales/bg.ts` | Same |
| `src/stores/auth.ts` | Store user roles for admin check |
| `src/router/index.ts` | Add page title meta to routes |

---

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-UI-001 | Visual redesign of all views, navigation restructure, new notification pattern, new filtering capability. After implementation, SDD-UI-001 sections 2.7 (Layout), 2.8 (Notifications), and 6 (Tech Stack) must be updated to reflect the new behavior. |

---

## Migration Plan

1. **Pre-deployment:** No preparation needed — frontend-only changes.
2. **Deployment:** Replace frontend build artifacts.
3. **Post-deployment:** Verify login flow, navigation, all CRUD operations, column filters.
4. **Rollback:** Revert to previous frontend build.

---

## Open Questions

None — all requirements confirmed by user.
