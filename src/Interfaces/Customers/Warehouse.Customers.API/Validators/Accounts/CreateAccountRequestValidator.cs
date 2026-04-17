using FluentValidation;
using Microsoft.FeatureManagement;
using Warehouse.Infrastructure.Caching;
using Warehouse.Infrastructure.Configuration;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Validators;

/// <summary>
/// Validates the create account request payload per SDD-CUST-001 section 2.2.1
/// and CHG-ENH-001 currency code validation against Nomenclature cache.
/// <para>See <see cref="INomenclatureResolver"/>, <see cref="IFeatureManager"/>.</para>
/// </summary>
public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    /// <summary>
    /// Initializes validation rules for account creation.
    /// </summary>
    public CreateAccountRequestValidator(
        INomenclatureResolver nomenclatureResolver,
        IFeatureManager featureManager)
    {
        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithErrorCode("INVALID_CURRENCY_CODE").WithMessage("Currency code is required.")
            .Length(3).WithErrorCode("INVALID_CURRENCY_CODE").WithMessage("Currency code must be exactly 3 characters.")
            .Matches("^[A-Z]{3}$").WithErrorCode("INVALID_CURRENCY_CODE").WithMessage("Currency code must be 3 uppercase letters (ISO 4217).")
            .MustAsync(async (code, cancellation) =>
            {
                if (!await featureManager.IsEnabledAsync(FeatureFlags.EnableNomenclatureValidation).ConfigureAwait(false))
                    return true;

                bool? valid = await nomenclatureResolver.ValidateCurrencyCodeAsync(code, cancellation).ConfigureAwait(false);
                return valid ?? true;
            })
            .WithErrorCode("INVALID_CURRENCY_CODE")
            .WithMessage(x => $"The currency code '{x.CurrencyCode}' is not recognized. Please select a valid currency.");
    }
}
