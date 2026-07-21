using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

public sealed record QuoteMaterialPreview(Guid MaterialId, string Code, string Name, decimal RequiredQuantity, string Unit, decimal PhysicalStock, decimal ReservedStock, decimal FreeStock, decimal MissingQuantity, decimal? UnitCost, string CostCurrency, decimal? ConvertedUnitCost, decimal? TotalCost);
public sealed record QuoteCalculationResult(IReadOnlyList<QuoteMaterialPreview> Materials, decimal SalesAmount, decimal? EstimatedCost, decimal? GrossProfit, decimal? GrossMarginPercent, decimal? LeadTimeDays, DateTime? EarliestStart, DateTime? EstimatedFinish, DateTime? ReadyToShip, IReadOnlyList<string> Warnings, IReadOnlyList<string> CapacityAssumptions);

public static class QuoteCalculationSupport
{
    public static async Task<QuoteCalculationResult> Calculate(ApplicationDbContext db, Quote quote, CancellationToken ct)
    {
        var warnings = new List<string>(); var assumptions = new List<string>(); var requirements = new Dictionary<Guid, (Material Material, decimal Quantity, string Unit)>();
        decimal? materialCost = 0; decimal maxLeadDays = 0; var today = DateTime.UtcNow.Date;
        foreach (var item in quote.Items.OrderBy(x => x.LineNumber))
        {
            var recipe = await db.Recipes.AsNoTracking().Include(x => x.Items).ThenInclude(x => x.Material)
                .Where(x => x.ProductId == item.ProductId && x.IsActive && (x.EffectiveFrom == null || x.EffectiveFrom <= today) && (x.EffectiveTo == null || x.EffectiveTo >= today))
                .OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.Version).FirstOrDefaultAsync(ct);
            if (recipe is null) { warnings.Add($"{item.LineNumber}. kalem için aktif reçete bulunamadı."); item.CalculationWarnings = "Aktif reçete bulunamadı."; item.UnitEstimatedCost = null; item.TotalEstimatedCost = null; materialCost = null; continue; }
            var output = recipe.OutputQuantity > 0 ? recipe.OutputQuantity : 1;
            foreach (var recipeItem in recipe.Items.OrderBy(x => x.Sequence))
            {
                var material = recipeItem.Material; var descriptor = $"{material.MaterialType} {material.Category} {material.ChemicalRole} {material.Name}".ToLowerInvariant();
                if (!item.DtfRequired && descriptor.Contains("dtf")) continue;
                if (!item.FabricRequired && (descriptor.Contains("kumaş") || descriptor.Contains("fabric") || descriptor.Contains("interlok"))) continue;
                if (recipeItem.IsOptional && !descriptor.Contains("dtf") && !descriptor.Contains("kumaş") && !descriptor.Contains("fabric") && !descriptor.Contains("interlok")) continue;
                var required = decimal.Round(item.Quantity * recipeItem.Quantity / output * (1 + recipeItem.WastePercent / 100), 4);
                if (requirements.TryGetValue(material.Id, out var current)) requirements[material.Id] = (material, current.Quantity + required, recipeItem.Unit);
                else requirements[material.Id] = (material, required, recipeItem.Unit);
            }
            var molds = await db.Molds.AsNoTracking().Where(x => x.IsActive && x.ProductId == item.ProductId && (string.IsNullOrEmpty(item.Size) || x.Size == item.Size || x.SizeRange == item.Size)).ToListAsync(ct);
            if (molds.Count == 0) { warnings.Add($"{item.LineNumber}. kalem için uygun aktif kalıp bulunamadı."); item.EstimatedLeadTimeDays = null; maxLeadDays = -1; continue; }
            var cycle = molds.Where(x => x.StandardCycleTimeSeconds.HasValue && x.StandardCycleTimeSeconds > 0).Select(x => (decimal)x.StandardCycleTimeSeconds!.Value).DefaultIfEmpty().Average();
            if (cycle <= 0) { var productCycle = await db.Products.Where(x => x.Id == item.ProductId).Select(x => x.StandardCycleTime).FirstAsync(ct); cycle = productCycle ?? 0; }
            var activeStations = Math.Min(24, await db.InjectionStations.CountAsync(x => x.IsActive && x.Status == "Aktif", ct));
            if (cycle <= 0 || activeStations == 0) { warnings.Add($"{item.LineNumber}. kalem için çevrim süresi veya aktif istasyon verisi eksik."); item.EstimatedLeadTimeDays = null; maxLeadDays = -1; continue; }
            var parallel = Math.Min(activeStations, molds.Count); var cavities = Math.Max(1, molds.Sum(x => Math.Max(1, x.CavityCount)) / molds.Count);
            const decimal dailyHours = 8; var dailyCapacity = dailyHours * 3600 / cycle * parallel * cavities;
            var openLoad = await db.WorkOrders.Where(x => x.IsActive && !x.IsCancelled && x.Status != "Completed").SumAsync(x => (int?)(x.PlannedPairs), ct) ?? 0;
            var queueDays = decimal.Ceiling(openLoad / Math.Max(1, dailyCapacity)); var productionDays = decimal.Ceiling(item.Quantity / Math.Max(1, dailyCapacity)); item.EstimatedLeadTimeDays = queueDays + productionDays + 1; maxLeadDays = Math.Max(maxLeadDays, item.EstimatedLeadTimeDays.Value);
            assumptions.Add($"{item.LineNumber}. kalem: {parallel} paralel kalıp, {activeStations} aktif istasyon, {cycle:0.##} sn çevrim, {dailyHours} saat/gün.");
        }

