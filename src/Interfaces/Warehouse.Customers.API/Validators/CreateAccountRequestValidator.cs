using FluentValidation;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Validators;

/// <summary>
/// Validates the create account request payload per SDD-CUST-001 section 2.2.1.
/// </summary>
public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    /// <summary>
    /// Initializes validation rules for account creation.
    /// </summary>
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithErrorCode("INVALID_CURRENCY_CODE").WithMessage("Currency code is required.")
            .Length(3).WithErrorCode("INVALID_CURRENCY_CODE").WithMessage("Currency code must be exactly 3 characters.")
            .Matches("^[A-Z]{3}$").WithErrorCode("INVALID_CURRENCY_CODE").WithMessage("Currency code must be 3 uppercase letters (ISO 4217).");
    }
}
