using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Warehouse.Infrastructure.Sequences;

namespace Warehouse.Infrastructure.Tests.Sequences;

/// <summary>
/// Integration tests for <see cref="SequenceGenerator.NextBatchNumberAsync"/> covering
/// format output, per-product counters, monthly reset, and argument validation.
/// Runs against the Docker SQL Server instance.
/// </summary>
[TestFixture]
[Category("SDD-INFRA-003")]
public sealed class NextBatchNumberAsyncTests
{
    private const string ConnectionString =
        "Server=localhost,1433;Database=Warehouse;User Id=sa;Password=Warehouse@Dev123;TrustServerCertificate=True";

    private TestSequenceDbContext _context = null!;
    private IReadOnlyDictionary<string, SequenceDefinition> _definitions = null!;
    private SequenceGenerator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<TestSequenceDbContext> options = new DbContextOptionsBuilder<TestSequenceDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        _context = new TestSequenceDbContext(options);
        _definitions = SequenceDefinitions.GetBuiltInDefinitions();
        _sut = new SequenceGenerator(_context, _definitions);

        _context.Database.ExecuteSqlRaw(
            "DELETE FROM [infrastructure].[Sequences] WHERE [SequenceKey] = 'BATCH'");
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task NextBatchNumberAsync_ValidProductCode_ReturnsFormattedBatchNumber()
    {
        // Arrange
        string monthSegment = DateTime.UtcNow.ToString("yyyyMM", CultureInfo.InvariantCulture);

        // Act
        string result = await _sut.NextBatchNumberAsync("PROD-000001", CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo($"PROD-000001-{monthSegment}-001"));
    }

    [Test]
    public async Task NextBatchNumberAsync_DifferentProducts_IndependentCounters()
    {
        // Arrange
        string monthSegment = DateTime.UtcNow.ToString("yyyyMM", CultureInfo.InvariantCulture);

        // Act
        string resultA = await _sut.NextBatchNumberAsync("PROD-001", CancellationToken.None);
        string resultB = await _sut.NextBatchNumberAsync("PROD-002", CancellationToken.None);

        // Assert — both should be counter 001 (independent sequences)
        Assert.Multiple(() =>
        {
            Assert.That(resultA, Is.EqualTo($"PROD-001-{monthSegment}-001"));
            Assert.That(resultB, Is.EqualTo($"PROD-002-{monthSegment}-001"));
        });
    }

    [Test]
    public async Task NextBatchNumberAsync_NewMonth_ResetsCounter()
    {
        // Arrange — insert a row for last month with a high counter
        DateTime lastMonth = DateTime.UtcNow.AddMonths(-1);
        string lastMonthSegment = lastMonth.ToString("yyyyMM", CultureInfo.InvariantCulture);
        string lastMonthKey = $"BATCH:PROD-RESET:{lastMonthSegment}";

        _context.Database.ExecuteSqlRaw(
            "INSERT INTO [infrastructure].[Sequences] (CompositeKey, SequenceKey, CurrentValue, ResetPolicy) VALUES ({0}, {1}, {2}, {3})",
            lastMonthKey, "BATCH", 25, "Monthly");

        // Act — this month's call should start at 1
        string result = await _sut.NextBatchNumberAsync("PROD-RESET", CancellationToken.None);

        // Assert
        Assert.That(result, Does.EndWith("-001"));
    }

    [Test]
    public void NextBatchNumberAsync_NullProductCode_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.That(
            async () => await _sut.NextBatchNumberAsync(null!, CancellationToken.None),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void NextBatchNumberAsync_EmptyProductCode_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.That(
            async () => await _sut.NextBatchNumberAsync("", CancellationToken.None),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public async Task NextBatchNumberAsync_ProductCodeWithHyphens_HandledCorrectly()
    {
        // Arrange
        string monthSegment = DateTime.UtcNow.ToString("yyyyMM", CultureInfo.InvariantCulture);

        // Act
        string result = await _sut.NextBatchNumberAsync("PROD-001-A", CancellationToken.None);

        // Assert — product code with hyphens should work correctly
        Assert.That(result, Is.EqualTo($"PROD-001-A-{monthSegment}-001"));
    }
}
