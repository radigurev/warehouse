using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the update storage location request payload.
/// </summary>
public sealed class UpdateStorageLocationRequestValidator : AbstractValidator<UpdateStorageLocationRequest>
{
    /// <summary>
    /// Initializes validation rules for storage location update.
    /// </summary>
    public UpdateStorageLocationRequestValidator()
    {
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
