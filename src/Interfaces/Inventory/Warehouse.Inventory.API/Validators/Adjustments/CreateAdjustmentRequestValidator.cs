using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create adjustment request payload.
/// </summary>
public sealed class CreateAdjustmentRequestValidator : AbstractValidator<CreateAdjustmentRequest>
{
    /// <summary>
    /// Initializes validation rules for adjustment creation.
    /// </summary>
    public CreateAdjustmentRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithErrorCode("INVALID_WAREHOUSE_ID").WithMessage("Warehouse ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithErrorCode("INVALID_REASON").WithMessage("Adjustment reason is required.")
            .MaximumLength(200).WithErrorCode("INVALID_REASON").WithMessage("Reason must not exceed 200 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("INVALID_ADJUSTMENT_LINES").WithMessage("At least one adjustment line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId)
                .GreaterThan(0).WithErrorCode("INVALID_PRODUCT_ID").WithMessage("Product ID is required.");

            line.RuleFor(l => l.ActualQuantity)
                .GreaterThanOrEqualTo(0).WithErrorCode("INVALID_ACTUAL_QUANTITY").WithMessage("Actual quantity must be zero or greater.");
        });
    }
}
