# CHG-ENH-005 — Error Code Localization Contract

> Status: Draft
> Last updated: 2026-04-09
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**
The backend currently returns English error messages in the `detail` field of ProblemDetails responses. The frontend translates error codes via i18n when a matching key exists, but falls back to the raw English `detail` string when the key is missing — showing English to Bulgarian users. FluentValidation errors bypass the error code system entirely: the `errors` dictionary contains English messages from `.WithMessage()`, not the machine-readable codes defined via `.WithErrorCode()`. Additionally, error codes are magic strings scattered across services with no compile-time safety, making it easy to introduce typos or mismatches between backend codes and frontend i18n keys.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [x] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### 2.1 Error Code Constants (Backend)

- All service-layer error codes MUST be defined as `public const string` fields in static classes within `Warehouse.Common/ErrorCodes/`.
- Error code constants MUST be organized by domain: one static class per domain (e.g., `AuthErrorCodes`, `CustomerErrorCodes`, `InventoryErrorCodes`, `PurchasingErrorCodes`, `FulfillmentErrorCodes`).
- Validation error codes shared across domains (e.g., `INVALID_NAME`, `MISSING_FIELD`) MUST be defined in a `CommonValidationErrorCodes` class.
- Infrastructure error codes (e.g., `INTERNAL_ERROR`, `VALIDATION_ERROR`) MUST be defined in an `InfraErrorCodes` class.
- All `Result.Failure()` and `Result<T>.Failure()` calls MUST reference a constant from the error code classes — raw string literals are not allowed.
- All FluentValidation `.WithErrorCode()` calls MUST reference a constant from the error code classes.
- Error code values MUST follow `SCREAMING_SNAKE_CASE` convention (e.g., `CUSTOMER_NOT_FOUND`).
- Error code values MUST NOT contain domain prefixes that duplicate the class name (e.g., use `NOT_FOUND` in `CustomerErrorCodes`, not `CUSTOMER_NOT_FOUND`) — **exception:** codes that are already established in the frontend i18n files MUST keep their current value to avoid a breaking rename. New codes SHOULD follow this rule.

### 2.2 ProblemDetails Response Contract (Backend)

- The `title` field MUST contain the machine-readable error code (e.g., `CUSTOMER_NOT_FOUND`).
- The `detail` field MUST contain a developer-oriented English description. This field is for logging, debugging, and API consumers — it is NOT intended for end-user display.
- The `type` field MUST follow the pattern `https://warehouse.local/errors/{errorCode}`.
- The `status` field MUST contain the HTTP status code.
- The `instance` field MUST contain the request path.
- This contract is already implemented in `BaseApiController.ToProblemResult()` — no changes needed to the response shape.

### 2.3 FluentValidation Error Response (Backend)

- The system MUST customize `ApiBehaviorOptions.InvalidModelStateResponseFactory` to produce a ProblemDetails response where the `errors` dictionary values contain **error codes**, not English messages.
- The customized response MUST set `title` to `VALIDATION_ERROR`.
- The customized response MUST set `status` to `400`.
- The `errors` dictionary MUST use the property name (PascalCase as sent by ASP.NET) as the key and an array of error code strings as the value.
- When a validation failure has no `ErrorCode` set (third-party or default validators), the system SHOULD fall back to the `ErrorMessage` as-is.
- The `detail` field SHOULD contain a generic developer message such as `"One or more validation errors occurred."`.

