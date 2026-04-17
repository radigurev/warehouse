using FluentValidation;
using Microsoft.FeatureManagement;
using Warehouse.Infrastructure.Caching;
using Warehouse.Infrastructure.Configuration;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Validators;

/// <summary>
/// Validates the create supplier address request payload per SDD-PURCH-001 section 3.3
/// with optional Nomenclature country code validation per CHG-ENH-001.
/// </summary>
public sealed class CreateSupplierAddressRequestValidator : AbstractValidator<CreateSupplierAddressRequest>
{
    private static readonly string[] AllowedAddressTypes = ["Billing", "Shipping", "Both"];

    /// <summary>
    /// Initializes validation rules for supplier address creation.
    /// </summary>
    public CreateSupplierAddressRequestValidator(
        IFeatureManager featureManager,
        INomenclatureResolver nomenclatureResolver)
    {
        RuleFor(x => x.AddressType)
            .NotEmpty().WithErrorCode("INVALID_ADDRESS_TYPE").WithMessage("Address type is required.")
            .Must(type => AllowedAddressTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode("INVALID_ADDRESS_TYPE")
            .WithMessage("Address type must be one of: Billing, Shipping, Both.");

        RuleFor(x => x.StreetLine1)
            .NotEmpty().WithErrorCode("INVALID_STREET").WithMessage("Street line 1 is required.")
            .MaximumLength(200).WithErrorCode("INVALID_STREET").WithMessage("Street line 1 must not exceed 200 characters.");

        RuleFor(x => x.StreetLine2)
            .MaximumLength(200).WithErrorCode("INVALID_STREET").WithMessage("Street line 2 must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.StreetLine2));

        RuleFor(x => x.City)
            .NotEmpty().WithErrorCode("INVALID_CITY").WithMessage("City is required.")
            .MaximumLength(100).WithErrorCode("INVALID_CITY").WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.StateProvince)
            .MaximumLength(100).WithErrorCode("INVALID_STATE_PROVINCE").WithMessage("State/province must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.StateProvince));

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithErrorCode("INVALID_POSTAL_CODE").WithMessage("Postal code is required.")
            .MaximumLength(20).WithErrorCode("INVALID_POSTAL_CODE").WithMessage("Postal code must not exceed 20 characters.");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithErrorCode("INVALID_COUNTRY_CODE").WithMessage("Country code is required.")
            .Length(2).WithErrorCode("INVALID_COUNTRY_CODE").WithMessage("Country code must be exactly 2 characters.")
            .Matches("^[A-Z]{2}$").WithErrorCode("INVALID_COUNTRY_CODE").WithMessage("Country code must be 2 uppercase letters (ISO 3166-1 alpha-2).")
            .MustAsync(async (code, cancellationToken) =>
            {
                bool isEnabled = await featureManager.IsEnabledAsync(FeatureFlags.EnableNomenclatureValidation).ConfigureAwait(false);
                if (!isEnabled)
                    return true;

                bool? isValid = await nomenclatureResolver.ValidateCountryCodeAsync(code, cancellationToken).ConfigureAwait(false);
                return isValid ?? true;
            })
            .WithErrorCode("INVALID_COUNTRY_CODE")
            .WithMessage(x => $"The country code '{x.CountryCode}' is not recognized. Please select a valid country.");
    }
}
