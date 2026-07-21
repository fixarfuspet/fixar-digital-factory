using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Infrastructure.Identity;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.CanViewSystemHealth)]
[Route("api/v{version:apiVersion}/system-control")]
public sealed class SystemControlController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var databaseConnected = await db.Database.CanConnectAsync(cancellationToken);
        if (!databaseConnected)
        {
            return StatusCode(503, ApiResponse<object>.Fail("Veritabanı bağlantısı kurulamadı.", "DATABASE_UNAVAILABLE"));
        }

        var pendingMigrations = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        var stockRows = await db.StockItems.AsNoTracking().Select(stock => new
        {
            stock.Id,
            stock.Code,
            stock.Name,
            stock.Unit,
            stock.CurrentQuantity,
            stock.MaterialId,
            stock.Category,
            LotQuantity = db.MaterialLots.Where(lot => lot.StockItemId == stock.Id && lot.IsActive).Sum(lot => (decimal?)lot.CurrentQuantity) ?? 0,
            ReservedQuantity = db.StockReservationLines
                .Where(line => line.StockItemId == stock.Id && line.StockReservation.Status == "Active")
                .Sum(line => (decimal?)(line.ReservedQuantity - line.ReleasedQuantity)) ?? 0
        }).ToListAsync(cancellationToken);

        var lotRows = await db.MaterialLots.AsNoTracking().Select(lot => new
        {
            lot.Id,
            lot.LotNumber,
            lot.StockItemId,
            lot.CurrentQuantity,
            lot.Unit,
            lot.Status,
            lot.IsActive,
            StockUnit = lot.StockItem.Unit,
            ContainerCount = lot.Containers.Count(container => container.IsActive),
            ContainerQuantity = lot.Containers.Where(container => container.IsActive).Sum(container => (decimal?)container.CurrentQuantity) ?? 0
        }).ToListAsync(cancellationToken);

        var negativeStocks = stockRows.Where(x => x.CurrentQuantity < 0).Select(x => Item(x.Code, x.Name, x.CurrentQuantity, x.Unit)).ToArray();
        var stockLotDifferences = stockRows.Where(x => x.MaterialId.HasValue && x.CurrentQuantity != x.LotQuantity)
            .Select(x => new { x.Id, x.Code, x.Name, StockQuantity = x.CurrentQuantity, x.LotQuantity, Difference = x.CurrentQuantity - x.LotQuantity, x.Unit }).ToArray();
        var excessiveReservations = stockRows.Where(x => x.ReservedQuantity > x.CurrentQuantity)
            .Select(x => new { x.Id, x.Code, x.Name, x.CurrentQuantity, x.ReservedQuantity, Difference = x.ReservedQuantity - x.CurrentQuantity, x.Unit }).ToArray();
        var lotContainerDifferences = lotRows.Where(x => x.ContainerCount > 0 && x.CurrentQuantity != x.ContainerQuantity)
            .Select(x => new { x.Id, x.LotNumber, LotQuantity = x.CurrentQuantity, x.ContainerQuantity, Difference = x.CurrentQuantity - x.ContainerQuantity, x.Unit }).ToArray();
        var depletedActiveLots = lotRows.Where(x => x.IsActive && x.CurrentQuantity == 0 && x.Status is not "FullyUsed" and not "Cancelled")
            .Select(x => new { x.Id, x.LotNumber, x.Status }).ToArray();
        var unitMismatches = lotRows.Where(x => x.Unit != x.StockUnit)
            .Select(x => new { x.Id, x.LotNumber, LotUnit = x.Unit, x.StockUnit }).ToArray();
        var openEmptyContainers = await db.MaterialContainers.AsNoTracking()
            .Where(x => x.IsActive && (x.Status == "Open" || x.Status == "PartiallyUsed") && x.CurrentQuantity == 0)
            .Select(x => new { x.Id, x.ContainerCode, x.Status }).ToArrayAsync(cancellationToken);
        var materialsWithoutStock = await db.Materials.AsNoTracking().Where(x => x.IsActive && !x.StockItems.Any())
            .Select(x => new { x.Id, x.Code, x.Name }).ToArrayAsync(cancellationToken);
        var rawMaterialStocksWithoutMaterial = stockRows.Where(x => !x.MaterialId.HasValue && x.Category.Contains("Hammadde", StringComparison.OrdinalIgnoreCase))
            .Select(x => new { x.Id, x.Code, x.Name }).ToArray();
        var productsWithoutRecipe = await db.Products.AsNoTracking().Where(x => x.IsActive && !x.Recipes.Any(r => r.IsActive))
            .Select(x => new { x.Id, x.Code, x.Name }).ToArrayAsync(cancellationToken);
        var productsWithoutMold = await db.Products.AsNoTracking().Where(x => x.IsActive && !x.Molds.Any(m => m.IsActive))
            .Select(x => new { x.Id, x.Code, x.Name }).ToArrayAsync(cancellationToken);
        var incompleteProductData = await db.Products.AsNoTracking()
            .Where(x => x.IsActive && (!x.AverageWeight.HasValue || !x.StandardCycleTime.HasValue))
            .Select(x => new { x.Id, x.Code, x.Name, MissingWeight = !x.AverageWeight.HasValue, MissingCycleTime = !x.StandardCycleTime.HasValue }).ToArrayAsync(cancellationToken);
        var unassignedWorkOrders = await db.WorkOrders.AsNoTracking()
            .Where(x => x.IsActive && !x.IsCancelled && x.Status != "Completed" && !x.StationAssignments.Any())
            .Select(x => new { x.Id, x.WorkOrderNumber, x.Status }).ToArrayAsync(cancellationToken);
        var productionAboveTarget = await db.StationAssignments.AsNoTracking().Where(x => x.ProducedPairs > x.PlannedPairs)
            .Select(x => new { x.Id, x.WorkOrderId, x.StationNumberSnapshot, x.PlannedPairs, x.ProducedPairs, Difference = x.ProducedPairs - x.PlannedPairs }).ToArrayAsync(cancellationToken);
        var cuttingNotBoxed = await db.CuttingRecords.AsNoTracking().Where(x => x.IsActive && !x.IsCancelled && x.GoodPairs > (db.ProductionBoxes.Where(b => b.CuttingRecordId == x.Id && b.IsActive && !b.IsCancelled).Sum(b => (int?)b.PairCount) ?? 0))
            .Select(x => new { x.Id, x.RecordNumber, x.GoodPairs, BoxedPairs = db.ProductionBoxes.Where(b => b.CuttingRecordId == x.Id && b.IsActive && !b.IsCancelled).Sum(b => (int?)b.PairCount) ?? 0 }).ToArrayAsync(cancellationToken);
        var shippedBoxesWithoutReference = await db.ProductionBoxes.AsNoTracking().Where(x => x.IsActive && !x.IsCancelled && x.Status == "Shipped" && x.ShipmentReference == null)
            .Select(x => new { x.Id, x.BoxCode, x.TraceabilityCode }).ToArrayAsync(cancellationToken);
        var confirmedOrdersWithoutReceivable = await db.Orders.AsNoTracking().Where(x => x.IsActive && !x.IsCancelled && x.Status != "Draft" && x.Status != "Cancelled" && !db.CustomerReceivables.Any(r => r.OrderId == x.Id && r.IsActive && !r.IsCancelled))
            .Select(x => new { x.Id, x.OrderNumber, x.Status, x.GrandTotal, x.Currency }).ToArrayAsync(cancellationToken);
        var collectionsWithoutFinance = await db.CustomerCollections.AsNoTracking().Where(x => !x.IsReversed && x.Status != "Draft" && x.Status != "Cancelled" && x.PaymentMethod != "Cheque" && x.FinancialTransactionId == null)
            .Select(x => new { x.Id, x.CollectionNumber, x.PaymentMethod, x.Amount, x.Currency, x.FinancePostingStatus }).ToArrayAsync(cancellationToken);
        var supplierPaymentsWithoutFinance = await db.SupplierPayments.AsNoTracking().Where(x => !x.IsReversed && x.Status != "Draft" && x.Status != "Cancelled" && x.PaymentMethod != "CustomerChequeEndorsement" && x.FinancialTransactionId == null)
            .Select(x => new { x.Id, x.PaymentNumber, x.PaymentMethod, x.Amount, x.Currency, x.FinancePostingStatus }).ToArrayAsync(cancellationToken);
        var overdueMaintenance = await db.PreventiveMaintenancePlans.AsNoTracking().Where(x => x.IsActive && x.NextDueDate < DateTime.UtcNow.Date)
            .Select(x => new { x.Id, x.PlanCode, x.Name, x.NextDueDate }).ToArrayAsync(cancellationToken);
        var expiredOpenQuotes = await db.Quotes.AsNoTracking().Where(x => x.ValidUntil < DateTime.UtcNow.Date && (x.Status == "Draft" || x.Status == "Sent" || x.Status == "Approved"))
            .Select(x => new { x.Id, x.QuoteNumber, x.ValidUntil, x.Status }).ToArrayAsync(cancellationToken);
        var approvedUnconvertedQuotes = await db.Quotes.AsNoTracking().Where(x => x.Status == "Approved" && x.ConvertedOrderId == null)
            .Select(x => new { x.Id, x.QuoteNumber, x.ApprovedAt }).ToArrayAsync(cancellationToken);
        var quoteItemsWithoutRecipe = await db.QuoteItems.AsNoTracking().Where(x => !db.Recipes.Any(r => r.ProductId == x.ProductId && r.IsActive))
            .Select(x => new { x.Id, x.QuoteId, x.LineNumber, x.ProductId }).ToArrayAsync(cancellationToken);
        var quotesWithCostWarnings = await db.Quotes.AsNoTracking().Where(x => x.CalculationWarnings != null && x.TotalEstimatedCost == null)
            .Select(x => new { x.Id, x.QuoteNumber, x.CalculationWarnings }).ToArrayAsync(cancellationToken);
        var quotesWithoutLeadTime = await db.Quotes.AsNoTracking().Where(x => !x.IsCancelled && x.Status != "Converted" && x.EstimatedLeadTimeDays == null)
            .Select(x => new { x.Id, x.QuoteNumber, x.Status }).ToArrayAsync(cancellationToken);
        var convertedQuotesWithoutOrder = await db.Quotes.AsNoTracking().Where(x => x.Status == "Converted" && x.ConvertedOrderId == null)
            .Select(x => new { x.Id, x.QuoteNumber }).ToArrayAsync(cancellationToken);
        var quotesWithMismatchedOrder = await db.Quotes.AsNoTracking().Where(x => x.ConvertedOrderId != null && x.ConvertedOrder != null && x.ConvertedOrder.CustomerId != x.CustomerId)
            .Select(x => new { x.Id, x.QuoteNumber, x.CustomerId, x.ConvertedOrderId, OrderCustomerId = x.ConvertedOrder!.CustomerId }).ToArrayAsync(cancellationToken);

        var checks = new object[]
        {
            Check("pending-migrations", "Bekleyen migration", pendingMigrations.Length, pendingMigrations),
            Check("negative-stock", "Negatif stok", negativeStocks.Length, negativeStocks),
            Check("stock-lot-difference", "Ana stok / lot toplamı farkı", stockLotDifferences.Length, stockLotDifferences),
            Check("lot-container-difference", "Lot / ambalaj toplamı farkı", lotContainerDifferences.Length, lotContainerDifferences),
            Check("reservation-exceeds-stock", "Fiziksel stoğu aşan rezervasyon", excessiveReservations.Length, excessiveReservations),
            Check("depleted-active-lot", "Tükenmiş fakat aktif lot", depletedActiveLots.Length, depletedActiveLots),
            Check("open-empty-container", "Açık fakat miktarı sıfır ambalaj", openEmptyContainers.Length, openEmptyContainers),
            Check("material-without-stock", "Stok kartı olmayan malzeme", materialsWithoutStock.Length, materialsWithoutStock),
            Check("stock-without-material", "Malzemesi olmayan hammadde stok kartı", rawMaterialStocksWithoutMaterial.Length, rawMaterialStocksWithoutMaterial),
            Check("unit-mismatch", "Lot / stok birimi uyuşmazlığı", unitMismatches.Length, unitMismatches),
            Check("product-without-recipe", "Reçetesiz ürün", productsWithoutRecipe.Length, productsWithoutRecipe),
            Check("product-without-mold", "Kalıpsız ürün", productsWithoutMold.Length, productsWithoutMold),
            Check("incomplete-product-data", "Eksik gramaj veya çevrim süresi", incompleteProductData.Length, incompleteProductData),
            Check("unassigned-work-order", "Açık fakat atamasız iş emri", unassignedWorkOrders.Length, unassignedWorkOrders),
            Check("production-above-target", "Hedefi aşan üretim", productionAboveTarget.Length, productionAboveTarget),
            Check("cutting-not-boxed", "Koliye aktarılmamış kesim", cuttingNotBoxed.Length, cuttingNotBoxed),
            Check("shipped-box-without-reference", "Sevk referansı olmayan koli", shippedBoxesWithoutReference.Length, shippedBoxesWithoutReference),
            Check("order-without-receivable", "Cari kaydı olmayan onaylı sipariş", confirmedOrdersWithoutReceivable.Length, confirmedOrdersWithoutReceivable),
            Check("collection-without-finance", "Finans hesabına işlenmemiş tahsilat", collectionsWithoutFinance.Length, collectionsWithoutFinance),
            Check("supplier-payment-without-finance", "Finans hesabına işlenmemiş tedarikçi ödemesi", supplierPaymentsWithoutFinance.Length, supplierPaymentsWithoutFinance),
            Check("overdue-maintenance", "Gecikmiş bakım planı", overdueMaintenance.Length, overdueMaintenance),
            Check("expired-open-quote", "Süresi geçmiş açık teklif", expiredOpenQuotes.Length, expiredOpenQuotes),
            Check("approved-unconverted-quote", "Onaylı fakat siparişe dönüşmemiş teklif", approvedUnconvertedQuotes.Length, approvedUnconvertedQuotes),
            Check("quote-item-without-recipe", "Reçetesi eksik teklif kalemi", quoteItemsWithoutRecipe.Length, quoteItemsWithoutRecipe),
            Check("quote-cost-warning", "Eksik maliyet girdili teklif", quotesWithCostWarnings.Length, quotesWithCostWarnings),
            Check("quote-without-lead-time", "Termin hesaplanamayan teklif", quotesWithoutLeadTime.Length, quotesWithoutLeadTime),
            Check("converted-quote-without-order", "Siparişe dönüşmüş fakat sipariş bağlantısı boş teklif", convertedQuotesWithoutOrder.Length, convertedQuotesWithoutOrder),
            Check("quote-order-mismatch", "Teklif / dönüştürülen sipariş müşteri tutarsızlığı", quotesWithMismatchedOrder.Length, quotesWithMismatchedOrder)
        };

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            CheckedAt = DateTime.UtcNow,
            Backend = "Çalışıyor",
            Database = "Bağlı",
            Authentication = "Doğrulandı",
            ActiveUser = User.Identity?.Name,
            Healthy = checks.All(x => (int)(x.GetType().GetProperty("Count")?.GetValue(x) ?? 0) == 0),
            Checks = checks
        }));
    }

    private static object Item(string? code, string name, decimal quantity, string unit) => new { Code = code, Name = name, Quantity = quantity, Unit = unit };
    private static object Check(string key, string title, int count, object details) => new { Key = key, Title = title, Count = count, Status = count == 0 ? "Healthy" : "Warning", Details = details };
}
