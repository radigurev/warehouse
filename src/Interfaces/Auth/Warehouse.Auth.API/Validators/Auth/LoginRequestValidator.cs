using FluentValidation;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Validators;

/// <summary>
/// Validates the login request payload.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>
    /// Initializes validation rules for login requests.
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithErrorCode("MISSING_USERNAME").WithMessage("Username is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("MISSING_PASSWORD").WithMessage("Password is required.");
    }
}
