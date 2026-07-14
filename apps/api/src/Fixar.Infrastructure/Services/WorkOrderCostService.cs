using System.Data;
using Fixar.Application.Common.Interfaces;
using Fixar.Application.Features.Costing;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fixar.Infrastructure.Services;

public sealed class WorkOrderCostService(ApplicationDbContext db) : IWorkOrderCostService
{
    public Task<CostPreviewDto> CalculateEstimatedCostAsync(Guid workOrderId, string? currency, DateTime? at, CancellationToken ct) =>
        CalculateAsync(workOrderId, currency, at, ct);

    public Task<CostPreviewDto> CalculateActualCostAsync(Guid workOrderId, string? currency, DateTime? at, CancellationToken ct) =>
        CalculateAsync(workOrderId, currency, at, ct);

    public async Task<IReadOnlyList<string>> ValidateCostInputsAsync(Guid workOrderId, string? currency, DateTime? at, CancellationToken ct) =>
        (await CalculateAsync(workOrderId, currency, at, ct)).MissingInputs;

    public Task<WorkOrderCostSnapshot?> GetLatestSnapshotAsync(Guid workOrderId, CancellationToken ct) =>
        db.WorkOrderCostSnapshots.AsNoTracking().Where(x => x.WorkOrderId == workOrderId)
            .OrderByDescending(x => x.SnapshotDate).ThenByDescending(x => x.Created).FirstOrDefaultAsync(ct);

