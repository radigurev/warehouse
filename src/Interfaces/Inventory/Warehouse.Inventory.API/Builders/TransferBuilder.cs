using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Builders;

/// <summary>
/// Fluent builder for constructing <see cref="WarehouseTransfer"/> entities with nested <see cref="WarehouseTransferLine"/> collections.
/// <para>New instance per build -- not reusable after <see cref="Build"/> is called.</para>
/// </summary>
public sealed class TransferBuilder
{
    private int? _sourceWarehouseId;
    private int? _destinationWarehouseId;
    private string? _notes;
    private int _createdByUserId;
    private readonly List<WarehouseTransferLine> _lines = [];

    /// <summary>
    /// Sets the source and destination warehouse identifiers for the transfer.
    /// </summary>
    public TransferBuilder Between(int sourceWarehouseId, int destinationWarehouseId)
    {
        _sourceWarehouseId = sourceWarehouseId;
        _destinationWarehouseId = destinationWarehouseId;
        return this;
    }

    /// <summary>
    /// Sets optional notes for the transfer.
    /// </summary>
    public TransferBuilder WithNotes(string notes)
    {
        _notes = notes;
        return this;
    }

    /// <summary>
    /// Sets the user who created the transfer.
    /// </summary>
    public TransferBuilder CreatedBy(int userId)
    {
        _createdByUserId = userId;
        return this;
    }

    /// <summary>
    /// Adds a single transfer line with explicit field values.
    /// </summary>
    public TransferBuilder AddLine(int productId, decimal quantity, int? sourceLocationId, int? destinationLocationId)
    {
        _lines.Add(new WarehouseTransferLine
        {
            ProductId = productId,
            Quantity = quantity,
            SourceLocationId = sourceLocationId,
            DestinationLocationId = destinationLocationId
        });

        return this;
    }

    /// <summary>
    /// Builds the <see cref="WarehouseTransfer"/> entity with all configured values.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when source/destination warehouses are not set or no lines have been added.</exception>
    public WarehouseTransfer Build()
    {
        if (_sourceWarehouseId is null || _destinationWarehouseId is null)
            throw new InvalidOperationException("Source and destination warehouses are required. Call Between() before Build().");

        if (_lines.Count == 0)
            throw new InvalidOperationException("At least one line is required.");

        return new WarehouseTransfer
        {
            SourceWarehouseId = _sourceWarehouseId.Value,
            DestinationWarehouseId = _destinationWarehouseId.Value,
            Status = "Draft",
            Notes = _notes,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _createdByUserId,
            Lines = _lines
        };
    }
}
