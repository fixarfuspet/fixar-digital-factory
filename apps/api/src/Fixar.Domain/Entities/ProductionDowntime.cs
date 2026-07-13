using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class ProductionDowntime : BaseAuditableEntity
{
    public Guid ProductionSessionId { get; set; }
    public ProductionSession? ProductionSession { get; set; }
    public Guid? ProductionStationId { get; set; }
    public ProductionStation? ProductionStation { get; set; }
    public string ReasonType { get; set; } = "Other";
    public string? Reason { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public decimal? DurationMinutes { get; set; }
    public bool IsOpen { get; set; } = true;
    public Guid? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