**Example — current behavior (English messages):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Username": ["Username is required."],
    "Password": ["Password must be at least 8 characters."]
  }
}
```

**Example — target behavior (error codes):**
```json
{
  "type": "https://warehouse.local/errors/VALIDATION_ERROR",
  "title": "VALIDATION_ERROR",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/users",
  "errors": {
    "Username": ["MISSING_USERNAME"],
    "Password": ["INVALID_PASSWORD"]
  }
}
```

### 2.4 Frontend Error Translation (Frontend)

- The frontend MUST translate all error codes via `t('errors.{CODE}')` before displaying to the user.
- The frontend MUST NOT display the `detail` field from ProblemDetails to end users.
- When a validation error response is received (status 400 with `errors` dictionary), the frontend MUST translate each error code in the `errors` arrays via i18n before displaying.
- When an error code has no matching i18n key, the frontend MUST display a generic translated fallback message (`errors.UNEXPECTED_ERROR`) and SHOULD log the untranslated code to the console for developer awareness.
- Field-level error display in form dialogs MUST use the translated error message from i18n, not the raw code.

### 2.5 i18n Key Synchronization

- Every error code constant defined in `Warehouse.Common/ErrorCodes/` MUST have a corresponding key in both `en.ts` and `bg.ts` under the `errors` namespace.
- New error codes added to the backend MUST include the corresponding i18n translations in the same changeset.
- Validation error codes MUST be added under `errors` in the i18n files, using the same key as the constant value (e.g., `errors.MISSING_USERNAME`).

### 2.6 Error Code Naming Conventions

- **Not found:** `{ENTITY}_NOT_FOUND` (e.g., `CUSTOMER_NOT_FOUND`, `PRODUCT_NOT_FOUND`)
- **Duplicate/conflict:** `DUPLICATE_{FIELD_OR_ENTITY}` (e.g., `DUPLICATE_TAX_ID`, `DUPLICATE_PRODUCT_CODE`)
- **Invalid state:** `{ENTITY}_{CONSTRAINT}` (e.g., `ADJUSTMENT_NOT_PENDING`, `PO_NOT_EDITABLE`)
- **Missing required field:** `MISSING_{FIELD}` (e.g., `MISSING_USERNAME`, `MISSING_PASSWORD`)
- **Invalid field value:** `INVALID_{FIELD}` (e.g., `INVALID_EMAIL`, `INVALID_PRODUCT_CODE`)
- **In-use constraint:** `{ENTITY}_IN_USE` (e.g., `ROLE_IN_USE`, `CATEGORY_IN_USE`)
- **Business rule:** descriptive code (e.g., `INSUFFICIENT_STOCK`, `BOM_CIRCULAR_REFERENCE`)

## 3. Validation Rules

Not applicable — this spec defines infrastructure behavior, not a user-facing entity.

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | FluentValidation fails on one or more fields | 400 | `VALIDATION_ERROR` | One or more validation errors occurred. |
| E2 | Unhandled exception in pipeline | 500 | `INTERNAL_ERROR` | An unexpected error occurred. Please try again later. |
| E3 | Service returns `Result.Failure(...)` | Varies | Per-service code | Per-service developer message |

## 5. Versioning Notes

**API version impact:** None — the ProblemDetails shape does not change. Only the content of `errors` dictionary values changes from English messages to error codes.

**Database migration required:** No

**Backwards compatibility:** Mostly compatible. Frontend consumers that parse the `errors` dictionary values as human-readable messages will see error codes instead. The frontend `getApiErrorMessage.ts` already handles error codes, but the validation error branch needs updating. Any external API consumers relying on exact validation message text will break — this is acceptable as validation messages were never a stable contract.

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] ErrorCodeConstants_AllCodes_AreUniqueWithinClass` — verify no duplicate values within each error code class
- [ ] `[Unit] ErrorCodeConstants_AllCodes_FollowScreamingSnakeCase` — verify naming convention via reflection
- [ ] `[Unit] ValidationResponseFactory_WhenValidationFails_ReturnsErrorCodesInErrorsDictionary` — verify error codes surface instead of messages
- [ ] `[Unit] ValidationResponseFactory_WhenErrorCodeMissing_FallsBackToErrorMessage` — verify fallback for validators without error codes
- [ ] `[Unit] ValidationResponseFactory_SetsTitle_ToValidationError` — verify title field
- [ ] `[Unit] ValidationResponseFactory_SetsCorrectProblemDetailsShape` — verify type, status, instance fields

