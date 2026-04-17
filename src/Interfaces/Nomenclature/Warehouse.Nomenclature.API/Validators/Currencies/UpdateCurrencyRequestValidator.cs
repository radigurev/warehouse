using FluentValidation;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Validators;

/// <summary>
/// Validates the update currency request payload per SDD-NOM-001.
/// </summary>
public sealed class UpdateCurrencyRequestValidator : AbstractValidator<UpdateCurrencyRequest>
{
    /// <summary>
    /// Initializes validation rules for currency update.
    /// </summary>
    public UpdateCurrencyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_NAME").WithMessage("Currency name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_NAME").WithMessage("Currency name must not exceed 100 characters.");

        RuleFor(x => x.Symbol)
            .MaximumLength(5).WithErrorCode("INVALID_SYMBOL").WithMessage("Currency symbol must not exceed 5 characters.")
            .When(x => !string.IsNullOrEmpty(x.Symbol));
    }
}
