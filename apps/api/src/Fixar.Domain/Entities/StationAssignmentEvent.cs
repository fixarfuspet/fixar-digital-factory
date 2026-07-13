using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class StationAssignmentEvent : BaseAuditableEntity
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

    public string EventType { get; set; } = string.Empty;

    public DateTime EventTime { get; set; } = DateTime.UtcNow;

    public int? Quantity { get; set; }

    public string? Reason { get; set; }

    public string? Note { get; set; }

    public string? RecordedBy { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
