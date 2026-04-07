using FluentValidation;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Validators;

/// <summary>
/// Validates the create shipment request payload per SDD-FULF-001 section 3.7.
/// </summary>
public sealed class CreateShipmentRequestValidator : AbstractValidator<CreateShipmentRequest>
{
    /// <summary>
    /// Initializes validation rules for shipment creation.
    /// </summary>
    public CreateShipmentRequestValidator()
    {
        RuleFor(x => x.SalesOrderId).GreaterThan(0).WithErrorCode("INVALID_SO_REFERENCE").WithMessage("Sales order ID is required.");
        RuleFor(x => x.Notes).MaximumLength(2000).WithErrorCode("INVALID_NOTES").When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
