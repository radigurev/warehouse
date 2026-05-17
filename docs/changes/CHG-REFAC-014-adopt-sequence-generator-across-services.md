# CHG-REFAC-014 — Adopt `ISequenceGenerator` across all numbered entities (with frontend preview)

> Status: Draft
> Last updated: 2026-04-23
> Owner: TBD
> Priority: P2

---

## 1. Context & Scope

**Why this change is needed:**

`src/Warehouse.Infrastructure/Sequences/` already ships a race-proof, row-locking `ISequenceGenerator` with 12 pre-registered sequence keys (`PO`, `GR`, `SR`, `SO`, `PL`, `PKG`, `SHP`, `CR`, `SUPP`, `PROD`, `CUST`, `BATCH`). Today only `BATCH` is wired through it. Every other numbered entity (`SalesOrder.OrderNumber`, `PickList.PickListNumber`, `Parcel.ParcelNumber`, `Shipment.ShipmentNumber`, `CustomerReturn.ReturnNumber`, `PurchaseOrder.OrderNumber`, `GoodsReceipt.ReceiptNumber`, `SupplierReturn.ReturnNumber`, `Customer.Code`, `Supplier.Code`) is generated via ad-hoc `CountAsync() + 1`, which is not atomic and can produce duplicates under concurrent creates.

A second gap exists in the frontend: `CustomerFormDialog.vue` calls `getNextCustomerCode()` when the dialog opens and treats the returned string as if it were the customer's final code. In future this becomes misleading because `ISequenceGenerator.NextAsync()` cannot reveal a "preview" without consuming the counter. The form must instead display a non-consuming preview that is clearly marked as provisional, with the final value assigned server-side at save time.

**Scope:**
- [x] Backend API changes
- [x] Database schema changes (new number columns on 3 entities)
- [x] Frontend changes
- [x] Configuration changes (3 new sequence key registrations)

---

## 2. Behavior (RFC 2119)

### Server-side number assignment
- **B1.** Every service that creates an entity with a number/code field MUST obtain that value from `ISequenceGenerator.NextAsync(<key>, ct)` (or `NextBatchNumberAsync` for batches) inside the same `SaveChangesAsync` transaction as the entity insert.
- **B2.** Services MUST NOT compute numbers via `CountAsync()`, `Max()`, `OrderByDescending().FirstOrDefault()`, or any non-atomic query.
- **B3.** If the `CreateXxxRequest` exposes the number/code field (e.g., `CreateCustomerRequest.Code`), the service MUST ignore any client-provided value on Create and always assign the generated value. Manual override MAY be allowed only behind an explicit feature flag (not in scope here).
- **B4.** On `SaveChangesAsync` failure, the sequence row remains incremented. This is intentional (counters are allowed to have gaps) and MUST NOT be "compensated" by decrementing.

### Non-consuming preview
- **B5.** `ISequenceGenerator` MUST expose a `PeekNextAsync(string sequenceKey, CancellationToken ct)` method that returns the formatted string that *would* be produced by the next `NextAsync` call, **without** incrementing the counter.
- **B6.** `PeekNextAsync` MUST produce a format-correct string even when the counter row does not yet exist (by treating the implicit `CurrentValue` as `0` and returning `padded(1)`).
- **B7.** The peek value MUST be treated by clients as provisional — the server MAY assign a different value on save if another request consumes the counter in the meantime.

### Peek endpoint
- **B8.** Each service that owns a sequence-keyed entity MUST expose `GET /api/v1/{resource}/next-number` returning `{ "preview": "<string>" }`. Authorization MUST match the Create endpoint for the same resource.
- **B9.** The peek endpoint MUST NOT be cached by the HTTP pipeline (`Cache-Control: no-store`).

### Frontend preview
- **F1.** Every create-form that owns a numbered entity SHOULD display a read-only field populated by the peek endpoint on dialog open.
- **F2.** The preview field MUST be visually distinct from saved fields: italicised value, muted color, `mdi-auto-fix` prefix icon, and a tooltip reading "Preview — final number is assigned when saved".
- **F3.** Edit-mode MUST display the entity's real number as a regular read-only field, not a preview.
- **F4.** The preview value MUST NOT be sent back to the server in the Create payload.