        var previews = new List<QuoteMaterialPreview>();
        foreach (var (_, requirement) in requirements)
        {
            var material = requirement.Material; var stocks = await db.StockItems.AsNoTracking().Where(x => x.MaterialId == material.Id && x.IsActive).ToListAsync(ct);
            var physical = stocks.Sum(x => x.CurrentQuantity); var reserved = await db.StockReservationLines.AsNoTracking().Where(x => x.MaterialId == material.Id && x.StockReservation.Status == "Active").SumAsync(x => (decimal?)(x.ReservedQuantity - x.ReleasedQuantity), ct) ?? 0; var free = physical - reserved;
            var priced = stocks.Where(x => x.LastPurchasePrice.HasValue && x.LastPurchasePrice > 0).OrderByDescending(x => x.LastModified).FirstOrDefault(); var unitPrice = priced?.LastPurchasePrice ?? material.LastPurchasePrice; var currency = priced?.Currency ?? material.Currency ?? quote.Currency; decimal? converted = null;
            if (!unitPrice.HasValue || unitPrice <= 0) { warnings.Add($"{material.Code} · {material.Name} için alış fiyatı eksik."); materialCost = null; }
            else { converted = await Convert(db, unitPrice.Value, currency, quote.Currency, quote.QuoteDate, ct); if (!converted.HasValue) { warnings.Add($"{currency}/{quote.Currency} kuru eksik ({material.Code})."); materialCost = null; } }
            var lineCost = converted * requirement.Quantity; if (materialCost.HasValue && lineCost.HasValue) materialCost += lineCost.Value;
            previews.Add(new(material.Id, material.Code, material.Name, requirement.Quantity, requirement.Unit, physical, reserved, free, Math.Max(0, requirement.Quantity - free), unitPrice, currency, converted, lineCost));
        }

        decimal? nonMaterialCost = null; var settings = await db.CostSettings.AsNoTracking().Where(x => x.IsActive && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
        if (settings is null) { warnings.Add("Aktif maliyet ayarı bulunamadı; işçilik, enerji, genel gider ve ambalaj hesaplanamadı."); materialCost = null; }
        else if (materialCost.HasValue)
        {
            var quantities = quote.Items.Sum(x => x.Quantity); var productionHours = maxLeadDays > 0 ? maxLeadDays * 8 : 0;
            var extras = settings.DefaultHourlyLaborCost * productionHours + settings.DefaultEnergyCostPerKwh * productionHours + settings.DefaultMachineCostPerHour * productionHours + settings.DefaultPackagingCostPerPair * quantities + settings.DefaultCuttingCostPerPair * quantities + settings.DefaultQualityCostPerPair * quantities;
            nonMaterialCost = await Convert(db, extras, settings.ReportingCurrency, quote.Currency, quote.QuoteDate, ct);
            if (!nonMaterialCost.HasValue) { warnings.Add($"{settings.ReportingCurrency}/{quote.Currency} kuru eksik (operasyon maliyetleri)."); materialCost = null; }
            else materialCost += nonMaterialCost.Value + (materialCost.Value + nonMaterialCost.Value) * settings.DefaultOverheadRatePercent / 100;
        }
        var sales = quote.Items.Sum(x => x.Quantity * x.UnitPrice); var profit = materialCost.HasValue ? sales - materialCost : null; var margin = profit.HasValue && sales != 0 ? profit / sales * 100 : null;
        DateTime? earliest = maxLeadDays >= 0 ? today : null; DateTime? finish = maxLeadDays >= 0 ? today.AddDays((double)maxLeadDays) : null; DateTime? ready = finish?.AddDays(1);
        foreach (var item in quote.Items) { item.TotalSalesAmount = item.Quantity * item.UnitPrice; if (materialCost.HasValue && sales > 0) { item.TotalEstimatedCost = materialCost * item.TotalSalesAmount / sales; item.UnitEstimatedCost = item.Quantity > 0 ? item.TotalEstimatedCost / item.Quantity : null; item.EstimatedGrossProfit = item.TotalSalesAmount - item.TotalEstimatedCost; item.EstimatedGrossMarginPercent = item.TotalSalesAmount > 0 ? item.EstimatedGrossProfit / item.TotalSalesAmount * 100 : null; } }
        return new(previews, sales, materialCost, profit, margin, maxLeadDays >= 0 ? maxLeadDays : null, earliest, finish, ready, warnings.Distinct().ToArray(), assumptions.Distinct().ToArray());
    }

    private static async Task<decimal?> Convert(ApplicationDbContext db, decimal amount, string from, string to, DateTime date, CancellationToken ct)
    {
        if (from.Equals(to, StringComparison.OrdinalIgnoreCase)) return amount;
        var direct = await db.ExchangeRates.AsNoTracking().Where(x => x.IsActive && x.BaseCurrency == from && x.QuoteCurrency == to && x.RateDate <= date.Date).OrderByDescending(x => x.RateDate).Select(x => (decimal?)x.Rate).FirstOrDefaultAsync(ct);
        if (direct.HasValue && direct > 0) return amount * direct;
        var inverse = await db.ExchangeRates.AsNoTracking().Where(x => x.IsActive && x.BaseCurrency == to && x.QuoteCurrency == from && x.RateDate <= date.Date).OrderByDescending(x => x.RateDate).Select(x => (decimal?)x.Rate).FirstOrDefaultAsync(ct);
        return inverse.HasValue && inverse > 0 ? amount / inverse : null;
    }
}
