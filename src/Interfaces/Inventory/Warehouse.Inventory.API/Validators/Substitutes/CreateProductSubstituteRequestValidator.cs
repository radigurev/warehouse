using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create product substitute request payload.
/// </summary>
public sealed class CreateProductSubstituteRequestValidator : AbstractValidator<CreateProductSubstituteRequest>
{
    /// <summary>
    /// Initializes validation rules for product substitute creation.
    /// </summary>
    public CreateProductSubstituteRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithErrorCode("INVALID_PRODUCT").WithMessage("Product ID is required.");

        RuleFor(x => x.SubstituteProductId)
            .GreaterThan(0).WithErrorCode("INVALID_SUBSTITUTE_PRODUCT_ID").WithMessage("Substitute product ID is required.");
    }
}
