using FluentValidation;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Validators;

/// <summary>
/// Validates the assign roles request payload.
/// </summary>
public sealed class AssignRolesRequestValidator : AbstractValidator<AssignRolesRequest>
{
    /// <summary>
    /// Initializes validation rules for role assignment.
    /// </summary>
    public AssignRolesRequestValidator()
    {
        RuleFor(x => x.RoleIds)
            .NotEmpty().WithMessage("At least one role ID is required.");
    }
}
