using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class MaterialConsumption : BaseAuditableEntity
{
    public string ConsumptionNumber { get; set; } = string.Empty;
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = default!;
    public Guid? StationAssignmentId { get; set; }
    public StationAssignment? StationAssignment { get; set; }
    public Guid StockReservationId { get; set; }
    public StockReservation StockReservation { get; set; } = default!;
    public Guid StockReservationLineId { get; set; }
    public StockReservationLine StockReservationLine { get; set; } = default!;
    public Guid MaterialId { get; set; }
    public Material Material { get; set; } = default!;
    public Guid StockItemId { get; set; }
    public StockItem StockItem { get; set; } = default!;
    public Guid MaterialLotId { get; set; }
    public MaterialLot MaterialLot { get; set; } = default!;
    public Guid? MaterialContainerId { get; set; }
    public MaterialContainer? MaterialContainer { get; set; }
    public DateTime ConsumptionDate { get; set; } = DateTime.UtcNow;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string ConsumptionType { get; set; } = "Production";
    public string? Notes { get; set; }
    public Guid? StockMovementId { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
    public string? ReversedByName { get; set; }
    public string? ReversalReason { get; set; }
    public Guid? ReversalStockMovementId { get; set; }
}
