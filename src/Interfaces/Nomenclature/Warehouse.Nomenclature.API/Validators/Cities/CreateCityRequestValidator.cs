using FluentValidation;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Validators;

/// <summary>
/// Validates the create city request payload per SDD-NOM-001.
/// </summary>
public sealed class CreateCityRequestValidator : AbstractValidator<CreateCityRequest>
{
    /// <summary>
    /// Initializes validation rules for city creation.
    /// </summary>
    public CreateCityRequestValidator()
    {
        RuleFor(x => x.StateProvinceId)
            .GreaterThan(0).WithErrorCode("INVALID_STATE_PROVINCE_ID").WithMessage("State/province ID must be a positive integer.");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_NAME").WithMessage("City name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_NAME").WithMessage("City name must not exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithErrorCode("INVALID_POSTAL_CODE").WithMessage("Postal code must not exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.PostalCode));
    }
}