---

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | `sequenceKey` param to peek endpoint | Must be a registered key | `UNKNOWN_SEQUENCE_KEY` |
| V2 | `CreateCustomerRequest.Code` | Ignored on Create (server assigns) | — |
| V3 | `CreateProductRequest.Code` | Ignored on Create (server assigns) | — |
| V4 | Any `Create*Request` carrying a number field | Number field removed from contract where present | — |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Peek endpoint called with unknown key | 400 | `UNKNOWN_SEQUENCE_KEY` | "Sequence key '{key}' is not registered." |
| E2 | `NextAsync` fails inside Create txn | 503 | `SEQUENCE_UNAVAILABLE` | "Unable to allocate a sequential number. Please retry." |
| E3 | Peek endpoint called without required permission | 403 | `FORBIDDEN` | (standard) |

---

## 5. Versioning Notes

**API version impact:** Non-breaking additive (new `GET /next-number` endpoints; removal of client-provided `code` fields on Create is accepted by server for backwards compat but ignored).

**Database migration required:** Yes — three new number columns (see §8.2).

**Backwards compatibility:** Fully compatible for existing data. Already-assigned numbers remain unchanged. Counter rows in `infrastructure.Sequences` are seeded lazily on first `NextAsync`.

---

## 6. Test Plan

### Unit Tests
- [ ] `[Unit] SequenceGeneratorTests.PeekNextAsync_UnknownKey_Throws`
- [ ] `[Unit] SequenceGeneratorTests.PeekNextAsync_DoesNotIncrementCounter`
- [ ] `[Unit] SequenceGeneratorTests.PeekNextAsync_RowMissing_ReturnsFormatWithCounterOne`
- [ ] `[Unit] SalesOrderServiceTests.CreateAsync_UsesSequenceGenerator`
- [ ] `[Unit] PickListServiceTests.CreateAsync_UsesSequenceGenerator`
- [ ] `[Unit] PackingServiceTests.CreateParcelAsync_UsesSequenceGenerator`
- [ ] `[Unit] ShipmentServiceTests.CreateAsync_UsesSequenceGenerator`
- [ ] `[Unit] CustomerReturnServiceTests.CreateAsync_UsesSequenceGenerator`
- [ ] `[Unit] PurchaseOrderServiceTests.CreateAsync_UsesSequenceGenerator`
- [ ] `[Unit] GoodsReceiptServiceTests.CreateAsync_UsesSequenceGenerator`
- [ ] `[Unit] SupplierReturnServiceTests.CreateAsync_UsesSequenceGenerator`
- [ ] `[Unit] CustomerServiceTests.CreateAsync_IgnoresClientCode_UsesSequenceGenerator`
- [ ] `[Unit] SupplierServiceTests.CreateAsync_UsesSequenceGenerator`
- [ ] `[Unit] ProductServiceTests.CreateAsync_UsesSequenceGenerator`
- [ ] `[Unit] WarehouseTransferServiceTests.CreateAsync_UsesSequenceGenerator`
- [ ] `[Unit] InventoryAdjustmentServiceTests.CreateAsync_UsesSequenceGenerator`
- [ ] `[Unit] StocktakeSessionServiceTests.CreateAsync_UsesSequenceGenerator`

### Integration Tests
- [ ] `[Integration] SequencePeekEndpoints_ReturnFormatCorrectPreview` (one case per resource)
- [ ] `[Integration] ConcurrentCreate_100Parallel_ProducesNoDuplicateNumbers` (SalesOrder, PickList, PurchaseOrder)
- [ ] `[Integration] PeekDoesNotConsumeCounter_FollowUpCreateUsesSameValue`

---

## 7. Detailed Design

### 7.1 New sequence keys to register

Add to `SequenceDefinitions.GetBuiltInDefinitions()`:

| Key | Prefix | Format | Reset | Padding | Consumer |
|---|---|---|---|---|---|
| `WT` | `WT-` | `WT-YYYYMMDD-NNNN` | Daily | 4 | WarehouseTransfer |
| `IA` | `IA-` | `IA-YYYYMMDD-NNNN` | Daily | 4 | InventoryAdjustment |
| `ST` | `ST-` | `ST-YYYYMMDD-NNNN` | Daily | 4 | StocktakeSession |

Existing key `PROD` will also be used for `Product.Sku` (see §7.4).

### 7.2 `ISequenceGenerator` additions

