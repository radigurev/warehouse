using Warehouse.Auth.API.Interfaces;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// BCrypt implementation of password hashing and verification.
/// <para>See <see cref="IPasswordHasher"/>.</para>
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <inheritdoc />
    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
