using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class StockReservation : BaseAuditableEntity
{
    public string ReservationNumber { get; set; } = string.Empty;
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = default!;
    public DateTime ReservationDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByName { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? ActivatedByName { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ReleasedByName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
    public ICollection<StockReservationLine> Lines { get; set; } = [];
}