```csharp
// src/Warehouse.Infrastructure/Sequences/ISequenceGenerator.cs
Task<string> PeekNextAsync(string sequenceKey, CancellationToken cancellationToken);
```

Implementation (in `SequenceGenerator`):

```csharp
public async Task<string> PeekNextAsync(string sequenceKey, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(sequenceKey))
    {
        throw new ArgumentException("Sequence key must not be null or empty.", nameof(sequenceKey));
    }

    if (!_definitions.TryGetValue(sequenceKey, out SequenceDefinition? definition))
    {
        throw new ArgumentException($"Unknown sequence key: '{sequenceKey}'.", nameof(sequenceKey));
    }

    DateTime utcNow = DateTime.UtcNow;
    string compositeKey = BuildCompositeKey(definition, utcNow);
    int currentValue = await ReadCurrentValueAsync(compositeKey, cancellationToken)
        .ConfigureAwait(false);

    return FormatOutput(definition, utcNow, currentValue + 1);
}

private async Task<int> ReadCurrentValueAsync(string compositeKey, CancellationToken ct)
{
    const string sql =
        "SELECT CurrentValue FROM [infrastructure].[Sequences] WHERE CompositeKey = @CompositeKey;";

    System.Data.Common.DbConnection connection = _context.Database.GetDbConnection();
    bool opened = false;
    if (connection.State != ConnectionState.Open)
    {
        await connection.OpenAsync(ct).ConfigureAwait(false);
        opened = true;
    }
    try
    {
        await using System.Data.Common.DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
        cmd.Parameters.Add(new SqlParameter("@CompositeKey", SqlDbType.NVarChar, 200) { Value = compositeKey });
        object? result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }
    finally
    {
        if (opened)
        {
            await connection.CloseAsync().ConfigureAwait(false);
        }
    }
}
```

### 7.3 Unified peek controller

Place once, shared across services, in `Warehouse.Infrastructure`:

```csharp
// src/Warehouse.Infrastructure/Sequences/SequencePeekController.cs
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sequences")]
public sealed class SequencePeekController : ControllerBase
{
    private readonly ISequenceGenerator _generator;

    public SequencePeekController(ISequenceGenerator generator) => _generator = generator;

    /// <summary>Returns the formatted string the next NextAsync call would produce, without consuming it.</summary>
    [HttpGet("{key}/peek")]
    [Authorize]
    [ProducesResponseType(typeof(SequencePeekResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ResponseCache(NoStore = true)]
    public async Task<ActionResult<SequencePeekResponse>> Peek(
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            string preview = await _generator.PeekNextAsync(key, cancellationToken).ConfigureAwait(false);
            return Ok(new SequencePeekResponse { Preview = preview });
        }
        catch (ArgumentException)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "UNKNOWN_SEQUENCE_KEY",
                detail: $"Sequence key '{key}' is not registered.");
        }
    }
}

public sealed record SequencePeekResponse
{
    public required string Preview { get; init; }
}
```

> **Note:** Each service that creates numbered entities already calls `AddSequenceGenerator<TContext>()`. The controller is picked up automatically by `AddControllers()` because `Warehouse.Infrastructure` assembly is scanned. No additional registration needed.

### 7.4 Candidate matrix — what to change, where

