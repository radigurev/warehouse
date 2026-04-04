using FluentValidation;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Validators;

/// <summary>
/// Validates the change password request payload.
/// </summary>
public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    /// <summary>
    /// Initializes validation rules for password change requests.
    /// </summary>
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithErrorCode("INVALID_PASSWORD").WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithErrorCode("INVALID_PASSWORD").WithMessage("New password is required.")
            .MinimumLength(8).WithErrorCode("INVALID_PASSWORD").WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithErrorCode("INVALID_PASSWORD").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithErrorCode("INVALID_PASSWORD").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithErrorCode("INVALID_PASSWORD").WithMessage("Password must contain at least one digit.");
    }
}
