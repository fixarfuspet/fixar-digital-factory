using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class ProductionRecord : BaseAuditableEntity
{
    public Guid InjectionStationId { get; set; }
    public InjectionStation InjectionStation { get; set; } = default!;

    public Guid MoldId { get; set; }
    public Mold Mold { get; set; } = default!;

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public int ProducedPairs { get; set; }

    public string Status { get; set; } = "Üretimde";
}
