using FluentValidation;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Validators;

/// <summary>
/// Validates the update account request payload per SDD-CUST-001 section 2.2.2.
/// </summary>
public sealed class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    /// <summary>
    /// Initializes validation rules for account update.
    /// </summary>
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("INVALID_DESCRIPTION").WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
