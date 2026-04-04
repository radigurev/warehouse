using FluentValidation;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Validators;

/// <summary>
/// Validates the create customer request payload per SDD-CUST-001 section 3.
/// </summary>
public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    /// <summary>
    /// Initializes validation rules for customer creation.
    /// </summary>
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_NAME").WithMessage("Customer name is required.")
            .MaximumLength(200).WithErrorCode("INVALID_NAME").WithMessage("Customer name must not exceed 200 characters.");

        RuleFor(x => x.Code)
            .MaximumLength(20).WithErrorCode("INVALID_CODE").WithMessage("Customer code must not exceed 20 characters.")
            .Matches("^[A-Za-z0-9-]*$").WithErrorCode("INVALID_CODE").WithMessage("Customer code must contain only alphanumeric characters and hyphens.")
            .When(x => !string.IsNullOrEmpty(x.Code));

        RuleFor(x => x.TaxId)
            .MaximumLength(50).WithErrorCode("INVALID_TAX_ID").WithMessage("Tax ID must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.TaxId));

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
