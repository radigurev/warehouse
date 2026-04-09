using FluentValidation;
using Warehouse.ServiceModel.Requests.EventLog;

namespace Warehouse.EventLog.API.Validators;

/// <summary>
/// Validates the SearchEventsRequest query parameters.
/// </summary>
public sealed class SearchEventsRequestValidator : AbstractValidator<SearchEventsRequest>
{
    private static readonly string[] AllowedDomains =
        ["Auth", "Purchasing", "Fulfillment", "Inventory", "Customers"];

    private static readonly string[] AllowedSortFields =
        ["occurredAtUtc", "receivedAtUtc", "eventType", "entityType", "domain"];

    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    /// <summary>
    /// Initializes the validation rules for SearchEventsRequest.
    /// </summary>
    public SearchEventsRequestValidator()
    {
        RuleFor(x => x.Domain)
            .Must(d => AllowedDomains.Contains(d, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode("INVALID_DOMAIN")
            .WithMessage("Domain must be one of: Auth, Purchasing, Fulfillment, Inventory, Customers.")
            .When(x => !string.IsNullOrWhiteSpace(x.Domain));

        RuleFor(x => x.EventType)
            .MaximumLength(100)
            .WithErrorCode("INVALID_EVENT_TYPE")
            .WithMessage("EventType must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.EventType));

        RuleFor(x => x.EntityType)
            .MaximumLength(100)
            .WithErrorCode("INVALID_ENTITY_TYPE")
            .WithMessage("EntityType must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.EntityType));

        RuleFor(x => x.EntityId)
            .GreaterThan(0)
            .WithErrorCode("INVALID_ENTITY_ID")
            .WithMessage("EntityId must be greater than 0.")
            .When(x => x.EntityId.HasValue);

        RuleFor(x => x.EntityType)
            .NotEmpty()
            .WithErrorCode("ENTITY_TYPE_REQUIRED")
            .WithMessage("EntityType is required when filtering by EntityId.")
            .When(x => x.EntityId.HasValue);

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithErrorCode("INVALID_USER_ID")
            .WithMessage("UserId must be greater than 0.")
            .When(x => x.UserId.HasValue);

        RuleFor(x => x.CorrelationId)
            .MaximumLength(36)
            .WithErrorCode("INVALID_CORRELATION_ID")
            .WithMessage("CorrelationId must not exceed 36 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.CorrelationId));

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .WithErrorCode("INVALID_DATE_RANGE")
            .WithMessage("dateFrom must be on or before dateTo.")
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("INVALID_PAGE")
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithErrorCode("INVALID_PAGE_SIZE")
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SortBy)
            .Must(s => AllowedSortFields.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode("INVALID_SORT_FIELD")
            .WithMessage("SortBy must be one of: occurredAtUtc, receivedAtUtc, eventType, entityType, domain.")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));

        RuleFor(x => x.SortDirection)
            .Must(s => AllowedSortDirections.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode("INVALID_SORT_DIRECTION")
            .WithMessage("SortDirection must be asc or desc.")
            .When(x => !string.IsNullOrWhiteSpace(x.SortDirection));
    }
}
