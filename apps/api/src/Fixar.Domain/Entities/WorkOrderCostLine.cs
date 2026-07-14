using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class WorkOrderCostLine : BaseAuditableEntity
{
    public Guid WorkOrderCostSnapshotId { get; set; }
    public WorkOrderCostSnapshot WorkOrderCostSnapshot { get; set; } = default!;
    public string CostCategory { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string SourceCurrency { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; } = 1;
    public string ReportingCurrency { get; set; } = "TRY";
    public decimal TotalSourceAmount { get; set; }
    public decimal TotalReportingAmount { get; set; }
    public string? Notes { get; set; }
}
