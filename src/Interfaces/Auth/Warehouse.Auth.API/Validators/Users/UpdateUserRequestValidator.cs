using FluentValidation;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Validators;

/// <summary>
/// Validates the update user request payload.
/// </summary>
public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    /// <summary>
    /// Initializes validation rules for user profile updates.
    /// </summary>
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("INVALID_EMAIL").WithMessage("Email is required.")
            .MaximumLength(256).WithErrorCode("INVALID_EMAIL").WithMessage("Email must not exceed 256 characters.")
            .EmailAddress().WithErrorCode("INVALID_EMAIL").WithMessage("Email format is invalid.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithErrorCode("INVALID_FIRST_NAME").WithMessage("First name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_FIRST_NAME").WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithErrorCode("INVALID_LAST_NAME").WithMessage("Last name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_LAST_NAME").WithMessage("Last name must not exceed 100 characters.");
    }
}
