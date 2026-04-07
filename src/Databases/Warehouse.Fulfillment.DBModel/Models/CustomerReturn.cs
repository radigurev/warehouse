using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a customer return (RMA) with status lifecycle.
/// <para>Conforms to ISA-95 Part 3 -- Material Receipt (return) activity.</para>
/// <para>See <see cref="CustomerReturnLine"/>, <see cref="SalesOrder"/>.</para>
/// </summary>
public sealed class CustomerReturn : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique return number (format: RMA-YYYYMMDD-NNNN).
    /// </summary>
    public required string ReturnNumber { get; set; }

    /// <summary>
    /// Gets or sets the customer ID (cross-schema ref to customers.Customers -- no EF navigation).
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the optional sales order ID (FK to fulfillment.SalesOrders).
    /// </summary>
    public int? SalesOrderId { get; set; }

    /// <summary>
    /// Gets or sets the return status (stored as nvarchar(20)).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the return reason (max 500 characters).
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this return (cross-schema ref to auth.Users).
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the return was confirmed.
    /// </summary>
    public DateTime? ConfirmedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who confirmed this return.
    /// </summary>
    public int? ConfirmedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the return was physically received.
    /// </summary>
    public DateTime? ReceivedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who received this return.
    /// </summary>
    public int? ReceivedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the return was closed.
    /// </summary>
    public DateTime? ClosedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who closed this return.
    /// </summary>
    public int? ClosedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the original sales order.
    /// </summary>
    public SalesOrder? SalesOrder { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of return lines.
    /// </summary>
    public ICollection<CustomerReturnLine> Lines { get; set; } = [];
}
