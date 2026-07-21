using System.Text.Json;
using Asp.Versioning;
using Fixar.API.Security;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Domain.Services;
using Fixar.Infrastructure.Identity;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize(Policy = AuthorizationPolicies.CanViewQuotes)]
[Route("api/v{version:apiVersion}/quotes")]
public sealed class QuotesController(ApplicationDbContext db) : ControllerBase
{
    private static readonly string[] Currencies = ["TRY", "EUR", "USD", "GBP"];

    [HttpGet]
    public async Task<IActionResult> List(Guid? customerId, string? status, DateTime? dateFrom, DateTime? dateTo, string? currency, string? search, CancellationToken ct)
    {
        var q = db.Quotes.AsNoTracking().AsQueryable();
        if (customerId.HasValue) q = q.Where(x => x.CustomerId == customerId); if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        if (dateFrom.HasValue) q = q.Where(x => x.QuoteDate >= dateFrom); if (dateTo.HasValue) q = q.Where(x => x.QuoteDate <= dateTo);
        if (!string.IsNullOrWhiteSpace(currency)) q = q.Where(x => x.Currency == currency); if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim().ToLower(); q = q.Where(x => x.QuoteNumber.ToLower().Contains(s) || x.Customer.Name.ToLower().Contains(s) || (x.Customer.CompanyName != null && x.Customer.CompanyName.ToLower().Contains(s))); }
        var rows = await q.OrderByDescending(x => x.QuoteDate).Select(x => new { x.Id, x.QuoteNumber, x.CustomerId, CustomerName = x.Customer.CompanyName ?? x.Customer.Name, x.QuoteDate, x.ValidUntil, x.Currency, x.PaymentTermDays, x.PartialDeliveryAllowed, x.TotalSalesAmount, x.TotalEstimatedCost, x.EstimatedGrossProfit, x.EstimatedGrossMarginPercent, x.EstimatedLeadTimeDays, x.EstimatedDeliveryDate, x.Status, x.ConvertedOrderId, ItemCount = x.Items.Count, WarningCount = x.CalculationWarnings == null ? 0 : 1 }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var row = await db.Quotes.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.QuoteNumber, x.CustomerId, CustomerName = x.Customer.CompanyName ?? x.Customer.Name, x.QuoteDate, x.ValidUntil, x.Currency, x.PaymentTermDays, x.PartialDeliveryAllowed, x.Status, x.Notes, x.TotalSalesAmount, x.TotalEstimatedCost, x.EstimatedGrossProfit, x.EstimatedGrossMarginPercent, x.EstimatedLeadTimeDays, x.EstimatedDeliveryDate, x.ConvertedOrderId, x.ApprovedAt, x.ApprovedBy, x.RejectedAt, x.RejectionReason, x.IsCancelled, x.CalculationWarnings, x.LastCalculatedAt, Items = x.Items.OrderBy(i => i.LineNumber).Select(i => new { i.Id, i.LineNumber, i.ProductId, ProductCode = i.Product.Code, ProductName = i.Product.Name, i.Size, i.Color, i.Quantity, i.UnitPrice, i.FabricRequired, i.DtfRequired, i.LabelDescription, i.UnitEstimatedCost, i.TotalEstimatedCost, i.TotalSalesAmount, i.EstimatedGrossProfit, i.EstimatedGrossMarginPercent, i.EstimatedLeadTimeDays, i.CalculationWarnings, i.Notes }) }).FirstOrDefaultAsync(ct);
        return row is null ? NotFound(ApiResponse<object>.Fail("Teklif bulunamadı.", "QUOTE_NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(row));
    }

    [HttpPost, Authorize(Policy = AuthorizationPolicies.CanManageQuotes), Idempotent]
    public async Task<IActionResult> Create(QuoteRequest request, CancellationToken ct)
    {
        var validation = await Validate(request, ct); if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation, "VALIDATION_ERROR"));
        await using var tx = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81003)", ct);
        var quote = new Quote { QuoteNumber = await NextNumber(request.QuoteDate, ct), Status = "Draft" }; Apply(quote, request); db.Quotes.Add(quote); await db.SaveChangesAsync(ct); await Recalculate(quote, ct); await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Detail), new { id = quote.Id, version = "1.0" }, ApiResponse<object>.SuccessResponse(new { quote.Id, quote.QuoteNumber, quote.Status }, "Teklif oluşturuldu."));
    }

    [HttpPut("{id:guid}"), Authorize(Policy = AuthorizationPolicies.CanManageQuotes)]
    public async Task<IActionResult> Update(Guid id, QuoteRequest request, CancellationToken ct)
    {
        var quote = await db.Quotes.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct); if (quote is null) return NotFound(ApiResponse<object>.Fail("Teklif bulunamadı.", "QUOTE_NOT_FOUND"));
        if (quote.Status != "Draft") return Conflict(ApiResponse<object>.Fail("Yalnız taslak teklif değiştirilebilir.", "QUOTE_LOCKED")); var validation = await Validate(request, ct); if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation, "VALIDATION_ERROR"));
        await using var tx = await db.Database.BeginTransactionAsync(ct); db.QuoteItems.RemoveRange(quote.Items); quote.Items.Clear(); Apply(quote, request); await db.SaveChangesAsync(ct); await Recalculate(quote, ct); await tx.CommitAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { quote.Id, quote.QuoteNumber }, "Teklif güncellendi."));
    }

    [HttpPost("{id:guid}/send"), Authorize(Policy = AuthorizationPolicies.CanManageQuotes), Idempotent]
    public Task<IActionResult> Send(Guid id, CancellationToken ct) => Transition(id, "Draft", "Sent", "Teklif gönderildi.", null, ct);

    [HttpPost("{id:guid}/approve"), Authorize(Policy = AuthorizationPolicies.CanManageQuotes), Idempotent]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var quote = await db.Quotes.FirstOrDefaultAsync(x => x.Id == id, ct); if (quote is null) return NotFound(ApiResponse<object>.Fail("Teklif bulunamadı.")); if (!QuoteWorkflowRules.CanApproveOrReject(quote.Status)) return Conflict(ApiResponse<object>.Fail("Yalnız gönderilmiş teklif onaylanabilir."));
        quote.Status = "Approved"; quote.ApprovedAt = DateTime.UtcNow; quote.ApprovedBy = User.Identity?.Name; await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { quote.Id, quote.Status }, "Teklif onaylandı."));
    }

    [HttpPost("{id:guid}/reject"), Authorize(Policy = AuthorizationPolicies.CanManageQuotes), Idempotent]
    public async Task<IActionResult> Reject(Guid id, QuoteReasonRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(ApiResponse<object>.Fail("Ret nedeni zorunludur.")); var quote = await db.Quotes.FirstOrDefaultAsync(x => x.Id == id, ct); if (quote is null) return NotFound(ApiResponse<object>.Fail("Teklif bulunamadı.")); if (!QuoteWorkflowRules.CanApproveOrReject(quote.Status)) return Conflict(ApiResponse<object>.Fail("Yalnız gönderilmiş teklif reddedilebilir.")); quote.Status = "Rejected"; quote.RejectedAt = DateTime.UtcNow; quote.RejectionReason = request.Reason.Trim(); await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { quote.Id, quote.Status }, "Teklif reddedildi."));
    }

    [HttpPost("{id:guid}/cancel"), Authorize(Policy = AuthorizationPolicies.CanManageQuotes), Idempotent]
    public async Task<IActionResult> Cancel(Guid id, QuoteReasonRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(ApiResponse<object>.Fail("İptal nedeni zorunludur.")); var quote = await db.Quotes.FirstOrDefaultAsync(x => x.Id == id, ct); if (quote is null) return NotFound(ApiResponse<object>.Fail("Teklif bulunamadı.")); if (!QuoteWorkflowRules.CanCancel(quote.Status)) return Conflict(ApiResponse<object>.Fail("Yalnız taslak veya gönderilmiş teklif iptal edilebilir.")); quote.Status = "Cancelled"; quote.IsCancelled = true; quote.CancelledAt = DateTime.UtcNow; quote.CancellationReason = request.Reason.Trim(); await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { quote.Id, quote.Status }, "Teklif iptal edildi."));
    }

    [HttpPost("{id:guid}/recalculate"), Idempotent]
    public async Task<IActionResult> RecalculateEndpoint(Guid id, CancellationToken ct) { var quote = await Load(id, ct); if (quote is null) return NotFound(ApiResponse<object>.Fail("Teklif bulunamadı.")); if (quote.Status is "Converted" or "Cancelled") return Conflict(ApiResponse<object>.Fail("Bu teklif yeniden hesaplanamaz.")); var result = await Recalculate(quote, ct); return Ok(ApiResponse<object>.SuccessResponse(Preview(result), "Teklif yeniden hesaplandı.")); }
    [HttpGet("{id:guid}/material-preview")] public async Task<IActionResult> MaterialPreview(Guid id, CancellationToken ct) { var result = await PreviewFor(id, ct); return result.Error ?? Ok(ApiResponse<object>.SuccessResponse(result.Value!.Materials)); }
    [HttpGet("{id:guid}/cost-preview")] public async Task<IActionResult> CostPreview(Guid id, CancellationToken ct) { var result = await PreviewFor(id, ct); return result.Error ?? Ok(ApiResponse<object>.SuccessResponse(new { result.Value!.SalesAmount, result.Value.EstimatedCost, result.Value.GrossProfit, result.Value.GrossMarginPercent, result.Value.Warnings })); }
    [HttpGet("{id:guid}/lead-time-preview")] public async Task<IActionResult> LeadTimePreview(Guid id, CancellationToken ct) { var result = await PreviewFor(id, ct); return result.Error ?? Ok(ApiResponse<object>.SuccessResponse(new { result.Value!.LeadTimeDays, result.Value.EarliestStart, result.Value.EstimatedFinish, result.Value.ReadyToShip, result.Value.CapacityAssumptions, result.Value.Warnings })); }

    [HttpPost("{id:guid}/convert-to-order"), Authorize(Policy = AuthorizationPolicies.CanManageQuotes), Idempotent]
    public async Task<IActionResult> ConvertToOrder(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81003)", ct);
        var quote = await Load(id, ct); if (quote is null) return NotFound(ApiResponse<object>.Fail("Teklif bulunamadı.")); if (quote.ConvertedOrderId.HasValue || quote.Status == "Converted") return Conflict(ApiResponse<object>.Fail("Bu teklif daha önce siparişe dönüştürüldü.", "QUOTE_ALREADY_CONVERTED")); if (!QuoteWorkflowRules.CanConvert(quote.Status, quote.ConvertedOrderId)) return Conflict(ApiResponse<object>.Fail("Yalnız onaylı teklif siparişe dönüştürülebilir."));
        var orderDate = DateTime.UtcNow; var prefix = $"SO-{orderDate:yyyyMMdd}-"; var numbers = await db.Orders.Where(x => x.OrderNumber.StartsWith(prefix)).Select(x => x.OrderNumber).ToListAsync(ct); var next = numbers.Select(x => int.TryParse(x[prefix.Length..], out var n) ? n : 0).DefaultIfEmpty().Max() + 1;
        var first = quote.Items.OrderBy(x => x.LineNumber).First(); var order = new Order { OrderNumber = $"{prefix}{next:0000}", CustomerId = quote.CustomerId, ProductId = first.ProductId, OrderDate = orderDate, DueDate = quote.EstimatedDeliveryDate, Currency = quote.Currency, ExpectedPaymentMethod = "OpenAccount", PaymentTermDays = quote.PaymentTermDays, SizeRange = first.Size ?? "", Color = first.Color ?? "", Quantity = quote.Items.Sum(x => x.Quantity), Notes = $"Teklif: {quote.QuoteNumber}\nKısmi teslimat: {(quote.PartialDeliveryAllowed ? "Evet" : "Hayır")}\n{quote.Notes}".Trim(), Status = "Draft", IsActive = true };
        var line = 0; foreach (var item in quote.Items.OrderBy(x => x.LineNumber)) { var moldId = await db.Molds.Where(x => x.IsActive && x.ProductId == item.ProductId && (string.IsNullOrEmpty(item.Size) || x.Size == item.Size || x.SizeRange == item.Size)).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct); order.Items.Add(new OrderItem { LineNumber = ++line, ProductId = item.ProductId, MoldId = moldId, ProductionType = item.FabricRequired ? "Kumaşlı" : "Kumaşsız", SizeRange = item.Size, Color = item.Color, FabricColor = item.Color, QuantityPairs = item.Quantity, UnitPrice = item.UnitPrice, RequestedDeliveryDate = quote.EstimatedDeliveryDate, Note = $"DTF: {(item.DtfRequired ? "Evet" : "Hayır")} {item.LabelDescription} {item.Notes}".Trim(), Status = "Bekliyor", IsActive = true, LineSubtotal = item.TotalSalesAmount, LineTotal = item.TotalSalesAmount }); }
        order.Subtotal = order.Items.Sum(x => x.LineSubtotal); order.GrandTotal = order.Items.Sum(x => x.LineTotal); db.Orders.Add(order); await db.SaveChangesAsync(ct); quote.ConvertedOrderId = order.Id; quote.Status = "Converted"; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { QuoteId = quote.Id, OrderId = order.Id, order.OrderNumber }, "Teklif satış siparişine dönüştürüldü."));
    }

    private async Task<Quote?> Load(Guid id, CancellationToken ct) => await db.Quotes.Include(x => x.Customer).Include(x => x.Items).ThenInclude(x => x.Product).FirstOrDefaultAsync(x => x.Id == id, ct);
    private async Task<(QuoteCalculationResult? Value, IActionResult? Error)> PreviewFor(Guid id, CancellationToken ct) { var q = await Load(id, ct); return q is null ? (null, NotFound(ApiResponse<object>.Fail("Teklif bulunamadı."))) : (await QuoteCalculationSupport.Calculate(db, q, ct), null); }
    private async Task<QuoteCalculationResult> Recalculate(Quote quote, CancellationToken ct) { var result = await QuoteCalculationSupport.Calculate(db, quote, ct); quote.TotalSalesAmount = result.SalesAmount; quote.TotalEstimatedCost = result.EstimatedCost; quote.EstimatedGrossProfit = result.GrossProfit; quote.EstimatedGrossMarginPercent = result.GrossMarginPercent; quote.EstimatedLeadTimeDays = result.LeadTimeDays; quote.EstimatedDeliveryDate = result.ReadyToShip; quote.CalculationWarnings = result.Warnings.Count == 0 ? null : JsonSerializer.Serialize(result.Warnings); quote.LastCalculatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); return result; }
    private static object Preview(QuoteCalculationResult x) => new { x.Materials, x.SalesAmount, x.EstimatedCost, x.GrossProfit, x.GrossMarginPercent, x.LeadTimeDays, x.EarliestStart, x.EstimatedFinish, x.ReadyToShip, x.Warnings, x.CapacityAssumptions };
    private async Task<IActionResult> Transition(Guid id, string from, string to, string message, Action<Quote>? apply, CancellationToken ct) { var quote = await db.Quotes.FirstOrDefaultAsync(x => x.Id == id, ct); if (quote is null) return NotFound(ApiResponse<object>.Fail("Teklif bulunamadı.")); if (quote.Status != from) return Conflict(ApiResponse<object>.Fail($"Bu işlem yalnız {from} durumundaki teklif için yapılabilir.")); quote.Status = to; apply?.Invoke(quote); await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { quote.Id, quote.Status }, message)); }
    private async Task<string> NextNumber(DateTime date, CancellationToken ct) { var prefix = $"TKL-{date:yyyy}-"; var values = await db.Quotes.Where(x => x.QuoteNumber.StartsWith(prefix)).Select(x => x.QuoteNumber).ToListAsync(ct); var next = values.Select(x => int.TryParse(x[prefix.Length..], out var n) ? n : 0).DefaultIfEmpty().Max() + 1; return $"{prefix}{next:000000}"; }
    private async Task<string?> Validate(QuoteRequest request, CancellationToken ct) { if (request.QuoteDate == default || request.ValidUntil < request.QuoteDate.Date) return "Teklif ve geçerlilik tarihleri geçersiz."; if (!Currencies.Contains(request.Currency)) return "Desteklenmeyen para birimi."; if (request.PaymentTermDays < 0) return "Vade günü negatif olamaz."; if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId && x.IsActive, ct)) return "Aktif müşteri bulunamadı."; if (request.Items.Count == 0) return "En az bir teklif kalemi zorunludur."; foreach (var item in request.Items) { if (item.Quantity <= 0 || item.UnitPrice < 0) return "Miktar sıfırdan büyük, fiyat negatif olmamalıdır."; if (!await db.Products.AnyAsync(x => x.Id == item.ProductId && x.IsActive, ct)) return "Aktif ürün bulunamadı."; } return null; }
    private static void Apply(Quote quote, QuoteRequest request) { quote.CustomerId = request.CustomerId; quote.QuoteDate = DateTime.SpecifyKind(request.QuoteDate, DateTimeKind.Utc); quote.ValidUntil = DateTime.SpecifyKind(request.ValidUntil, DateTimeKind.Utc); quote.Currency = request.Currency; quote.PaymentTermDays = request.PaymentTermDays; quote.PartialDeliveryAllowed = request.PartialDeliveryAllowed; quote.Notes = request.Notes; var line = 0; foreach (var item in request.Items) quote.Items.Add(new QuoteItem { LineNumber = ++line, ProductId = item.ProductId, Size = item.Size, Color = item.Color, Quantity = item.Quantity, UnitPrice = item.UnitPrice, FabricRequired = item.FabricRequired, DtfRequired = item.DtfRequired, LabelDescription = item.LabelDescription, Notes = item.Notes }); }
}

public sealed record QuoteRequest(Guid CustomerId, DateTime QuoteDate, DateTime ValidUntil, string Currency, int PaymentTermDays, bool PartialDeliveryAllowed, string? Notes, List<QuoteItemRequest> Items);
public sealed record QuoteItemRequest(Guid ProductId, string? Size, string? Color, int Quantity, decimal UnitPrice, bool FabricRequired, bool DtfRequired, string? LabelDescription, string? Notes);
public sealed record QuoteReasonRequest(string Reason);
