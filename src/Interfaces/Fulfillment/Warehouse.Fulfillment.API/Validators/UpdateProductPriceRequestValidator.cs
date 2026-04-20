using FluentValidation;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Validators;

/// <summary>
/// Validates <see cref="UpdateProductPriceRequest"/> per CHG-FEAT-007 §3 validation rules V3-V4.
/// Note: ProductId and CurrencyCode are NOT present on the update payload (immutable after creation, V7).
/// </summary>
public sealed class UpdateProductPriceRequestValidator : AbstractValidator<UpdateProductPriceRequest>
{
    /// <summary>
    /// Initializes the validation rules for updating a product price.
    /// </summary>
    public UpdateProductPriceRequestValidator()
    {
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

    private static bool BeValidRange(UpdateProductPriceRequest request)
    {
        return request.ValidTo!.Value > request.ValidFrom!.Value;
    }
}
