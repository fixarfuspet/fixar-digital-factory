using Fixar.Application.Features.Profitability;

namespace Fixar.Application.Common.Interfaces;

public interface IProfitabilityReportService
{
    Task<ExecutiveProfitabilitySummaryDto> GetExecutiveSummaryAsync(ProfitabilityFilter filter, CancellationToken ct);
    Task<IReadOnlyList<CustomerProfitabilityListDto>> GetCustomerProfitabilityAsync(ProfitabilityFilter filter, CancellationToken ct);
    Task<IReadOnlyList<OrderProfitabilityListDto>> GetOrderProfitabilityAsync(ProfitabilityFilter filter, CancellationToken ct);
    Task<IReadOnlyList<ProductProfitabilityListDto>> GetProductProfitabilityAsync(ProfitabilityFilter filter, CancellationToken ct);
    Task<IReadOnlyList<WorkOrderProfitabilityDto>> GetWorkOrderProfitabilityAsync(ProfitabilityFilter filter, CancellationToken ct);
    Task<IReadOnlyList<MonthlyProfitabilityTrendDto>> GetMonthlyTrendAsync(ProfitabilityFilter filter, CancellationToken ct);
    Task<IReadOnlyList<CostCategoryBreakdownDto>> GetCostCategoryBreakdownAsync(ProfitabilityFilter filter, CancellationToken ct);
    Task<ProfitabilityTopBottomDto> GetTopAndBottomPerformersAsync(ProfitabilityFilter filter, CancellationToken ct);
    Task<ProfitabilityDataQualityDto> GetDataQualityAsync(ProfitabilityFilter filter, CancellationToken ct);
}
