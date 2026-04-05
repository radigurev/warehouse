using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Customers.DBModel;
using Warehouse.Infrastructure.Services;

namespace Warehouse.Customers.API.Services;

/// <summary>
/// Intermediate base class for customer domain services.
/// Provides shared validation methods against the customers context.
/// <para>See <see cref="BaseEntityService{TContext}"/>, <see cref="CustomersDbContext"/>.</para>
/// </summary>
public abstract class BaseCustomerEntityService : BaseEntityService<CustomersDbContext>
{
    /// <summary>
    /// Initializes a new instance with the specified customers context and mapper.
    /// </summary>
    protected BaseCustomerEntityService(CustomersDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <summary>
    /// Validates that a customer exists and is not soft-deleted.
    /// </summary>
    protected async Task<Result?> ValidateCustomerExistsAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        bool exists = await Context.Customers
            .AnyAsync(c => c.Id == customerId && !c.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : Result.Failure("CUSTOMER_NOT_FOUND", "Customer not found.", 404);
    }
}
