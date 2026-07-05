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

    public string Status { get; set; } = "Üretimde";

    public string? Note { get; set; }
}