using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the create batch request payload.
/// </summary>
public sealed class CreateBatchRequestValidator : AbstractValidator<CreateBatchRequest>
{
    /// <summary>
    /// Initializes validation rules for batch creation.
    /// </summary>
    public CreateBatchRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithErrorCode("INVALID_PRODUCT").WithMessage("Product ID is required.");

        RuleFor(x => x.BatchNumber)
            .NotEmpty().WithErrorCode("INVALID_BATCH_NUMBER").WithMessage("Batch number is required.")
            .MaximumLength(50).WithErrorCode("INVALID_BATCH_NUMBER").WithMessage("Batch number must not exceed 50 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
