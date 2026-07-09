using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
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
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
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

        var purchase = new PurchaseOrder
        {
            SupplierName = request.SupplierName.Trim(),
            SupplierCode = request.SupplierCode,
            DocumentNo = request.DocumentNo,
            InvoiceNo = request.InvoiceNo,
            OrderDate = request.OrderDate ?? DateTime.UtcNow,
            DueDate = request.DueDate,
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

            stockItem.CurrentQuantity += requestLine.Quantity;
            stockItem.LastPurchasePrice = requestLine.UnitPrice;
            stockItem.SupplierName = purchase.SupplierName;
            stockItem.SupplierCode = purchase.SupplierCode;
            stockItem.Currency = purchase.Currency;

            _db.StockMovements.Add(new StockMovement
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

        return Ok(ApiResponse<object>.SuccessResponse(ToPurchaseResponse(purchase), "Satın alma oluşturuldu."));
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
