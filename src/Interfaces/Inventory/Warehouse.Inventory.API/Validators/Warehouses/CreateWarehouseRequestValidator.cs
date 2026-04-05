using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create warehouse request payload per SDD-INV-003 section 3.
/// </summary>
public sealed class CreateWarehouseRequestValidator : AbstractValidator<CreateWarehouseRequest>
{
    /// <summary>
    /// Initializes validation rules for warehouse creation.
    /// </summary>
    public CreateWarehouseRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("INVALID_WAREHOUSE_CODE").WithMessage("Warehouse code is required.")
            .MaximumLength(20).WithErrorCode("INVALID_WAREHOUSE_CODE").WithMessage("Warehouse code must not exceed 20 characters.")
            .Matches("^[A-Za-z0-9-]+$").WithErrorCode("INVALID_WAREHOUSE_CODE").WithMessage("Warehouse code must contain only alphanumeric characters and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_WAREHOUSE_NAME").WithMessage("Warehouse name is required.")
            .MaximumLength(200).WithErrorCode("INVALID_WAREHOUSE_NAME").WithMessage("Warehouse name must not exceed 200 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithErrorCode("INVALID_WAREHOUSE_ADDRESS").WithMessage("Address must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
