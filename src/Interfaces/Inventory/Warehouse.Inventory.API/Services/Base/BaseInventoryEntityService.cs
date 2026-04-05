using AutoMapper;
using Warehouse.Infrastructure.Services;
using Warehouse.Inventory.DBModel;

namespace Warehouse.Inventory.API.Services;

/// <summary>
/// Intermediate base class for inventory domain services.
/// Provides shared access to the inventory context and mapper.
/// <para>See <see cref="BaseEntityService{TContext}"/>, <see cref="InventoryDbContext"/>.</para>
/// </summary>
public abstract class BaseInventoryEntityService : BaseEntityService<InventoryDbContext>
{
    /// <summary>
    /// Initializes a new instance with the specified inventory context and mapper.
    /// </summary>
    protected BaseInventoryEntityService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }
}
