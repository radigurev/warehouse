using FluentAssertions;
using Moq;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for supplier lifecycle operations: CRUD, search, soft-delete, and reactivation.
/// <para>Links to specification SDD-PURCH-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class SupplierServiceTests : PurchasingTestBase
{
    private Mock<IPurchaseEventService> _mockEventService = null!;
    private SupplierService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockEventService = new Mock<IPurchaseEventService>();
        _sut = new SupplierService(Context, Mapper, _mockEventService.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedSupplier()
    {
        // Arrange
        SupplierCategory category = await SeedCategoryAsync().ConfigureAwait(false);
        CreateSupplierRequest request = new()
        {
            Name = "Acme Supplies",
            Code = "ACME-001",
            TaxId = "BG123456789",
            CategoryId = category.Id,
            PaymentTermDays = 30,
            Notes = "Important supplier"
        };

        // Act
        Result<SupplierDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Acme Supplies");
        result.Value.Code.Should().Be("ACME-001");
        result.Value.TaxId.Should().Be("BG123456789");
        result.Value.IsActive.Should().BeTrue();
        result.Value.CategoryName.Should().Be("Raw Materials");
    }

    [Test]
    public async Task CreateAsync_DuplicateCode_ReturnsConflictError()
    {
        // Arrange
        await SeedSupplierAsync(code: "DUPE-001").ConfigureAwait(false);
        CreateSupplierRequest request = new() { Name = "New Supplier", Code = "DUPE-001" };

        // Act
        Result<SupplierDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_SUPPLIER_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_DuplicateTaxId_ReturnsConflictError()
    {
        // Arrange
        await SeedSupplierAsync(code: "EXIST-001", taxId: "TAX-DUPE").ConfigureAwait(false);
        CreateSupplierRequest request = new() { Name = "New Supplier", Code = "NEW-001", TaxId = "TAX-DUPE" };

        // Act
        Result<SupplierDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_TAX_ID");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_InvalidCategoryId_ReturnsValidationError()
    {
        // Arrange
        CreateSupplierRequest request = new() { Name = "New Supplier", Code = "NEW-001", CategoryId = 9999 };

        // Act
        Result<SupplierDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CATEGORY");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    [Description("InMemory provider cannot translate Convert.ToInt32 used in code generation. Verifying with pre-seeded SUPP- supplier to force MaxAsync path is not feasible. This test verifies that a provided code is accepted instead.")]
    public async Task CreateAsync_WithExplicitCode_UsesProvidedCode()
    {
        // Arrange
        CreateSupplierRequest request = new() { Name = "Explicit Code Supplier", Code = "SUPP-000042" };

        // Act
        Result<SupplierDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("SUPP-000042");
    }

    [Test]
    public async Task UpdateAsync_ValidRequest_ReturnsUpdatedSupplier()
    {
        // Arrange
        SupplierCategory category = await SeedCategoryAsync().ConfigureAwait(false);
        Supplier supplier = await SeedSupplierAsync(code: "UPD-001").ConfigureAwait(false);
        UpdateSupplierRequest request = new()
        {
            Name = "Updated Name",
            TaxId = "NEW-TAX",
            CategoryId = category.Id,
            PaymentTermDays = 60,
            Notes = "Updated notes"
        };

        // Act
        Result<SupplierDetailDto> result = await _sut.UpdateAsync(supplier.Id, request, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
        result.Value.TaxId.Should().Be("NEW-TAX");
    }

    [Test]
    public async Task UpdateAsync_SoftDeletedSupplier_ReturnsNotFound()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "DEL-001", isDeleted: true, isActive: false).ConfigureAwait(false);
        UpdateSupplierRequest request = new() { Name = "Will Not Update" };

        // Act
        Result<SupplierDetailDto> result = await _sut.UpdateAsync(supplier.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUPPLIER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task UpdateAsync_DuplicateTaxId_ReturnsConflictError()
    {
        // Arrange
        await SeedSupplierAsync(code: "OTHER-001", taxId: "TAKEN-TAX").ConfigureAwait(false);
        Supplier supplier = await SeedSupplierAsync(code: "TARGET-001").ConfigureAwait(false);
        UpdateSupplierRequest request = new() { Name = "Target Supplier", TaxId = "TAKEN-TAX" };

        // Act
        Result<SupplierDetailDto> result = await _sut.UpdateAsync(supplier.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_TAX_ID");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task GetByIdAsync_ExistingSupplier_ReturnsSupplierWithDetails()
    {
        // Arrange
        SupplierCategory category = await SeedCategoryAsync().ConfigureAwait(false);
        Supplier supplier = await SeedSupplierAsync(code: "GET-001", categoryId: category.Id).ConfigureAwait(false);

        // Act
        Result<SupplierDetailDto> result = await _sut.GetByIdAsync(supplier.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("GET-001");
        result.Value.CategoryName.Should().Be("Raw Materials");
    }

    [Test]
    public async Task GetByIdAsync_SoftDeletedSupplier_ReturnsNotFound()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "SOFT-DEL", isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result<SupplierDetailDto> result = await _sut.GetByIdAsync(supplier.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUPPLIER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task SearchAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        await SeedSupplierAsync(code: "FILT-001", name: "Alpha Supplier").ConfigureAwait(false);
        await SeedSupplierAsync(code: "FILT-002", name: "Beta Supplier").ConfigureAwait(false);
        await SeedSupplierAsync(code: "OTHER-001", name: "Gamma Supplier").ConfigureAwait(false);
        SearchSuppliersRequest request = new() { Code = "FILT" };

        // Act
        Result<PaginatedResponse<SupplierDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task SearchAsync_DefaultSort_SortsByNameAscending()
    {
        // Arrange
        await SeedSupplierAsync(code: "S-003", name: "Zulu").ConfigureAwait(false);
        await SeedSupplierAsync(code: "S-001", name: "Alpha").ConfigureAwait(false);
        await SeedSupplierAsync(code: "S-002", name: "Mike").ConfigureAwait(false);
        SearchSuppliersRequest request = new() { Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<SupplierDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        IReadOnlyList<SupplierDto> items = result.Value!.Items;
        items[0].Name.Should().Be("Alpha");
        items[1].Name.Should().Be("Mike");
        items[2].Name.Should().Be("Zulu");
    }

    [Test]
    public async Task DeactivateAsync_ActiveSupplier_SetsIsDeletedAndDeletedAt()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "DEACT-001").ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(supplier.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await Context.Entry(supplier).ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        supplier.IsDeleted.Should().BeTrue();
        supplier.IsActive.Should().BeFalse();
        supplier.DeletedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task DeactivateAsync_SupplierWithOpenPOs_ReturnsConflict()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "OPENPO-001").ConfigureAwait(false);
        await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Draft)).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(supplier.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUPPLIER_HAS_OPEN_POS");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeactivateAsync_AlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "ALRDEL-001", isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(supplier.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUPPLIER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task ReactivateAsync_SoftDeletedSupplier_ClearsDeletedFlags()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "REACT-001", isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result<SupplierDetailDto> result = await _sut.ReactivateAsync(supplier.Id, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();
        await Context.Entry(supplier).ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        supplier.IsDeleted.Should().BeFalse();
        supplier.DeletedAtUtc.Should().BeNull();
    }

    [Test]
    public async Task ReactivateAsync_ConflictingCode_ReturnsConflictError()
    {
        // Arrange
        await SeedSupplierAsync(code: "CONFLICT-CODE").ConfigureAwait(false);
        Supplier deletedSupplier = await SeedSupplierAsync(code: "CONFLICT-CODE-2", isDeleted: true, isActive: false).ConfigureAwait(false);
        deletedSupplier.Code = "CONFLICT-CODE";
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result<SupplierDetailDto> result = await _sut.ReactivateAsync(deletedSupplier.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_SUPPLIER_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ReactivateAsync_ConflictingTaxId_ReturnsConflictError()
    {
        // Arrange
        await SeedSupplierAsync(code: "ACTIVE-TAX", taxId: "SHARED-TAX").ConfigureAwait(false);
        Supplier deletedSupplier = await SeedSupplierAsync(code: "DELETED-TAX", taxId: "SHARED-TAX", isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result<SupplierDetailDto> result = await _sut.ReactivateAsync(deletedSupplier.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_TAX_ID");
        result.StatusCode.Should().Be(409);
    }
}
