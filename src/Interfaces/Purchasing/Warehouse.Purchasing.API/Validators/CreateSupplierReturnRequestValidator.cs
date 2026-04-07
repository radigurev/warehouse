using FluentValidation;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Validators;

/// <summary>
/// Validates the create supplier return request payload per SDD-PURCH-001 section 3.11-3.12.
/// </summary>
public sealed class CreateSupplierReturnRequestValidator : AbstractValidator<CreateSupplierReturnRequest>
{
    /// <summary>
    /// Initializes validation rules for supplier return creation.
    /// </summary>
    public CreateSupplierReturnRequestValidator()
    {
        RuleFor(x => x.SupplierId)
            .GreaterThan(0).WithErrorCode("INVALID_SUPPLIER").WithMessage("Supplier ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithErrorCode("INVALID_RETURN_REASON").WithMessage("Return reason is required.")
            .MaximumLength(500).WithErrorCode("INVALID_RETURN_REASON").WithMessage("Return reason must not exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("RETURN_MUST_HAVE_LINES").WithMessage("Supplier return must have at least one line.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId)
                .GreaterThan(0).WithErrorCode("INVALID_PRODUCT").WithMessage("Product ID is required.");

            line.RuleFor(l => l.WarehouseId)
                .GreaterThan(0).WithErrorCode("INVALID_WAREHOUSE").WithMessage("Warehouse ID is required.");

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithErrorCode("INVALID_QUANTITY").WithMessage("Return quantity must be greater than 0.");
        });
    }
}
