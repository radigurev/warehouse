namespace Warehouse.Auth.API.Interfaces;

/// <summary>
/// Defines operations for hashing and verifying passwords using BCrypt.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored hash.
    /// </summary>
    bool Verify(string password, string hash);
}
