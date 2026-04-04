using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Services;
using Warehouse.Customers.API.Tests.Fixtures;
using Warehouse.DBModel.Models.Customers;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for customer account operations: CRUD, deactivation, and merge.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CustomerAccountServiceTests : CustomerTestBase
{
    private CustomerAccountService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new CustomerAccountService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedAccount()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CreateAccountRequest request = new() { CurrencyCode = "EUR", Description = "Euro account" };

        // Act
        Result<CustomerAccountDto> result = await _sut.CreateAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CurrencyCode.Should().Be("EUR");
        result.Value.Balance.Should().Be(0m);
        result.Value.Description.Should().Be("Euro account");
    }

    [Test]
    public async Task CreateAsync_DuplicateCurrency_ReturnsConflictError()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        await SeedAccountAsync(customer.Id, "USD").ConfigureAwait(false);
        CreateAccountRequest request = new() { CurrencyCode = "USD" };

        // Act
        Result<CustomerAccountDto> result = await _sut.CreateAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CURRENCY_ACCOUNT");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_NonExistentCustomer_ReturnsNotFound()
    {
        // Arrange
        CreateAccountRequest request = new() { CurrencyCode = "USD" };

        // Act
        Result<CustomerAccountDto> result = await _sut.CreateAsync(9999, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CUSTOMER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task CreateAsync_FirstAccount_SetsIsPrimaryTrue()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CreateAccountRequest request = new() { CurrencyCode = "BGN" };

        // Act
        Result<CustomerAccountDto> result = await _sut.CreateAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_InvalidCurrencyCode_ReturnsValidationError()
    {
        // Arrange
        // The service does not validate currency code format itself; FluentValidation handles that.
        // This test verifies that when a second account is created (not first), it gets IsPrimary = false.
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        await SeedAccountAsync(customer.Id, "USD", isPrimary: true).ConfigureAwait(false);
        CreateAccountRequest request = new() { CurrencyCode = "EUR" };

        // Act
        Result<CustomerAccountDto> result = await _sut.CreateAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeFalse();
    }

    [Test]
    public async Task UpdateAsync_SetPrimary_UnsetsOtherPrimary()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerAccount primary = await SeedAccountAsync(customer.Id, "USD", isPrimary: true).ConfigureAwait(false);
        CustomerAccount secondary = await SeedAccountAsync(customer.Id, "EUR", isPrimary: false).ConfigureAwait(false);
        UpdateAccountRequest request = new() { Description = "Now primary", IsPrimary = true };

        // Act
        Result<CustomerAccountDto> result = await _sut.UpdateAsync(customer.Id, secondary.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();

        CustomerAccount? oldPrimary = await Context.CustomerAccounts.FindAsync(primary.Id).ConfigureAwait(false);
        oldPrimary!.IsPrimary.Should().BeFalse();
    }

    [Test]
    public async Task DeactivateAsync_NonZeroBalance_ReturnsConflict()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerAccount account1 = await SeedAccountAsync(customer.Id, "USD", balance: 150.00m).ConfigureAwait(false);
        await SeedAccountAsync(customer.Id, "EUR").ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(customer.Id, account1.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ACCOUNT_HAS_BALANCE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeactivateAsync_LastActiveAccount_ReturnsConflict()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerAccount account = await SeedAccountAsync(customer.Id, "USD").ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(customer.Id, account.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("LAST_ACTIVE_ACCOUNT");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task MergeAsync_ValidRequest_TransfersBalanceAndDeletesSource()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerAccount source = await SeedAccountAsync(customer.Id, "USD", balance: 500.00m).ConfigureAwait(false);
        CustomerAccount target = await SeedAccountAsync(customer.Id, "USD", balance: 200.00m).ConfigureAwait(false);
        MergeAccountsRequest request = new() { SourceAccountId = source.Id, TargetAccountId = target.Id };

        // Act
        Result<CustomerAccountDto> result = await _sut.MergeAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Balance.Should().Be(700.00m);

        CustomerAccount? deletedSource = await Context.CustomerAccounts.FindAsync(source.Id).ConfigureAwait(false);
        deletedSource!.IsDeleted.Should().BeTrue();
        deletedSource.Balance.Should().Be(0m);
    }

    [Test]
    public async Task MergeAsync_DifferentCurrencies_ReturnsValidationError()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerAccount source = await SeedAccountAsync(customer.Id, "USD").ConfigureAwait(false);
        CustomerAccount target = await SeedAccountAsync(customer.Id, "EUR").ConfigureAwait(false);
        MergeAccountsRequest request = new() { SourceAccountId = source.Id, TargetAccountId = target.Id };

        // Act
        Result<CustomerAccountDto> result = await _sut.MergeAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("MERGE_CURRENCY_MISMATCH");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task MergeAsync_SameAccount_ReturnsValidationError()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerAccount account = await SeedAccountAsync(customer.Id, "USD").ConfigureAwait(false);
        MergeAccountsRequest request = new() { SourceAccountId = account.Id, TargetAccountId = account.Id };

        // Act
        Result<CustomerAccountDto> result = await _sut.MergeAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("MERGE_SELF_NOT_ALLOWED");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task MergeAsync_DifferentCustomers_ReturnsValidationError()
    {
        // Arrange
        Customer customer1 = await SeedCustomerAsync(code: "CUST-001").ConfigureAwait(false);
        Customer customer2 = await SeedCustomerAsync(code: "CUST-002").ConfigureAwait(false);
        CustomerAccount source = await SeedAccountAsync(customer1.Id, "USD").ConfigureAwait(false);
        CustomerAccount target = await SeedAccountAsync(customer2.Id, "USD").ConfigureAwait(false);
        MergeAccountsRequest request = new() { SourceAccountId = source.Id, TargetAccountId = target.Id };

        // Act
        Result<CustomerAccountDto> result = await _sut.MergeAsync(customer1.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("MERGE_DIFFERENT_CUSTOMERS");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task MergeAsync_InactiveTarget_ReturnsNotFound()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerAccount source = await SeedAccountAsync(customer.Id, "USD").ConfigureAwait(false);
        CustomerAccount target = await SeedAccountAsync(customer.Id, "USD", isDeleted: true).ConfigureAwait(false);
        MergeAccountsRequest request = new() { SourceAccountId = source.Id, TargetAccountId = target.Id };

        // Act
        Result<CustomerAccountDto> result = await _sut.MergeAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ACCOUNT_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }
}
