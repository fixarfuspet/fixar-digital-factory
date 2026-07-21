using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Fixar.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fixar.API.Security;
using System.Data;

namespace Fixar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.CanManagePurchases)]
[Route("api/v{version:apiVersion}/purchases")]
public class PurchasesController : ControllerBase
{
    private static readonly string[] PaymentTypes =
    {
        "Nakit",
        "Havale",
        "Kredi Kartı",
        "Çek",
        "Cari Hesap"
    };

    private readonly ApplicationDbContext _db;

    public PurchasesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var purchases = await _db.PurchaseOrders
            .Include(x => x.Lines)
            .OrderByDescending(x => x.OrderDate)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(purchases.Select(ToPurchaseResponse)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var purchase = await _db.PurchaseOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (purchase is null)
            return NotFound(ApiResponse<object>.Fail("Satın alma bulunamadı.", "PURCHASE_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(ToPurchaseResponse(purchase)));
    }

    [HttpPost]
    [Idempotent]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockPurchaseWrites(cancellationToken);
        if (string.IsNullOrWhiteSpace(request.SupplierName))
            return BadRequest(ApiResponse<object>.Fail("Tedarikçi adı zorunludur.", "SUPPLIER_NAME_REQUIRED"));

        var paymentType = request.PaymentType?.Trim() ?? "Cari Hesap";

        if (!PaymentTypes.Contains(paymentType))
            return BadRequest(ApiResponse<object>.Fail("Geçersiz ödeme tipi.", "INVALID_PAYMENT_TYPE"));

        if (request.Lines is null || request.Lines.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("En az bir satın alma satırı zorunludur.", "LINES_REQUIRED"));

        var stockIds = request.Lines.Select(x => x.StockItemId).Distinct().ToList();
        var stockItems = await _db.StockItems
            .Where(x => stockIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var line in request.Lines)
        {
            if (!stockItems.ContainsKey(line.StockItemId))
                return NotFound(ApiResponse<object>.Fail("Stok kartı bulunamadı.", "STOCK_NOT_FOUND"));

            if (line.Quantity <= 0)
                return BadRequest(ApiResponse<object>.Fail("Satır miktarı 0'dan büyük olmalıdır.", "INVALID_QUANTITY"));

            if (line.UnitPrice < 0)
                return BadRequest(ApiResponse<object>.Fail("Birim fiyat negatif olamaz.", "INVALID_UNIT_PRICE"));
        }

        var orderDate = ToUtc(request.OrderDate) ?? DateTime.UtcNow;
        var dueDate = ToUtc(request.DueDate);

        var purchase = new PurchaseOrder
        {
            SupplierName = request.SupplierName.Trim(),
            SupplierCode = request.SupplierCode,
            DocumentNo = request.DocumentNo,
            InvoiceNo = request.InvoiceNo,
            OrderDate = orderDate,
            DueDate = dueDate,
            PaymentType = paymentType,
            Currency = request.Currency ?? "TRY",
            VatRate = request.VatRate,
            Status = request.Status ?? "Oluşturuldu",
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        var sourceDocumentNo = request.InvoiceNo ?? request.DocumentNo;

        foreach (var requestLine in request.Lines)
        {
            var stockItem = stockItems[requestLine.StockItemId];
            var lineTotal = requestLine.LineTotal ?? requestLine.Quantity * requestLine.UnitPrice;

            purchase.Lines.Add(new PurchaseOrderLine
            {
                StockItemId = stockItem.Id,
                StockName = string.IsNullOrWhiteSpace(requestLine.StockName) ? stockItem.Name : requestLine.StockName.Trim(),
                Quantity = requestLine.Quantity,
                Unit = string.IsNullOrWhiteSpace(requestLine.Unit) ? stockItem.Unit : requestLine.Unit.Trim(),
                UnitPrice = requestLine.UnitPrice,
                LineTotal = lineTotal,
                Note = requestLine.Note
            });

            if (!stockItem.MaterialId.HasValue)
                stockItem.CurrentQuantity += requestLine.Quantity;
            stockItem.LastPurchasePrice = requestLine.UnitPrice;
            stockItem.SupplierName = purchase.SupplierName;
            stockItem.SupplierCode = purchase.SupplierCode;
            stockItem.Currency = purchase.Currency;

            if (!stockItem.MaterialId.HasValue) _db.StockMovements.Add(new StockMovement
            {
                StockItemId = stockItem.Id,
                MovementType = "Giriş",
                Quantity = requestLine.Quantity,
                UnitPrice = requestLine.UnitPrice,
                MovementDate = purchase.OrderDate,
                SourceType = "Satın Alma",
                SourceDocumentNo = sourceDocumentNo,
                Note = requestLine.Note ?? purchase.Note
            });
        }

        purchase.SubTotal = request.SubTotal ?? purchase.Lines.Sum(x => x.LineTotal);
        purchase.VatTotal = request.VatTotal ?? (purchase.VatRate.HasValue ? purchase.SubTotal * purchase.VatRate.Value / 100 : 0);
        purchase.GrandTotal = request.GrandTotal ?? purchase.SubTotal + purchase.VatTotal;

        _db.PurchaseOrders.Add(purchase);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToPurchaseResponse(purchase), "Satın alma oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    [Idempotent]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockPurchaseWrites(cancellationToken);
        var purchase = await _db.PurchaseOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (purchase is null)
            return NotFound(ApiResponse<object>.Fail("Satın alma bulunamadı.", "PURCHASE_NOT_FOUND"));

        if (IsCancelled(purchase))
            return BadRequest(ApiResponse<object>.Fail("İptal edilmiş satın alma düzenlenemez.", "PURCHASE_CANCELLED"));
        if (await _db.MaterialLots.AnyAsync(x => x.PurchaseOrderId == id, cancellationToken))
            return Conflict(ApiResponse<object>.Fail("Lot kabulü yapılmış satın alma satırları değiştirilemez.", "PURCHASE_HAS_LOTS"));

        var validation = await ValidatePurchaseRequest(request, cancellationToken);

        if (validation is not null)
            return validation;

        var existingLines = purchase.Lines.ToList();
        var oldQuantities = existingLines
            .GroupBy(x => x.StockItemId)
            .ToDictionary(x => x.Key, x => x.Sum(line => line.Quantity));
        var newQuantities = request.Lines!
            .GroupBy(x => x.StockItemId)
            .ToDictionary(x => x.Key, x => x.Sum(line => line.Quantity));
        var allStockIds = oldQuantities.Keys
            .Union(newQuantities.Keys)
            .ToList();
        var stockItems = await _db.StockItems
            .Where(x => allStockIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var stockId in allStockIds)
        {
            if (!stockItems.ContainsKey(stockId))
                return NotFound(ApiResponse<object>.Fail("Stok kartı bulunamadı.", "STOCK_NOT_FOUND"));
        }

        var orderDate = ToUtc(request.OrderDate) ?? ToUtc(purchase.OrderDate) ?? DateTime.UtcNow;
        var dueDate = ToUtc(request.DueDate);
        var paymentType = request.PaymentType?.Trim() ?? "Cari Hesap";

        purchase.SupplierName = request.SupplierName.Trim();
        purchase.SupplierCode = request.SupplierCode;
        purchase.DocumentNo = request.DocumentNo;
        purchase.InvoiceNo = request.InvoiceNo;
        purchase.OrderDate = orderDate;
        purchase.DueDate = dueDate;
        purchase.PaymentType = paymentType;
        purchase.Currency = request.Currency ?? "TRY";
        purchase.VatRate = request.VatRate;
        if (!string.Equals(request.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            purchase.Status = request.Status ?? purchase.Status;
        purchase.Note = request.Note;

        var sourceDocumentNo = request.InvoiceNo ?? request.DocumentNo;

        foreach (var stockId in allStockIds)
        {
            var oldQuantity = oldQuantities.TryGetValue(stockId, out var currentOldQuantity) ? currentOldQuantity : 0;
            var newQuantity = newQuantities.TryGetValue(stockId, out var currentNewQuantity) ? currentNewQuantity : 0;
            var difference = newQuantity - oldQuantity;

            if (difference == 0)
                continue;

            var stockItem = stockItems[stockId];
            if (!stockItem.MaterialId.HasValue && stockItem.CurrentQuantity + difference < 0)
                return Conflict(ApiResponse<object>.Fail("Satın alma güncellemesi stok miktarını negatife düşüremez.", "PURCHASE_STOCK_CONFLICT"));
            if (!stockItem.MaterialId.HasValue)
                stockItem.CurrentQuantity += difference;
            stockItem.SupplierName = purchase.SupplierName;
            stockItem.SupplierCode = purchase.SupplierCode;
            stockItem.Currency = purchase.Currency;

            var requestLine = request.Lines!.LastOrDefault(x => x.StockItemId == stockId);

            if (requestLine is not null)
                stockItem.LastPurchasePrice = requestLine.UnitPrice;

            if (!stockItem.MaterialId.HasValue) _db.StockMovements.Add(new StockMovement
            {
                StockItemId = stockItem.Id,
                MovementType = difference > 0 ? "Giriş" : "Çıkış",
                Quantity = Math.Abs(difference),
                UnitPrice = requestLine?.UnitPrice,
                MovementDate = DateTime.UtcNow,
                SourceType = "Satın Alma Güncelleme",
                SourceDocumentNo = sourceDocumentNo,
                Note = requestLine?.Note ?? purchase.Note
            });
        }

        _db.PurchaseOrderLines.RemoveRange(existingLines);
        var newLines = new List<PurchaseOrderLine>();

        foreach (var requestLine in request.Lines!)
        {
            var stockItem = stockItems[requestLine.StockItemId];
            var lineTotal = requestLine.LineTotal ?? requestLine.Quantity * requestLine.UnitPrice;

            stockItem.LastPurchasePrice = requestLine.UnitPrice;
            stockItem.SupplierName = purchase.SupplierName;
            stockItem.SupplierCode = purchase.SupplierCode;
            stockItem.Currency = purchase.Currency;

            newLines.Add(new PurchaseOrderLine
            {
                PurchaseOrderId = purchase.Id,
                StockItemId = stockItem.Id,
                StockName = string.IsNullOrWhiteSpace(requestLine.StockName) ? stockItem.Name : requestLine.StockName.Trim(),
                Quantity = requestLine.Quantity,
                Unit = string.IsNullOrWhiteSpace(requestLine.Unit) ? stockItem.Unit : requestLine.Unit.Trim(),
                UnitPrice = requestLine.UnitPrice,
                LineTotal = lineTotal,
                Note = requestLine.Note
            });
        }

        _db.PurchaseOrderLines.AddRange(newLines);

        purchase.SubTotal = request.SubTotal ?? newLines.Sum(x => x.LineTotal);
        purchase.VatTotal = request.VatTotal ?? (purchase.VatRate.HasValue ? purchase.SubTotal * purchase.VatRate.Value / 100 : 0);
        purchase.GrandTotal = request.GrandTotal ?? purchase.SubTotal + purchase.VatTotal;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ApiResponse<object>.Fail("Satın alma kaydı güncellenemedi. Kayıt değişmiş veya iptal edilmiş olabilir.", "PURCHASE_CONCURRENCY_ERROR"));
        }

        purchase.Lines = newLines;

        return Ok(ApiResponse<object>.SuccessResponse(ToPurchaseResponse(purchase), "Satın alma güncellendi."));
    }

    [HttpPost("{id:guid}/cancel")]
    [Idempotent]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockPurchaseWrites(cancellationToken);
        var purchase = await _db.PurchaseOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (purchase is null)
            return NotFound(ApiResponse<object>.Fail("Satın alma bulunamadı.", "PURCHASE_NOT_FOUND"));

        if (IsCancelled(purchase))
            return BadRequest(ApiResponse<object>.Fail("Satın alma zaten iptal edilmiş.", "PURCHASE_ALREADY_CANCELLED"));
        if (await _db.MaterialLots.AnyAsync(x => x.PurchaseOrderId == id && x.IsActive, cancellationToken))
            return Conflict(ApiResponse<object>.Fail("Aktif lot kabulü bulunan satın alma iptal edilemez.", "PURCHASE_HAS_ACTIVE_LOTS"));

        var stockIds = purchase.Lines.Select(x => x.StockItemId).Distinct().ToList();
        var stockItems = await _db.StockItems
            .Where(x => stockIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var sourceDocumentNo = purchase.InvoiceNo ?? purchase.DocumentNo;

        foreach (var line in purchase.Lines)
        {
            if (!stockItems.TryGetValue(line.StockItemId, out var stockItem))
                return NotFound(ApiResponse<object>.Fail("Stok kartı bulunamadı.", "STOCK_NOT_FOUND"));

            if (!stockItem.MaterialId.HasValue && stockItem.CurrentQuantity < line.Quantity)
                return Conflict(ApiResponse<object>.Fail("Satın alma iptali stok miktarını negatife düşüremez.", "PURCHASE_STOCK_CONFLICT"));
            if (!stockItem.MaterialId.HasValue)
                stockItem.CurrentQuantity -= line.Quantity;

            if (!stockItem.MaterialId.HasValue) _db.StockMovements.Add(new StockMovement
            {
                StockItemId = stockItem.Id,
                MovementType = "Çıkış",
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                MovementDate = DateTime.UtcNow,
                SourceType = "Satın Alma İptal",
                SourceDocumentNo = sourceDocumentNo,
                Note = line.Note ?? purchase.Note
            });
        }

        purchase.Status = "Cancelled";

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToPurchaseResponse(purchase), "Satın alma iptal edildi."));
    }

