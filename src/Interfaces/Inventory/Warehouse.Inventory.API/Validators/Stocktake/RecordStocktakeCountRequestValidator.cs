using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the record stocktake count request payload.
/// </summary>
public sealed class RecordStocktakeCountRequestValidator : AbstractValidator<RecordStocktakeCountRequest>
{
    /// <summary>
    /// Initializes validation rules for stocktake count recording.
    /// </summary>
    public RecordStocktakeCountRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithErrorCode("INVALID_PRODUCT").WithMessage("Product ID is required.");

        RuleFor(x => x.CountedQuantity)
            .GreaterThanOrEqualTo(0).WithErrorCode("INVALID_COUNT_QUANTITY").WithMessage("Counted quantity must be zero or greater.");
    }
}