    public async Task<WorkOrderCostSnapshot> CreateSnapshotAsync(Guid workOrderId, CreateCostSnapshotRequest request, CancellationToken ct)
    {
        var preview = await CalculateAsync(workOrderId, request.ReportingCurrency, DateTime.UtcNow, ct);
        if (!preview.CanCreateSnapshot) throw new InvalidOperationException(string.Join(" ", preview.MissingInputs));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81201)", ct);
        var today = DateTime.UtcNow.Date;
        var prefix = $"COST-{today:yyyyMMdd}-";
        var last = await db.WorkOrderCostSnapshots.Where(x => x.SnapshotNumber.StartsWith(prefix))
            .OrderByDescending(x => x.SnapshotNumber).Select(x => x.SnapshotNumber).FirstOrDefaultAsync(ct);
        var sequence = last is null ? 1 : int.Parse(last[^4..]) + 1;
        var snapshot = new WorkOrderCostSnapshot
        {
            WorkOrderId = workOrderId, SnapshotNumber = $"{prefix}{sequence:0000}", SnapshotDate = preview.CalculationDate,
            CalculationType = NormalizeCalculationType(request.CalculationType), ReportingCurrency = preview.ReportingCurrency,
            PlannedPairs = preview.PlannedPairs, ProducedPairs = preview.ProducedPairs, GoodPairs = preview.GoodPairs,
            FirePairs = preview.FirePairs, CutPairs = preview.CutPairs, PackedPairs = preview.PackedPairs,
            EstimatedMaterialCost = preview.EstimatedMaterialCost, ActualMaterialCost = preview.ActualMaterialCost,
            LaborCost = preview.LaborCost, EnergyCost = preview.EnergyCost, MachineCost = preview.MachineCost,
            FireCost = preview.FireCost, CuttingCost = preview.CuttingCost, PackagingCost = preview.PackagingCost,
            QualityCost = preview.QualityCost, OverheadCost = preview.OverheadCost, OtherCost = preview.OtherCost,
            TotalEstimatedCost = preview.TotalEstimatedCost, TotalActualCost = preview.TotalActualCost,
            EstimatedCostPerPair = preview.EstimatedCostPerPair, ActualCostPerProducedPair = preview.ActualCostPerProducedPair,
            ActualCostPerGoodPair = preview.ActualCostPerGoodPair, VarianceAmount = preview.VarianceAmount,
            VariancePercent = preview.VariancePercent, SalesRevenue = preview.SalesRevenue, GrossProfit = preview.GrossProfit,
            GrossMarginPercent = preview.GrossMarginPercent, Notes = request.Notes
        };
        foreach (var line in preview.Lines) snapshot.Lines.Add(new WorkOrderCostLine
        {
            CostCategory = line.CostCategory, SourceType = line.SourceType, SourceId = line.SourceId,
            Description = line.Description, Quantity = line.Quantity, Unit = line.Unit, UnitCost = line.UnitCost,
            SourceCurrency = line.SourceCurrency, ExchangeRate = line.ExchangeRate, ReportingCurrency = line.ReportingCurrency,
            TotalSourceAmount = line.TotalSourceAmount, TotalReportingAmount = line.TotalReportingAmount, Notes = line.Notes
        });
        db.WorkOrderCostSnapshots.Add(snapshot);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return snapshot;
    }

    public async Task<WorkOrderCostDetailDto?> GetCostBreakdownAsync(Guid snapshotId, CancellationToken ct)
    {
        var x = await db.WorkOrderCostSnapshots.AsNoTracking().Include(s => s.Lines)
            .Include(s => s.WorkOrder).ThenInclude(w => w.OrderItem).ThenInclude(i => i.Order).ThenInclude(o => o.Customer)
            .Include(s => s.WorkOrder).ThenInclude(w => w.Product).FirstOrDefaultAsync(s => s.Id == snapshotId, ct);
        if (x is null) return null;
        var list = ToListDto(x);
        return new WorkOrderCostDetailDto(list, x.PlannedPairs, x.CutPairs, x.PackedPairs, x.LaborCost, x.EnergyCost,
            x.MachineCost, x.FireCost, x.CuttingCost, x.PackagingCost, x.QualityCost, x.OverheadCost, x.OtherCost,
            x.EstimatedCostPerPair, x.ActualCostPerProducedPair, x.Lines.Select(ToLineDto).ToList(), [],
            ["Net çalışma süresi toplam süreden kayıtlı duruşlar çıkarılarak hesaplandı.", "Genel gider kendi maliyetini içermeyen taban üzerinden hesaplandı."], x.Notes);
    }

    public static WorkOrderCostListDto ToListDto(WorkOrderCostSnapshot x) => new(x.Id, x.SnapshotNumber, x.WorkOrderId,
        x.WorkOrder.WorkOrderNumber, x.WorkOrder.CustomerNameSnapshot ?? x.WorkOrder.OrderItem.Order.Customer.Name,
        x.WorkOrder.ProductCodeSnapshot ?? x.WorkOrder.Product.Code, x.WorkOrder.ProductNameSnapshot ?? x.WorkOrder.Product.Name,
        x.SnapshotDate, x.CalculationType, x.ReportingCurrency, x.ProducedPairs, x.GoodPairs, x.FirePairs,
        x.EstimatedMaterialCost, x.ActualMaterialCost, x.TotalEstimatedCost, x.TotalActualCost, x.ActualCostPerGoodPair,
        x.SalesRevenue, x.GrossProfit, x.GrossMarginPercent, x.VarianceAmount, x.VariancePercent, x.IsFinal);

    private async Task<CostPreviewDto> CalculateAsync(Guid workOrderId, string? requestedCurrency, DateTime? calculationAt, CancellationToken ct)
    {
        var at = calculationAt.HasValue ? calculationAt.Value.ToUniversalTime() : DateTime.UtcNow;
        var workOrder = await db.WorkOrders.AsNoTracking().Include(x => x.Product)
            .Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Customer)
            .Include(x => x.Recipe).ThenInclude(x => x!.Items).ThenInclude(x => x.Material)
            .Include(x => x.AssignedMachine).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == workOrderId, ct)
            ?? throw new KeyNotFoundException("İş emri bulunamadı.");
        var settings = await db.CostSettings.AsNoTracking().Where(x => x.IsActive && x.EffectiveFrom <= at && (x.EffectiveTo == null || x.EffectiveTo >= at))
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
        if (settings is null) throw new InvalidOperationException("Maliyet ayarı bulunamadı.");
        var reporting = NormalizeCurrency(requestedCurrency ?? settings.ReportingCurrency);
        var warnings = new List<string>(); var missing = new List<string>(); var assumptions = new List<string>(); var lines = new List<CostLineDto>();
        var rateCache = new Dictionary<string, decimal>();
        async Task<decimal?> Rate(string from)
        {
            from = NormalizeCurrency(from); if (from == reporting) return 1;
            var key = $"{from}:{reporting}"; if (rateCache.TryGetValue(key, out var cached)) return cached;
            var direct = await db.ExchangeRates.AsNoTracking().Where(x => x.IsActive && x.BaseCurrency == from && x.QuoteCurrency == reporting && x.RateDate.Date <= at.Date).OrderByDescending(x => x.RateDate).FirstOrDefaultAsync(ct);
            if (direct is not null) return rateCache[key] = direct.Rate;
            var inverse = await db.ExchangeRates.AsNoTracking().Where(x => x.IsActive && x.BaseCurrency == reporting && x.QuoteCurrency == from && x.RateDate.Date <= at.Date).OrderByDescending(x => x.RateDate).FirstOrDefaultAsync(ct);
            if (inverse is not null && inverse.Rate > 0) return rateCache[key] = 1 / inverse.Rate;
            missing.Add($"{from}/{reporting}: Gerekli döviz kuru bulunamadı."); return null;
        }
        async Task Add(string category, string sourceType, Guid? sourceId, string description, decimal quantity, string unit, decimal unitCost, string currency, string? notes = null)
        {
            var exchange = await Rate(currency); if (!exchange.HasValue) return;
            var source = Round(quantity * unitCost); lines.Add(new(null, category, sourceType, sourceId, description, Round(quantity), unit,
                Round(unitCost), NormalizeCurrency(currency), Round(exchange.Value), reporting, source, Round(source * exchange.Value), notes));
        }

        var recipeMaterialIds = workOrder.Recipe?.Items.Select(x => x.MaterialId).Distinct().ToList() ?? [];
        var pricedLots = await db.MaterialLots.AsNoTracking().Where(x => recipeMaterialIds.Contains(x.MaterialId) && x.IsActive && x.UnitPrice != null).ToListAsync(ct);
        var pricedStocks = await db.StockItems.AsNoTracking().Where(x => x.MaterialId.HasValue && recipeMaterialIds.Contains(x.MaterialId.Value) && x.IsActive).ToListAsync(ct);
        if (workOrder.Recipe is null) missing.Add("İş emri için geçerli reçete bulunamadı.");
        else foreach (var item in workOrder.Recipe.Items.Where(x => !x.IsOptional))
        {
            var quantity = workOrder.Recipe.OutputQuantity > 0 ? item.Quantity * workOrder.PlannedPairs / workOrder.Recipe.OutputQuantity * (1 + item.WastePercent / 100) : 0;
            var lots = pricedLots.Where(x => x.MaterialId == item.MaterialId).ToList();
            decimal? price = null; string currency = item.Material.Currency ?? "TRY"; string source = "Material";
            if (lots.Count > 0) { var weight = lots.Sum(x => x.InitialQuantity); if (weight > 0) { price = lots.Sum(x => x.UnitPrice!.Value * x.InitialQuantity) / weight; currency = lots[0].Currency; source = "ActiveLotWeightedAverage"; } }
            price ??= item.Material.LastPurchasePrice;
            if (!price.HasValue) { var stock = pricedStocks.FirstOrDefault(x => x.MaterialId == item.MaterialId); price = stock?.LastPurchasePrice; currency = stock?.Currency ?? currency; source = "StockItemLastPurchasePrice"; }
            if (!price.HasValue) { missing.Add($"{item.Material.Code}: Tüketilen malzeme için birim fiyat bulunamadı."); continue; }
            await Add("Material", "RecipeItem", item.Id, $"{item.Material.Code} · {item.Material.Name}", quantity, item.Unit, price.Value, currency, $"Fiyat kaynağı: {source}");
        }
        var estimatedMaterial = Sum(lines, "Material", "RecipeItem");

        var consumptions = await db.MaterialConsumptions.AsNoTracking().Where(x => x.WorkOrderId == workOrderId && !x.IsReversed)
            .Include(x => x.Material).Include(x => x.MaterialLot).ThenInclude(x => x.PurchaseOrderLine).ThenInclude(x => x!.PurchaseOrder)
            .Include(x => x.StockItem).ToListAsync(ct);
        if (consumptions.Count == 0) warnings.Add("Gerçek hammadde tüketimi bulunamadı.");
        foreach (var c in consumptions)
        {
            decimal? price = c.MaterialLot.UnitPrice; var currency = c.MaterialLot.Currency; var source = "MaterialLot";
            if (!price.HasValue && c.MaterialLot.PurchaseOrderLine is not null) { price = c.MaterialLot.PurchaseOrderLine.UnitPrice; currency = c.MaterialLot.PurchaseOrderLine.PurchaseOrder.Currency; source = "PurchaseOrderLine"; }
            if (!price.HasValue && c.StockItem.LastPurchasePrice.HasValue) { price = c.StockItem.LastPurchasePrice; currency = c.StockItem.Currency; source = "StockItemLastPurchasePrice"; }
            if (!price.HasValue && c.Material.LastPurchasePrice.HasValue) { price = c.Material.LastPurchasePrice; currency = c.Material.Currency ?? currency; source = "MaterialLastPurchasePrice"; }
            if (!price.HasValue) { missing.Add($"{c.ConsumptionNumber}: Tüketilen malzeme için birim fiyat bulunamadı."); continue; }
            var converted = ConvertQuantity(c.Quantity, c.Unit, c.MaterialLot.Unit);
            if (!converted.HasValue) { missing.Add($"{c.ConsumptionNumber}: {c.Unit} ile {c.MaterialLot.Unit} arasında desteklenen birim dönüşümü yok."); continue; }
            await Add("Material", "MaterialConsumption", c.Id, $"{c.ConsumptionNumber} · {c.Material.Name}", converted.Value, c.MaterialLot.Unit, price.Value, currency, $"Fiyat kaynağı: {source}; tüketim tipi: {c.ConsumptionType}");
        }
        var actualMaterial = Sum(lines, "Material", "MaterialConsumption");

        var assignments = await db.StationAssignments.AsNoTracking().Where(x => x.WorkOrderId == workOrderId)
            .Include(x => x.Downtimes).Include(x => x.Fires).ToListAsync(ct);
        decimal totalHours = 0; var produced = assignments.Sum(x => x.ProducedPairs); var fire = assignments.SelectMany(x => x.Fires).Where(x => !x.IsCancelled).Sum(x => x.FirePairs);
        foreach (var assignment in assignments)
        {
            var end = assignment.FinishedAt ?? at; var totalMinutes = Math.Max(0, (decimal)(end - assignment.StartedAt).TotalMinutes);
            var downtime = assignment.Downtimes.Where(x => !x.IsCancelled).Sum(x => x.DurationMinutes ?? (decimal)((x.EndedAt ?? at) - x.StartedAt).TotalMinutes);
            var hours = Math.Max(0, totalMinutes - Math.Max(0, downtime)) / 60; totalHours += hours;
            await Add("Labor", "StationAssignment", assignment.Id, $"İstasyon {assignment.StationNumberSnapshot} işçilik", hours, "saat", settings.DefaultHourlyLaborCost, reporting, "CostSettings varsayılan saatlik işçilik");
        }
        var machine = workOrder.AssignedMachine; var power = machine?.PowerKw ?? machine?.EnergyConsumption;
        var energyRate = machine?.DefaultEnergyCostPerKwh ?? settings.DefaultEnergyCostPerKwh;
        if (power.HasValue) await Add("Energy", "StationAssignment", workOrderId, "Üretim enerji tüketimi", totalHours * power.Value, "kWh", energyRate, reporting, machine?.PowerKw.HasValue == true ? "Makine PowerKw" : "Makine EnergyConsumption tahmini güç değeri");
        else warnings.Add("Makine güç bilgisi bulunamadı; enerji maliyeti sıfır bırakıldı.");
        await Add("Machine", "StationAssignment", workOrderId, "Makine çalışma maliyeti", totalHours, "saat", machine?.HourlyOperatingCost ?? settings.DefaultMachineCostPerHour, reporting, machine?.HourlyOperatingCost.HasValue == true ? "Makine saatlik maliyeti" : "CostSettings varsayılan makine maliyeti");

        var good = Math.Max(produced - fire, 0);
        var nonMaterialUnit = good > 0 ? (Sum(lines, "Labor") + Sum(lines, "Energy") + Sum(lines, "Machine")) / good : 0;
        if (fire > 0) await Add("Fire", "StationAssignment", workOrderId, "Fire üretim kaybı (malzeme hariç)", fire, "çift", nonMaterialUnit, reporting, "Waste tüketimleri Material maliyetinde kaldığı için yalnız malzeme dışı pay");
        var cuttingRecords = await db.CuttingRecords.AsNoTracking().Where(x => x.WorkOrderId == workOrderId && x.IsActive && !x.IsCancelled).ToListAsync(ct);
        var cutPairs = cuttingRecords.Sum(x => x.GoodPairs > 0 ? x.GoodPairs : x.CutPairs);
        foreach (var cut in cuttingRecords) await Add("Cutting", "CuttingRecord", cut.Id, cut.RecordNumber ?? "Kesim kaydı", cut.GoodPairs > 0 ? cut.GoodPairs : cut.CutPairs, "çift", settings.DefaultCuttingCostPerPair, reporting);
        var boxes = await db.ProductionBoxes.AsNoTracking().Where(x => x.WorkOrderId == workOrderId && x.IsActive && !x.IsCancelled).ToListAsync(ct);
        var packedPairs = boxes.Sum(x => x.PairCount > 0 ? x.PairCount : x.QuantityPairs);
        foreach (var box in boxes) { var pairs = box.PairCount > 0 ? box.PairCount : box.QuantityPairs; await Add("Packaging", "ProductionBox", box.Id, box.BoxCode, pairs, "çift", settings.DefaultPackagingCostPerPair, reporting); if (box.ReadyForShipmentAt.HasValue || box.ShippedAt.HasValue) await Add("Other", "ProductionBox", box.Id, $"{box.BoxCode} sevkiyat hazırlığı", 1, "koli", settings.DefaultShipmentPreparationCostPerBox, reporting); }
        var inspections = await db.QualityInspections.AsNoTracking().Where(x => x.WorkOrderId == workOrderId && x.IsActive && !x.IsCancelled).ToListAsync(ct);
        foreach (var inspection in inspections) await Add("Quality", "QualityInspection", inspection.Id, inspection.InspectionNumber, inspection.CheckedPairs, "çift", settings.DefaultQualityCostPerPair, reporting);
        var overheadBase = Sum(lines, "Material", "MaterialConsumption") + Sum(lines, "Labor") + Sum(lines, "Energy") + Sum(lines, "Machine") + Sum(lines, "Cutting") + Sum(lines, "Packaging") + Sum(lines, "Quality");
        await Add("Overhead", "Manual", settings.Id, "Genel üretim gideri", overheadBase, reporting, settings.DefaultOverheadRatePercent / 100, reporting, "Taban: malzeme + işçilik + enerji + makine + kesim + paketleme + kalite");

        var orderItemPlanned = await db.WorkOrders.AsNoTracking().Where(x => x.OrderItemId == workOrder.OrderItemId && !x.IsCancelled).SumAsync(x => x.PlannedPairs, ct);
        var allocation = orderItemPlanned > 0 ? (decimal)workOrder.PlannedPairs / orderItemPlanned : 0;
        var netOrderItemRevenue = workOrder.OrderItem.LineSubtotal - workOrder.OrderItem.DiscountAmount;
        var revenueRate = await Rate(workOrder.OrderItem.Order.Currency);
        var salesRevenue = revenueRate.HasValue ? Round(netOrderItemRevenue * allocation * revenueRate.Value) : 0;
        assumptions.Add($"Satış geliri sipariş kalemi net tutarının iş emri plan oranına göre %{allocation * 100:0.####} tahsisidir; vergi gelir dışıdır.");
        assumptions.Add("Reverse edilmiş tüketimler gerçek malzeme maliyetine dahil edilmedi.");
        var actual = lines.Where(x => x.SourceType != "RecipeItem").Sum(x => x.TotalReportingAmount);
        var estimated = estimatedMaterial;
        var variance = actual - estimated; var profit = salesRevenue - actual;
        return new CostPreviewDto(workOrder.Id, workOrder.WorkOrderNumber, workOrder.CustomerNameSnapshot ?? workOrder.OrderItem.Order.Customer.Name,
            workOrder.ProductCodeSnapshot ?? workOrder.Product.Code, workOrder.ProductNameSnapshot ?? workOrder.Product.Name, at, reporting,
            workOrder.PlannedPairs, produced, good, fire, cutPairs, packedPairs, Round(estimatedMaterial), Round(actualMaterial),
            Round(Sum(lines, "Labor")), Round(Sum(lines, "Energy")), Round(Sum(lines, "Machine")), Round(Sum(lines, "Fire")),
            Round(Sum(lines, "Cutting")), Round(Sum(lines, "Packaging")), Round(Sum(lines, "Quality")), Round(Sum(lines, "Overhead")),
            Round(Sum(lines, "Other")), Round(estimated), Round(actual), workOrder.PlannedPairs > 0 ? Round(estimated / workOrder.PlannedPairs) : null,
            produced > 0 ? Round(actual / produced) : null, good > 0 ? Round(actual / good) : null, Round(variance), estimated != 0 ? Round(variance / estimated * 100) : null,
            salesRevenue, Round(profit), salesRevenue != 0 ? Round(profit / salesRevenue * 100) : null, lines, warnings, missing, assumptions, missing.Count == 0);
    }

    private static decimal Sum(IEnumerable<CostLineDto> lines, string category, string? sourceType = null) =>
        lines.Where(x => x.CostCategory == category && (sourceType is null || x.SourceType == sourceType)).Sum(x => x.TotalReportingAmount);
    private static CostLineDto ToLineDto(WorkOrderCostLine x) => new(x.Id, x.CostCategory, x.SourceType, x.SourceId, x.Description, x.Quantity, x.Unit, x.UnitCost, x.SourceCurrency, x.ExchangeRate, x.ReportingCurrency, x.TotalSourceAmount, x.TotalReportingAmount, x.Notes);
    private static decimal? ConvertQuantity(decimal value, string from, string to) { from = Unit(from); to = Unit(to); if (from == to) return value; return (from, to) switch { ("g", "kg") => value / 1000, ("kg", "g") => value * 1000, ("ml", "l") => value / 1000, ("l", "ml") => value * 1000, _ => null }; }
    private static string Unit(string x) => x.Trim().ToLowerInvariant() switch { "gr" or "gram" => "g", "kilogram" => "kg", "lt" or "liter" or "litre" => "l", _ => x.Trim().ToLowerInvariant() };
    private static string NormalizeCurrency(string value) { var x = value.Trim().ToUpperInvariant(); return x is "TRY" or "EUR" or "USD" or "GBP" ? x : throw new InvalidOperationException("Desteklenmeyen para birimi."); }
    private static string NormalizeCalculationType(string value) => value.Trim() switch { "Draft" => "Draft", "Recalculation" => "Recalculation", "Final" => "Final", _ => throw new InvalidOperationException("Geçersiz hesaplama tipi.") };
    private static decimal Round(decimal value) => Math.Round(value, 4);
}
