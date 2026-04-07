using FluentValidation;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Validators;

/// <summary>
/// Validates the create supplier category request payload per SDD-PURCH-001 section 3.2.
/// </summary>
public sealed class CreateSupplierCategoryRequestValidator : AbstractValidator<CreateSupplierCategoryRequest>
{
    /// <summary>
    /// Initializes validation rules for supplier category creation.
    /// </summary>
    public CreateSupplierCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_CATEGORY_NAME").WithMessage("Category name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_CATEGORY_NAME").WithMessage("Category name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("INVALID_CATEGORY_DESCRIPTION").WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
