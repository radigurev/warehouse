using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Services;
using Warehouse.Customers.API.Tests.Fixtures;
using Warehouse.Customers.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for customer email operations.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CustomerEmailServiceTests : CustomerTestBase
{
    private CustomerEmailService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new CustomerEmailService(Context, Mapper);
    }

    [Test]
    public async Task CreateEmail_ValidRequest_ReturnsCreatedEmail()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CreateEmailRequest request = new()
        {
            EmailType = "General",
            EmailAddress = "info@acme.com"
        };

        // Act
        Result<CustomerEmailDto> result = await _sut.CreateEmailAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.EmailAddress.Should().Be("info@acme.com");
        result.Value.EmailType.Should().Be("General");
    }

    [Test]
    public async Task CreateEmail_DuplicateForCustomer_ReturnsConflict()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        await SeedEmailAsync(customer.Id, "General", "dupe@acme.com").ConfigureAwait(false);
        CreateEmailRequest request = new() { EmailType = "Billing", EmailAddress = "dupe@acme.com" };

        // Act
        Result<CustomerEmailDto> result = await _sut.CreateEmailAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CUSTOMER_EMAIL");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateEmail_FirstEmail_SetsPrimaryTrue()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CreateEmailRequest request = new()
        {
            EmailType = "General",
            EmailAddress = "first@acme.com"
        };

        // Act
        Result<CustomerEmailDto> result = await _sut.CreateEmailAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();
    }

    [Test]
    public async Task UpdateEmail_DuplicateForCustomer_ReturnsConflict()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        await SeedEmailAsync(customer.Id, "General", "existing@acme.com", isPrimary: true).ConfigureAwait(false);
        CustomerEmail emailToUpdate = await SeedEmailAsync(customer.Id, "Billing", "other@acme.com").ConfigureAwait(false);
        UpdateEmailRequest request = new()
        {
            EmailType = "Billing",
            EmailAddress = "existing@acme.com",
            IsPrimary = false
        };

        // Act
        Result<CustomerEmailDto> result = await _sut.UpdateEmailAsync(customer.Id, emailToUpdate.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CUSTOMER_EMAIL");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeleteEmail_PrimaryEmail_PromotesNext()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerEmail primary = await SeedEmailAsync(customer.Id, "General", "primary@acme.com", isPrimary: true).ConfigureAwait(false);

        CustomerEmail second = new()
        {
            CustomerId = customer.Id,
            EmailType = "Billing",
            EmailAddress = "secondary@acme.com",
            IsPrimary = false,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1)
        };
        Context.CustomerEmails.Add(second);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteEmailAsync(customer.Id, primary.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        CustomerEmail? promoted = await Context.CustomerEmails.FindAsync(second.Id).ConfigureAwait(false);
        promoted!.IsPrimary.Should().BeTrue();
    }
}
