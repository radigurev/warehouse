using FluentValidation;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Validators;

/// <summary>
/// Validates the create phone request payload per SDD-CUST-001 section 2.4.1.
/// </summary>
public sealed class CreatePhoneRequestValidator : AbstractValidator<CreatePhoneRequest>
{
    private static readonly string[] AllowedPhoneTypes = ["Mobile", "Landline", "Fax"];

    /// <summary>
    /// Initializes validation rules for phone creation.
    /// </summary>
    public CreatePhoneRequestValidator()
    {
        RuleFor(x => x.PhoneType)
            .NotEmpty().WithErrorCode("INVALID_PHONE_TYPE").WithMessage("Phone type is required.")
            .Must(type => AllowedPhoneTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode("INVALID_PHONE_TYPE")
            .WithMessage("Phone type must be one of: Mobile, Landline, Fax.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithErrorCode("INVALID_PHONE_NUMBER").WithMessage("Phone number is required.")
            .MinimumLength(5).WithErrorCode("INVALID_PHONE_NUMBER").WithMessage("Phone number must be at least 5 characters.")
            .MaximumLength(20).WithErrorCode("INVALID_PHONE_NUMBER").WithMessage("Phone number must not exceed 20 characters.")
            .Matches(@"^\+?[\d\s\-\(\)]+$").WithErrorCode("INVALID_PHONE_NUMBER").WithMessage("Phone number format is invalid.");

        RuleFor(x => x.Extension)
            .MaximumLength(10).WithErrorCode("INVALID_EXTENSION").WithMessage("Extension must not exceed 10 characters.")
            .Matches("^[0-9]*$").WithErrorCode("INVALID_EXTENSION").WithMessage("Extension must contain digits only.")
            .When(x => !string.IsNullOrEmpty(x.Extension));
    }
}
