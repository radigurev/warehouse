using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Services;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for bill of materials operations: CRUD and line management.
/// <para>Links to specification SDD-INV-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-001")]
public sealed class BomServiceTests : InventoryTestBase
{
    private BomService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new BomService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedBom()
    {
        // Arrange
        Product parent = await SeedProductAsync(code: "BOM-PARENT").ConfigureAwait(false);
        Product child = await SeedProductAsync(code: "BOM-CHILD").ConfigureAwait(false);
        CreateBomRequest request = new()
        {
            ParentProductId = parent.Id,
            Name = "Assembly V1",
            Lines = [new CreateBomLineRequest { ChildProductId = child.Id, Quantity = 2m }]
        };

        // Act
        Result<BomDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Assembly V1");
        result.Value.Lines.Should().HaveCount(1);
    }

    [Test]
    public async Task CreateAsync_InvalidParentProduct_ReturnsError()
    {
        // Arrange
        CreateBomRequest request = new()
        {
            ParentProductId = 9999,
            Name = "Invalid BOM",
            Lines = []
        };

        // Act
        Result<BomDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PRODUCT");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task GetByIdAsync_ExistingBom_ReturnsBom()
    {
        // Arrange
        Product parent = await SeedProductAsync(code: "BOM-GET-P").ConfigureAwait(false);
        BillOfMaterials bom = new()
        {
            ParentProductId = parent.Id,
            Name = "Test BOM",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };
        Context.BillOfMaterials.Add(bom);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result<BomDto> result = await _sut.GetByIdAsync(bom.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(bom.Id);
    }

    [Test]
    public async Task GetByIdAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result<BomDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("BOM_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task AddLineAsync_DuplicateChild_ReturnsConflict()
    {
        // Arrange
        Product parent = await SeedProductAsync(code: "BOM-DUP-P").ConfigureAwait(false);
        Product child = await SeedProductAsync(code: "BOM-DUP-C").ConfigureAwait(false);
        BillOfMaterials bom = new()
        {
            ParentProductId = parent.Id,
            Name = "BOM with line",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };
        bom.Lines.Add(new BomLine { ChildProductId = child.Id, Quantity = 1m });
        Context.BillOfMaterials.Add(bom);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        AddBomLineRequest request = new() { ChildProductId = child.Id, Quantity = 3m };

        // Act
        Result<BomDto> result = await _sut.AddLineAsync(bom.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_BOM_LINE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task RemoveLineAsync_ExistingLine_RemovesSuccessfully()
    {
        // Arrange
        Product parent = await SeedProductAsync(code: "BOM-REM-P").ConfigureAwait(false);
        Product child = await SeedProductAsync(code: "BOM-REM-C").ConfigureAwait(false);
        BillOfMaterials bom = new()
        {
            ParentProductId = parent.Id,
            Name = "BOM remove test",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };
        BomLine line = new() { ChildProductId = child.Id, Quantity = 5m };
        bom.Lines.Add(line);
        Context.BillOfMaterials.Add(bom);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result result = await _sut.RemoveLineAsync(bom.Id, line.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_ExistingBom_DeletesSuccessfully()
    {
        // Arrange
        Product parent = await SeedProductAsync(code: "BOM-DEL-P").ConfigureAwait(false);
        BillOfMaterials bom = new()
        {
            ParentProductId = parent.Id,
            Name = "Delete BOM",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };
        Context.BillOfMaterials.Add(bom);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAsync(bom.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
