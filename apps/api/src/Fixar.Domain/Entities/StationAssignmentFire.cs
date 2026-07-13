using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class StationAssignmentFire : BaseAuditableEntity
{
    public Guid StationAssignmentId { get; set; }

    public StationAssignment StationAssignment { get; set; } = default!;

    public Guid OrderItemId { get; set; }

    public Guid InjectionStationId { get; set; }

    public int StationNumberSnapshot { get; set; }

    public Guid? ProductId { get; set; }

    public string? ProductCodeSnapshot { get; set; }

    public string? ProductNameSnapshot { get; set; }

    public Guid? MoldId { get; set; }

    public string? MoldCodeSnapshot { get; set; }

    public string? MoldNameSnapshot { get; set; }

    public Guid? OperatorId { get; set; }

    public string? OperatorNameSnapshot { get; set; }

    public int FirePairs { get; set; }

    public string ReasonType { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public string? Note { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public string? RecordedBy { get; set; }

    public bool IsCancelled { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelledBy { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
