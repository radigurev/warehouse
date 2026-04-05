using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the add BOM line request payload.
/// </summary>
public sealed class AddBomLineRequestValidator : AbstractValidator<AddBomLineRequest>
{
    /// <summary>
    /// Initializes validation rules for adding a BOM line.
    /// </summary>
    public AddBomLineRequestValidator()
    {
        RuleFor(x => x.ChildProductId)
            .GreaterThan(0).WithErrorCode("INVALID_CHILD_PRODUCT").WithMessage("Child product ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithErrorCode("INVALID_QUANTITY").WithMessage("Quantity must be greater than zero.");
    }
}
