namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for inspecting a goods receipt line.
/// </summary>
public sealed record InspectLineRequest
{
    /// <summary>
    /// Gets the inspection status. Required. One of: Accepted, Rejected, Quarantined.
    /// </summary>
    public required string InspectionStatus { get; init; }

    /// <summary>
    /// Gets the inspection note. Optional, max 2000 characters.
    /// </summary>
    public string? InspectionNote { get; init; }
}