### Integration Tests

- [ ] `[Integration] POST_InvalidRequest_ReturnsErrorCodesInValidationResponse` — send invalid request, verify response contains error codes not English messages
- [ ] `[Integration] POST_InvalidRequest_AllErrorCodesHaveI18nKeys` — verify every code returned exists in i18n files (build-time or CI check)

## 7. Detailed Design

### 7.1 Error Code Constants (`Warehouse.Common/ErrorCodes/`)

```
Warehouse.Common/
  ErrorCodes/
    AuthErrorCodes.cs
    CustomerErrorCodes.cs
    InventoryErrorCodes.cs
    PurchasingErrorCodes.cs
    FulfillmentErrorCodes.cs
    CommonValidationErrorCodes.cs
    InfraErrorCodes.cs
```

**Example — `AuthErrorCodes.cs`:**
```csharp
namespace Warehouse.Common.ErrorCodes;

public static class AuthErrorCodes
{
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
    public const string REFRESH_TOKEN_EXPIRED = "REFRESH_TOKEN_EXPIRED";
    public const string REFRESH_TOKEN_REVOKED = "REFRESH_TOKEN_REVOKED";
    public const string DUPLICATE_USERNAME = "DUPLICATE_USERNAME";
    public const string DUPLICATE_EMAIL = "DUPLICATE_EMAIL";
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";
    public const string ROLE_NOT_FOUND = "ROLE_NOT_FOUND";
    public const string DUPLICATE_ROLE_NAME = "DUPLICATE_ROLE_NAME";
    public const string DUPLICATE_PERMISSION = "DUPLICATE_PERMISSION";
    public const string ROLE_IN_USE = "ROLE_IN_USE";
    public const string PROTECTED_ROLE = "PROTECTED_ROLE";
    public const string PERMISSION_NOT_FOUND = "PERMISSION_NOT_FOUND";
    public const string INVALID_CURRENT_PASSWORD = "INVALID_CURRENT_PASSWORD";
}
```

**Example — `CommonValidationErrorCodes.cs`:**
```csharp
namespace Warehouse.Common.ErrorCodes;

public static class CommonValidationErrorCodes
{
    public const string MISSING_USERNAME = "MISSING_USERNAME";
    public const string MISSING_PASSWORD = "MISSING_PASSWORD";
    public const string INVALID_USERNAME = "INVALID_USERNAME";
    public const string INVALID_EMAIL = "INVALID_EMAIL";
    public const string INVALID_PASSWORD = "INVALID_PASSWORD";
    public const string INVALID_FIRST_NAME = "INVALID_FIRST_NAME";
    public const string INVALID_LAST_NAME = "INVALID_LAST_NAME";
    // ... all validation codes
}
```

**Example — `InfraErrorCodes.cs`:**
```csharp
namespace Warehouse.Common.ErrorCodes;

public static class InfraErrorCodes
{
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
}
```

### 7.2 Custom Validation Response Factory (`Warehouse.Infrastructure`)

Register in a shared configuration method called by all API `Program.cs` files:

```csharp
services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        Dictionary<string, string[]> errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors
                    .Select(e => !string.IsNullOrEmpty(e.ErrorMessage)
                        ? e.ErrorMessage
                        : "VALIDATION_ERROR")
                    .ToArray()
            );

        ProblemDetails problem = new()
        {
            Type = "https://warehouse.local/errors/VALIDATION_ERROR",
            Title = "VALIDATION_ERROR",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path,
            Extensions = { ["errors"] = errors }
        };

        return new BadRequestObjectResult(problem);
    };
});
```

**Key detail:** FluentValidation populates `ModelState` with `ErrorMessage` set to the value from `.WithErrorCode()` when auto-validation is used. The factory reads this field. For validators that use `.WithErrorCode("CODE").WithMessage("msg")`, the `ErrorMessage` contains the message text and the `ErrorCode` is stored separately. To surface the error code instead of the message, the factory MUST inspect the underlying `ModelValidationState` entries or the validators MUST be updated to use the error code as the message parameter (`.WithMessage("MISSING_USERNAME")`).

