using FluentValidation;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Validators;

/// <summary>
/// Validates the update shipment status request payload per SDD-FULF-001 section 3.8.
/// </summary>
public sealed class UpdateShipmentStatusRequestValidator : AbstractValidator<UpdateShipmentStatusRequest>
{
    /// <summary>
    /// Initializes validation rules for shipment status updates.
    /// </summary>
    public UpdateShipmentStatusRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty().WithErrorCode("INVALID_SHIPMENT_STATUS").WithMessage("Shipment status is required.");
        RuleFor(x => x.TrackingNumber).MaximumLength(100).WithErrorCode("INVALID_TRACKING_NUMBER").When(x => !string.IsNullOrEmpty(x.TrackingNumber));
        RuleFor(x => x.TrackingUrl).MaximumLength(500).WithErrorCode("INVALID_TRACKING_URL").When(x => !string.IsNullOrEmpty(x.TrackingUrl));
        RuleFor(x => x.Notes).MaximumLength(2000).WithErrorCode("INVALID_NOTES").When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
