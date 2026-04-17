using FluentValidation;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Validators;

/// <summary>
/// Validates the update city request payload per SDD-NOM-001.
/// </summary>
public sealed class UpdateCityRequestValidator : AbstractValidator<UpdateCityRequest>
{
    /// <summary>
    /// Initializes validation rules for city update.
    /// </summary>
    public UpdateCityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_NAME").WithMessage("City name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_NAME").WithMessage("City name must not exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithErrorCode("INVALID_POSTAL_CODE").WithMessage("Postal code must not exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.PostalCode));
    }
}
