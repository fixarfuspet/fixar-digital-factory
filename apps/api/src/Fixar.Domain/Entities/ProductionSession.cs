using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class ProductionSession : BaseAuditableEntity
{
    public string SessionNumber { get; set; } = string.Empty;
    public Guid? WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? Size { get; set; }
    public string? FoamType { get; set; }
    public Guid MachineId { get; set; }
    public Machine? Machine { get; set; }
    public string MachineCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public Guid OperatorId { get; set; }
    public Operator? Operator { get; set; }
    public string OperatorCode { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public int Shift { get; set; } = 1;
    public string Status { get; set; } = "Planned";
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long PlannedPairs { get; set; }
    public long ProducedPairs { get; set; }
    public long GoodPairs { get; set; }
    public long FirePairs { get; set; }
    public decimal? TargetPairWeight { get; set; }
    public decimal? ActualAveragePairWeight { get; set; }
    public decimal? TargetDensity { get; set; }
    public decimal? ActualAverageDensity { get; set; }
    public int? TargetCuringTimeSeconds { get; set; }
    public decimal? ActualAverageCuringTimeSeconds { get; set; }
    public int? TargetCycleTimeSeconds { get; set; }
    public decimal? ActualAverageCycleTimeSeconds { get; set; }
    public long TotalCycleCount { get; set; }
    public decimal TotalDowntimeMinutes { get; set; }
    public decimal TotalRunningMinutes { get; set; }
    public decimal? EstimatedMaterialCost { get; set; }
    public decimal? ActualMaterialCost { get; set; }
    public string? ProductionNote { get; set; }
    public string? QualityNote { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ProductionStation> Stations { get; set; } = new List<ProductionStation>();
    public ICollection<ProductionEvent> Events { get; set; } = new List<ProductionEvent>();
    public ICollection<ProductionDowntime> Downtimes { get; set; } = new List<ProductionDowntime>();
}
