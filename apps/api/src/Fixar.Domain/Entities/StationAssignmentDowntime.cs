using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class StationAssignmentDowntime : BaseAuditableEntity
{
    public Guid StationAssignmentId { get; set; }

    public StationAssignment StationAssignment { get; set; } = default!;

    public Guid OrderItemId { get; set; }

    public Guid InjectionStationId { get; set; }

    public int StationNumberSnapshot { get; set; }

    public Guid? MachineId { get; set; }

    public Guid? OperatorId { get; set; }

    public string? OperatorNameSnapshot { get; set; }

    public string DowntimeType { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public string? Note { get; set; }

    public string PreviousAssignmentStatus { get; set; } = "Üretimde";

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public decimal? DurationMinutes { get; set; }

    public bool IsOpen { get; set; } = true;

    public string? StartedBy { get; set; }

    public string? EndedBy { get; set; }

    public bool IsCancelled { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelledBy { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
