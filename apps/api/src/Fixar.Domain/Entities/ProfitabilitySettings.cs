using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class ProfitabilitySettings : BaseAuditableEntity
{
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public decimal HighMarginThresholdPercent { get; set; }
    public decimal LowMarginThresholdPercent { get; set; }
    public decimal BreakEvenTolerancePercent { get; set; }
    public decimal FireWarningThresholdPercent { get; set; }
    public decimal CostVarianceWarningThresholdPercent { get; set; }
    public int DeliveryDelayWarningDays { get; set; }
    public decimal MinimumDataCompletenessPercent { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}
