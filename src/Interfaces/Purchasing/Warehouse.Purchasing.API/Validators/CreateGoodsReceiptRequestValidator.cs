using FluentValidation;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Validators;

/// <summary>
/// Validates the create goods receipt request payload per SDD-PURCH-001 section 3.8-3.9.
/// </summary>
public sealed class CreateGoodsReceiptRequestValidator : AbstractValidator<CreateGoodsReceiptRequest>
{
    /// <summary>
    /// Initializes validation rules for goods receipt creation.
    /// </summary>
    public CreateGoodsReceiptRequestValidator()
    {
        RuleFor(x => x.PurchaseOrderId)
            .GreaterThan(0).WithErrorCode("INVALID_PO").WithMessage("Purchase order ID is required.");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithErrorCode("INVALID_WAREHOUSE").WithMessage("Warehouse ID is required.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("RECEIPT_MUST_HAVE_LINES").WithMessage("Goods receipt must have at least one line.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.PurchaseOrderLineId)
                .GreaterThan(0).WithErrorCode("INVALID_PO_LINE").WithMessage("Purchase order line ID is required.");

            line.RuleFor(l => l.ReceivedQuantity)
                .GreaterThan(0).WithErrorCode("INVALID_QUANTITY").WithMessage("Received quantity must be greater than 0.");

            line.RuleFor(l => l.BatchNumber)
                .MaximumLength(50).WithErrorCode("INVALID_BATCH_NUMBER").WithMessage("Batch number must not exceed 50 characters.")
                .When(l => !string.IsNullOrEmpty(l.BatchNumber));

            line.RuleFor(l => l.ManufacturingDate)
                .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow.Date))
                .WithErrorCode("INVALID_MANUFACTURING_DATE").WithMessage("Manufacturing date must not be in the future.")
                .When(l => l.ManufacturingDate.HasValue);

            line.RuleFor(l => l.ExpiryDate)
                .GreaterThan(l => l.ManufacturingDate!.Value)
                .WithErrorCode("INVALID_EXPIRY_DATE").WithMessage("Expiry date must be after manufacturing date.")
                .When(l => l.ExpiryDate.HasValue && l.ManufacturingDate.HasValue);
        });
    }
}
