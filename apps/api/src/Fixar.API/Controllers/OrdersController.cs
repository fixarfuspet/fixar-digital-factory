using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Fixar.Infrastructure.Identity;
using Fixar.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/orders")]
public class OrdersController(ApplicationDbContext db) : ControllerBase
{
    private static readonly string[] Currencies = ["TRY", "EUR", "USD", "GBP"];
    private static readonly string[] PaymentMethods = ["Cash", "BankTransfer", "CreditCard", "Cheque", "OpenAccount"];
    private static readonly string[] EditableStatuses = ["Draft", "Confirmed", "Aktif"];

    [HttpGet]
    public async Task<IActionResult> List(Guid? customerId, Guid? productId, string? status, DateTime? dateFrom, DateTime? dateTo, DateTime? dueFrom, DateTime? dueTo, string? currency, string? search, bool? isActive, CancellationToken ct)
    {
        var q = db.Orders.AsNoTracking().AsQueryable();
        if (customerId.HasValue) q = q.Where(x => x.CustomerId == customerId);
        if (productId.HasValue) q = q.Where(x => x.Items.Any(i => i.ProductId == productId));
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        if (dateFrom.HasValue) q = q.Where(x => x.OrderDate >= dateFrom); if (dateTo.HasValue) q = q.Where(x => x.OrderDate <= dateTo);
        if (dueFrom.HasValue) q = q.Where(x => x.DueDate >= dueFrom); if (dueTo.HasValue) q = q.Where(x => x.DueDate <= dueTo);
        if (!string.IsNullOrWhiteSpace(currency)) q = q.Where(x => x.Currency == currency.ToUpper()); if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive);
        if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim().ToLower(); q = q.Where(x => x.OrderNumber.ToLower().Contains(s) || x.Customer.Name.ToLower().Contains(s) || (x.Customer.CompanyName != null && x.Customer.CompanyName.ToLower().Contains(s))); }
        var rows = await q.OrderByDescending(x => x.OrderDate).Select(x => new { x.Id, x.OrderNumber, x.OrderDate, x.CustomerId, x.Customer.CustomerCode, CustomerName = x.Customer.CompanyName ?? x.Customer.Name, x.Currency, x.ExpectedPaymentMethod, x.PaymentTermDays, x.DueDate, ItemCount = x.Items.Count(i => i.IsActive && !i.IsCancelled), TotalPairs = x.Items.Where(i => i.IsActive && !i.IsCancelled).Sum(i => i.QuantityPairs), ProducedPairs = x.Items.Sum(i => i.ProducedPairs), CutPairs = x.Items.Sum(i => i.CutPairs), ShippedPairs = x.Items.Sum(i => i.ShippedPairs), x.Subtotal, x.DiscountAmount, x.TaxAmount, x.GrandTotal, x.Status, x.IsActive, Receivable=db.CustomerReceivables.Where(r=>r.OrderId==x.Id&&!r.IsCancelled).Select(r=>new{r.Id,r.ReceivableNumber,r.Status,r.OriginalAmount,r.AllocatedAmount,r.OutstandingAmount,r.DueDate,OverdueDays=r.OutstandingAmount>0&&r.DueDate<DateTime.UtcNow.Date?(DateTime.UtcNow.Date-r.DueDate.Date).Days:0,CollectionCount=r.Allocations.Count(a=>!a.IsReversed)}).FirstOrDefault(), CreatedAt = x.Created, UpdatedAt = x.LastModified }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var x = await db.Orders.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.OrderNumber, x.OrderDate, x.CustomerId, x.CustomerReference, x.Currency, x.ExpectedPaymentMethod, x.PaymentTermDays, x.DiscountPercent, x.TaxPercent, x.Subtotal, x.DiscountAmount, x.TaxAmount, x.GrandTotal, x.Notes, x.DueDate, x.Status, x.IsActive, x.IsCancelled, x.CancellationReason, Customer = new { x.Customer.CustomerCode, Name = x.Customer.CompanyName ?? x.Customer.Name }, Receivable=db.CustomerReceivables.Where(r=>r.OrderId==x.Id&&!r.IsCancelled).Select(r=>new{r.Id,r.ReceivableNumber,r.Status,r.OriginalAmount,r.AllocatedAmount,r.OutstandingAmount,r.DueDate,OverdueDays=r.OutstandingAmount>0&&r.DueDate<DateTime.UtcNow.Date?(DateTime.UtcNow.Date-r.DueDate.Date).Days:0,CollectionCount=r.Allocations.Count(a=>!a.IsReversed)}).FirstOrDefault(), Items = x.Items.OrderBy(i => i.LineNumber).Select(i => new { i.Id, i.LineNumber, i.ProductId, ProductCode = i.Product != null ? i.Product.Code : null, ProductName = i.Product != null ? i.Product.Name : null, i.MoldId, MoldName = i.Mold != null ? i.Mold.Name : null, i.ProductionType, i.FabricColor, i.SizeRange, i.Color, i.QuantityPairs, i.ProducedPairs, i.CutPairs, i.ShippedPairs, RemainingPairs = i.QuantityPairs - i.ProducedPairs, i.UnitPrice, i.DiscountPercent, i.TaxPercent, i.LineSubtotal, i.DiscountAmount, i.TaxAmount, i.LineTotal, i.RequestedDeliveryDate, i.Note, i.Status, i.IsActive, i.IsCancelled, WorkOrderCount = i.WorkOrders.Count, StationAssignmentCount = i.WorkOrders.SelectMany(w => w.StationAssignments).Count() }) }).FirstOrDefaultAsync(ct);
        return x is null ? NotFound(ApiResponse<object>.Fail("Sipariş bulunamadı.", "ORDER_NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(x));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanManageSalesOrders), Idempotent]
    public async Task<IActionResult> Create(OrderRequest request, CancellationToken ct)
    {
        var validation = await Validate(request, null, ct); if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation, "VALIDATION_ERROR"));
        await using var tx = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81002)", ct);
        var number = await NextNumber(request.OrderDate, ct); var customer = await db.Customers.FindAsync([request.CustomerId], ct);
        var order = new Order { OrderNumber = number, CustomerId = request.CustomerId, ProductId = request.Items[0].ProductId, SizeRange = request.Items[0].SizeRange ?? string.Empty, Color = request.Items[0].Color ?? request.Items[0].FabricColor ?? string.Empty, Quantity = request.Items.Sum(x => x.QuantityPairs), Status = "Draft", IsActive = true };
        ApplyHeader(order, request, customer!); ApplyItems(order, request.Items); Calculate(order); db.Orders.Add(order); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Detail), new { id = order.Id, version = "1.0" }, ApiResponse<object>.SuccessResponse(new { order.Id, order.OrderNumber, order.GrandTotal }, "Sipariş oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanManageSalesOrders)]
    public async Task<IActionResult> Update(Guid id, OrderRequest request, CancellationToken ct)
    {
        var order = await db.Orders.Include(x => x.Items).ThenInclude(x => x.WorkOrders).FirstOrDefaultAsync(x => x.Id == id, ct); if (order is null) return NotFound(ApiResponse<object>.Fail("Sipariş bulunamadı.", "ORDER_NOT_FOUND"));
        if (!EditableStatuses.Contains(order.Status)) return Conflict(ApiResponse<object>.Fail("Tamamlanan veya iptal edilen sipariş değiştirilemez.", "ORDER_LOCKED"));
        var validation = await Validate(request, order, ct); if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation, "VALIDATION_ERROR"));
        await using var tx = await db.Database.BeginTransactionAsync(ct); var customer = await db.Customers.FindAsync([request.CustomerId], ct); ApplyHeader(order, request, customer!);
        foreach (var current in order.Items) { var incoming = request.Items.FirstOrDefault(x => x.Id == current.Id); if (incoming is null) { if (current.WorkOrders.Count > 0 || current.ProducedPairs > 0) { current.IsActive = false; current.IsCancelled = true; current.CancellationReason = "Sipariş güncellemesinde kaldırıldı"; } else current.IsActive = false; } else ApplyItem(current, incoming, current.LineNumber); }
        var newItems = request.Items.Where(x => !x.Id.HasValue || x.Id == Guid.Empty).ToList(); var line = order.Items.Select(x => x.LineNumber).DefaultIfEmpty().Max(); foreach (var item in newItems) { var entity = new OrderItem(); ApplyItem(entity, item, ++line); order.Items.Add(entity); }
        order.ProductId = request.Items[0].ProductId; order.Quantity = order.Items.Where(x => x.IsActive && !x.IsCancelled).Sum(x => x.QuantityPairs); Calculate(order); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { order.Id, order.OrderNumber, order.GrandTotal }, "Sipariş güncellendi."));
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = AuthorizationPolicies.CanManageSalesOrders), Idempotent]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct) { await using var tx=await db.Database.BeginTransactionAsync(ct); var x = await db.Orders.Include(o => o.Items).Include(o=>o.Customer).FirstOrDefaultAsync(o => o.Id == id, ct); if (x is null) return NotFound(ApiResponse<object>.Fail("Sipariş bulunamadı.", "ORDER_NOT_FOUND")); if (x.Status != "Draft" && x.Status != "Aktif") return Conflict(ApiResponse<object>.Fail("Yalnız taslak sipariş onaylanabilir.", "INVALID_STATUS")); if (!x.Items.Any(i => i.IsActive && !i.IsCancelled)) return BadRequest(ApiResponse<object>.Fail("Siparişte aktif kalem bulunmalıdır.", "NO_ACTIVE_ITEMS")); if(await db.CustomerReceivables.AnyAsync(r=>r.OrderId==x.Id&&!r.IsCancelled,ct))return Conflict(ApiResponse<object>.Fail("Bu sipariş için alacak kaydı zaten oluşturulmuş.","RECEIVABLE_EXISTS")); x.Status = "Confirmed"; var due=x.OrderDate.AddDays(x.PaymentTermDays);var receivable=await CustomerFinanceSupport.CreateReceivable(db,x.Customer,x,x.GrandTotal,x.Currency,x.OrderDate,due,"SalesOrder",$"{x.OrderNumber} sipariş alacağı (vergi dahil GrandTotal)",ct);CustomerFinanceSupport.Audit(db,this,"Customer Receivable Created",receivable.Id,new{x.Id,x.OrderNumber,x.GrandTotal,x.Currency}); await db.SaveChangesAsync(ct);await tx.CommitAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { OrderId=x.Id, x.Status,ReceivableId=receivable.Id,receivable.ReceivableNumber }, "Sipariş onaylandı ve alacak oluşturuldu.")); }
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.CanManageSalesOrders), Idempotent]
    public async Task<IActionResult> Cancel(Guid id, CancelOrderRequest request, CancellationToken ct) { var x = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct); if (x is null) return NotFound(ApiResponse<object>.Fail("Sipariş bulunamadı.", "ORDER_NOT_FOUND")); if (x.Items.Any(i => i.ProducedPairs > 0)) return Conflict(ApiResponse<object>.Fail("Üretime başlamış sipariş doğrudan iptal edilemez.", "PRODUCTION_STARTED")); if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(ApiResponse<object>.Fail("İptal nedeni zorunludur.", "VALIDATION_ERROR")); var receivable=await db.CustomerReceivables.FirstOrDefaultAsync(r=>r.OrderId==id&&!r.IsCancelled,ct);if(receivable?.AllocatedAmount>0)return Conflict(ApiResponse<object>.Fail("Kısmi tahsilatı bulunan sipariş alacağı doğrudan iptal edilemez. Finansal düzeltme oluşturulmalıdır.","FINANCIAL_ADJUSTMENT_REQUIRED"));await using var tx=await db.Database.BeginTransactionAsync(ct); x.Status = "Cancelled"; x.IsCancelled = true; x.IsActive = false; x.CancellationReason = request.Reason; x.CancelledAt = DateTime.UtcNow; foreach (var i in x.Items) { i.IsCancelled = true; i.IsActive = false; i.CancellationReason = request.Reason; }if(receivable is not null){receivable.IsCancelled=true;receivable.IsActive=false;receivable.Status="Cancelled";receivable.CancellationReason=$"Sipariş iptali: {request.Reason}";receivable.CancelledAt=DateTime.UtcNow;receivable.CancelledBy=User.Identity?.Name;db.CustomerLedgerEntries.Add(new CustomerLedgerEntry{Id=Guid.NewGuid(),EntryNumber=await CustomerFinanceSupport.NextNumber(db,"LED",DateTime.UtcNow,ct),CustomerId=receivable.CustomerId,TransactionDate=DateTime.UtcNow,EntryType="Credit",SourceType="Adjustment",SourceId=receivable.Id,ReferenceNumber=receivable.ReceivableNumber,Currency=receivable.Currency,CreditAmount=receivable.OutstandingAmount,Description="Sipariş iptali alacak kapama",Created=DateTime.UtcNow});} await db.SaveChangesAsync(ct);await tx.CommitAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { x.Id, x.Status }, "Sipariş iptal edildi.")); }

    [HttpPost("{id:guid}/duplicate")]
    [Authorize(Policy = AuthorizationPolicies.CanManageSalesOrders), Idempotent]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken ct) { var source = await db.Orders.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct); if (source is null) return NotFound(ApiResponse<object>.Fail("Sipariş bulunamadı.", "ORDER_NOT_FOUND")); await using var tx = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81002)", ct); var copy = new Order { OrderNumber = await NextNumber(DateTime.UtcNow, ct), OrderDate = DateTime.UtcNow, CustomerId = source.CustomerId, ProductId = source.ProductId, SizeRange = source.SizeRange, Color = source.Color, Quantity = source.Quantity, DueDate = source.DueDate, Currency = source.Currency, ExpectedPaymentMethod = source.ExpectedPaymentMethod, PaymentTermDays = source.PaymentTermDays, DiscountPercent = source.DiscountPercent, TaxPercent = source.TaxPercent, Notes = source.Notes, Status = "Draft", IsActive = true }; foreach (var i in source.Items.Where(i => i.IsActive && !i.IsCancelled)) copy.Items.Add(new OrderItem { LineNumber = i.LineNumber, ProductId = i.ProductId, MoldId = i.MoldId, ProductionType = i.ProductionType, FabricColor = i.FabricColor, SizeRange = i.SizeRange, Color = i.Color, QuantityPairs = i.QuantityPairs, UnitPrice = i.UnitPrice, DiscountPercent = i.DiscountPercent, TaxPercent = i.TaxPercent, RequestedDeliveryDate = i.RequestedDeliveryDate, Note = i.Note, Status = "Bekliyor", IsActive = true }); Calculate(copy); db.Orders.Add(copy); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { copy.Id, copy.OrderNumber }, "Sipariş çoğaltıldı.")); }
    [HttpPost("{id:guid}/recalculate")]
    [Authorize(Policy = AuthorizationPolicies.CanManageSalesOrders), Idempotent]
    public async Task<IActionResult> Recalculate(Guid id, CancellationToken ct) { var x = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct); if (x is null) return NotFound(ApiResponse<object>.Fail("Sipariş bulunamadı.", "ORDER_NOT_FOUND")); Calculate(x); await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { x.Subtotal, x.DiscountAmount, x.TaxAmount, x.GrandTotal }, "Toplamlar yeniden hesaplandı.")); }
    [HttpGet("{id:guid}/progress")]
    public async Task<IActionResult> Progress(Guid id, CancellationToken ct) { var x = await db.Orders.AsNoTracking().Where(o => o.Id == id).Select(o => new { TotalPairs = o.Items.Where(i => i.IsActive && !i.IsCancelled).Sum(i => i.QuantityPairs), ProducedPairs = o.Items.Sum(i => i.ProducedPairs), CutPairs = o.Items.Sum(i => i.CutPairs), ShippedPairs = o.Items.Sum(i => i.ShippedPairs) }).FirstOrDefaultAsync(ct); return x is null ? NotFound(ApiResponse<object>.Fail("Sipariş bulunamadı.", "ORDER_NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(x)); }
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct) { var q = db.Orders.AsNoTracking(); var result = new { TotalOrders = await q.CountAsync(ct), OpenOrders = await q.CountAsync(x => x.IsActive && !x.IsCancelled && x.Status != "Completed", ct), TotalPairs = await q.SelectMany(x => x.Items).SumAsync(x => (int?)x.QuantityPairs, ct) ?? 0, ProducedPairs = await q.SelectMany(x => x.Items).SumAsync(x => (int?)x.ProducedPairs, ct) ?? 0, ShippedPairs = await q.SelectMany(x => x.Items).SumAsync(x => (int?)x.ShippedPairs, ct) ?? 0, OpenAmounts = await q.Where(x => x.IsActive && !x.IsCancelled && x.Status != "Completed").GroupBy(x => x.Currency).Select(x => new { Currency = x.Key, Amount = x.Sum(o => o.GrandTotal) }).ToListAsync(ct) }; return Ok(ApiResponse<object>.SuccessResponse(result)); }

    private async Task<string?> Validate(OrderRequest r, Order? existing, CancellationToken ct)
    {
        if (r.OrderDate == default) return "Sipariş tarihi zorunludur.";
        if (r.DueDate < r.OrderDate) return "Teslim tarihi sipariş tarihinden önce olamaz.";
        if (!Currencies.Contains(r.Currency.ToUpper())) return "Desteklenmeyen para birimi.";
        if (!PaymentMethods.Contains(r.ExpectedPaymentMethod)) return "Desteklenmeyen ödeme yöntemi.";
        if (r.PaymentTermDays < 0 || r.DiscountPercent is < 0 or > 100 || r.TaxPercent is < 0 or > 100) return "Vade, iskonto veya KDV değeri geçersiz.";
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.CustomerId, ct);
        if (customer is null || !customer.IsActive) return "Aktif müşteri bulunamadı.";
        if (r.Items.Count == 0) return "En az bir sipariş kalemi zorunludur.";
        if (r.Items.Where(x => x.Id.HasValue && x.Id != Guid.Empty).GroupBy(x => x.Id).Any(g => g.Count() > 1)) return "Sipariş kalemi tekrarlanamaz.";
        foreach (var i in r.Items)
        {
            if (i.QuantityPairs <= 0) return "Sipariş miktarı sıfırdan büyük olmalıdır.";
            if (i.UnitPrice < 0 || i.DiscountPercent is < 0 or > 100 || i.TaxPercent is < 0 or > 100) return "Birim fiyat, iskonto veya KDV değeri geçersiz.";
            if (!await db.Products.AsNoTracking().AnyAsync(x => x.Id == i.ProductId && x.IsActive, ct)) return "Aktif ürün bulunamadı.";
            if (i.MoldId.HasValue && !await db.Molds.AsNoTracking().AnyAsync(x => x.Id == i.MoldId && (!x.ProductId.HasValue || x.ProductId == i.ProductId), ct)) return "Kalıp seçilen ürünle uyumlu değildir.";
            if (existing is not null && i.Id.HasValue && i.Id != Guid.Empty)
            {
                var old = existing.Items.FirstOrDefault(x => x.Id == i.Id);
                if (old is null) return "Sipariş kalemi bulunamadı.";
                if (i.QuantityPairs < Math.Max(old.ProducedPairs, Math.Max(old.CutPairs, old.ShippedPairs))) return "Miktar üretilen, kesilen veya sevk edilen çift sayısının altına indirilemez.";
                if ((old.ProducedPairs > 0 || old.WorkOrders.Count > 0) && i.ProductId != old.ProductId) return "Üretime geçmiş kalemin ürünü değiştirilemez.";
            }
        }
        return null;
    }
    private async Task<string> NextNumber(DateTime date, CancellationToken ct) { var prefix = $"SO-{date:yyyyMMdd}-"; var values = await db.Orders.Where(x => x.OrderNumber.StartsWith(prefix)).Select(x => x.OrderNumber).ToListAsync(ct); var next = values.Select(x => int.TryParse(x[prefix.Length..], out var n) ? n : 0).DefaultIfEmpty().Max() + 1; return $"{prefix}{next:0000}"; }
    private static void ApplyHeader(Order x, OrderRequest r, Customer customer) { x.CustomerId = r.CustomerId; x.OrderDate = DateTime.SpecifyKind(r.OrderDate, DateTimeKind.Utc); x.DueDate = r.DueDate.HasValue ? DateTime.SpecifyKind(r.DueDate.Value, DateTimeKind.Utc) : null; x.CustomerReference = r.CustomerReference; x.Currency = string.IsNullOrWhiteSpace(r.Currency) ? customer.DefaultCurrency : r.Currency.ToUpper(); x.ExpectedPaymentMethod = r.ExpectedPaymentMethod; x.PaymentTermDays = r.PaymentTermDays; x.DiscountPercent = r.DiscountPercent; x.TaxPercent = r.TaxPercent; x.Notes = r.Notes; }
    private static void ApplyItems(Order order, IReadOnlyList<OrderItemRequest> items) { var line = 0; foreach (var r in items) { var x = new OrderItem(); ApplyItem(x, r, ++line); order.Items.Add(x); } }
    private static void ApplyItem(OrderItem x, OrderItemRequest r, int line) { x.LineNumber = line; x.ProductId = r.ProductId; x.MoldId = r.MoldId; x.ProductionType = r.ProductionType; x.FabricColor = r.FabricColor; x.SizeRange = r.SizeRange; x.Color = r.Color; x.QuantityPairs = r.QuantityPairs; x.UnitPrice = r.UnitPrice; x.DiscountPercent = r.DiscountPercent; x.TaxPercent = r.TaxPercent; x.RequestedDeliveryDate = r.RequestedDeliveryDate.HasValue ? DateTime.SpecifyKind(r.RequestedDeliveryDate.Value, DateTimeKind.Utc) : null; x.Note = r.Note; x.Status = string.IsNullOrWhiteSpace(x.Status) ? "Bekliyor" : x.Status; x.IsActive = true; }
    private static void Calculate(Order x) { foreach (var i in x.Items.Where(i => i.IsActive && !i.IsCancelled)) { i.LineSubtotal = decimal.Round(i.QuantityPairs * i.UnitPrice, 2); i.DiscountAmount = decimal.Round(i.LineSubtotal * i.DiscountPercent / 100, 2); var taxBase = i.LineSubtotal - i.DiscountAmount; i.TaxAmount = decimal.Round(taxBase * i.TaxPercent / 100, 2); i.LineTotal = taxBase + i.TaxAmount; } var active = x.Items.Where(i => i.IsActive && !i.IsCancelled).ToList(); x.Subtotal = active.Sum(i => i.LineSubtotal); var lineDiscount = active.Sum(i => i.DiscountAmount); var orderDiscount = decimal.Round((x.Subtotal - lineDiscount) * x.DiscountPercent / 100, 2); x.DiscountAmount = lineDiscount + orderDiscount; var ratio = x.Subtotal - lineDiscount == 0 ? 1 : (x.Subtotal - x.DiscountAmount) / (x.Subtotal - lineDiscount); x.TaxAmount = decimal.Round(active.Sum(i => i.TaxAmount) * ratio, 2); x.GrandTotal = x.Subtotal - x.DiscountAmount + x.TaxAmount; }
}

public record OrderRequest(Guid CustomerId, DateTime OrderDate, DateTime? DueDate, string? CustomerReference, string Currency, string ExpectedPaymentMethod, int PaymentTermDays, decimal DiscountPercent, decimal TaxPercent, string? Notes, List<OrderItemRequest> Items);
public record OrderItemRequest(Guid? Id, Guid ProductId, Guid? MoldId, string? ProductionType, string? FabricColor, string? SizeRange, string? Color, int QuantityPairs, decimal UnitPrice, decimal DiscountPercent, decimal TaxPercent, DateTime? RequestedDeliveryDate, string? Note);
public record CancelOrderRequest(string Reason);
