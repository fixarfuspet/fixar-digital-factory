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
[Route("api/v{version:apiVersion}/stocks")]
public class StocksController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StocksController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var stocks = await _db.StockItems
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.Category,
                x.Unit,
                x.CurrentQuantity,
                x.CriticalQuantity,
                x.MinimumQuantity,
                x.MaximumQuantity,
                x.LastPurchasePrice,
                x.Currency,
                x.VatRate,
                x.SupplierName,
                x.SupplierCode,
                x.LeadTimeDays,
                x.WarehouseName,
                x.LocationCode,
                x.LotNumber,
                x.ExpiryDate,
                x.RecipeUsageAmount,
                x.WasteRate,
                x.SafetyInfo,
                x.Note,
                x.IsActive,
                IsCritical = x.CurrentQuantity <= x.CriticalQuantity
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(stocks));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStockItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Stok adı zorunludur.", "NAME_REQUIRED"));

        var item = new StockItem
        {
            Name = request.Name,
            Code = request.Code,
            Category = request.Category ?? "Genel",
            Unit = request.Unit ?? "kg",
            CurrentQuantity = request.CurrentQuantity,
            CriticalQuantity = request.CriticalQuantity,
            MinimumQuantity = request.MinimumQuantity,
            MaximumQuantity = request.MaximumQuantity,
            LastPurchasePrice = request.LastPurchasePrice,
            Currency = request.Currency ?? "EUR",
            VatRate = request.VatRate,
            SupplierName = request.SupplierName,
            SupplierCode = request.SupplierCode,
            LeadTimeDays = request.LeadTimeDays,
            WarehouseName = request.WarehouseName,
            LocationCode = request.LocationCode,
            LotNumber = request.LotNumber,
            ExpiryDate = request.ExpiryDate,
            RecipeUsageAmount = request.RecipeUsageAmount,
            WasteRate = request.WasteRate,
            SafetyInfo = request.SafetyInfo,
            Note = request.Note,
            IsActive = true
        };

        _db.StockItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(item, "Stok kartı oluşturuldu."));
    }

    [HttpPost("movement")]
    public async Task<IActionResult> AddMovement([FromBody] CreateStockMovementRequest request, CancellationToken cancellationToken)
    {
        var item = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == request.StockItemId, cancellationToken);

        if (item is null)
            return NotFound(ApiResponse<object>.Fail("Stok kartı bulunamadı.", "STOCK_NOT_FOUND"));

        if (request.Quantity <= 0)
            return BadRequest(ApiResponse<object>.Fail("Miktar 0'dan büyük olmalıdır.", "INVALID_QUANTITY"));

        var movementType = request.MovementType?.Trim() ?? "Giriş";

        if (movementType != "Giriş" && movementType != "Çıkış")
            return BadRequest(ApiResponse<object>.Fail("Hareket tipi Giriş veya Çıkış olmalıdır.", "INVALID_MOVEMENT_TYPE"));

        if (movementType == "Çıkış" && item.CurrentQuantity < request.Quantity)
            return BadRequest(ApiResponse<object>.Fail("Yetersiz stok.", "INSUFFICIENT_STOCK"));

        var movement = new StockMovement
        {
            StockItemId = item.Id,
            MovementType = movementType,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            MovementDate = request.MovementDate ?? DateTime.UtcNow,
            SourceType = request.SourceType,
            SourceDocumentNo = request.SourceDocumentNo,
            Note = request.Note
        };

        if (movementType == "Giriş")
            item.CurrentQuantity += request.Quantity;
        else
            item.CurrentQuantity -= request.Quantity;

        if (request.UnitPrice.HasValue)
            item.LastPurchasePrice = request.UnitPrice.Value;

        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            StockItemId = item.Id,
            item.Name,
            item.CurrentQuantity,
            Movement = new
            {
                movement.Id,
                movement.MovementType,
                movement.Quantity,
                movement.UnitPrice,
                movement.MovementDate,
                movement.SourceType,
                movement.SourceDocumentNo,
                movement.Note
            }
        }, "Stok hareketi işlendi."));
    }

    [HttpGet("{id:guid}/movements")]
    public async Task<IActionResult> GetMovements(Guid id, CancellationToken cancellationToken)
    {
        var movements = await _db.StockMovements
            .Where(x => x.StockItemId == id)
            .OrderByDescending(x => x.MovementDate)
            .Select(x => new
            {
                x.Id,
                x.MovementType,
                x.Quantity,
                x.UnitPrice,
                x.MovementDate,
                x.SourceType,
                x.SourceDocumentNo,
                x.Note
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(movements));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateStockItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item is null)
            return NotFound(ApiResponse<object>.Fail("Stok kartı bulunamadı.", "STOCK_NOT_FOUND"));

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Stok adı zorunludur.", "NAME_REQUIRED"));

        item.Name = request.Name;
        item.Code = request.Code;
        item.Category = request.Category;
        item.Unit = request.Unit;
        item.CurrentQuantity = request.CurrentQuantity;
        item.CriticalQuantity = request.CriticalQuantity;
        item.MinimumQuantity = request.MinimumQuantity;
        item.MaximumQuantity = request.MaximumQuantity;
        item.LastPurchasePrice = request.LastPurchasePrice;
        item.Currency = request.Currency;
        item.VatRate = request.VatRate;
        item.SupplierName = request.SupplierName;
        item.SupplierCode = request.SupplierCode;
        item.LeadTimeDays = request.LeadTimeDays;
        item.WarehouseName = request.WarehouseName;
        item.LocationCode = request.LocationCode;
        item.LotNumber = request.LotNumber;
        item.ExpiryDate = request.ExpiryDate;
        item.RecipeUsageAmount = request.RecipeUsageAmount;
        item.WasteRate = request.WasteRate;
        item.SafetyInfo = request.SafetyInfo;
        item.Note = request.Note;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(item, "Stok kartı güncellendi."));
    }
}

public record CreateStockItemRequest(
    string Name,
    string? Code,
    string? Category,
    string? Unit,
    decimal CurrentQuantity,
    decimal CriticalQuantity,
    decimal? MinimumQuantity,
    decimal? MaximumQuantity,
    decimal? LastPurchasePrice,
    string? Currency,
    decimal? VatRate,
    string? SupplierName,
    string? SupplierCode,
    int? LeadTimeDays,
    string? WarehouseName,
    string? LocationCode,
    string? LotNumber,
    DateTime? ExpiryDate,
    decimal? RecipeUsageAmount,
    decimal? WasteRate,
    string? SafetyInfo,
    string? Note
);

public record CreateStockMovementRequest(
    Guid StockItemId,
    string? MovementType,
    decimal Quantity,
    decimal? UnitPrice,
    DateTime? MovementDate,
    string? SourceType,
    string? SourceDocumentNo,
    string? Note
);
