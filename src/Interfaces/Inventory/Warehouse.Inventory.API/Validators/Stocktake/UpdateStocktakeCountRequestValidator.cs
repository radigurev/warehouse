using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the update stocktake count request payload.
/// </summary>
public sealed class UpdateStocktakeCountRequestValidator : AbstractValidator<UpdateStocktakeCountRequest>
{
    /// <summary>
    /// Initializes validation rules for stocktake count updating.
    /// </summary>
    public UpdateStocktakeCountRequestValidator()
    {
        RuleFor(x => x.CountedQuantity)
            .GreaterThanOrEqualTo(0).WithErrorCode("INVALID_COUNTED_QUANTITY").WithMessage("Counted quantity must be zero or greater.");
    }
}
