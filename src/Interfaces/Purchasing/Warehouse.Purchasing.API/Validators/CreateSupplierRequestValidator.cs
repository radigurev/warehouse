using FluentValidation;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Validators;

/// <summary>
/// Validates the create supplier request payload per SDD-PURCH-001 section 3.1.
/// </summary>
public sealed class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    /// <summary>
    /// Initializes validation rules for supplier creation.
    /// </summary>
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_SUPPLIER_NAME").WithMessage("Supplier name is required.")
            .MaximumLength(200).WithErrorCode("INVALID_SUPPLIER_NAME").WithMessage("Supplier name must not exceed 200 characters.");

        RuleFor(x => x.Code)
            .MaximumLength(20).WithErrorCode("INVALID_SUPPLIER_CODE").WithMessage("Supplier code must not exceed 20 characters.")
            .Matches("^[A-Za-z0-9-]+$").WithErrorCode("INVALID_SUPPLIER_CODE").WithMessage("Supplier code must contain only alphanumeric characters and hyphens.")
            .When(x => !string.IsNullOrEmpty(x.Code));

        RuleFor(x => x.TaxId)
            .MaximumLength(50).WithErrorCode("INVALID_TAX_ID").WithMessage("Tax ID must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.TaxId));

        RuleFor(x => x.PaymentTermDays)
            .GreaterThanOrEqualTo(0).WithErrorCode("INVALID_PAYMENT_TERMS").WithMessage("Payment terms must be 0 or greater.")
            .LessThanOrEqualTo(365).WithErrorCode("INVALID_PAYMENT_TERMS").WithMessage("Payment terms must not exceed 365 days.")
            .When(x => x.PaymentTermDays.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
