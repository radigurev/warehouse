using FluentValidation;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Validators;

/// <summary>
/// Validates the create country request payload per SDD-NOM-001.
/// </summary>
public sealed class CreateCountryRequestValidator : AbstractValidator<CreateCountryRequest>
{
    /// <summary>
    /// Initializes validation rules for country creation.
    /// </summary>
    public CreateCountryRequestValidator()
    {
        RuleFor(x => x.Iso2Code)
            .NotEmpty().WithErrorCode("INVALID_ISO2_CODE").WithMessage("ISO 3166-1 alpha-2 code is required.")
            .Matches(@"^[A-Z]{2}$").WithErrorCode("INVALID_ISO2_CODE").WithMessage("ISO 3166-1 alpha-2 code must be exactly 2 uppercase letters.");

        RuleFor(x => x.Iso3Code)
            .NotEmpty().WithErrorCode("INVALID_ISO3_CODE").WithMessage("ISO 3166-1 alpha-3 code is required.")
            .Matches(@"^[A-Z]{3}$").WithErrorCode("INVALID_ISO3_CODE").WithMessage("ISO 3166-1 alpha-3 code must be exactly 3 uppercase letters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_NAME").WithMessage("Country name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_NAME").WithMessage("Country name must not exceed 100 characters.");

        RuleFor(x => x.PhonePrefix)
            .MaximumLength(10).WithErrorCode("INVALID_PHONE_PREFIX").WithMessage("Phone prefix must not exceed 10 characters.")
            .When(x => !string.IsNullOrEmpty(x.PhonePrefix));
    }
}