| # | Entity | Seq Key | Service (remove ad-hoc gen) | Model (field exists?) | Create DTO (drop field?) | Frontend form |
|---|---|---|---|---|---|---|
| 1 | SalesOrder | `SO` | `SalesOrderService.cs:591` | ✔ `OrderNumber` | n/a (not in request) | `SalesOrderFormDialog.vue` |
| 2 | PickList | `PL` | `PickListService.cs:212` | ✔ `PickListNumber` | n/a | (generated after SO; no form) |
| 3 | Parcel | `PKG` | `PackingService.cs:175` | ✔ `ParcelNumber` | n/a | `ParcelFormDialog.vue` |
| 4 | Shipment | `SHP` | `ShipmentService.cs:202` | ✔ `ShipmentNumber` | n/a | `ShipmentFormDialog.vue` |
| 5 | CustomerReturn | `CR` | `CustomerReturnService.cs:177` | ✔ `ReturnNumber` | n/a | `CustomerReturnFormDialog.vue` |
| 6 | PurchaseOrder | `PO` | `PurchaseOrderService.cs:248` | ✔ `OrderNumber` | n/a | `PurchaseOrderFormDialog.vue` |
| 7 | GoodsReceipt | `GR` | `GoodsReceiptService.cs:218` | ✔ `ReceiptNumber` | n/a | `GoodsReceiptFormDialog.vue` |
| 8 | SupplierReturn | `SR` | `SupplierReturnService.cs:159` | ✔ `ReturnNumber` | n/a | `SupplierReturnFormDialog.vue` |
| 9 | Customer | `CUST` | `CustomerService.cs:308` | ✔ `Code` | **drop** `CreateCustomerRequest.Code` | `CustomerFormDialog.vue` (replace `getNextCustomerCode`) |
| 10 | Supplier | `SUPP` | `SupplierService.cs:276` | ✔ `Code` | **drop** `CreateSupplierRequest.Code` | `SupplierFormDialog.vue` |
| 11 | Product | `PROD` | `ProductService.CreateAsync` | ✔ `Code` | **drop** `CreateProductRequest.Code` | `ProductFormDialog.vue` |
| 12 | Product | `PROD` | `ProductService.CreateAsync` | ✔ `Sku` | **drop** `CreateProductRequest.Sku` | `ProductFormDialog.vue` |
| 13 | WarehouseTransfer | `WT` (new) | `WarehouseTransferService.CreateAsync` | **add** `TransferNumber NVARCHAR(50) NOT NULL` | — | `TransferFormDialog.vue` |
| 14 | InventoryAdjustment | `IA` (new) | `InventoryAdjustmentService.CreateAsync` | **add** `AdjustmentNumber NVARCHAR(50) NOT NULL` | — | `AdjustmentFormDialog.vue` |
| 15 | StocktakeSession | `ST` (new) | `StocktakeSessionService.CreateAsync` | **add** `SessionNumber NVARCHAR(50) NOT NULL` (keep `Name` as free-text label) | — | `StocktakeFormDialog.vue` |

> Row 12 assumption: `Product.Sku` reuses the `PROD` sequence. If marketing/finance needs a separate pattern, register a dedicated `SKU` key first.

---

## 8. Implementation Guide

### 8.1 Backend — migrating a service from ad-hoc `CountAsync()` to `ISequenceGenerator`

Example: **SalesOrderService**.

#### Step 1. Inject the generator

`src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/SalesOrderService.cs` — add to the constructor parameter list and assign to a field. The DI registration already exists in `Program.cs` via `services.AddSequenceGenerator<FulfillmentDbContext>()`.

```csharp
private readonly ISequenceGenerator _sequenceGenerator;

public SalesOrderService(
    FulfillmentDbContext context,
    IMapper mapper,
    IPublishEndpoint publishEndpoint,
    ISequenceGenerator sequenceGenerator,          // <-- add
    ILogger<SalesOrderService> logger)
    : base(context, logger)
{
    _mapper = mapper;
    _publishEndpoint = publishEndpoint;
    _sequenceGenerator = sequenceGenerator;         // <-- add
}
```

#### Step 2. Replace the generator call site

```csharp
// BEFORE (inside CreateAsync, around SalesOrderService.cs:57)
string orderNumber = await GenerateOrderNumberAsync(cancellationToken).ConfigureAwait(false);

// AFTER
string orderNumber = await _sequenceGenerator
    .NextAsync("SO", cancellationToken)
    .ConfigureAwait(false);
```

#### Step 3. Delete the obsolete private method

Remove `GenerateOrderNumberAsync` at `SalesOrderService.cs:591–596`. One class per file still holds.

#### Step 4. Register the peek controller target (only if the service does not already reference `Warehouse.Infrastructure`)

All four API projects (Auth, Customers, Inventory, Fulfillment, Purchasing) already reference `Warehouse.Infrastructure`. The `SequencePeekController` will be discovered automatically by `AddControllers()`. Verify with a quick smoke call `GET /api/v1/sequences/SO/peek`.

#### Step 5. Ensure the service method is transactional

`NextAsync` opens its own serializable transaction if none is ambient. When the service wraps the create in `Database.BeginTransactionAsync`, `NextAsync` joins it. Either pattern is correct — do **not** call `NextAsync` after `SaveChangesAsync` has already been committed, because a failure downstream would then waste a real sequence value without creating the entity (gap is tolerable but avoid it when trivially preventable).

#### Step 6. Update unit tests

