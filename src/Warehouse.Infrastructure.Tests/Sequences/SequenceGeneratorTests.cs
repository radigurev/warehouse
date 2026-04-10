using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Warehouse.Infrastructure.Sequences;

namespace Warehouse.Infrastructure.Tests.Sequences;

/// <summary>
/// Integration tests for <see cref="SequenceGenerator.NextAsync"/> covering format output,
/// daily/never reset policies, counter increment behavior, argument validation, and padding.
/// Runs against the Docker SQL Server instance.
/// </summary>
[TestFixture]
[Category("SDD-INFRA-003")]
public sealed class SequenceGeneratorTests
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

        // Clean test data — delete rows created by previous test runs
        _context.Database.ExecuteSqlRaw(
            "DELETE FROM [infrastructure].[Sequences] WHERE [SequenceKey] IN ('PO','GR','SR','SO','PL','PKG','SHP','CR','SUPP','PROD','CUST','BATCH')");
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task NextAsync_ValidSequenceKey_ReturnsFormattedString()
    {
        // Arrange & Act
        string result = await _sut.NextAsync("PO", CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.That(result, Does.StartWith("PO-"));
    }

    [Test]
    public async Task NextAsync_DailyReset_IncludesDateSegment()
    {
        // Arrange
        string todayDate = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        // Act
        string result = await _sut.NextAsync("GR", CancellationToken.None);

        // Assert
        Assert.That(result, Does.Contain(todayDate));
    }

    [Test]
    public async Task NextAsync_NeverReset_OmitsDateSegment()
    {
        // Arrange
        string todayDate = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        // Act
        string result = await _sut.NextAsync("CUST", CancellationToken.None);

        // Assert
        Assert.That(result, Does.Not.Contain(todayDate));
        Assert.That(result, Does.Match(@"^CUST-\d{6}$"));
    }

    [Test]
    public async Task NextAsync_FirstCallForKey_ReturnsCounterOne()
    {
        // Arrange & Act
        string result = await _sut.NextAsync("SR", CancellationToken.None);

        // Assert
        Assert.That(result, Does.EndWith("-0001"));
    }

    [Test]
    public async Task NextAsync_SecondCallForSameKey_ReturnsCounterTwo()
    {
        // Arrange
        await _sut.NextAsync("SO", CancellationToken.None);

        // Act
        string result = await _sut.NextAsync("SO", CancellationToken.None);

        // Assert
        Assert.That(result, Does.EndWith("-0002"));
    }

    [Test]
    public async Task NextAsync_DailyReset_NewDateResetsCounter()
    {
        // Arrange — insert a row for yesterday's composite key with a high counter
        string yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string yesterdayKey = $"PL:{yesterday}";

        _context.Database.ExecuteSqlRaw(
            "INSERT INTO [infrastructure].[Sequences] (CompositeKey, SequenceKey, CurrentValue, ResetPolicy) VALUES ({0}, {1}, {2}, {3})",
            yesterdayKey, "PL", 99, "Daily");

        // Act — today's call should start at 1 (new composite key for today)
        string result = await _sut.NextAsync("PL", CancellationToken.None);

        // Assert
        Assert.That(result, Does.EndWith("-0001"));
    }

    [Test]
    public async Task NextAsync_MonthlyReset_NewMonthResetsCounter()
    {
        // Arrange — insert a row for last month's BATCH composite key
        DateTime lastMonth = DateTime.UtcNow.AddMonths(-1);
        string lastMonthSegment = lastMonth.ToString("yyyyMM", CultureInfo.InvariantCulture);
        string lastMonthKey = $"BATCH:TESTPROD:{lastMonthSegment}";

        _context.Database.ExecuteSqlRaw(
            "INSERT INTO [infrastructure].[Sequences] (CompositeKey, SequenceKey, CurrentValue, ResetPolicy) VALUES ({0}, {1}, {2}, {3})",
            lastMonthKey, "BATCH", 50, "Monthly");

        // Act — this month's call should start at 1
        string result = await _sut.NextBatchNumberAsync("TESTPROD", CancellationToken.None);

        // Assert
        Assert.That(result, Does.EndWith("-001"));
    }

    [Test]
    public void NextAsync_UnknownSequenceKey_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.That(
            async () => await _sut.NextAsync("UNKNOWN", CancellationToken.None),
            Throws.InstanceOf<ArgumentException>()
                .With.Message.Contains("UNKNOWN"));
    }

    [Test]
    public void NextAsync_NullSequenceKey_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.That(
            async () => await _sut.NextAsync(null!, CancellationToken.None),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void NextAsync_EmptySequenceKey_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.That(
            async () => await _sut.NextAsync("", CancellationToken.None),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public async Task NextAsync_OutputMatchesExpectedFormat_PO()
    {
        // Arrange
        string todayDate = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        // Act
        string result = await _sut.NextAsync("PO", CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo($"PO-{todayDate}-0001"));
    }

    [Test]
    public async Task NextAsync_OutputMatchesExpectedFormat_CUST()
    {
        // Arrange & Act
        string result = await _sut.NextAsync("CUST", CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo("CUST-000001"));
    }

    [Test]
    public async Task NextAsync_OutputMatchesExpectedFormat_SUPP()
    {
        // Arrange & Act
        string result = await _sut.NextAsync("SUPP", CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo("SUPP-000001"));
    }

    [Test]
    public async Task NextAsync_PaddingAppliedCorrectly()
    {
        // Arrange & Act
        string poResult = await _sut.NextAsync("PO", CancellationToken.None);
        string custResult = await _sut.NextAsync("CUST", CancellationToken.None);

        // Assert — PO has 4-digit padding, CUST has 6-digit padding
        Assert.Multiple(() =>
        {
            Assert.That(Regex.IsMatch(poResult, @"-\d{4}$"), Is.True, "PO should have 4-digit counter");
            Assert.That(Regex.IsMatch(custResult, @"-\d{6}$"), Is.True, "CUST should have 6-digit counter");
        });
    }
}
