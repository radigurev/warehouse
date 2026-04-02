namespace Warehouse.ServiceModel.Requests.Auth;

/// <summary>
/// Request payload for changing a user password.
/// </summary>
public sealed record ChangePasswordRequest
{
    /// <summary>
    /// Gets the current password for verification.
    /// </summary>
    public required string CurrentPassword { get; init; }

    /// <summary>
    /// Gets the new password.
    /// </summary>
    public required string NewPassword { get; init; }
}
