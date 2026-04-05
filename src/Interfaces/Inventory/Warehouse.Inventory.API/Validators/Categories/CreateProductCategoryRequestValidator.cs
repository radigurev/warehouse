using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create product category request payload per SDD-INV-001 section 3.
/// </summary>
public sealed class CreateProductCategoryRequestValidator : AbstractValidator<CreateProductCategoryRequest>
{
    /// <summary>
    /// Initializes validation rules for product category creation.
    /// </summary>
    public CreateProductCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_CATEGORY_NAME").WithMessage("Category name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_CATEGORY_NAME").WithMessage("Category name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("INVALID_CATEGORY_DESCRIPTION").WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
