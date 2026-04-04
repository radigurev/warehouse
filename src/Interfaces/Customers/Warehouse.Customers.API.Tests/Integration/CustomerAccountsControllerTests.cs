using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Customers.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Integration;

/// <summary>
/// Integration tests for the CustomerAccountsController: create, list, update, deactivate, and merge.
/// <para>Covers SDD-CUST-001 sections 2.2 (Customer Accounts) and error rules.</para>
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
[Category("Integration")]
public sealed class CustomerAccountsControllerTests : CustomerApiTestBase
{
    private HttpClient _client = null!;
    private int _customerId;

    /// <summary>
    /// Creates an authenticated client and seeds a customer before each test.
    /// </summary>
    [SetUp]
    public override async Task SetUpAsync()
    {
        await base.SetUpAsync();
        _client = CreateAuthenticatedClient("customers:write", "customers:read", "customers:update");
        CustomerDetailDto customer = await CreateCustomerAndReadAsync(_client, name: "Account Test Corp");
        _customerId = customer.Id;
    }

    [Test]
    public async Task CreateAccount_ValidPayload_Returns201()
    {
        // Arrange
        CreateAccountRequest request = new()
        {
            CurrencyCode = "EUR",
            Description = "Euro account"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/customers/{_customerId}/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CustomerAccountDto? body = await response.Content.ReadFromJsonAsync<CustomerAccountDto>();
        body.Should().NotBeNull();
        body!.CurrencyCode.Should().Be("EUR");
        body.Description.Should().Be("Euro account");
        body.Balance.Should().Be(0m);
    }

    [Test]
    public async Task CreateAccount_DuplicateCurrency_Returns409()
    {
        // Arrange
        await CreateAccountViaApiAsync(_client, _customerId, "USD");

        CreateAccountRequest request = new()
        {
            CurrencyCode = "USD"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/customers/{_customerId}/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task CreateAccount_NonExistentCustomer_Returns404()
    {
        // Arrange
        CreateAccountRequest request = new()
        {
            CurrencyCode = "USD"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/v1/customers/999999/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ListAccounts_ReturnsAllAccountsForCustomer()
    {
        // Arrange
        await CreateAccountViaApiAsync(_client, _customerId, "USD");
        await CreateAccountViaApiAsync(_client, _customerId, "EUR");
        await CreateAccountViaApiAsync(_client, _customerId, "BGN");

        // Act
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/v1/customers/{_customerId}/accounts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CustomerAccountDto>? body = await response.Content
            .ReadFromJsonAsync<List<CustomerAccountDto>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(3);
    }

    [Test]
    public async Task UpdateAccount_SetPrimary_Returns200()
    {
        // Arrange
        CustomerAccountDto account = await CreateAccountAndReadAsync(_client, _customerId, "USD");

        UpdateAccountRequest request = new()
        {
            IsPrimary = true,
            Description = "Primary account"
        };

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/v1/customers/{_customerId}/accounts/{account.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerAccountDto? body = await response.Content.ReadFromJsonAsync<CustomerAccountDto>();
        body.Should().NotBeNull();
        body!.IsPrimary.Should().BeTrue();
        body.Description.Should().Be("Primary account");
    }

    [Test]
    public async Task DeactivateAccount_NonZeroBalance_Returns409()
    {
        // Arrange
        CustomerAccountDto account = await CreateAccountAndReadAsync(_client, _customerId, "USD");

        await WithDbContextAsync(async ctx =>
        {
            Customers.DBModel.Models.CustomerAccount? entity = await ctx.CustomerAccounts
                .FindAsync(account.Id);
            entity!.Balance = 100m;
            await ctx.SaveChangesAsync(CancellationToken.None);
        });

        // Act
        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/customers/{_customerId}/accounts/{account.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task DeactivateAccount_LastAccount_Returns409()
    {
        // Arrange
        CustomerAccountDto singleAccount = await CreateAccountAndReadAsync(_client, _customerId, "USD");

        // Act
        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/customers/{_customerId}/accounts/{singleAccount.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task MergeAccounts_ValidRequest_Returns200AndTransfersBalance()
    {
        // Arrange
        CustomerAccountDto sourceAccount = await CreateAccountAndReadAsync(_client, _customerId, "USD");
        CustomerAccountDto targetAccount = await CreateAccountAndReadAsync(_client, _customerId, "EUR");

        await WithDbContextAsync(async ctx =>
        {
            Customers.DBModel.Models.CustomerAccount? source = await ctx.CustomerAccounts
                .FindAsync(sourceAccount.Id);
            Customers.DBModel.Models.CustomerAccount? target = await ctx.CustomerAccounts
                .FindAsync(targetAccount.Id);
            source!.CurrencyCode = "USD";
            source.Balance = 500m;
            target!.CurrencyCode = "USD";
            target.Balance = 200m;
            await ctx.SaveChangesAsync(CancellationToken.None);
        });

        MergeAccountsRequest request = new()
        {
            SourceAccountId = sourceAccount.Id,
            TargetAccountId = targetAccount.Id
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/customers/{_customerId}/accounts/merge", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerAccountDto? body = await response.Content.ReadFromJsonAsync<CustomerAccountDto>();
        body.Should().NotBeNull();
        body!.Balance.Should().Be(700m);
    }

    [Test]
    public async Task MergeAccounts_DifferentCurrencies_Returns400()
    {
        // Arrange
        CustomerAccountDto usdAccount = await CreateAccountAndReadAsync(_client, _customerId, "USD");
        CustomerAccountDto eurAccount = await CreateAccountAndReadAsync(_client, _customerId, "EUR");

        MergeAccountsRequest request = new()
        {
            SourceAccountId = usdAccount.Id,
            TargetAccountId = eurAccount.Id
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/customers/{_customerId}/accounts/merge", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task MergeAccounts_SameAccount_Returns400()
    {
        // Arrange
        CustomerAccountDto account = await CreateAccountAndReadAsync(_client, _customerId, "USD");

        MergeAccountsRequest request = new()
        {
            SourceAccountId = account.Id,
            TargetAccountId = account.Id
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/customers/{_customerId}/accounts/merge", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
