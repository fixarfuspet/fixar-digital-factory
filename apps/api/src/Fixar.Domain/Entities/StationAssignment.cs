using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class StationAssignment : BaseAuditableEntity
{
    public Guid InjectionStationId { get; set; }
    public InjectionStation InjectionStation { get; set; } = default!;

    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = default!;

    public Guid? MoldId { get; set; }
    public Mold? Mold { get; set; }

    public int StationNumberSnapshot { get; set; }

    public string? OperatorName { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinishedAt { get; set; }

    public int ProducedPairs { get; set; }

    public int FirePairs { get; set; }

    public int TotalTurns { get; set; }

    public DateTime? LastTurnAt { get; set; }

    public int TurnsSinceLastRelease { get; set; }

    public int? ReleaseFrequencyTurns { get; set; }

    public DateTime? LastReleaseAt { get; set; }

    public int? LastReleaseTurn { get; set; }

    public string Status { get; set; } = "Üretimde";

    public string? Note { get; set; }

    public ICollection<StationAssignmentFire> Fires { get; set; } = new List<StationAssignmentFire>();

    public ICollection<StationAssignmentDowntime> Downtimes { get; set; } = new List<StationAssignmentDowntime>();

    public ICollection<StationAssignmentEvent> Events { get; set; } = new List<StationAssignmentEvent>();
}
