using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create product accessory request payload.
/// </summary>
public sealed class CreateProductAccessoryRequestValidator : AbstractValidator<CreateProductAccessoryRequest>
{
    /// <summary>
    /// Initializes validation rules for product accessory creation.
    /// </summary>
    public CreateProductAccessoryRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithErrorCode("INVALID_PRODUCT").WithMessage("Product ID is required.");

        RuleFor(x => x.AccessoryProductId)
            .GreaterThan(0).WithErrorCode("INVALID_ACCESSORY_PRODUCT_ID").WithMessage("Accessory product ID is required.");
    }
}
