using FluentValidation;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Validators;

/// <summary>
/// Validates the create email request payload per SDD-CUST-001 section 2.5.1.
/// </summary>
public sealed class CreateEmailRequestValidator : AbstractValidator<CreateEmailRequest>
{
    private static readonly string[] AllowedEmailTypes = ["General", "Billing", "Support"];

    /// <summary>
    /// Initializes validation rules for email creation.
    /// </summary>
    public CreateEmailRequestValidator()
    {
        RuleFor(x => x.EmailType)
            .NotEmpty().WithErrorCode("INVALID_EMAIL_TYPE").WithMessage("Email type is required.")
            .Must(type => AllowedEmailTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode("INVALID_EMAIL_TYPE")
            .WithMessage("Email type must be one of: General, Billing, Support.");

        RuleFor(x => x.EmailAddress)
            .NotEmpty().WithErrorCode("INVALID_EMAIL").WithMessage("Email address is required.")
            .MaximumLength(256).WithErrorCode("INVALID_EMAIL").WithMessage("Email address must not exceed 256 characters.")
            .EmailAddress().WithErrorCode("INVALID_EMAIL").WithMessage("Email address format is invalid.");
    }
}
