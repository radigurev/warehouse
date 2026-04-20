using FluentValidation;
using Microsoft.FeatureManagement;
using Warehouse.Infrastructure.Caching;
using Warehouse.Infrastructure.Configuration;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Validators;

/// <summary>
/// Validates the create sales order request payload per SDD-FULF-001 section 3.1-3.2
/// with optional Nomenclature country code validation per CHG-ENH-001.
/// <para>See <see cref="INomenclatureResolver"/>, <see cref="IFeatureManager"/>.</para>
/// </summary>
public sealed class CreateSalesOrderRequestValidator : AbstractValidator<CreateSalesOrderRequest>
{
    /// <summary>
    /// Initializes validation rules for sales order creation.
    /// </summary>
    public CreateSalesOrderRequestValidator(
        INomenclatureResolver nomenclatureResolver,
        IFeatureManager featureManager)
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithErrorCode("INVALID_CUSTOMER").WithMessage("Customer ID is required.");
        RuleFor(x => x.CustomerAccountId).GreaterThan(0).WithErrorCode("SO_INVALID_CUSTOMER_ACCOUNT").WithMessage("CustomerAccountId must be a positive integer.");
        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithErrorCode("SO_INVALID_CURRENCY").WithMessage("CurrencyCode is required.")
            .Length(3).WithErrorCode("SO_INVALID_CURRENCY").WithMessage("CurrencyCode must be exactly 3 characters (ISO 4217).")
            .Matches("^[A-Z]{3}$").WithErrorCode("SO_INVALID_CURRENCY").WithMessage("CurrencyCode must be exactly 3 uppercase ASCII letters (ISO 4217).");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithErrorCode("INVALID_WAREHOUSE").WithMessage("Warehouse ID is required.");
        RuleFor(x => x.RequestedShipDate).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.Date)).WithErrorCode("INVALID_SHIP_DATE").WithMessage("Requested ship date must be today or a future date.").When(x => x.RequestedShipDate.HasValue);
        RuleFor(x => x.ShippingStreetLine1).NotEmpty().MaximumLength(200).WithErrorCode("INVALID_SHIPPING_ADDRESS").WithMessage("Shipping street line 1 is required (max 200 characters).");
        RuleFor(x => x.ShippingStreetLine2).MaximumLength(200).WithErrorCode("INVALID_SHIPPING_ADDRESS").When(x => !string.IsNullOrEmpty(x.ShippingStreetLine2));
        RuleFor(x => x.ShippingCity).NotEmpty().MaximumLength(100).WithErrorCode("INVALID_SHIPPING_ADDRESS").WithMessage("Shipping city is required (max 100 characters).");
        RuleFor(x => x.ShippingStateProvince).MaximumLength(100).WithErrorCode("INVALID_SHIPPING_ADDRESS").When(x => !string.IsNullOrEmpty(x.ShippingStateProvince));
        RuleFor(x => x.ShippingPostalCode).NotEmpty().MaximumLength(20).WithErrorCode("INVALID_SHIPPING_ADDRESS").WithMessage("Shipping postal code is required (max 20 characters).");

        RuleFor(x => x.ShippingCountryCode)
            .NotEmpty().WithErrorCode("INVALID_SHIPPING_ADDRESS").WithMessage("Shipping country code is required.")
            .Length(2).WithErrorCode("INVALID_SHIPPING_ADDRESS").WithMessage("Shipping country code must be a 2-letter ISO 3166-1 alpha-2 code.")
            .Must(code => ValidateCountryCodeSync(nomenclatureResolver, featureManager, code))
            .WithErrorCode("INVALID_COUNTRY_CODE")
            .WithMessage(x => $"The country code '{x.ShippingCountryCode}' is not recognized. Please select a valid country.");

        RuleFor(x => x.Notes).MaximumLength(2000).WithErrorCode("INVALID_NOTES").When(x => !string.IsNullOrEmpty(x.Notes));
        RuleFor(x => x.Lines).NotEmpty().WithErrorCode("SO_MUST_HAVE_LINES").WithMessage("Sales order must have at least one line.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0).WithErrorCode("INVALID_PRODUCT").WithMessage("Product ID is required.");
            line.RuleFor(l => l.OrderedQuantity).GreaterThan(0).WithErrorCode("INVALID_QUANTITY").WithMessage("Ordered quantity must be greater than 0.");
            line.RuleFor(l => l.UnitPrice!.Value)
                .GreaterThanOrEqualTo(0)
                .When(l => l.UnitPrice.HasValue)
                .WithErrorCode("INVALID_UNIT_PRICE")
                .WithMessage("Unit price must be 0 or greater when provided.");
            line.RuleFor(l => l.Notes).MaximumLength(500).WithErrorCode("INVALID_LINE_NOTES").When(l => !string.IsNullOrEmpty(l.Notes));
        });
    }

    /// <summary>
    /// Synchronously validates the country code against the Nomenclature cache when enabled by feature flag.
    /// Blocks briefly on the async resolver — safe because the underlying resolver is fail-open (returns null on Redis errors)
    /// and this validator runs inside ASP.NET's synchronous auto-validation pipeline.
    /// </summary>
    private static bool ValidateCountryCodeSync(
        INomenclatureResolver nomenclatureResolver,
        IFeatureManager featureManager,
        string code)
    {
        bool enabled = featureManager.IsEnabledAsync(FeatureFlags.EnableNomenclatureValidation).GetAwaiter().GetResult();
        if (!enabled) return true;

        bool? valid = nomenclatureResolver.ValidateCountryCodeAsync(code, CancellationToken.None).GetAwaiter().GetResult();
        return valid ?? true;
    }
}
