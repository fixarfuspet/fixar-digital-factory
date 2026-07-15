namespace Fixar.Application.Common.Interfaces;
public interface IFinancialCashFlowService
{
 Task<object> GetAccountBalanceAsync(Guid accountId,CancellationToken ct);
 Task<IReadOnlyList<object>> GetBalancesAsync(CancellationToken ct);
 Task<IReadOnlyList<object>> GetCashFlowAsync(DateTime from,DateTime to,string? currency,CancellationToken ct);
 Task<IReadOnlyList<object>> GetDailyCashFlowAsync(DateTime from,DateTime to,string? currency,CancellationToken ct);
 Task<IReadOnlyList<object>> GetMonthlyCashFlowAsync(DateTime from,DateTime to,string? currency,CancellationToken ct);
 Task<IReadOnlyList<object>> GetPaymentMethodBreakdownAsync(DateTime from,DateTime to,CancellationToken ct);
 Task<IReadOnlyList<object>> GetChequeMaturityAsync(CancellationToken ct);
 Task<IReadOnlyList<object>> GetLiquiditySummaryAsync(CancellationToken ct);
}
