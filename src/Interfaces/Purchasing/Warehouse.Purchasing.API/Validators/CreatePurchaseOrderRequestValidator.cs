using FluentValidation;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Validators;

/// <summary>
/// Validates the create purchase order request payload per SDD-PURCH-001 section 3.6-3.7.
/// </summary>
public sealed class CreatePurchaseOrderRequestValidator : AbstractValidator<CreatePurchaseOrderRequest>
{
    /// <summary>
    /// Initializes validation rules for purchase order creation.
    /// </summary>
    public CreatePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId)
            .GreaterThan(0).WithErrorCode("INVALID_SUPPLIER").WithMessage("Supplier ID is required.");

        RuleFor(x => x.DestinationWarehouseId)
            .GreaterThan(0).WithErrorCode("INVALID_WAREHOUSE").WithMessage("Destination warehouse ID is required.");

        RuleFor(x => x.ExpectedDeliveryDate)
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithErrorCode("INVALID_DELIVERY_DATE").WithMessage("Expected delivery date must be today or a future date.")
            .When(x => x.ExpectedDeliveryDate.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("PO_MUST_HAVE_LINES").WithMessage("Purchase order must have at least one line.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId)
                .GreaterThan(0).WithErrorCode("INVALID_PRODUCT").WithMessage("Product ID is required.");

            line.RuleFor(l => l.OrderedQuantity)
                .GreaterThan(0).WithErrorCode("INVALID_QUANTITY").WithMessage("Ordered quantity must be greater than 0.");

            line.RuleFor(l => l.UnitPrice)
                .GreaterThanOrEqualTo(0).WithErrorCode("INVALID_UNIT_PRICE").WithMessage("Unit price must be 0 or greater.");

            line.RuleFor(l => l.Notes)
                .MaximumLength(500).WithErrorCode("INVALID_LINE_NOTES").WithMessage("Line notes must not exceed 500 characters.")
                .When(l => !string.IsNullOrEmpty(l.Notes));
        });
    }
}
