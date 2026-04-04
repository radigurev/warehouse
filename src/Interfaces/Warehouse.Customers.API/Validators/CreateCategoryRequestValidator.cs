using FluentValidation;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Validators;

/// <summary>
/// Validates the create category request payload per SDD-CUST-001 section 2.6.
/// </summary>
public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    /// <summary>
    /// Initializes validation rules for category creation.
    /// </summary>
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_NAME").WithMessage("Category name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_NAME").WithMessage("Category name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("INVALID_DESCRIPTION").WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
