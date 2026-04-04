using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Warehouse.Customers.DBModel;
using Warehouse.Customers.DBModel.Models;
using Warehouse.Mapping.Profiles.Customers;

namespace Warehouse.Customers.API.Tests.Fixtures;

/// <summary>
/// Base class for customer domain unit tests. Provides a fresh InMemory database and real AutoMapper per test.
/// </summary>
public abstract class CustomerTestBase
{
    /// <summary>
    /// Gets the InMemory EF Core context for the current test.
    /// </summary>
    protected CustomersDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the pre-configured AutoMapper mapper with all customer mapping profiles.
    /// </summary>
    protected IMapper Mapper { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        DbContextOptions<CustomersDbContext> options = new DbContextOptionsBuilder<CustomersDbContext>()
            .UseInMemoryDatabase(databaseName: $"WarehouseTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Context = new CustomersDbContext(options);

        MapperConfiguration config = new(cfg =>
        {
            cfg.AddProfile<CustomerMappingProfile>();
        });

        Mapper = config.CreateMapper();
    }

    [TearDown]
    public virtual void TearDown()
    {
        Context.Dispose();
    }

    /// <summary>
    /// Seeds a customer category and returns the persisted entity.
    /// </summary>
    protected async Task<CustomerCategory> SeedCategoryAsync(string name = "Wholesale", string? description = null)
    {
        CustomerCategory category = new()
        {
            Name = name,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.CustomerCategories.Add(category);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return category;
    }

    /// <summary>
    /// Seeds an active customer and returns the persisted entity.
    /// </summary>
    protected async Task<Customer> SeedCustomerAsync(
        string code = "CUST-000001",
        string name = "Test Customer",
        string? taxId = null,
        int? categoryId = null,
        bool isDeleted = false,
        bool isActive = true)
    {
        Customer customer = new()
        {
            Code = code,
            Name = name,
            TaxId = taxId,
            CategoryId = categoryId,
            IsActive = isActive,
            IsDeleted = isDeleted,
            DeletedAtUtc = isDeleted ? DateTime.UtcNow : null,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        Context.Customers.Add(customer);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return customer;
    }

    /// <summary>
    /// Seeds a customer account and returns the persisted entity.
    /// </summary>
    protected async Task<CustomerAccount> SeedAccountAsync(
        int customerId,
        string currencyCode = "USD",
        decimal balance = 0m,
        bool isPrimary = false,
        bool isDeleted = false)
    {
        CustomerAccount account = new()
        {
            CustomerId = customerId,
            CurrencyCode = currencyCode,
            Balance = balance,
            IsPrimary = isPrimary,
            IsDeleted = isDeleted,
            DeletedAtUtc = isDeleted ? DateTime.UtcNow : null,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.CustomerAccounts.Add(account);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return account;
    }

    /// <summary>
    /// Seeds a customer address and returns the persisted entity.
    /// </summary>
    protected async Task<CustomerAddress> SeedAddressAsync(
        int customerId,
        string addressType = "Billing",
        bool isDefault = false)
    {
        CustomerAddress address = new()
        {
            CustomerId = customerId,
            AddressType = addressType,
            StreetLine1 = "123 Main St",
            City = "Sofia",
            PostalCode = "1000",
            CountryCode = "BG",
            IsDefault = isDefault,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.CustomerAddresses.Add(address);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return address;
    }

    /// <summary>
    /// Seeds a customer phone and returns the persisted entity.
    /// </summary>
    protected async Task<CustomerPhone> SeedPhoneAsync(
        int customerId,
        string phoneType = "Mobile",
        string phoneNumber = "+359888123456",
        bool isPrimary = false)
    {
        CustomerPhone phone = new()
        {
            CustomerId = customerId,
            PhoneType = phoneType,
            PhoneNumber = phoneNumber,
            IsPrimary = isPrimary,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.CustomerPhones.Add(phone);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return phone;
    }

    /// <summary>
    /// Seeds a customer email and returns the persisted entity.
    /// </summary>
    protected async Task<CustomerEmail> SeedEmailAsync(
        int customerId,
        string emailType = "General",
        string emailAddress = "test@example.com",
        bool isPrimary = false)
    {
        CustomerEmail email = new()
        {
            CustomerId = customerId,
            EmailType = emailType,
            EmailAddress = emailAddress,
            IsPrimary = isPrimary,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.CustomerEmails.Add(email);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return email;
    }
}
