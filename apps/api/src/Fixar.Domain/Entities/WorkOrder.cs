using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class WorkOrder : BaseAuditableEntity
{
    public string WorkOrderNumber { get; set; } = string.Empty;

    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = default!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public Guid? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    public string? CustomerNameSnapshot { get; set; }

    public string? ProductCodeSnapshot { get; set; }

    public string? ProductNameSnapshot { get; set; }

    public int PlannedPairs { get; set; }

    public string Priority { get; set; } = "Normal";

    public string Status { get; set; } = "Draft";

    public DateTime? PlannedStartDate { get; set; }

    public DateTime? PlannedEndDate { get; set; }

    public DateTime? ActualStartDate { get; set; }

    public DateTime? ActualEndDate { get; set; }

    public Guid? AssignedMachineId { get; set; }
    public Machine? AssignedMachine { get; set; }

    public int? Shift { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsCancelled { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelledBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? UpdatedBy { get; set; }

    public ICollection<StationAssignment> StationAssignments { get; set; } = new List<StationAssignment>();
}
