using FluentAssertions;
using Warehouse.Auth.API.Services;

namespace Warehouse.Auth.API.Tests.Unit;

/// <summary>
/// Unit tests for BCrypt password hashing and verification.
/// </summary>
[TestFixture]
[Category("SDD-AUTH-001")]
public sealed class PasswordHasherTests
{
    private PasswordHasher _hasher = null!;

    [SetUp]
    public void SetUp()
    {
        _hasher = new PasswordHasher();
    }

    [Test]
    public void Hash_ReturnsNonEmptyString()
    {
        string hash = _hasher.Hash("Password123!");

        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void Hash_ReturnsBCryptFormat()
    {
        string hash = _hasher.Hash("Password123!");

        hash.Should().StartWith("$2");
    }

    [Test]
    public void Hash_SamePasswordProducesDifferentHashes()
    {
        string hash1 = _hasher.Hash("Password123!");
        string hash2 = _hasher.Hash("Password123!");

        hash1.Should().NotBe(hash2);
    }

    [Test]
    public void Hash_DifferentPasswordsProduceDifferentHashes()
    {
        string hash1 = _hasher.Hash("Password123!");
        string hash2 = _hasher.Hash("Different456!");

        hash1.Should().NotBe(hash2);
    }

    [Test]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        string hash = _hasher.Hash("Password123!");

        bool result = _hasher.Verify("Password123!", hash);

        result.Should().BeTrue();
    }

    [Test]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        string hash = _hasher.Hash("Password123!");

        bool result = _hasher.Verify("WrongPassword!", hash);

        result.Should().BeFalse();
    }

    [Test]
    public void Verify_EmptyPassword_ReturnsFalse()
    {
        string hash = _hasher.Hash("Password123!");

        bool result = _hasher.Verify("", hash);

        result.Should().BeFalse();
    }

    [Test]
    public void Verify_CaseSensitive()
    {
        string hash = _hasher.Hash("Password123!");

        bool result = _hasher.Verify("password123!", hash);

        result.Should().BeFalse();
    }
}
