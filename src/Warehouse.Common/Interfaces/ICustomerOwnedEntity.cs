namespace Warehouse.Common.Interfaces;

/// <summary>
/// Marker interface for entities owned by a customer (having a CustomerId FK).
/// </summary>
public interface ICustomerOwnedEntity : IEntity
{
    /// <summary>
    /// Gets or sets the foreign key to the owning customer.
    /// </summary>
    int CustomerId { get; set; }
}