    [HttpPost("{id:guid}/complete"),Idempotent]
    public async Task<IActionResult>Complete(Guid id,CancellationToken ct){await using var tx=await _db.Database.BeginTransactionAsync(ct);var p=await _db.PurchaseOrders.FirstOrDefaultAsync(x=>x.Id==id,ct);if(p is null)return NotFound(ApiResponse<object>.Fail("Satın alma siparişi bulunamadı.","NOT_FOUND"));if(await _db.SupplierPayables.AnyAsync(x=>x.PurchaseOrderId==id&&!x.IsCancelled,ct))return Conflict(ApiResponse<object>.Fail("Bu satın alma siparişi için borç kaydı zaten oluşturulmuş.","DUPLICATE"));var s=await _db.Suppliers.FirstOrDefaultAsync(x=>(p.SupplierCode!=null&&x.Code==p.SupplierCode)||x.Name==p.SupplierName,ct);if(s is null)return Conflict(ApiResponse<object>.Fail("Tedarikçi kartı bulunamadı; kod veya ad eşleştirmesi gereklidir.","SUPPLIER_NOT_FOUND"));p.Status="Completed";var due=p.DueDate??p.OrderDate.AddDays(s.PaymentTermDays??0);var payable=new SupplierPayable{Id=Guid.NewGuid(),PayableNumber=await SupplierFinanceSupport.Next(_db,"PAY",p.OrderDate,ct),SupplierId=s.Id,PurchaseOrderId=p.Id,PurchaseOrderNumberSnapshot=p.InvoiceNo??p.DocumentNo,TransactionDate=p.OrderDate,DueDate=due,Currency=p.Currency,OriginalAmount=p.GrandTotal,OutstandingAmount=p.GrandTotal,Status=due.Date<DateTime.UtcNow.Date?"Overdue":"Open",SourceType="PurchaseOrder",Description="Satın alma GrandTotal (KDV dahil)"};_db.SupplierPayables.Add(payable);_db.SupplierLedgerEntries.Add(new SupplierLedgerEntry{Id=Guid.NewGuid(),EntryNumber=await SupplierFinanceSupport.Next(_db,"SLE",p.OrderDate,ct),SupplierId=s.Id,TransactionDate=p.OrderDate,DueDate=due,EntryType="Credit",SourceType="Payable",SourceId=payable.Id,ReferenceNumber=payable.PayableNumber,Currency=p.Currency,CreditAmount=p.GrandTotal,Description=payable.Description,Created=DateTime.UtcNow});await _db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Ok(ApiResponse<object>.SuccessResponse(new{PurchaseOrderId=p.Id,p.Status,PayableId=payable.Id,payable.PayableNumber}));}