**Recommended approach:** Update all `.WithMessage()` calls to pass the error code string. This is simpler than trying to extract `ErrorCode` from the validation pipeline, and the English message is no longer needed since the frontend translates by code.

**Before:**
```csharp
RuleFor(x => x.Username)
    .NotEmpty().WithErrorCode("MISSING_USERNAME").WithMessage("Username is required.");
```

**After:**
```csharp
RuleFor(x => x.Username)
    .NotEmpty().WithErrorCode(CommonValidationErrorCodes.MISSING_USERNAME)
    .WithMessage(CommonValidationErrorCodes.MISSING_USERNAME);
```

### 7.3 Frontend Changes (`frontend/src/shared/utils/getApiErrorMessage.ts`)

Update the validation error branch to translate codes:

**Before (lines 23-34):**
```typescript
if (status === 400 && data?.errors) {
    const messages = Object.values(data.errors).flat();
    return messages.join(' ');
}
```

**After:**
```typescript
if (status === 400 && data?.errors) {
    const codes = Object.values(data.errors).flat();
    const translated = codes.map(code =>
        te(`errors.${code}`) ? t(`errors.${code}`) : code
    );
    return translated.join(' ');
}
```

### 7.4 Frontend Fallback Improvement

Update the final fallback to never show raw English:

**Before (line 37-39):**
```typescript
if (data?.detail) {
    return data.detail;
}
```

**After:**
```typescript
// detail is developer-only — do not display to user
// Fall through to generic error handling below
```

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-INFRA-001 | BaseApiController error handling — no shape change, but validation response factory is a new behavior |
| SDD-AUTH-001 | Auth validators and service error codes — codes become constants |
| SDD-CUST-001 | Customer validators and service error codes — codes become constants |
| SDD-INV-001 | Product/catalog validators and service error codes — codes become constants |
| SDD-INV-002 | Stock management service error codes — codes become constants |
| SDD-INV-003 | Warehouse structure service error codes — codes become constants |
| SDD-INV-004 | Stocktaking service error codes — codes become constants |
| SDD-UI-001 | Frontend error display — validation errors will show translated codes |

## Migration Plan

1. **Pre-deployment:** No preparation needed — change is additive.
2. **Deployment:**
   - Phase A: Add error code constant classes to `Warehouse.Common`. Update all `Result.Failure()` calls and `.WithErrorCode()` calls to use constants. Update `.WithMessage()` calls to pass the error code string. This is safe to deploy independently — no behavioral change yet.
   - Phase B: Register custom `InvalidModelStateResponseFactory` in all API projects. Deploy backend. Validation responses now return codes instead of messages.
   - Phase C: Update `getApiErrorMessage.ts` and deploy frontend. Ensure all validation codes have i18n entries in both `en.ts` and `bg.ts`.
3. **Post-deployment:** Monitor logs for untranslated error codes (console warnings from frontend fallback).
4. **Rollback:** Remove the custom `InvalidModelStateResponseFactory` registration to revert to default ASP.NET validation responses. Error code constants can remain — they are inert without the factory.

## Open Questions

- [ ] Should the `detail` field be removed entirely from ProblemDetails, or kept for developer/API consumer use?
- [ ] Should there be a CI lint step that verifies every error code constant has a matching i18n key in both locales?
- [ ] Should validation codes be domain-specific (e.g., `CustomerValidationErrorCodes`) or shared (`CommonValidationErrorCodes`)? Current draft uses shared for cross-domain codes.
- [ ] For validators with multiple rules on the same field (e.g., NotEmpty + MinLength + Regex on Username), should each rule have a distinct code (e.g., `MISSING_USERNAME`, `USERNAME_TOO_SHORT`, `USERNAME_INVALID_FORMAT`) or share one code (`INVALID_USERNAME`)?
