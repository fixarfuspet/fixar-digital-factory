using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class StockReservationLine : BaseEntity
{
    public Guid StockReservationId { get; set; }
    public StockReservation StockReservation { get; set; } = default!;
    public Guid MaterialId { get; set; }
    public Material Material { get; set; } = default!;
    public Guid StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;
    public Guid MaterialLotId { get; set; }
    public MaterialLot MaterialLot { get; set; } = default!;
    public Guid? MaterialContainerId { get; set; }
    public MaterialContainer? MaterialContainer { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal ReleasedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public DateTime? LastConsumedAt { get; set; }
    public string? LastConsumedByName { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public bool IsFifoSuggested { get; set; }
    public bool IsFifoOverride { get; set; }
    public string? FifoOverrideReason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
