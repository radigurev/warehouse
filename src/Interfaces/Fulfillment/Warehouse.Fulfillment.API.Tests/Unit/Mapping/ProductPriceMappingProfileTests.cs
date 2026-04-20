using AutoMapper;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.Mapping.Profiles.Fulfillment;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Tests.Unit.Mapping;

/// <summary>
/// Unit tests for <see cref="ProductPriceMappingProfile"/> verifying mapping completeness
/// and field-level propagation between entity, DTO, and request models.
/// <para>Linked to CHG-FEAT-007 §7 AutoMapper Profile requirement.</para>
/// </summary>
[TestFixture]
[Category("CHG-FEAT-007")]
public sealed class ProductPriceMappingProfileTests
{
    private MapperConfiguration _config = null!;
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        _config = new MapperConfiguration(cfg => cfg.AddProfile<ProductPriceMappingProfile>());
        _mapper = _config.CreateMapper();
    }

    /// <summary>CHG-FEAT-007 §7 — profile configuration must validate with AssertConfigurationIsValid.</summary>
    [Test]
    public void ProductPriceMappingProfile_IsValid()
    {
        // Arrange + Act
        // Assert (no exception means configuration is valid)
        Assert.DoesNotThrow(() => _config.AssertConfigurationIsValid());
    }

    /// <summary>CHG-FEAT-007 §7 — entity to DTO copies every public field exposed on the DTO contract.</summary>
    [Test]
    public void ProductPriceDto_Map_PopulatesAllFields()
    {
        // Arrange
        DateTime created = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime modified = new(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc);
        ProductPrice entity = new()
        {
            Id = 7,
            ProductId = 100,
            CurrencyCode = "USD",
            UnitPrice = 99.9900m,
            ValidFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ValidTo = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = created,
            CreatedByUserId = 11,
            ModifiedAtUtc = modified,
            ModifiedByUserId = 22
        };

        // Act
        ProductPriceDto dto = _mapper.Map<ProductPriceDto>(entity);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(7));
            Assert.That(dto.ProductId, Is.EqualTo(100));
            Assert.That(dto.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(dto.UnitPrice, Is.EqualTo(99.99m));
            Assert.That(dto.ValidFrom, Is.EqualTo(entity.ValidFrom));
            Assert.That(dto.ValidTo, Is.EqualTo(entity.ValidTo));
            Assert.That(dto.CreatedAtUtc, Is.EqualTo(created));
            Assert.That(dto.CreatedByUserId, Is.EqualTo(11));
            Assert.That(dto.ModifiedAtUtc, Is.EqualTo(modified));
            Assert.That(dto.ModifiedByUserId, Is.EqualTo(22));
        });
    }

    /// <summary>CHG-FEAT-007 §7 — CreateProductPriceRequest maps to a new ProductPrice with audit fields ignored (set by service).</summary>
    [Test]
    public void CreateProductPriceRequest_Map_PropagatesCoreFields()
    {
        // Arrange
        CreateProductPriceRequest request = new()
        {
            ProductId = 100,
            CurrencyCode = "EUR",
            UnitPrice = 12.50m,
            ValidFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ValidTo = null
        };

        // Act
        ProductPrice entity = _mapper.Map<ProductPrice>(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(entity.ProductId, Is.EqualTo(100));
            Assert.That(entity.CurrencyCode, Is.EqualTo("EUR"));
            Assert.That(entity.UnitPrice, Is.EqualTo(12.50m));
            Assert.That(entity.ValidFrom, Is.EqualTo(request.ValidFrom));
            Assert.That(entity.ValidTo, Is.Null);
            Assert.That(entity.Id, Is.EqualTo(0), "Id MUST NOT be mapped from request (IDENTITY).");
        });
    }
}