    private async Task<IActionResult?> ValidatePurchaseRequest(CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SupplierName))
            return BadRequest(ApiResponse<object>.Fail("Tedarikçi adı zorunludur.", "SUPPLIER_NAME_REQUIRED"));

        var paymentType = request.PaymentType?.Trim() ?? "Cari Hesap";

        if (!PaymentTypes.Contains(paymentType))
            return BadRequest(ApiResponse<object>.Fail("Geçersiz ödeme tipi.", "INVALID_PAYMENT_TYPE"));

        if (request.Lines is null || request.Lines.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("En az bir satın alma satırı zorunludur.", "LINES_REQUIRED"));

        var stockIds = request.Lines.Select(x => x.StockItemId).Distinct().ToList();
        var existingStockIds = await _db.StockItems
            .Where(x => stockIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var existingStockIdSet = existingStockIds.ToHashSet();

        foreach (var line in request.Lines)
        {
            if (!existingStockIdSet.Contains(line.StockItemId))
                return NotFound(ApiResponse<object>.Fail("Stok kartı bulunamadı.", "STOCK_NOT_FOUND"));

            if (line.Quantity <= 0)
                return BadRequest(ApiResponse<object>.Fail("Satır miktarı 0'dan büyük olmalıdır.", "INVALID_QUANTITY"));

            if (line.UnitPrice < 0)
                return BadRequest(ApiResponse<object>.Fail("Birim fiyat negatif olamaz.", "INVALID_UNIT_PRICE"));
        }

        return null;
    }

    private static bool IsCancelled(PurchaseOrder purchase)
    {
        return string.Equals(purchase.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
    }

    private async Task LockPurchaseWrites(CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational())
            await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81007)", cancellationToken);
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

    private static object ToPurchaseResponse(PurchaseOrder purchase)
    {
        return new
        {
            purchase.Id,
            purchase.SupplierName,
            purchase.SupplierCode,
            purchase.DocumentNo,
            purchase.InvoiceNo,
            purchase.OrderDate,
            purchase.DueDate,
            purchase.PaymentType,
            purchase.Currency,
            purchase.VatRate,
            purchase.SubTotal,
            purchase.VatTotal,
            purchase.GrandTotal,
            purchase.Status,
            purchase.Note,
            purchase.CreatedAt,
            Lines = purchase.Lines.Select(line => new
            {
                line.Id,
                line.PurchaseOrderId,
                line.StockItemId,
                line.StockName,
                line.Quantity,
                line.Unit,
                line.UnitPrice,
                line.LineTotal,
                line.Note
            })
        };
    }
}

public record CreatePurchaseOrderRequest(
    string SupplierName,
    string? SupplierCode,
    string? DocumentNo,
    string? InvoiceNo,
    DateTime? OrderDate,
    DateTime? DueDate,
    string? PaymentType,
    string? Currency,
    decimal? VatRate,
    decimal? SubTotal,
    decimal? VatTotal,
    decimal? GrandTotal,
    string? Status,
    string? Note,
    List<CreatePurchaseOrderLineRequest>? Lines
);

public record CreatePurchaseOrderLineRequest(
    Guid StockItemId,
    string? StockName,
    decimal Quantity,
    string? Unit,
    decimal UnitPrice,
    decimal? LineTotal,
    string? Note
);
