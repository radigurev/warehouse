using FluentValidation;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Validators;

/// <summary>
/// Validates the update customer request payload per SDD-CUST-001 section 3.
/// </summary>
public sealed class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    /// <summary>
    /// Initializes validation rules for customer update.
    /// </summary>
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_NAME").WithMessage("Customer name is required.")
            .MaximumLength(200).WithErrorCode("INVALID_NAME").WithMessage("Customer name must not exceed 200 characters.");

        RuleFor(x => x.NativeLanguageName)
            .MaximumLength(200).WithErrorCode("INVALID_NATIVE_NAME").WithMessage("Native language name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.NativeLanguageName));

        RuleFor(x => x.TaxId)
            .MaximumLength(50).WithErrorCode("INVALID_TAX_ID").WithMessage("Tax ID must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.TaxId));

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
