using Microsoft.EntityFrameworkCore;

namespace Warehouse.Infrastructure.Tests.Sequences;

/// <summary>
/// Minimal <see cref="DbContext"/> for sequence generator integration tests.
/// Connects to the Docker SQL Server instance.
/// </summary>
public sealed class TestSequenceDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance with the provided options.
    /// </summary>
    public TestSequenceDbContext(DbContextOptions<TestSequenceDbContext> options)
        : base(options)
    {
    }
}
