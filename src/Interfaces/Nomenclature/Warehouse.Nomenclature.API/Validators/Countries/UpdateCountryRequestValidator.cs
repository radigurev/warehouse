using FluentValidation;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Validators;

/// <summary>
/// Validates the update country request payload per SDD-NOM-001.
/// </summary>
public sealed class UpdateCountryRequestValidator : AbstractValidator<UpdateCountryRequest>
{
    /// <summary>
    /// Initializes validation rules for country update.
    /// </summary>
    public UpdateCountryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_NAME").WithMessage("Country name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_NAME").WithMessage("Country name must not exceed 100 characters.");

        RuleFor(x => x.PhonePrefix)
            .MaximumLength(10).WithErrorCode("INVALID_PHONE_PREFIX").WithMessage("Phone prefix must not exceed 10 characters.")
            .When(x => !string.IsNullOrEmpty(x.PhonePrefix));
    }
}
