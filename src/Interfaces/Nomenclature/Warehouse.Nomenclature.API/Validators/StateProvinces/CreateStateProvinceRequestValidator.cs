using FluentValidation;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Validators;

/// <summary>
/// Validates the create state/province request payload per SDD-NOM-001.
/// </summary>
public sealed class CreateStateProvinceRequestValidator : AbstractValidator<CreateStateProvinceRequest>
{
    /// <summary>
    /// Initializes validation rules for state/province creation.
    /// </summary>
    public CreateStateProvinceRequestValidator()
    {
        RuleFor(x => x.CountryId)
            .GreaterThan(0).WithErrorCode("INVALID_COUNTRY_ID").WithMessage("Country ID must be a positive integer.");

        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("INVALID_CODE").WithMessage("State/province code is required.")
            .MaximumLength(10).WithErrorCode("INVALID_CODE").WithMessage("State/province code must not exceed 10 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_NAME").WithMessage("State/province name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_NAME").WithMessage("State/province name must not exceed 100 characters.");
    }
}
