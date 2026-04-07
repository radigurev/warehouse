using FluentValidation;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Validators;

/// <summary>
/// Validates the create customer return request payload per SDD-FULF-001 section 3.11-3.12.
/// </summary>
public sealed class CreateCustomerReturnRequestValidator : AbstractValidator<CreateCustomerReturnRequest>
{
    /// <summary>
    /// Initializes validation rules for customer return creation.
    /// </summary>
    public CreateCustomerReturnRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithErrorCode("INVALID_CUSTOMER").WithMessage("Customer ID is required.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithErrorCode("INVALID_RETURN_REASON").WithMessage("Return reason is required (1-500 characters).");
        RuleFor(x => x.Notes).MaximumLength(2000).WithErrorCode("INVALID_NOTES").When(x => !string.IsNullOrEmpty(x.Notes));
        RuleFor(x => x.Lines).NotEmpty().WithErrorCode("RETURN_MUST_HAVE_LINES").WithMessage("Customer return must have at least one line.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0).WithErrorCode("INVALID_PRODUCT").WithMessage("Product ID is required.");
            line.RuleFor(l => l.WarehouseId).GreaterThan(0).WithErrorCode("INVALID_WAREHOUSE").WithMessage("Warehouse ID is required.");
            line.RuleFor(l => l.Quantity).GreaterThan(0).WithErrorCode("INVALID_QUANTITY").WithMessage("Quantity must be greater than 0.");
            line.RuleFor(l => l.Notes).MaximumLength(500).WithErrorCode("INVALID_LINE_NOTES").When(l => !string.IsNullOrEmpty(l.Notes));
        });
    }
}
