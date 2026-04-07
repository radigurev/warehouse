using FluentValidation;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Validators;

/// <summary>
/// Validates the confirm pick request payload per SDD-FULF-001 section 3.4.
/// </summary>
public sealed class ConfirmPickRequestValidator : AbstractValidator<ConfirmPickRequest>
{
    /// <summary>
    /// Initializes validation rules for pick confirmation.
    /// </summary>
    public ConfirmPickRequestValidator()
    {
        RuleFor(x => x.ActualQuantity).GreaterThan(0).WithErrorCode("INVALID_QUANTITY").WithMessage("Actual quantity must be greater than 0.");
    }
}
