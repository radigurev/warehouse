using FluentValidation;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Validators;

/// <summary>
/// Validates the create permission request payload per SDD-AUTH-001 V11-V12.
/// </summary>
public sealed class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequest>
{
    private static readonly string[] AllowedActions = ["read", "write", "update", "delete", "all"];

    /// <summary>
    /// Initializes validation rules for permission creation.
    /// </summary>
    public CreatePermissionRequestValidator()
    {
        RuleFor(x => x.Resource)
            .NotEmpty().WithErrorCode("INVALID_RESOURCE").WithMessage("Resource is required.")
            .MaximumLength(100).WithErrorCode("INVALID_RESOURCE").WithMessage("Resource must not exceed 100 characters.")
            .Matches("^[a-z0-9.]+$").WithErrorCode("INVALID_RESOURCE").WithMessage("Resource must be lowercase alphanumeric with dots only.");

        RuleFor(x => x.Action)
            .NotEmpty().WithErrorCode("INVALID_ACTION").WithMessage("Action is required.")
            .Must(action => AllowedActions.Contains(action))
            .WithErrorCode("INVALID_ACTION").WithMessage("Action must be one of: read, write, update, delete, all.");
    }
}
