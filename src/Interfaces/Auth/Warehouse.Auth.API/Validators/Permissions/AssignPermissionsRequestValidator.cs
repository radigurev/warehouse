using FluentValidation;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Validators;

/// <summary>
/// Validates the assign permissions request payload.
/// </summary>
public sealed class AssignPermissionsRequestValidator : AbstractValidator<AssignPermissionsRequest>
{
    /// <summary>
    /// Initializes validation rules for permission assignment.
    /// </summary>
    public AssignPermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionIds)
            .NotEmpty().WithMessage("At least one permission ID is required.");
    }
}
