using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class WorkOrderCostSnapshot : BaseAuditableEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = default!;
    public string SnapshotNumber { get; set; } = string.Empty;
    public DateTime SnapshotDate { get; set; }
    public string CalculationType { get; set; } = "Draft";
    public string ReportingCurrency { get; set; } = "TRY";
    public int PlannedPairs { get; set; }
    public int ProducedPairs { get; set; }
    public int GoodPairs { get; set; }
    public int FirePairs { get; set; }
    public int CutPairs { get; set; }
    public int PackedPairs { get; set; }
    public decimal EstimatedMaterialCost { get; set; }
    public decimal ActualMaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal EnergyCost { get; set; }
    public decimal MachineCost { get; set; }
    public decimal FireCost { get; set; }
    public decimal CuttingCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal QualityCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal OtherCost { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalActualCost { get; set; }
    public decimal? EstimatedCostPerPair { get; set; }
    public decimal? ActualCostPerProducedPair { get; set; }
    public decimal? ActualCostPerGoodPair { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal? VariancePercent { get; set; }
    public decimal SalesRevenue { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal? GrossMarginPercent { get; set; }
    public bool IsFinal { get; set; }
    public string? Notes { get; set; }
    public ICollection<WorkOrderCostLine> Lines { get; set; } = new List<WorkOrderCostLine>();
}
