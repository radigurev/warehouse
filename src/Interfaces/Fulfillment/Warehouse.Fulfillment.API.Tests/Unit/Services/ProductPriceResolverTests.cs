using Warehouse.Fulfillment.API.Services;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.Fulfillment.DBModel.Models;

namespace Warehouse.Fulfillment.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="ProductPriceResolver"/> covering the temporal-validity
/// resolution algorithm (§2.3 steps 2-3 and the §2.5 diagnostic resolver sharing the same logic).
/// <para>Linked to CHG-FEAT-007 §2.3, §2.5.</para>
/// </summary>
[TestFixture]
[Category("CHG-FEAT-007")]
public sealed class ProductPriceResolverTests : FulfillmentTestBase
{
    private ProductPriceResolver _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new ProductPriceResolver(Context);
    }

    /// <summary>CHG-FEAT-007 §2.3 — one open-ended active price is returned.</summary>
    [Test]
    public async Task ResolveAsync_SingleActivePrice_ReturnsThatPrice()
    {
        // Arrange
        await SeedPriceAsync(productId: 100, currency: "USD", price: 19.99m, validFrom: null, validTo: null);

        // Act
        ProductPrice? result = await _sut.ResolveAsync(100, "USD", DateTime.UtcNow, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.UnitPrice, Is.EqualTo(19.99m));
        });
    }

    /// <summary>CHG-FEAT-007 §2.3 — bounded window covering UtcNow matches.</summary>
    [Test]
    public async Task ResolveAsync_ValidFromInPast_ValidToInFuture_ReturnsPrice()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        await SeedPriceAsync(productId: 100, currency: "USD", price: 10m,
            validFrom: now.AddDays(-5), validTo: now.AddDays(5));

        // Act
        ProductPrice? result = await _sut.ResolveAsync(100, "USD", now, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    /// <summary>CHG-FEAT-007 §2.3 — future-effective prices are excluded.</summary>
    [Test]
    public async Task ResolveAsync_ValidFromInFuture_DoesNotMatch()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        await SeedPriceAsync(productId: 100, currency: "USD", price: 10m,
            validFrom: now.AddDays(1), validTo: null);

        // Act
        ProductPrice? result = await _sut.ResolveAsync(100, "USD", now, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>CHG-FEAT-007 §2.3 — expired prices are excluded.</summary>
    [Test]
    public async Task ResolveAsync_ValidToInPast_DoesNotMatch()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        await SeedPriceAsync(productId: 100, currency: "USD", price: 10m,
            validFrom: now.AddDays(-10), validTo: now.AddDays(-1));

        // Act
        ProductPrice? result = await _sut.ResolveAsync(100, "USD", now, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>CHG-FEAT-007 §2.3 step 3 — among multiple matches, the row with the most recent concrete ValidFrom wins.</summary>
    [Test]
    public async Task ResolveAsync_MultipleMatches_PicksMostRecentValidFrom()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        await SeedPriceAsync(productId: 100, currency: "USD", price: 10m,
            validFrom: now.AddDays(-30), validTo: null);
        await SeedPriceAsync(productId: 100, currency: "USD", price: 15m,
            validFrom: now.AddDays(-5), validTo: null);
        await SeedPriceAsync(productId: 100, currency: "USD", price: 12m,
            validFrom: now.AddDays(-15), validTo: null);

        // Act
        ProductPrice? result = await _sut.ResolveAsync(100, "USD", now, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.UnitPrice, Is.EqualTo(15m));
        });
    }

    /// <summary>CHG-FEAT-007 §2.3 step 3 — null ValidFrom ranks below any concrete date.</summary>
    [Test]
    public async Task ResolveAsync_MultipleMatches_NullValidFromTreatedAsOldest()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        await SeedPriceAsync(productId: 100, currency: "USD", price: 10m,
            validFrom: null, validTo: null);
        await SeedPriceAsync(productId: 100, currency: "USD", price: 20m,
            validFrom: now.AddDays(-365), validTo: null);

        // Act
        ProductPrice? result = await _sut.ResolveAsync(100, "USD", now, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.UnitPrice, Is.EqualTo(20m), "Concrete -365d ValidFrom MUST outrank null ValidFrom.");
        });
    }

    /// <summary>CHG-FEAT-007 §2.3 — zero matches returns null so callers can surface FULF_PRICE_NOT_FOUND.</summary>
    [Test]
    public async Task ResolveAsync_NoMatch_ReturnsNull()
    {
        // Arrange
        // (no rows seeded)

        // Act
        ProductPrice? result = await _sut.ResolveAsync(100, "USD", DateTime.UtcNow, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>CHG-FEAT-007 §2.3 — validity is computed against UTC; a local-time price far in the past MUST still be considered expired if ValidTo is UTC in the past.</summary>
    [Test]
    public async Task ResolveAsync_UsesUtcNow_NotLocalTime()
    {
        // Arrange
        DateTime utcNow = DateTime.UtcNow;
        // A price that expired exactly one hour ago in UTC.
        await SeedPriceAsync(productId: 100, currency: "USD", price: 5m,
            validFrom: utcNow.AddDays(-10), validTo: utcNow.AddHours(-1));

        // Act
        // Passing UTC anchor — the resolver MUST NOT translate to local time.
        ProductPrice? result = await _sut.ResolveAsync(100, "USD", utcNow, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null, "Expired UTC prices MUST NOT match when onUtc is UTC.");
    }

    /// <summary>CHG-FEAT-007 §2.3 — currency filter is exact; other currencies are excluded.</summary>
    [Test]
    public async Task ResolveAsync_WrongCurrency_DoesNotMatchOtherCurrencies()
    {
        // Arrange
        await SeedPriceAsync(productId: 100, currency: "USD", price: 10m, validFrom: null, validTo: null);

        // Act
        ProductPrice? result = await _sut.ResolveAsync(100, "EUR", DateTime.UtcNow, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Seeds a <see cref="ProductPrice"/> row directly via the shared Context.
    /// </summary>
    private async Task<ProductPrice> SeedPriceAsync(
        int productId,
        string currency,
        decimal price,
        DateTime? validFrom,
        DateTime? validTo)
    {
        ProductPrice entity = new()
        {
            ProductId = productId,
            CurrencyCode = currency,
            UnitPrice = price,
            ValidFrom = validFrom,
            ValidTo = validTo,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };
        Context.ProductPrices.Add(entity);
        await Context.SaveChangesAsync(CancellationToken.None);
        return entity;
    }
}
