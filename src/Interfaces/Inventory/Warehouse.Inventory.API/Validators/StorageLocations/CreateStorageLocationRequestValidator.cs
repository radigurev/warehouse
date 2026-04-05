using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create storage location request payload.
/// </summary>
public sealed class CreateStorageLocationRequestValidator : AbstractValidator<CreateStorageLocationRequest>
{
    /// <summary>
    /// Initializes validation rules for storage location creation.
    /// </summary>
    public CreateStorageLocationRequestValidator()
    {
        RuleFor(x => x.ZoneId)
            .GreaterThan(0).WithErrorCode("INVALID_ZONE_ID").WithMessage("Zone ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("INVALID_LOCATION_CODE").WithMessage("Location code is required.")
            .MaximumLength(30).WithErrorCode("INVALID_LOCATION_CODE").WithMessage("Location code must not exceed 30 characters.")
            .Matches("^[A-Za-z0-9_-]+$").WithErrorCode("INVALID_LOCATION_CODE").WithMessage("Location code must contain only alphanumeric characters, hyphens, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_LOCATION_NAME").WithMessage("Location name is required.")
            .MaximumLength(100).WithErrorCode("INVALID_LOCATION_NAME").WithMessage("Location name must not exceed 100 characters.");

        RuleFor(x => x.LocationType)
            .NotEmpty().WithErrorCode("INVALID_LOCATION_TYPE").WithMessage("Location type is required.")
            .Must(t => t is "Row" or "Shelf" or "Bin" or "Bulk").WithErrorCode("INVALID_LOCATION_TYPE").WithMessage("Location type must be Row, Shelf, Bin, or Bulk.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithErrorCode("INVALID_CAPACITY").WithMessage("Capacity must be greater than zero.")
            .When(x => x.Capacity.HasValue);
    }
}