Unit tests that previously asserted a `CountAsync`-shaped number (`SO-20260423-0001`) need to mock `ISequenceGenerator`:

```csharp
Mock<ISequenceGenerator> seqMock = new();
seqMock.Setup(s => s.NextAsync("SO", It.IsAny<CancellationToken>()))
       .ReturnsAsync("SO-20260423-0001");
```

Repeat steps 1–6 for every row in §7.4. For rows 13–15 (new number column), add an EF migration first (§8.2).

### 8.2 Backend — adding a number column to an existing entity

Example: **WarehouseTransfer** (gets a new `TransferNumber`).

#### Step 1. Edit the entity

`src/Databases/Warehouse.Inventory.DBModel/Models/WarehouseTransfer.cs` — add:

```csharp
/// <summary>Server-assigned transfer number (e.g., WT-20260423-0001).</summary>
public required string TransferNumber { get; set; }
```

#### Step 2. Configure the column

`src/Databases/Warehouse.Inventory.DBModel/Configurations/WarehouseTransferConfiguration.cs`:

```csharp
builder.Property(t => t.TransferNumber).HasMaxLength(50).IsRequired();
builder.HasIndex(t => t.TransferNumber).IsUnique();
```

#### Step 3. Generate a migration

From `src/Databases/Warehouse.Inventory.DBModel/`:

```bash
dotnet ef migrations add AddTransferNumber --startup-project ../../Interfaces/Inventory/Warehouse.Inventory.API
```

Inspect the generated file under `Migrations/`. Ensure the `Up` method:
- Adds the column as NULL first.
- Runs a data patch that back-fills existing rows with `WT-LEGACY-{Id:D6}`.
- Alters the column to `NOT NULL`.
- Creates the unique index last.

#### Step 4. Run the migration inside a review PR

Do not auto-apply against shared environments.

#### Step 5. Wire the service

Same pattern as §8.1 using the new `WT` sequence key.

#### Step 6. Add DTO + request + mapping

Expose `TransferNumber` on `WarehouseTransferDto` and `WarehouseTransferDetailDto`. Do **not** add it to `CreateWarehouseTransferRequest` — server assigns it. Update `FulfillmentMappingProfile` / `InventoryMappingProfile` accordingly.

### 8.3 Backend — removing a manual code field from a Create request

Example: **CreateCustomerRequest**.

#### Step 1. Drop the property

`src/Warehouse.ServiceModel/Requests/Customers/CreateCustomerRequest.cs` — delete `Code`.

#### Step 2. Drop the validator rule

`src/Interfaces/Customers/Warehouse.Customers.API/Validators/CreateCustomerRequestValidator.cs` — remove any `RuleFor(x => x.Code)` lines.

#### Step 3. Drop it from the service signature if passed through

In `CustomerService.CreateAsync`, remove the nullable `request.Code` coalesce. The assigned code is always `await _sequenceGenerator.NextAsync("CUST", ct)`.

#### Step 4. Remove the public "next code" endpoint

`GET /api/v1/customers/next-code` is now superseded by `GET /api/v1/sequences/CUST/peek`. Delete the controller action and its API contract. Apply the same deletion for any other `next-code`/`next-number` per-resource endpoints that exist today.

#### Step 5. Integration-test the Create path

Asserting the resulting customer's `Code` starts with `CUST-` and matches the expected padding.

### 8.4 Frontend — migrating a form to the shared preview pattern

The pattern is the same for every form. Apply it in these files:

```
frontend/src/features/customers/components/organisms/CustomerFormDialog.vue
frontend/src/features/fulfillment/components/organisms/SalesOrderFormDialog.vue
frontend/src/features/fulfillment/components/organisms/ParcelFormDialog.vue
frontend/src/features/fulfillment/components/organisms/ShipmentFormDialog.vue
frontend/src/features/fulfillment/components/organisms/CustomerReturnFormDialog.vue
frontend/src/features/purchasing/components/organisms/PurchaseOrderFormDialog.vue
frontend/src/features/purchasing/components/organisms/GoodsReceiptFormDialog.vue
frontend/src/features/purchasing/components/organisms/SupplierFormDialog.vue
frontend/src/features/purchasing/components/organisms/SupplierReturnFormDialog.vue
frontend/src/features/inventory/components/organisms/ProductFormDialog.vue
frontend/src/features/inventory/components/organisms/TransferFormDialog.vue
frontend/src/features/inventory/components/organisms/AdjustmentFormDialog.vue
frontend/src/features/inventory/components/organisms/StocktakeFormDialog.vue
```

