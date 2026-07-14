namespace Fixar.Application.Features.Costing;

public sealed record CostLineDto(Guid? Id, string CostCategory, string SourceType, Guid? SourceId, string Description,
    decimal Quantity, string Unit, decimal UnitCost, string SourceCurrency, decimal ExchangeRate,
    string ReportingCurrency, decimal TotalSourceAmount, decimal TotalReportingAmount, string? Notes);

public sealed record CostPreviewDto(Guid WorkOrderId, string WorkOrderNumber, string CustomerName, string ProductCode,
    string ProductName, DateTime CalculationDate, string ReportingCurrency, int PlannedPairs, int ProducedPairs,
    int GoodPairs, int FirePairs, int CutPairs, int PackedPairs, decimal EstimatedMaterialCost,
    decimal ActualMaterialCost, decimal LaborCost, decimal EnergyCost, decimal MachineCost, decimal FireCost,
    decimal CuttingCost, decimal PackagingCost, decimal QualityCost, decimal OverheadCost, decimal OtherCost,
    decimal TotalEstimatedCost, decimal TotalActualCost, decimal? EstimatedCostPerPair,
    decimal? ActualCostPerProducedPair, decimal? ActualCostPerGoodPair, decimal VarianceAmount,
    decimal? VariancePercent, decimal SalesRevenue, decimal GrossProfit, decimal? GrossMarginPercent,
    IReadOnlyList<CostLineDto> Lines, IReadOnlyList<string> Warnings, IReadOnlyList<string> MissingInputs,
    IReadOnlyList<string> Assumptions, bool CanCreateSnapshot);

public sealed record WorkOrderCostListDto(Guid Id, string SnapshotNumber, Guid WorkOrderId, string WorkOrderNumber,
    string CustomerName, string ProductCode, string ProductName, DateTime SnapshotDate, string CalculationType,
    string ReportingCurrency, int ProducedPairs, int GoodPairs, int FirePairs, decimal EstimatedMaterialCost,
    decimal ActualMaterialCost, decimal TotalEstimatedCost, decimal TotalActualCost, decimal? ActualCostPerGoodPair,
    decimal SalesRevenue, decimal GrossProfit, decimal? GrossMarginPercent, decimal VarianceAmount,
    decimal? VariancePercent, bool IsFinal);

public sealed record WorkOrderCostDetailDto(WorkOrderCostListDto Summary, int PlannedPairs, int CutPairs,
    int PackedPairs, decimal LaborCost, decimal EnergyCost, decimal MachineCost, decimal FireCost,
    decimal CuttingCost, decimal PackagingCost, decimal QualityCost, decimal OverheadCost, decimal OtherCost,
    decimal? EstimatedCostPerPair, decimal? ActualCostPerProducedPair, IReadOnlyList<CostLineDto> Lines,
    IReadOnlyList<string> Warnings, IReadOnlyList<string> Assumptions, string? Notes);

public sealed record CreateCostSnapshotRequest(string? ReportingCurrency, string CalculationType = "Draft", string? Notes = null);
