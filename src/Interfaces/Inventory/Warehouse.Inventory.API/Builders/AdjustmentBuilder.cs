using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Builders;

/// <summary>
/// Fluent builder for constructing <see cref="InventoryAdjustment"/> entities with nested <see cref="InventoryAdjustmentLine"/> collections.
/// <para>New instance per build -- not reusable after <see cref="Build"/> is called.</para>
/// </summary>
public sealed class AdjustmentBuilder
{
    private string? _reason;
    private string? _notes;
    private int? _sourceStocktakeSessionId;
    private int _createdByUserId;
    private readonly List<InventoryAdjustmentLine> _lines = [];

    /// <summary>
    /// Sets the adjustment reason.
    /// </summary>
    public AdjustmentBuilder WithReason(string reason)
    {
        _reason = reason;
        return this;
    }

    /// <summary>
    /// Sets optional notes for the adjustment.
    /// </summary>
    public AdjustmentBuilder WithNotes(string notes)
    {
        _notes = notes;
        return this;
    }

    /// <summary>
    /// Sets the user who created the adjustment.
    /// </summary>
    public AdjustmentBuilder CreatedBy(int userId)
    {
        _createdByUserId = userId;
        return this;
    }

    /// <summary>
    /// Populates the adjustment from a completed stocktake session and its variance counts.
    /// </summary>
    public AdjustmentBuilder FromStocktakeSession(StocktakeSession session, IReadOnlyList<StocktakeCount> varianceCounts)
    {
        _reason = "Stocktake variance";
        _notes = $"Auto-generated from stocktake session #{session.Id}: {session.Name}";
        _sourceStocktakeSessionId = session.Id;

        foreach (StocktakeCount count in varianceCounts)
        {
            _lines.Add(new InventoryAdjustmentLine
            {
                ProductId = count.ProductId,
                WarehouseId = session.WarehouseId,
                LocationId = count.LocationId,
                ExpectedQuantity = count.ExpectedQuantity,
                ActualQuantity = count.ActualQuantity
            });
        }

        return this;
    }

    /// <summary>
    /// Adds a single adjustment line with explicit field values.
    /// </summary>
    public AdjustmentBuilder AddLine(int productId, int warehouseId, int? locationId, decimal expectedQuantity, decimal actualQuantity)
    {
        _lines.Add(new InventoryAdjustmentLine
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            ExpectedQuantity = expectedQuantity,
            ActualQuantity = actualQuantity
        });

        return this;
    }

    /// <summary>
    /// Builds the <see cref="InventoryAdjustment"/> entity with all configured values.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when Reason is null/empty or no lines have been added.</exception>
    public InventoryAdjustment Build()
    {
        if (string.IsNullOrWhiteSpace(_reason))
            throw new InvalidOperationException("Reason is required.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("At least one line is required.");

        return new InventoryAdjustment
        {
            Reason = _reason,
            Notes = _notes,
            Status = "Pending",
            SourceStocktakeSessionId = _sourceStocktakeSessionId,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _createdByUserId,
            Lines = _lines
        };
    }
}
