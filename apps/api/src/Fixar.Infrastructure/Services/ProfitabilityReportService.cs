using Fixar.Application.Common.Interfaces;
using Fixar.Application.Features.Profitability;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fixar.Infrastructure.Services;

public sealed class ProfitabilityReportService(ApplicationDbContext db) : IProfitabilityReportService
{
    private sealed record Row(WorkOrderCostSnapshot Snapshot, decimal Rate, decimal Revenue, decimal Cost, decimal Profit, decimal Estimated, decimal Variance);

    public async Task<IReadOnlyList<WorkOrderProfitabilityDto>> GetWorkOrderProfitabilityAsync(ProfitabilityFilter filter, CancellationToken ct)
    {
        var (rows, settings, missingIds) = await LoadAsync(filter, ct);
        var result = rows.Select(r => ToWorkOrder(r, settings)).ToList();
        if (filter.IncludeIncomplete)
        {
            var missing = await db.WorkOrders.AsNoTracking().Where(x => missingIds.Contains(x.Id)).Include(x => x.Product).Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Customer).ToListAsync(ct);
            result.AddRange(missing.Select(x => new WorkOrderProfitabilityDto(x.Id, x.WorkOrderNumber, x.OrderItem.Order.CustomerId, x.OrderItem.Order.Customer.Name, x.OrderItem.OrderId, x.OrderItem.Order.OrderNumber, x.ProductId, x.Product.Code, x.Product.Name, x.OrderItem.ProductionType, x.PlannedPairs, 0, 0, 0, 0, 0, 0, 0, null, null, x.OrderItem.UnitPrice, 0, null, x.CreatedAt, "Missing", "IncompleteData", 0, ["Maliyet snapshot'ı bulunmuyor."])));
        }
        return result.OrderByDescending(x => x.SnapshotDate).ToList();
    }

    public async Task<IReadOnlyList<CustomerProfitabilityListDto>> GetCustomerProfitabilityAsync(ProfitabilityFilter filter, CancellationToken ct)
    {
        var workOrders = await GetWorkOrderProfitabilityAsync(filter, ct);
        var orders = await db.Orders.AsNoTracking().Where(x => (!filter.DateFrom.HasValue || x.OrderDate >= filter.DateFrom) && (!filter.DateTo.HasValue || x.OrderDate <= filter.DateTo)).Include(x => x.Customer).ToListAsync(ct);
        return workOrders.GroupBy(x => new { x.CustomerId, x.CustomerName }).Select(g =>
        {
            var customerOrders = orders.Where(x => x.CustomerId == g.Key.CustomerId).ToList(); var revenue = g.Sum(x => x.SalesRevenue); var cost = g.Sum(x => x.ActualCost); var profit = revenue - cost; var produced = g.Sum(x => x.ProducedPairs); var fire = g.Sum(x => x.FirePairs); var complete = g.Count(x => x.DataCompletenessPercent >= 100);
            var delays = customerOrders.Where(x => x.DueDate.HasValue).Select(x => Math.Max(0, ((x.Status == "Completed" ? x.LastModified : DateTime.UtcNow)!.Value - x.DueDate!.Value).TotalDays)).ToList();
            return new CustomerProfitabilityListDto(g.Key.CustomerId, customerOrders.FirstOrDefault()?.Customer.CustomerCode ?? "-", g.Key.CustomerName, customerOrders.Count, g.Count(), customerOrders.Sum(x => x.Quantity), produced, customerOrders.Sum(x => x.ShippedQuantity), revenue, cost, profit, Percent(profit, revenue), Per(revenue, produced), Per(cost, produced), Per(profit, produced), fire, Percent(fire, produced), delays.Count == 0 ? 0 : (decimal)delays.Average(), customerOrders.Count(x => x.IsActive && x.Status != "Completed"), customerOrders.Count(x => x.Status == "Completed"), g.Count(x => x.GrossProfit < 0), customerOrders.Max(x => (DateTime?)x.OrderDate), g.Max(x => (DateTime?)x.SnapshotDate), Percent(complete, g.Count()) ?? 0, complete < g.Count() ? [$"{g.Count() - complete} iş emrinde maliyet verisi eksik."] : []);
        }).OrderByDescending(x => x.GrossProfit).ToList();
    }

    public async Task<IReadOnlyList<OrderProfitabilityListDto>> GetOrderProfitabilityAsync(ProfitabilityFilter filter, CancellationToken ct)
    {
        var workOrders = await GetWorkOrderProfitabilityAsync(filter, ct); var ids = workOrders.Select(x => x.OrderId).Distinct().ToList();
        var orders = await db.Orders.AsNoTracking().Where(x => ids.Contains(x.Id)).Include(x => x.Customer).Include(x => x.Items).ToListAsync(ct);
        var settings = await CurrentSettings(ct);
        return orders.Select(o => { var ws = workOrders.Where(x => x.OrderId == o.Id).ToList(); var allocated = ws.Sum(x => x.SalesRevenue); var cost = ws.Sum(x => x.ActualCost); var profit = allocated - cost; var produced = o.Items.Sum(x => x.ProducedPairs); var pairs = o.Items.Sum(x => x.QuantityPairs); var missing = ws.Count(x => x.DataCompletenessPercent < 100); var delay = o.DueDate.HasValue ? Math.Max(0, (DateTime.UtcNow.Date - o.DueDate.Value.Date).Days) : 0; var completeness = ws.Count == 0 ? 0 : Percent(ws.Count - missing, ws.Count) ?? 0;
            return new OrderProfitabilityListDto(o.Id, o.OrderNumber, o.CustomerId, o.Customer.Name, o.OrderDate, o.DueDate, o.Status, filter.Currency, o.Subtotal - o.DiscountAmount, allocated, cost, profit, Percent(profit, allocated), pairs, produced, o.Items.Sum(x => x.ShippedPairs), Percent(produced, pairs) ?? 0, ws.Count, ws.Count - missing, missing, ws.Sum(x => x.FirePairs), Percent(ws.Sum(x => x.FirePairs), produced), delay > 0 && o.Status != "Completed", delay, Status(Percent(profit, allocated), completeness, settings), completeness, missing > 0 ? [$"{missing} iş emrinde maliyet verisi eksik."] : []); }).OrderByDescending(x => x.GrossProfit).ToList();
    }

    public async Task<IReadOnlyList<ProductProfitabilityListDto>> GetProductProfitabilityAsync(ProfitabilityFilter filter, CancellationToken ct)
    {
        var ws = await GetWorkOrderProfitabilityAsync(filter, ct);
        return ws.GroupBy(x => new { x.ProductId, x.ProductCode, x.ProductName, x.ProductionType }).Select(g => { var revenue = g.Sum(x => x.SalesRevenue); var cost = g.Sum(x => x.ActualCost); var profit = revenue - cost; var produced = g.Sum(x => x.ProducedPairs); var good = g.Sum(x => x.GoodPairs); var complete = g.Count(x => x.DataCompletenessPercent >= 100); var top = g.GroupBy(x => x.CustomerName).OrderByDescending(x => x.Sum(y => y.GrossProfit)).Select(x => x.Key).FirstOrDefault(); return new ProductProfitabilityListDto(g.Key.ProductId, g.Key.ProductCode, g.Key.ProductName, g.Key.ProductionType, g.Select(x => x.OrderId).Distinct().Count(), g.Select(x => x.CustomerId).Distinct().Count(), g.Count(), produced, good, g.Sum(x => x.FirePairs), Percent(g.Sum(x => x.FirePairs), produced), revenue, cost, profit, Percent(profit, revenue), Per(revenue, produced), Per(cost, good), Per(profit, good), Percent(g.Sum(x => x.VarianceAmount), g.Sum(x => x.EstimatedCost)), top, g.Max(x => (DateTime?)x.SnapshotDate), Percent(complete, g.Count()) ?? 0); }).OrderByDescending(x => x.GrossProfit).ToList();
    }

    public async Task<ExecutiveProfitabilitySummaryDto> GetExecutiveSummaryAsync(ProfitabilityFilter filter, CancellationToken ct)
    {
        var ws = await GetWorkOrderProfitabilityAsync(filter, ct); var customers = await GetCustomerProfitabilityAsync(filter, ct); var orders = await GetOrderProfitabilityAsync(filter, ct); var products = await GetProductProfitabilityAsync(filter, ct); var revenue = ws.Sum(x => x.SalesRevenue); var cost = ws.Sum(x => x.ActualCost); var profit = revenue - cost; var good = ws.Sum(x => x.GoodPairs); var complete = ws.Count(x => x.DataCompletenessPercent >= 100); var delayed = orders.Where(x => x.IsLate).OrderByDescending(x => x.DelayDays).FirstOrDefault();
        return new ExecutiveProfitabilitySummaryDto(filter.Currency, filter.DateFrom ?? DateTime.UtcNow.Date.AddMonths(-1), filter.DateTo ?? DateTime.UtcNow, revenue, cost, profit, Percent(profit, revenue), ws.Sum(x => x.ProducedPairs), good, ws.Sum(x => x.FirePairs), Percent(ws.Sum(x => x.FirePairs), ws.Sum(x => x.ProducedPairs)), orders.Sum(x => x.ShippedPairs), orders.Count(x => x.Status != "Completed"), orders.Count(x => x.IsLate), ws.Count(x => x.SnapshotStatus != "Final"), ws.Count(x => x.SnapshotStatus == "Final"), complete, ws.Count - complete, customers.Count(x => x.GrossProfit >= 0), customers.Count(x => x.GrossProfit < 0), orders.Count(x => x.GrossProfit >= 0), orders.Count(x => x.GrossProfit < 0), Per(cost, good), Per(revenue, good), Per(profit, good), customers.OrderByDescending(x => x.GrossProfit).FirstOrDefault()?.CustomerName, products.OrderByDescending(x => x.GrossProfit).FirstOrDefault()?.ProductName, orders.OrderByDescending(x => x.GrossProfit).FirstOrDefault()?.OrderNumber, ws.OrderByDescending(x => Math.Abs(x.VarianceAmount)).FirstOrDefault()?.WorkOrderNumber, delayed?.OrderNumber, ws.Count == 0 ? 0 : Percent(complete, ws.Count) ?? 0, ws.Count - complete > 0 ? [$"{ws.Count - complete} iş emrinde maliyet verisi eksik."] : []);
    }

    public async Task<IReadOnlyList<MonthlyProfitabilityTrendDto>> GetMonthlyTrendAsync(ProfitabilityFilter filter, CancellationToken ct)
    {
        var ws = await GetWorkOrderProfitabilityAsync(filter, ct); var from = (filter.DateFrom ?? DateTime.UtcNow.AddMonths(-11)).Date; var months = Enumerable.Range(0, 12).Select(i => new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(i)).ToList();
        return months.Select(m => { var g = ws.Where(x => x.SnapshotDate.Year == m.Year && x.SnapshotDate.Month == m.Month).ToList(); var revenue = g.Sum(x => x.SalesRevenue); var cost = g.Sum(x => x.ActualCost); var profit = revenue - cost; return new MonthlyProfitabilityTrendDto(m.ToString("yyyy-MM"), revenue, cost, profit, Percent(profit, revenue), g.Sum(x => x.ProducedPairs), g.Sum(x => x.GoodPairs), g.Sum(x => x.FirePairs), Percent(g.Sum(x => x.FirePairs), g.Sum(x => x.ProducedPairs)), Per(cost, g.Sum(x => x.GoodPairs)), Per(revenue, g.Sum(x => x.GoodPairs)), g.Count(x => x.SnapshotStatus == "Final"), g.Select(x => x.CustomerId).Distinct().Count(), g.Select(x => x.OrderId).Distinct().Count()); }).ToList();
    }

    public async Task<IReadOnlyList<CostCategoryBreakdownDto>> GetCostCategoryBreakdownAsync(ProfitabilityFilter filter, CancellationToken ct)
    {
        var (rows, _, _) = await LoadAsync(filter, ct); var ids = rows.Select(x => x.Snapshot.Id).ToList(); var rates = rows.ToDictionary(x => x.Snapshot.Id, x => x.Rate); var lines = await db.WorkOrderCostLines.AsNoTracking().Where(x => ids.Contains(x.WorkOrderCostSnapshotId) && x.SourceType != "RecipeItem").ToListAsync(ct); var total = lines.Sum(x => x.TotalReportingAmount * rates[x.WorkOrderCostSnapshotId]);
        return lines.GroupBy(x => x.CostCategory).Select(g => { var amount = g.Sum(x => x.TotalReportingAmount * rates[x.WorkOrderCostSnapshotId]); return new CostCategoryBreakdownDto(g.Key, amount, Percent(amount, total) ?? 0, g.Select(x => x.WorkOrderCostSnapshotId).Distinct().Count(), null); }).OrderByDescending(x => x.Amount).ToList();
    }

    public async Task<ProfitabilityTopBottomDto> GetTopAndBottomPerformersAsync(ProfitabilityFilter filter, CancellationToken ct)
    {
        var customers = await GetCustomerProfitabilityAsync(filter, ct); var orders = await GetOrderProfitabilityAsync(filter, ct); var products = await GetProductProfitabilityAsync(filter, ct); var ws = await GetWorkOrderProfitabilityAsync(filter, ct); var limit = Math.Clamp(filter.Limit, 1, 100);
        return new(customers.OrderByDescending(x => x.GrossProfit).Take(limit).ToList(), customers.OrderBy(x => x.GrossMarginPercent ?? decimal.MinValue).Take(limit).ToList(), orders.Where(x => x.GrossProfit < 0).OrderBy(x => x.GrossProfit).Take(limit).ToList(), products.OrderByDescending(x => x.GrossProfit).Take(limit).ToList(), products.OrderByDescending(x => x.FireRatePercent ?? 0).Take(limit).ToList(), ws.OrderByDescending(x => Math.Abs(x.VarianceAmount)).Take(limit).ToList(), orders.OrderByDescending(x => x.DelayDays).Take(limit).ToList(), ws.Where(x => x.DataCompletenessPercent < 100).Take(limit).ToList());
    }

    public async Task<ProfitabilityDataQualityDto> GetDataQualityAsync(ProfitabilityFilter filter, CancellationToken ct)
    {
        var ws = await GetWorkOrderProfitabilityAsync(filter with { IncludeIncomplete = true }, ct); var costed = ws.Count(x => x.SnapshotStatus != "Missing"); var missingConsumption = ws.Count(x => x.Warnings.Any(w => w.Contains("tüketim", StringComparison.OrdinalIgnoreCase))); var estimated = ws.Count(x => x.ActualCost == 0 && x.EstimatedCost > 0); var warnings = new List<string>(); if (missingConsumption > 0) warnings.Add($"{missingConsumption} iş emrinde gerçek tüketim bulunmuyor."); if (estimated > 0) warnings.Add($"{estimated} iş emrinde yalnız tahmini maliyet kullanıldı."); return new(ws.Count, costed, ws.Count - costed, missingConsumption, 0, estimated, Percent(costed, ws.Count) ?? 0, warnings);
    }

    private async Task<(List<Row> Rows, ProfitabilitySettings Settings, List<Guid> MissingIds)> LoadAsync(ProfitabilityFilter f, CancellationToken ct)
    {
        var currency = NormalizeCurrency(f.Currency); var settings = await CurrentSettings(ct); var q = db.WorkOrderCostSnapshots.AsNoTracking().Where(x => (!f.DateFrom.HasValue || x.SnapshotDate >= f.DateFrom) && (!f.DateTo.HasValue || x.SnapshotDate <= f.DateTo) && (!f.WorkOrderId.HasValue || x.WorkOrderId == f.WorkOrderId) && (!f.CustomerId.HasValue || x.WorkOrder.OrderItem.Order.CustomerId == f.CustomerId) && (!f.OrderId.HasValue || x.WorkOrder.OrderItem.OrderId == f.OrderId) && (!f.ProductId.HasValue || x.WorkOrder.ProductId == f.ProductId) && (string.IsNullOrWhiteSpace(f.ProductionType) || x.WorkOrder.OrderItem.ProductionType == f.ProductionType) && (string.IsNullOrWhiteSpace(f.Search) || x.WorkOrder.WorkOrderNumber.Contains(f.Search) || x.WorkOrder.Product.Name.Contains(f.Search) || x.WorkOrder.OrderItem.Order.Customer.Name.Contains(f.Search)));
        q = q.Where(x => !db.WorkOrderCostSnapshots.Any(y => y.WorkOrderId == x.WorkOrderId && ((y.IsFinal && !x.IsFinal) || (y.IsFinal == x.IsFinal && (y.SnapshotDate > x.SnapshotDate || (y.SnapshotDate == x.SnapshotDate && y.Created > x.Created))))));
        var snapshots = await q.Include(x => x.WorkOrder).ThenInclude(x => x.Product).Include(x => x.WorkOrder).ThenInclude(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Customer).AsSplitQuery().ToListAsync(ct);
        var rates = await db.ExchangeRates.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct); var rows = new List<Row>();
        foreach (var s in snapshots) { var rate = ResolveRate(s.ReportingCurrency, currency, s.SnapshotDate, rates); if (!rate.HasValue) { if (f.IncludeIncomplete) continue; throw new InvalidOperationException("Seçilen para birimi için gerekli döviz kuru bulunamadı."); } rows.Add(new(s, rate.Value, Round(s.SalesRevenue * rate.Value), Round(s.TotalActualCost * rate.Value), Round(s.GrossProfit * rate.Value), Round(s.TotalEstimatedCost * rate.Value), Round(s.VarianceAmount * rate.Value))); }
        var allIds = await db.WorkOrders.AsNoTracking().Where(x => !x.IsCancelled && (!f.CustomerId.HasValue || x.OrderItem.Order.CustomerId == f.CustomerId) && (!f.OrderId.HasValue || x.OrderItem.OrderId == f.OrderId) && (!f.ProductId.HasValue || x.ProductId == f.ProductId)).Select(x => x.Id).ToListAsync(ct); return (rows, settings, allIds.Except(rows.Select(x => x.Snapshot.WorkOrderId)).ToList());
    }

    private static WorkOrderProfitabilityDto ToWorkOrder(Row r, ProfitabilitySettings s) { var x = r.Snapshot; var completeness = x.ActualMaterialCost > 0 ? 100 : 80; var warnings = completeness < 100 ? new[] { "Gerçek hammadde tüketimi bulunmuyor veya maliyeti sıfır." } : []; return new(x.WorkOrderId, x.WorkOrder.WorkOrderNumber, x.WorkOrder.OrderItem.Order.CustomerId, x.WorkOrder.OrderItem.Order.Customer.Name, x.WorkOrder.OrderItem.OrderId, x.WorkOrder.OrderItem.Order.OrderNumber, x.WorkOrder.ProductId, x.WorkOrder.Product.Code, x.WorkOrder.Product.Name, x.WorkOrder.OrderItem.ProductionType, x.PlannedPairs, x.ProducedPairs, x.GoodPairs, x.FirePairs, r.Estimated, r.Cost, r.Revenue, r.Profit, Percent(r.Profit, r.Revenue), x.GoodPairs > 0 ? Round(r.Cost / x.GoodPairs) : null, x.PlannedPairs > 0 ? Round(r.Revenue / x.PlannedPairs) : null, r.Variance, x.VariancePercent, x.SnapshotDate, x.IsFinal ? "Final" : x.CalculationType, Status(Percent(r.Profit, r.Revenue), completeness, s), completeness, warnings); }
    private async Task<ProfitabilitySettings> CurrentSettings(CancellationToken ct) => await db.ProfitabilitySettings.AsNoTracking().Where(x => x.IsActive && x.EffectiveFrom <= DateTime.UtcNow && (x.EffectiveTo == null || x.EffectiveTo >= DateTime.UtcNow)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct) ?? throw new InvalidOperationException("Kârlılık raporu ayarı bulunamadı.");
    private static decimal? ResolveRate(string from, string to, DateTime at, List<ExchangeRate> rates) { from = NormalizeCurrency(from); to = NormalizeCurrency(to); if (from == to) return 1; var d = rates.Where(x => x.BaseCurrency == from && x.QuoteCurrency == to && x.RateDate <= at).OrderByDescending(x => x.RateDate).FirstOrDefault(); if (d is not null) return d.Rate; var i = rates.Where(x => x.BaseCurrency == to && x.QuoteCurrency == from && x.RateDate <= at).OrderByDescending(x => x.RateDate).FirstOrDefault(); return i?.Rate > 0 ? 1 / i.Rate : null; }
    private static string Status(decimal? margin, decimal completeness, ProfitabilitySettings s) => completeness < s.MinimumDataCompletenessPercent ? "IncompleteData" : margin >= s.HighMarginThresholdPercent ? "HighlyProfitable" : margin >= s.LowMarginThresholdPercent ? "Profitable" : margin > s.BreakEvenTolerancePercent ? "LowMargin" : margin >= -s.BreakEvenTolerancePercent ? "BreakEven" : "Loss";
    private static decimal? Percent(decimal value, decimal basis) => basis == 0 ? null : Round(value / basis * 100); private static decimal? Per(decimal value, int quantity) => quantity == 0 ? null : Round(value / quantity); private static decimal Round(decimal x) => Math.Round(x, 4); private static string NormalizeCurrency(string x) { x = x.Trim().ToUpperInvariant(); return x is "TRY" or "EUR" or "USD" or "GBP" ? x : throw new InvalidOperationException("Desteklenmeyen para birimi."); }
}
