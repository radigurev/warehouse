using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create BOM request payload.
/// </summary>
public sealed class CreateBomRequestValidator : AbstractValidator<CreateBomRequest>
{
    /// <summary>
    /// Initializes validation rules for BOM creation.
    /// </summary>
    public CreateBomRequestValidator()
    {
        RuleFor(x => x.ParentProductId)
            .GreaterThan(0).WithErrorCode("INVALID_PARENT_PRODUCT").WithMessage("Parent product ID is required.");

        RuleFor(x => x.Name)
            .MaximumLength(100).WithErrorCode("INVALID_BOM_NAME").WithMessage("BOM name must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("INVALID_BOM_LINES").WithMessage("At least one component line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ChildProductId)
                .GreaterThan(0).WithErrorCode("INVALID_CHILD_PRODUCT").WithMessage("Child product ID is required.");

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithErrorCode("INVALID_QUANTITY").WithMessage("Quantity must be greater than zero.");
        });
    }
}
