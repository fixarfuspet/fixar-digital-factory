using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class ProductionStation : BaseAuditableEntity
{
    public Guid ProductionSessionId { get; set; }
    public ProductionSession? ProductionSession { get; set; }
    public int StationNumber { get; set; }
    public Guid? MoldId { get; set; }
    public Mold? Mold { get; set; }
    public string? MoldCode { get; set; }
    public string? MoldName { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public Guid? OperatorId { get; set; }
    public Operator? Operator { get; set; }
    public string? OperatorCode { get; set; }
    public string? OperatorName { get; set; }
    public string Status { get; set; } = "Empty";
    public long CurrentCycleNumber { get; set; }
    public long ProducedPairs { get; set; }
    public long GoodPairs { get; set; }
    public long FirePairs { get; set; }
    public DateTime? LastCycleStartedAt { get; set; }
    public DateTime? LastCycleCompletedAt { get; set; }
    public DateTime? CuringStartedAt { get; set; }
    public DateTime? CuringExpectedEndAt { get; set; }
    public DateTime? LastReleaseAt { get; set; }
    public int CyclesSinceLastRelease { get; set; }
    public int? ReleaseFrequencyCycles { get; set; }
    public decimal? TargetPairWeight { get; set; }
    public decimal? LastPairWeight { get; set; }
    public decimal? TargetDensity { get; set; }
    public decimal? LastDensity { get; set; }
    public int? TargetCuringTimeSeconds { get; set; }
    public int? ActualLastCuringTimeSeconds { get; set; }
    public int? TargetCycleTimeSeconds { get; set; }
    public int? ActualLastCycleTimeSeconds { get; set; }
    public string? LastFaultReason { get; set; }
    public string? LastNote { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ProductionEvent> Events { get; set; } = new List<ProductionEvent>();
    public ICollection<ProductionDowntime> Downtimes { get; set; } = new List<ProductionDowntime>();
}
