using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class CostSettings : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string ReportingCurrency { get; set; } = "TRY";
    public decimal DefaultHourlyLaborCost { get; set; }
    public decimal DefaultEnergyCostPerKwh { get; set; }
    public decimal DefaultMachineCostPerHour { get; set; }
    public decimal DefaultOverheadRatePercent { get; set; }
    public string DefaultFireCostMethod { get; set; } = "ActualMaterialAverageCost";
    public decimal DefaultCuttingCostPerPair { get; set; }
    public decimal DefaultPackagingCostPerPair { get; set; }
    public decimal DefaultQualityCostPerPair { get; set; }
    public decimal DefaultShipmentPreparationCostPerBox { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
