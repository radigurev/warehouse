namespace Warehouse.ServiceModel.Requests.Auth;

/// <summary>
/// Request payload for updating user profile fields.
/// </summary>
public sealed record UpdateUserRequest
{
    /// <summary>
    /// Gets the updated email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the updated first name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Gets the updated last name.
    /// </summary>
    public required string LastName { get; init; }
}
