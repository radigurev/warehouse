namespace Warehouse.ServiceModel.Requests.EventLog;

/// <summary>
/// Request payload for searching and filtering centralized operations events with pagination.
/// </summary>
public sealed record SearchEventsRequest
{
    /// <summary>
    /// Gets the ISA-95 domain filter (Auth, Purchasing, Fulfillment, Inventory, Customers). Optional.
    /// </summary>
    public string? Domain { get; init; }

    /// <summary>
    /// Gets the event type filter (exact match). Optional.
    /// </summary>
    public string? EventType { get; init; }

    /// <summary>
    /// Gets the entity type filter (exact match). Optional.
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Gets the entity ID filter. Requires EntityType. Optional.
    /// </summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// Gets the user ID filter. Optional.
    /// </summary>
    public int? UserId { get; init; }

    /// <summary>
    /// Gets the correlation ID filter. Optional.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the date range start filter (inclusive). Optional.
    /// </summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>
    /// Gets the date range end filter (inclusive). Optional.
    /// </summary>
    public DateTime? DateTo { get; init; }

    /// <summary>
    /// Gets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the number of items per page (1-100). Defaults to 25.
    /// </summary>
    public int PageSize { get; init; } = 25;

    /// <summary>
    /// Gets the sort field. Defaults to occurredAtUtc.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Gets the sort direction (asc or desc). Defaults to desc.
    /// </summary>
    public string? SortDirection { get; init; }
}
