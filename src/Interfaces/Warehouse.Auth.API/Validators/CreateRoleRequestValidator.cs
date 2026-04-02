using FluentValidation;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Validators;

/// <summary>
/// Validates the create role request payload per SDD-AUTH-001 V8-V9.
/// </summary>
public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    /// <summary>
    /// Initializes validation rules for role creation.
    /// </summary>
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_ROLE_NAME").WithMessage("Role name is required.")
            .MinimumLength(2).WithErrorCode("INVALID_ROLE_NAME").WithMessage("Role name must be at least 2 characters.")
            .MaximumLength(50).WithErrorCode("INVALID_ROLE_NAME").WithMessage("Role name must not exceed 50 characters.")
            .Matches("^[a-zA-Z0-9 ]+$").WithErrorCode("INVALID_ROLE_NAME").WithMessage("Role name must be alphanumeric with spaces only.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("INVALID_ROLE_DESCRIPTION").WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}