#### Step 1. Add a shared API helper

`frontend/src/shared/api/sequences.ts` (new file):

```ts
import apiClient from '@shared/api/client';

export type SequenceKey =
  | 'SO' | 'PL' | 'PKG' | 'SHP' | 'CR'
  | 'PO' | 'GR' | 'SR'
  | 'CUST' | 'SUPP' | 'PROD'
  | 'WT' | 'IA' | 'ST';

export interface SequencePeekResponse {
  preview: string;
}

export function peekNextSequence(key: SequenceKey): Promise<string> {
  return apiClient
    .get<SequencePeekResponse>(`/sequences/${key}/peek`)
    .then((r) => r.data.preview);
}
```

#### Step 2. Add a shared composable

`frontend/src/shared/composables/useNumberPreview.ts` (new file):

```ts
import { ref, watch } from 'vue';
import { peekNextSequence, type SequenceKey } from '@shared/api/sequences';

export function useNumberPreview(key: SequenceKey, visible: { value: boolean }, isEdit: { value: boolean }) {
  const preview = ref<string>('');
  const loading = ref<boolean>(false);

  async function refresh(): Promise<void> {
    if (!visible.value || isEdit.value) return;
    loading.value = true;
    try {
      preview.value = await peekNextSequence(key);
    } catch {
      preview.value = '';
    } finally {
      loading.value = false;
    }
  }

  watch([visible, isEdit], refresh, { immediate: true });

  return { preview, loading, refresh };
}
```

#### Step 3. Add a shared preview atom

`frontend/src/shared/components/atoms/NumberPreviewField.vue` (new file):

```vue
<template>
  <v-text-field
    :model-value="isEdit ? realNumber : preview"
    :label="label"
    prepend-inner-icon="mdi-auto-fix"
    :density="density"
    readonly
    :loading="loading"
    :class="isEdit ? '' : 'number-preview'"
    :hint="isEdit ? undefined : t('common.numberPreviewHint')"
    persistent-hint
  />
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n';

defineProps<{
  label: string;
  preview: string;
  realNumber?: string;
  isEdit: boolean;
  loading: boolean;
  density?: 'default' | 'comfortable' | 'compact';
}>();

const { t } = useI18n();
</script>

<style scoped>
.number-preview :deep(input) {
  font-style: italic;
  color: rgb(var(--v-theme-secondary));
}
</style>
```

Add i18n entries:

```ts
// frontend/src/shared/i18n/locales/en.ts
common: {
  // ...
  numberPreviewHint: 'Preview — final number is assigned when saved',
}

// frontend/src/shared/i18n/locales/bg.ts
common: {
  // ...
  numberPreviewHint: 'Предварителен преглед — окончателният номер се присвоява при запис',
}
```

#### Step 4. Use it in a form

Example for `SalesOrderFormDialog.vue` — add a visible preview field at the top of the grid:

```vue
<template>
  <!-- existing FormWrapper scaffold -->
  <v-row dense>
    <v-col v-bind="grid.fieldCols">
      <NumberPreviewField
        :label="t('fulfillment.salesOrders.form.orderNumber')"
        :preview="numberPreview"
        :real-number="form.orderNumber"
        :is-edit="isEdit"
        :loading="numberLoading"
        :density="layout.vuetifyDensity"
      />
    </v-col>
    <!-- ...rest of the form unchanged -->
  </v-row>
</template>

<script setup lang="ts">
import NumberPreviewField from '@shared/components/atoms/NumberPreviewField.vue';
import { useNumberPreview } from '@shared/composables/useNumberPreview';

// ...existing imports

const { preview: numberPreview, loading: numberLoading } = useNumberPreview('SO', visible, isEdit);
</script>
```

#### Step 5. Remove the old "next-code" integration

For `CustomerFormDialog.vue` specifically:
- Delete the import of `getNextCustomerCode`.
- Delete the `codeLoading` ref and the code-population block inside `populateForm`.
- Remove `form.code` from the Create payload (it is no longer in `CreateCustomerRequest`).
- Replace the current readonly `<v-text-field v-model="form.code">` with `<NumberPreviewField :preview="numberPreview" :real-number="form.code" :is-edit="isEdit" ...>`.

