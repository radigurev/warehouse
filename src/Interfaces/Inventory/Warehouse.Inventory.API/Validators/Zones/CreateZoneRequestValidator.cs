using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create zone request payload.
/// </summary>
public sealed class CreateZoneRequestValidator : AbstractValidator<CreateZoneRequest>
{
    /// <summary>
    /// Initializes validation rules for zone creation.
    /// </summary>
    public CreateZoneRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithErrorCode("INVALID_WAREHOUSE").WithMessage("Warehouse ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("INVALID_ZONE_CODE").WithMessage("Zone code is required.")
            .MaximumLength(20).WithErrorCode("INVALID_ZONE_CODE").WithMessage("Zone code must not exceed 20 characters.")
            .Matches("^[A-Za-z0-9-]+$").WithErrorCode("INVALID_ZONE_CODE").WithMessage("Zone code must contain only alphanumeric characters and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_ZONE_NAME").WithMessage("Zone name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_ZONE_NAME").WithMessage("Zone name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("INVALID_ZONE_DESCRIPTION").WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
