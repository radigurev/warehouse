using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Immutable audit record for procurement operations events.
/// <para>Conforms to ISA-95 Operations Event Model.</para>
/// </summary>
[Table("PurchaseEvents", Schema = "purchasing")]
[Index(nameof(EventType), Name = "IX_PurchaseEvents_EventType")]
[Index(nameof(EntityType), nameof(EntityId), Name = "IX_PurchaseEvents_EntityType_EntityId")]
[Index(nameof(OccurredAtUtc), Name = "IX_PurchaseEvents_OccurredAtUtc")]
public sealed class PurchaseEvent : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the event type (max 50 characters).
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public required string EventType { get; set; }

    /// <summary>
    /// Gets or sets the entity type (max 50 characters).
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public required string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    [Required]
    public int EntityId { get; set; }

    /// <summary>
    /// Gets or sets the user ID (cross-schema ref to auth.Users).
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the event occurred.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional JSON payload with before/after state.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Payload { get; set; }
}
