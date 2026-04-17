using FluentValidation;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Validators;

/// <summary>
/// Validates the create currency request payload per SDD-NOM-001.
/// </summary>
public sealed class CreateCurrencyRequestValidator : AbstractValidator<CreateCurrencyRequest>
{
    /// <summary>
    /// Initializes validation rules for currency creation.
    /// </summary>
    public CreateCurrencyRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("INVALID_CODE").WithMessage("Currency code is required.")
            .Matches(@"^[A-Z]{3}$").WithErrorCode("INVALID_CODE").WithMessage("Currency code must be exactly 3 uppercase letters (ISO 4217).");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_NAME").WithMessage("Currency name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_NAME").WithMessage("Currency name must not exceed 100 characters.");

        RuleFor(x => x.Symbol)
            .MaximumLength(5).WithErrorCode("INVALID_SYMBOL").WithMessage("Currency symbol must not exceed 5 characters.")
            .When(x => !string.IsNullOrEmpty(x.Symbol));
    }
}
