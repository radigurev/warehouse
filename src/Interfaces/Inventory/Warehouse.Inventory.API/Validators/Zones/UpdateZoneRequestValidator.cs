using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the update zone request payload.
/// </summary>
public sealed class UpdateZoneRequestValidator : AbstractValidator<UpdateZoneRequest>
{
    /// <summary>
    /// Initializes validation rules for zone update.
    /// </summary>
    public UpdateZoneRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_ZONE_NAME").WithMessage("Zone name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_ZONE_NAME").WithMessage("Zone name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("INVALID_ZONE_DESCRIPTION").WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
