using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the record stock movement request payload.
/// </summary>
public sealed class RecordStockMovementRequestValidator : AbstractValidator<RecordStockMovementRequest>
{
    /// <summary>
    /// Initializes validation rules for stock movement recording.
    /// </summary>
    public RecordStockMovementRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithErrorCode("INVALID_PRODUCT").WithMessage("Product ID is required.");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithErrorCode("INVALID_WAREHOUSE").WithMessage("Warehouse ID is required.");

        RuleFor(x => x.Quantity)
            .NotEqual(0).WithErrorCode("INVALID_MOVEMENT_QUANTITY").WithMessage("Quantity must not be zero.");

        RuleFor(x => x.ReasonCode)
            .IsInEnum().WithErrorCode("INVALID_REASON_CODE").WithMessage("Invalid reason code.");

        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(100).WithErrorCode("INVALID_REFERENCE_NUMBER").WithMessage("Reference number must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.ReferenceNumber));

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
