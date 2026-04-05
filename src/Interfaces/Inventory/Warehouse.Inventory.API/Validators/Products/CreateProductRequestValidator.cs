using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create product request payload per SDD-INV-001 section 3.
/// </summary>
public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    /// <summary>
    /// Initializes validation rules for product creation.
    /// </summary>
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("INVALID_PRODUCT_CODE").WithMessage("Product code is required.")
            .MaximumLength(50).WithErrorCode("INVALID_PRODUCT_CODE").WithMessage("Product code must not exceed 50 characters.")
            .Matches("^[A-Za-z0-9_-]+$").WithErrorCode("INVALID_PRODUCT_CODE").WithMessage("Product code must contain only alphanumeric characters, hyphens, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_PRODUCT_NAME").WithMessage("Product name is required.")
            .MaximumLength(200).WithErrorCode("INVALID_PRODUCT_NAME").WithMessage("Product name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithErrorCode("INVALID_PRODUCT_DESCRIPTION").WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Sku)
            .MaximumLength(100).WithErrorCode("INVALID_SKU").WithMessage("SKU must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Sku));

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithErrorCode("INVALID_BARCODE").WithMessage("Barcode must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Barcode));

        RuleFor(x => x.UnitOfMeasureId)
            .GreaterThan(0).WithErrorCode("INVALID_UNIT_OF_MEASURE").WithMessage("Unit of measure ID is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
