using AutoMapper;
using Warehouse.Infrastructure.Services;
using Warehouse.Nomenclature.DBModel;

namespace Warehouse.Nomenclature.API.Services;

/// <summary>
/// Intermediate base class for nomenclature domain services.
/// <para>See <see cref="BaseEntityService{TContext}"/>, <see cref="NomenclatureDbContext"/>.</para>
/// </summary>
public abstract class BaseNomenclatureEntityService : BaseEntityService<NomenclatureDbContext>
{
    /// <summary>
    /// Initializes a new instance with the specified nomenclature context and mapper.
    /// </summary>
    protected BaseNomenclatureEntityService(NomenclatureDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }
}
