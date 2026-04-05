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
/// Unit tests for customer phone operations.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CustomerPhoneServiceTests : CustomerTestBase
{
    private CustomerPhoneService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new CustomerPhoneService(Context, Mapper);
    }

    [Test]
    public async Task CreatePhone_FirstPhone_SetsPrimaryTrue()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CreatePhoneRequest request = new()
        {
            PhoneType = "Mobile",
            PhoneNumber = "+359888000111"
        };

        // Act
        Result<CustomerPhoneDto> result = await _sut.CreatePhoneAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();
    }

    [Test]
    public async Task UpdatePhone_SetPrimary_UnsetsOtherPrimary()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerPhone primary = await SeedPhoneAsync(customer.Id, "Mobile", "+359888111222", isPrimary: true).ConfigureAwait(false);
        CustomerPhone secondary = await SeedPhoneAsync(customer.Id, "Landline", "+35932111222", isPrimary: false).ConfigureAwait(false);
        UpdatePhoneRequest request = new()
        {
            PhoneType = "Landline",
            PhoneNumber = "+35932111222",
            IsPrimary = true
        };

        // Act
        Result<CustomerPhoneDto> result = await _sut.UpdatePhoneAsync(customer.Id, secondary.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();

        CustomerPhone? oldPrimary = await Context.CustomerPhones.FindAsync(primary.Id).ConfigureAwait(false);
        oldPrimary!.IsPrimary.Should().BeFalse();
    }

    [Test]
    public async Task DeletePhone_PrimaryPhone_PromotesNext()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerPhone primary = await SeedPhoneAsync(customer.Id, "Mobile", "+359888111222", isPrimary: true).ConfigureAwait(false);

        CustomerPhone second = new()
        {
            CustomerId = customer.Id,
            PhoneType = "Landline",
            PhoneNumber = "+35932111222",
            IsPrimary = false,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1)
        };
        Context.CustomerPhones.Add(second);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeletePhoneAsync(customer.Id, primary.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        CustomerPhone? promoted = await Context.CustomerPhones.FindAsync(second.Id).ConfigureAwait(false);
        promoted!.IsPrimary.Should().BeTrue();
    }
}
