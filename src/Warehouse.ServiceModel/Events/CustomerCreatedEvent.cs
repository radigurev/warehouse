namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when a new customer is created.
/// </summary>
public sealed record CustomerCreatedEvent : ICorrelatedEvent
{
    /// <summary>
    /// Gets the database-assigned customer identifier.
    /// </summary>
    public required int CustomerId { get; init; }

    /// <summary>
    /// Gets the customer code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the customer name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the category identifier, if assigned.
    /// </summary>
    public required int? CategoryId { get; init; }

    /// <summary>
    /// Gets the user who created the customer.
    /// </summary>
    public required int CreatedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the customer was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the correlation ID from the originating HTTP request.
    /// </summary>
    public string? CorrelationId { get; set; }
}
