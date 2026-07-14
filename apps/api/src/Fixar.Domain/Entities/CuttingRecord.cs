using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class CuttingRecord : BaseAuditableEntity
{
    public Guid CuttingMachineId { get; set; }
    public CuttingMachine CuttingMachine { get; set; } = default!;

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public Guid? StationAssignmentId { get; set; }
    public StationAssignment? StationAssignment { get; set; }

    public Guid? WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    public Guid? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? OperatorId { get; set; }
    public Operator? Operator { get; set; }

    public string? RecordNumber { get; set; }

    public DateTime RecordDate { get; set; } = DateTime.UtcNow;

    public int? Shift { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int CutPairs { get; set; }

    public int InputPairs { get; set; }

    public int GoodPairs { get; set; }

    public int RejectedPairs { get; set; }

    public int ReworkPairs { get; set; }

    public string? Notes { get; set; }

    public string Status { get; set; } = "Kesimde";

    public bool IsActive { get; set; } = true;

    public bool IsCancelled { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelledBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedByName { get; set; }

    public string? UpdatedByName { get; set; }
}
