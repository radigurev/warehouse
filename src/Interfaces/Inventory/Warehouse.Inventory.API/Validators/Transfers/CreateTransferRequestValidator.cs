using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create transfer request payload.
/// </summary>
public sealed class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
{
    /// <summary>
    /// Initializes validation rules for transfer creation.
    /// </summary>
    public CreateTransferRequestValidator()
    {
        RuleFor(x => x.SourceWarehouseId)
            .GreaterThan(0).WithErrorCode("INVALID_WAREHOUSE").WithMessage("Source warehouse ID is required.");

        RuleFor(x => x.DestinationWarehouseId)
            .GreaterThan(0).WithErrorCode("INVALID_WAREHOUSE").WithMessage("Destination warehouse ID is required.")
            .NotEqual(x => x.SourceWarehouseId).WithErrorCode("TRANSFER_SAME_WAREHOUSE").WithMessage("Source and destination warehouses must be different.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("INVALID_TRANSFER_LINES").WithMessage("At least one transfer line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId)
                .GreaterThan(0).WithErrorCode("INVALID_PRODUCT").WithMessage("Product ID is required.");

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithErrorCode("INVALID_TRANSFER_QUANTITY").WithMessage("Quantity must be greater than zero.");
        });
    }
}
