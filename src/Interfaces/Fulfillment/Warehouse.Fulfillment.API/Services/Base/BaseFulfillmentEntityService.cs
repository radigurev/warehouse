using AutoMapper;
using Warehouse.Fulfillment.DBModel;
using Warehouse.Infrastructure.Services;

namespace Warehouse.Fulfillment.API.Services.Base;

/// <summary>
/// Intermediate base class for fulfillment domain services.
/// Provides shared access to the fulfillment context and mapper.
/// <para>See <see cref="BaseEntityService{TContext}"/>, <see cref="FulfillmentDbContext"/>.</para>
/// </summary>
public abstract class BaseFulfillmentEntityService : BaseEntityService<FulfillmentDbContext>
{
    /// <summary>
    /// Initializes a new instance with the specified fulfillment context and mapper.
    /// </summary>
    protected BaseFulfillmentEntityService(FulfillmentDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }
}
