using AutoMapper;
using Warehouse.Infrastructure.Services;
using Warehouse.Purchasing.DBModel;

namespace Warehouse.Purchasing.API.Services.Base;

/// <summary>
/// Intermediate base class for purchasing domain services.
/// Provides shared access to the purchasing context and mapper.
/// <para>See <see cref="BaseEntityService{TContext}"/>, <see cref="PurchasingDbContext"/>.</para>
/// </summary>
public abstract class BasePurchasingEntityService : BaseEntityService<PurchasingDbContext>
{
    /// <summary>
    /// Initializes a new instance with the specified purchasing context and mapper.
    /// </summary>
    protected BasePurchasingEntityService(PurchasingDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }
}
