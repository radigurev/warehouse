using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create stocktake session request payload.
/// </summary>
public sealed class CreateStocktakeSessionRequestValidator : AbstractValidator<CreateStocktakeSessionRequest>
{
    /// <summary>
    /// Initializes validation rules for stocktake session creation.
    /// </summary>
    public CreateStocktakeSessionRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithErrorCode("INVALID_WAREHOUSE_ID").WithMessage("Warehouse ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("INVALID_SESSION_NAME").WithMessage("Session name is required.")
            .MaximumLength(200).WithErrorCode("INVALID_SESSION_NAME").WithMessage("Session name must not exceed 200 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
