namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for searching and filtering fulfillment events with pagination.
/// </summary>
public sealed record SearchFulfillmentEventsRequest
{
    /// <summary>Gets the event type filter. Optional.</summary>
    public string? EventType { get; init; }

    /// <summary>Gets the entity type filter. Optional.</summary>
    public string? EntityType { get; init; }

    /// <summary>Gets the entity ID filter. Optional.</summary>
    public int? EntityId { get; init; }

    /// <summary>Gets the user ID filter. Optional.</summary>
    public int? UserId { get; init; }

    /// <summary>Gets the date range start filter. Optional.</summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>Gets the date range end filter. Optional.</summary>
    public DateTime? DateTo { get; init; }

    /// <summary>Gets the page number (1-based). Defaults to 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Gets the number of items per page. Defaults to 25.</summary>
    public int PageSize { get; init; } = 25;
}