#### Step 6. Edit mode

`NumberPreviewField` binds to `realNumber` when `isEdit` is true, so no special casing in the form. The saved entity's number is loaded in `populateForm` exactly as today.

#### Step 7. Don't send preview on save

The shared atom exposes nothing bindable to the form payload. There is no code path that can accidentally submit the preview.

### 8.5 Checklist per entity

Use this checklist when migrating each row from §7.4:

- [ ] Backend: service constructor accepts `ISequenceGenerator`
- [ ] Backend: `NextAsync("<key>", ct)` replaces ad-hoc counter
- [ ] Backend: obsolete `Generate*Async` private method deleted
- [ ] Backend: `Create*Request` no longer carries the number field
- [ ] Backend: FluentValidation rule for the dropped field removed
- [ ] Backend: controller action for any per-resource `next-code` / `next-number` deleted
- [ ] Backend: unit tests re-mock `ISequenceGenerator`
- [ ] Frontend: old per-resource `getNext*` API import removed
- [ ] Frontend: `NumberPreviewField` added in create dialog
- [ ] Frontend: `useNumberPreview('<key>', visible, isEdit)` wired
- [ ] Frontend: Create payload does not include the number/code
- [ ] Frontend: edit dialog shows real number from loaded entity
- [ ] i18n: form label + preview hint present in `en.ts` and `bg.ts`

---

## 9. Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-INFRA-003-sequence-generation | Add §PeekNextAsync method and `GET /sequences/{key}/peek` endpoint |
| SDD-CUST-001-customers-and-accounts | Drop client-supplied `Code`; point number assignment to `SequenceGenerator` |
| SDD-INV-001-products-and-catalog | Drop client-supplied `Code` and `Sku`; server assigns via `PROD` |
| SDD-INV-002-stock-management | Add `InventoryAdjustment.AdjustmentNumber` |
| SDD-INV-003-warehouse-structure | Add `WarehouseTransfer.TransferNumber` |
| SDD-INV-004-stocktaking | Add `StocktakeSession.SessionNumber` (separate from free-text `Name`) |
| SDD-FULF-001 (SO), -002 (PL), -003 (Parcel/Shipment), -004 (Return) | Number assignment now via `ISequenceGenerator` |
| SDD-PURCH-001 (PO), -002 (GR), -003 (SR), -004 (Supplier) | Number assignment now via `ISequenceGenerator` |

---

## 10. Migration Plan

1. **Pre-deployment**
   - Merge PR adding `PeekNextAsync`, `SequencePeekController`, and new `WT`/`IA`/`ST` keys. No behavior change in consumer services yet. Verify `/api/v1/sequences/<KEY>/peek` responds on every API.
2. **Deployment — backend migration wave**
   - Migrate services in this order to minimise coupling pain: Customer, Supplier, Product → PurchaseOrder/GoodsReceipt/SupplierReturn → SalesOrder/PickList/Parcel/Shipment/CustomerReturn → WarehouseTransfer/InventoryAdjustment/StocktakeSession (includes EF migration).
   - One PR per service keeps the blast radius small.
3. **Deployment — frontend wave**
   - Ship `NumberPreviewField`, `useNumberPreview`, `peekNextSequence` helpers first. Each form migration is then a <20-line diff per file.
4. **Post-deployment**
   - Remove legacy `GET /api/v1/customers/next-code` and any sibling endpoints after one release cycle.
   - Grafana alert on `infrastructure.Sequences` row growth to catch accidental key proliferation.
5. **Rollback**
   - Service-level: revert the service PR — the old `Generate*Async` helper is in git history. Sequence table rows do no harm if left behind.
   - Schema-level (rows 13–15): rolling back requires a follow-up migration dropping the new columns; prefer forward-fixing.

---

## 11. Open Questions

- [ ] Should `Product.Sku` use a dedicated `SKU` sequence key instead of reusing `PROD`? (Default: reuse.)
- [ ] Is there an appetite to also expose a bulk peek endpoint for list pre-fetching (e.g., "next 10 preview values")? (Default: no — preview is per-form.)
- [ ] Should the peek endpoint require the same permission as Create for the target resource, or a weaker "read" permission? (Default: same as Create.)
- [ ] Any existing rows in `Customer.Code` / `Supplier.Code` that collide with the new format? Audit before removing the manual field.
