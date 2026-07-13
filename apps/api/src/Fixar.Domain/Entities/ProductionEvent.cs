using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class ProductionEvent : BaseAuditableEntity
{
    public Guid ProductionSessionId { get; set; }
    public ProductionSession? ProductionSession { get; set; }
    public Guid? ProductionStationId { get; set; }
    public ProductionStation? ProductionStation { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
    public long? CycleNumber { get; set; }
    public long? ProducedPairs { get; set; }
    public long? GoodPairs { get; set; }
    public long? FirePairs { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Density { get; set; }
    public int? CuringTimeSeconds { get; set; }
    public int? CycleTimeSeconds { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }
    public Guid? OperatorId { get; set; }
    public string? OperatorCode { get; set; }
    public string? OperatorName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
