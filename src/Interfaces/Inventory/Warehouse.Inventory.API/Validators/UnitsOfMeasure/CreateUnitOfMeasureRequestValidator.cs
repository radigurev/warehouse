using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create unit of measure request payload per SDD-INV-001 section 3.
/// </summary>
public sealed class CreateUnitOfMeasureRequestValidator : AbstractValidator<CreateUnitOfMeasureRequest>
{
    /// <summary>
    /// Initializes validation rules for unit of measure creation.
    /// </summary>
    public CreateUnitOfMeasureRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("INVALID_UNIT_CODE").WithMessage("Unit code is required.")
            .MaximumLength(10).WithErrorCode("INVALID_UNIT_CODE").WithMessage("Unit code must not exceed 10 characters.")
            .Matches("^[A-Z0-9]+$").WithErrorCode("INVALID_UNIT_CODE").WithMessage("Unit code must contain only uppercase alphanumeric characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_UNIT_NAME").WithMessage("Unit name is required.")
            .MaximumLength(50).WithErrorCode("INVALID_UNIT_NAME").WithMessage("Unit name must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithErrorCode("INVALID_UNIT_DESCRIPTION").WithMessage("Description must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
