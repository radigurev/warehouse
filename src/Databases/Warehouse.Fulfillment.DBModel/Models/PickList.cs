using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a pick list generated from a confirmed sales order.
/// <para>Conforms to ISA-95 Part 3 -- Material Movement (outbound pick).</para>
/// <para>See <see cref="SalesOrder"/>, <see cref="PickListLine"/>.</para>
/// </summary>
public sealed class PickList : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique pick list number (format: PL-YYYYMMDD-NNNN).
    /// </summary>
    public required string PickListNumber { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the sales order.
    /// </summary>
    public int SalesOrderId { get; set; }

    /// <summary>
    /// Gets or sets the pick list status (stored as nvarchar(20)).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this pick list (cross-schema ref to auth.Users).
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the pick list was completed.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent sales order.
    /// </summary>
    public SalesOrder SalesOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation collection of pick list lines.
    /// </summary>
    public ICollection<PickListLine> Lines { get; set; } = [];
}
