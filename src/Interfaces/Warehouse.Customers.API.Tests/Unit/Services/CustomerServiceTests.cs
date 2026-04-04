using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Services;
using Warehouse.Customers.API.Tests.Fixtures;
using Warehouse.DBModel.Models.Customers;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Customers.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for customer lifecycle operations: CRUD, search, soft-delete, and reactivation.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CustomerServiceTests : CustomerTestBase
{
    private CustomerService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new CustomerService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedCustomer()
    {
        // Arrange
        CustomerCategory category = await SeedCategoryAsync().ConfigureAwait(false);
        CreateCustomerRequest request = new()
        {
            Name = "Acme Corp",
            Code = "ACME-001",
            TaxId = "BG123456789",
            CategoryId = category.Id,
            Notes = "Important customer"
        };

        // Act
        Result<CustomerDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Acme Corp");
        result.Value.Code.Should().Be("ACME-001");
        result.Value.TaxId.Should().Be("BG123456789");
        result.Value.IsActive.Should().BeTrue();
        result.Value.CategoryName.Should().Be("Wholesale");
    }

    [Test]
    public async Task CreateAsync_DuplicateCode_ReturnsConflictError()
    {
        // Arrange
        await SeedCustomerAsync(code: "DUPE-001").ConfigureAwait(false);
        CreateCustomerRequest request = new() { Name = "New Customer", Code = "DUPE-001" };

        // Act
        Result<CustomerDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CUSTOMER_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_DuplicateTaxId_ReturnsConflictError()
    {
        // Arrange
        await SeedCustomerAsync(code: "EXIST-001", taxId: "TAX-DUPE").ConfigureAwait(false);
        CreateCustomerRequest request = new() { Name = "New Customer", Code = "NEW-001", TaxId = "TAX-DUPE" };

        // Act
        Result<CustomerDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_TAX_ID");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_InvalidCategoryId_ReturnsValidationError()
    {
        // Arrange
        CreateCustomerRequest request = new() { Name = "New Customer", Code = "NEW-001", CategoryId = 9999 };

        // Act
        Result<CustomerDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CATEGORY");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    [Explicit("InMemory provider cannot translate Convert.ToInt32 in LINQ; covered by integration tests")]
    public async Task CreateAsync_NoCodeProvided_GeneratesCode()
    {
        // Arrange
        CreateCustomerRequest request = new() { Name = "Auto Code Customer" };

        // Act
        Result<CustomerDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().StartWith("CUST-");
        result.Value.Code.Should().Be("CUST-000001");
    }

    [Test]
    public async Task UpdateAsync_ValidRequest_ReturnsUpdatedCustomer()
    {
        // Arrange
        CustomerCategory category = await SeedCategoryAsync().ConfigureAwait(false);
        Customer customer = await SeedCustomerAsync(categoryId: category.Id).ConfigureAwait(false);
        UpdateCustomerRequest request = new()
        {
            Name = "Updated Name",
            TaxId = "NEW-TAX",
            CategoryId = category.Id,
            Notes = "Updated notes"
        };

        // Act
        Result<CustomerDetailDto> result = await _sut.UpdateAsync(customer.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
        result.Value.TaxId.Should().Be("NEW-TAX");
    }

    [Test]
    public async Task UpdateAsync_SoftDeletedCustomer_ReturnsNotFound()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync(isDeleted: true, isActive: false).ConfigureAwait(false);
        UpdateCustomerRequest request = new() { Name = "Ignored" };

        // Act
        Result<CustomerDetailDto> result = await _sut.UpdateAsync(customer.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CUSTOMER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task UpdateAsync_DuplicateTaxId_ReturnsConflictError()
    {
        // Arrange
        await SeedCustomerAsync(code: "OTHER-001", taxId: "TAX-TAKEN").ConfigureAwait(false);
        Customer customer = await SeedCustomerAsync(code: "MINE-001", taxId: "TAX-ORIG").ConfigureAwait(false);
        UpdateCustomerRequest request = new() { Name = "My Customer", TaxId = "TAX-TAKEN" };

        // Act
        Result<CustomerDetailDto> result = await _sut.UpdateAsync(customer.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_TAX_ID");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task GetByIdAsync_ExistingCustomer_ReturnsCustomerWithDetails()
    {
        // Arrange
        CustomerCategory category = await SeedCategoryAsync().ConfigureAwait(false);
        Customer customer = await SeedCustomerAsync(categoryId: category.Id).ConfigureAwait(false);
        await SeedAccountAsync(customer.Id, "USD", isPrimary: true).ConfigureAwait(false);
        await SeedAddressAsync(customer.Id, isDefault: true).ConfigureAwait(false);
        await SeedPhoneAsync(customer.Id, isPrimary: true).ConfigureAwait(false);
        await SeedEmailAsync(customer.Id, isPrimary: true).ConfigureAwait(false);

        // Act
        Result<CustomerDetailDto> result = await _sut.GetByIdAsync(customer.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(customer.Id);
        result.Value.Name.Should().Be("Test Customer");
        result.Value.CategoryName.Should().Be("Wholesale");
        result.Value.Accounts.Should().HaveCount(1);
        result.Value.Addresses.Should().HaveCount(1);
        result.Value.Phones.Should().HaveCount(1);
        result.Value.Emails.Should().HaveCount(1);
    }

    [Test]
    public async Task GetByIdAsync_SoftDeletedCustomer_ReturnsNotFound()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync(isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result<CustomerDetailDto> result = await _sut.GetByIdAsync(customer.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CUSTOMER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task SearchAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        await SeedCustomerAsync(code: "CUST-000001", name: "Alpha Corp").ConfigureAwait(false);
        await SeedCustomerAsync(code: "CUST-000002", name: "Beta Inc").ConfigureAwait(false);
        await SeedCustomerAsync(code: "CUST-000003", name: "Alpha Labs").ConfigureAwait(false);
        SearchCustomersRequest request = new() { Name = "Alpha", Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<CustomerDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().AllSatisfy(c => c.Name.Should().Contain("Alpha"));
    }

    [Test]
    public async Task SearchAsync_DefaultSort_SortsByNameAscending()
    {
        // Arrange
        await SeedCustomerAsync(code: "CUST-000001", name: "Zulu Corp").ConfigureAwait(false);
        await SeedCustomerAsync(code: "CUST-000002", name: "Alpha Inc").ConfigureAwait(false);
        await SeedCustomerAsync(code: "CUST-000003", name: "Mike Labs").ConfigureAwait(false);
        SearchCustomersRequest request = new() { Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<CustomerDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        List<string> names = result.Value!.Items.Select(c => c.Name).ToList();
        names.Should().BeInAscendingOrder();
    }

    [Test]
    public async Task DeactivateAsync_ActiveCustomer_SetsIsDeletedAndDeletedAt()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(customer.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Customer? updated = await Context.Customers.FindAsync(customer.Id).ConfigureAwait(false);
        updated!.IsDeleted.Should().BeTrue();
        updated.DeletedAtUtc.Should().NotBeNull();
        updated.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task DeactivateAsync_CustomerWithActiveAccounts_ReturnsConflict()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        await SeedAccountAsync(customer.Id, "USD", balance: 100.50m).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(customer.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CUSTOMER_HAS_ACTIVE_ACCOUNTS");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeactivateAsync_AlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync(isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(customer.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CUSTOMER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task ReactivateAsync_SoftDeletedCustomer_ClearsDeletedFlags()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync(isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result<CustomerDetailDto> result = await _sut.ReactivateAsync(customer.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Customer? reactivated = await Context.Customers.FindAsync(customer.Id).ConfigureAwait(false);
        reactivated!.IsDeleted.Should().BeFalse();
        reactivated.DeletedAtUtc.Should().BeNull();
        reactivated.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task ReactivateAsync_ConflictingCode_ReturnsConflictError()
    {
        // Arrange
        await SeedCustomerAsync(code: "SAME-CODE", name: "Active Customer").ConfigureAwait(false);
        Customer deleted = await SeedCustomerAsync(code: "SAME-CODE-DEL", name: "Deleted", isDeleted: true, isActive: false).ConfigureAwait(false);

        deleted.Code = "SAME-CODE";
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result<CustomerDetailDto> result = await _sut.ReactivateAsync(deleted.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CUSTOMER_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ReactivateAsync_ConflictingTaxId_ReturnsConflictError()
    {
        // Arrange
        await SeedCustomerAsync(code: "ACTIVE-001", taxId: "TAX-CONFLICT").ConfigureAwait(false);
        Customer deleted = await SeedCustomerAsync(
            code: "DEL-001", taxId: "TAX-CONFLICT", isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result<CustomerDetailDto> result = await _sut.ReactivateAsync(deleted.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_TAX_ID");
        result.StatusCode.Should().Be(409);
    }
}
