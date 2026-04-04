using FluentValidation;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Validators;

/// <summary>
/// Validates the create user request payload per SDD-AUTH-001 V1-V5.
/// </summary>
public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    /// <summary>
    /// Initializes validation rules for user creation.
    /// </summary>
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithErrorCode("INVALID_USERNAME").WithMessage("Username is required.")
            .MinimumLength(3).WithErrorCode("INVALID_USERNAME").WithMessage("Username must be at least 3 characters.")
            .MaximumLength(50).WithErrorCode("INVALID_USERNAME").WithMessage("Username must not exceed 50 characters.")
            .Matches("^[a-zA-Z0-9_]+$").WithErrorCode("INVALID_USERNAME").WithMessage("Username must be alphanumeric with underscores only.");

        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("INVALID_EMAIL").WithMessage("Email is required.")
            .MaximumLength(256).WithErrorCode("INVALID_EMAIL").WithMessage("Email must not exceed 256 characters.")
            .EmailAddress().WithErrorCode("INVALID_EMAIL").WithMessage("Email format is invalid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("INVALID_PASSWORD").WithMessage("Password is required.")
            .MinimumLength(8).WithErrorCode("INVALID_PASSWORD").WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithErrorCode("INVALID_PASSWORD").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithErrorCode("INVALID_PASSWORD").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithErrorCode("INVALID_PASSWORD").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithErrorCode("INVALID_FIRST_NAME").WithMessage("First name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_FIRST_NAME").WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithErrorCode("INVALID_LAST_NAME").WithMessage("Last name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_LAST_NAME").WithMessage("Last name must not exceed 100 characters.");
    }
}
