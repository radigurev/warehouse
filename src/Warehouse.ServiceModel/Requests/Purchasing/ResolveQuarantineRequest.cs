namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for resolving a quarantined goods receipt line.
/// </summary>
public sealed record ResolveQuarantineRequest
{
    /// <summary>
    /// Gets the resolution status. Required. One of: Accepted, Rejected.
    /// </summary>
    public required string Resolution { get; init; }

    /// <summary>
    /// Gets the resolution note. Optional, max 2000 characters.
    /// </summary>
    public string? Note { get; init; }
}
