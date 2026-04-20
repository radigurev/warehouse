using FluentValidation;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Validators;

/// <summary>
/// Validates <see cref="CreateProductPriceRequest"/> per CHG-FEAT-007 §3 validation rules V1-V4.
/// </summary>
public sealed class CreateProductPriceRequestValidator : AbstractValidator<CreateProductPriceRequest>
{
    /// <summary>
    /// Initializes the validation rules for creating a product price.
    /// </summary>
    public CreateProductPriceRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0)
            .WithErrorCode("FULF_PRICE_INVALID_PRODUCT")
            .WithMessage("Product ID must be greater than zero.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithErrorCode("FULF_PRICE_INVALID_CURRENCY")
            .WithMessage("Currency code is required.")
            .Length(3).WithErrorCode("FULF_PRICE_INVALID_CURRENCY")
            .WithMessage("Currency code must be exactly 3 characters (ISO 4217).")
            .Matches("^[A-Z]{3}$").WithErrorCode("FULF_PRICE_INVALID_CURRENCY")
            .WithMessage("Currency code must be exactly 3 uppercase letters (ISO 4217).");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0m)
            .WithErrorCode("FULF_PRICE_INVALID_AMOUNT")
            .WithMessage("Unit price must be zero or positive.");

        RuleFor(x => x)
            .Must(BeValidRange)
            .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue)
            .WithErrorCode("FULF_PRICE_INVALID_RANGE")
            .WithMessage("ValidTo must be strictly later than ValidFrom.");
    }

    private static bool BeValidRange(CreateProductPriceRequest request)
    {
        return request.ValidTo!.Value > request.ValidFrom!.Value;
    }
}
