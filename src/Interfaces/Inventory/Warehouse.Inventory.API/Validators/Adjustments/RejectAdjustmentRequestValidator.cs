using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the reject adjustment request payload.
/// </summary>
public sealed class RejectAdjustmentRequestValidator : AbstractValidator<RejectAdjustmentRequest>
{
    /// <summary>
    /// Initializes validation rules for adjustment rejection.
    /// </summary>
    public RejectAdjustmentRequestValidator()
    {
        RuleFor(x => x.Notes)
            .NotEmpty().WithErrorCode("INVALID_REJECTION_REASON").WithMessage("Rejection reason is required.")
            .MaximumLength(500).WithErrorCode("INVALID_REJECTION_REASON").WithMessage("Rejection reason must not exceed 500 characters.");
    }
}
