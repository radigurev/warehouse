using Moq;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="PackingService"/> covering parcel CRUD, item management, and packing completion.
/// <para>Linked to SDD-FULF-001 section 2.3.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class PackingServiceTests : FulfillmentTestBase
{
    private Mock<IFulfillmentEventService> _mockEventService = null!;
    private PackingService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockEventService = new Mock<IFulfillmentEventService>();
        _sut = new PackingService(Context, Mapper, _mockEventService.Object);
    }

    [Test]
    public async Task CreateParcelAsync_PickingSO_ReturnsCreatedParcel()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        await SeedPickListAsync(so.Id, so.Lines.First().Id, status: nameof(PickListStatus.Completed));
        CreateParcelRequest request = new() { Weight = 2.5m, Notes = "Fragile" };

        // Act
        Result<ParcelDto> result = await _sut.CreateParcelAsync(so.Id, request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.ParcelNumber, Does.StartWith("PKG-"));
            Assert.That(result.Value.Weight, Is.EqualTo(2.5m));
        });
    }

    [Test]
    public async Task CreateParcelAsync_GeneratesParcelNumber()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        await SeedPickListAsync(so.Id, so.Lines.First().Id, status: nameof(PickListStatus.Completed));
        CreateParcelRequest request = new();

        // Act
        Result<ParcelDto> result = await _sut.CreateParcelAsync(so.Id, request, 1, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.ParcelNumber, Does.Match(@"PKG-\d{8}-\d{4}"));
    }

    [Test]
    public async Task CreateParcelAsync_DraftSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        CreateParcelRequest request = new();

        // Act
        Result<ParcelDto> result = await _sut.CreateParcelAsync(so.Id, request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_PACKABLE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CreateParcelAsync_PickingSOWithNoCompletedPicks_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        CreateParcelRequest request = new();

        // Act
        Result<ParcelDto> result = await _sut.CreateParcelAsync(so.Id, request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_PACKABLE"));
        });
    }

    [Test]
    public async Task CreateParcelAsync_NonExistentSO_ReturnsNotFound()
    {
        // Arrange
        CreateParcelRequest request = new();

        // Act
        Result<ParcelDto> result = await _sut.CreateParcelAsync(999, request, 1, CancellationToken.None);

        // Assert
        Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_FOUND"));
    }

    [Test]
    public async Task AddItemAsync_ValidRequest_ReturnsCreatedItem()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id, requestedQuantity: 10m, status: nameof(PickListStatus.Completed));
        Parcel parcel = await SeedParcelAsync(so.Id);
        int pickLineId = pl.Lines.First().Id;
        AddParcelItemRequest request = new() { ProductId = 100, Quantity = 5m, PickListLineId = pickLineId };

        // Act
        Result<ParcelItemDto> result = await _sut.AddItemAsync(so.Id, parcel.Id, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.ProductId, Is.EqualTo(100));
            Assert.That(result.Value.Quantity, Is.EqualTo(5m));
        });
    }

    [Test]
    public async Task AddItemAsync_OverPack_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking), orderedQuantity: 10m);
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id, requestedQuantity: 10m, status: nameof(PickListStatus.Completed));
        Parcel parcel = await SeedParcelAsync(so.Id);
        int pickLineId = pl.Lines.First().Id;
        AddParcelItemRequest request = new() { ProductId = 100, Quantity = 15m, PickListLineId = pickLineId };

        // Act
        Result<ParcelItemDto> result = await _sut.AddItemAsync(so.Id, parcel.Id, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("OVER_PACK"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task AddItemAsync_UnpickedLine_ReturnsError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id, requestedQuantity: 10m, status: nameof(PickListStatus.Pending));
        await SeedPickListAsync(so.Id, so.Lines.First().Id, requestedQuantity: 10m, status: nameof(PickListStatus.Completed));
        Parcel parcel = await SeedParcelAsync(so.Id);
        int unpickedLineId = pl.Lines.First().Id;
        AddParcelItemRequest request = new() { ProductId = 100, Quantity = 5m, PickListLineId = unpickedLineId };

        // Act
        Result<ParcelItemDto> result = await _sut.AddItemAsync(so.Id, parcel.Id, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INVALID_PICK_LINE"));
        });
    }

    [Test]
    public async Task UpdateParcelAsync_ValidRequest_ReturnsUpdatedParcel()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        await SeedPickListAsync(so.Id, so.Lines.First().Id, status: nameof(PickListStatus.Completed));
        Parcel parcel = await SeedParcelAsync(so.Id, weight: 2.0m);
        UpdateParcelRequest request = new() { Weight = 5.0m, TrackingNumber = "TRK-001" };

        // Act
        Result<ParcelDto> result = await _sut.UpdateParcelAsync(so.Id, parcel.Id, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Weight, Is.EqualTo(5.0m));
        });
    }

    [Test]
    public async Task UpdateParcelAsync_ShippedSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        Parcel parcel = await SeedParcelAsync(so.Id);
        UpdateParcelRequest request = new() { Weight = 5.0m };

        // Act
        Result<ParcelDto> result = await _sut.UpdateParcelAsync(so.Id, parcel.Id, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("PARCEL_NOT_EDITABLE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task RemoveParcelAsync_ValidRequest_RemovesSuccessfully()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        Parcel parcel = await SeedParcelAsync(so.Id);

        // Act
        Result result = await _sut.RemoveParcelAsync(so.Id, parcel.Id, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task RemoveParcelAsync_ShippedSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        Parcel parcel = await SeedParcelAsync(so.Id);

        // Act
        Result result = await _sut.RemoveParcelAsync(so.Id, parcel.Id, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("PARCEL_NOT_EDITABLE"));
        });
    }

    [Test]
    public async Task RemoveItemAsync_ValidRequest_RemovesSuccessfully()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id, requestedQuantity: 10m, status: nameof(PickListStatus.Completed));
        Parcel parcel = await SeedParcelAsync(so.Id);
        ParcelItem item = await SeedParcelItemAsync(parcel.Id, pl.Lines.First().Id, productId: 100, quantity: 5m);

        // Act
        Result result = await _sut.RemoveItemAsync(so.Id, parcel.Id, item.Id, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task ListParcelsAsync_ExistingSO_ReturnsParcelsWithItems()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        await SeedParcelAsync(so.Id);
        await SeedParcelAsync(so.Id, weight: 3.0m);

        // Act
        Result<IReadOnlyList<ParcelDto>> result = await _sut.ListParcelsAsync(so.Id, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task PackingCompletion_AllLinesPacked_TransitionsSOToPacked()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking), orderedQuantity: 10m);
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id, requestedQuantity: 10m, status: nameof(PickListStatus.Completed));
        Parcel parcel = await SeedParcelAsync(so.Id);
        int pickLineId = pl.Lines.First().Id;
        AddParcelItemRequest request = new() { ProductId = 100, Quantity = 10m, PickListLineId = pickLineId };

        // Act
        await _sut.AddItemAsync(so.Id, parcel.Id, request, CancellationToken.None);

        // Assert
        await Context.Entry(so).ReloadAsync(CancellationToken.None);
        Assert.That(so.Status, Is.EqualTo(nameof(SalesOrderStatus.Packed)));
    }

    [Test]
    public async Task GetParcelByIdAsync_ExistingParcel_ReturnsParcel()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        Parcel parcel = await SeedParcelAsync(so.Id);

        // Act
        Result<ParcelDto> result = await _sut.GetParcelByIdAsync(so.Id, parcel.Id, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Id, Is.EqualTo(parcel.Id));
        });
    }

    [Test]
    public async Task GetParcelByIdAsync_NonExistentParcel_ReturnsNotFound()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));

        // Act
        Result<ParcelDto> result = await _sut.GetParcelByIdAsync(so.Id, 999, CancellationToken.None);

        // Assert
        Assert.That(result.ErrorCode, Is.EqualTo("PARCEL_NOT_FOUND"));
    }
}
