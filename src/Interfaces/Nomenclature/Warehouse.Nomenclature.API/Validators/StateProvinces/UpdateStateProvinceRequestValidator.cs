using FluentValidation;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Validators;

/// <summary>
/// Validates the update state/province request payload per SDD-NOM-001.
/// </summary>
public sealed class UpdateStateProvinceRequestValidator : AbstractValidator<UpdateStateProvinceRequest>
{
    /// <summary>
    /// Initializes validation rules for state/province update.
    /// </summary>
    public UpdateStateProvinceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_NAME").WithMessage("State/province name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_NAME").WithMessage("State/province name must not exceed 100 characters.");
    }
}
