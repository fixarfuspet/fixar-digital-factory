using System.Data;
using Asp.Versioning;
using Fixar.API.Security;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Identity;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
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
                x.MaterialId,
                MaterialCode = x.Material != null ? x.Material.Code : null,
                MaterialName = x.Material != null ? x.Material.Name : null,
                MaterialType = x.Material != null ? x.Material.MaterialType : null,
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
                IsCritical = x.CurrentQuantity <= x.CriticalQuantity,
                LotCount = _db.MaterialLots.Count(l => l.StockItemId == x.Id),
                ActiveLotCount = _db.MaterialLots.Count(l => l.StockItemId == x.Id && l.IsActive),
                ContainerCount = _db.MaterialContainers.Count(c => c.StockItemId == x.Id),
                OpenContainerCount = _db.MaterialContainers.Count(c => c.StockItemId == x.Id && c.IsActive && (c.Status == "Open" || c.Status == "PartiallyUsed")),
                NearestExpiryDate = _db.MaterialLots.Where(l => l.StockItemId == x.Id && l.IsActive && l.ExpiryDate != null).Min(l => l.ExpiryDate)
                ,ActiveReservedQuantity = _db.StockReservationLines.Where(l => l.StockItemId == x.Id && l.StockReservation.Status == "Active").Sum(l => l.ReservedQuantity - l.ReleasedQuantity)
                ,FreeStockQuantity = x.CurrentQuantity - _db.StockReservationLines.Where(l => l.StockItemId == x.Id && l.StockReservation.Status == "Active").Sum(l => l.ReservedQuantity - l.ReleasedQuantity)
                ,ActiveReservationCount = _db.StockReservationLines.Where(l => l.StockItemId == x.Id && l.StockReservation.Status == "Active").Select(l => l.StockReservationId).Distinct().Count()
                ,TotalConsumedQuantity = _db.MaterialConsumptions.Where(c => c.StockItemId == x.Id && !c.IsReversed).Sum(c => c.Quantity)
                ,LastConsumptionAt = _db.MaterialConsumptions.Where(c => c.StockItemId == x.Id && !c.IsReversed).Max(c => (DateTime?)c.ConsumptionDate)
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(stocks));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanManageMaterials), Idempotent]
    public async Task<IActionResult> Create([FromBody] CreateStockItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Stok adı zorunludur.", "NAME_REQUIRED"));
        if (request.CurrentQuantity < 0)
            return BadRequest(ApiResponse<object>.Fail("Stok miktarı negatif olamaz.", "STOCK_NEGATIVE"));

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
    [Authorize(Policy = AuthorizationPolicies.CanManageWarehouse), Idempotent]
    public async Task<IActionResult> AddMovement([FromBody] CreateStockMovementRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockStockWrites(cancellationToken);
        var item = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == request.StockItemId, cancellationToken);

        if (item is null)
            return NotFound(ApiResponse<object>.Fail("Stok kartı bulunamadı.", "STOCK_NOT_FOUND"));
        if (item.MaterialId.HasValue)
            return Conflict(ApiResponse<object>.Fail("Hammadde stok miktarı lot, tüketim veya geri alma işlemi üzerinden değiştirilmelidir.", "LOT_CONTROLLED_STOCK"));

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
        await transaction.CommitAsync(cancellationToken);

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
    [Authorize(Policy = AuthorizationPolicies.CanManageMaterials), Idempotent]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateStockItemRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockStockWrites(cancellationToken);
        var item = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item is null)
            return NotFound(ApiResponse<object>.Fail("Stok kartı bulunamadı.", "STOCK_NOT_FOUND"));

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Stok adı zorunludur.", "NAME_REQUIRED"));
        if (request.CurrentQuantity < 0)
            return BadRequest(ApiResponse<object>.Fail("Stok miktarı negatif olamaz.", "STOCK_NEGATIVE"));
        if (item.MaterialId.HasValue && request.CurrentQuantity != item.CurrentQuantity)
            return Conflict(ApiResponse<object>.Fail("Hammadde stok miktarı stok kartından değiştirilemez; lot işlemlerini kullanın.", "LOT_CONTROLLED_STOCK"));
        if (!item.MaterialId.HasValue && request.CurrentQuantity != item.CurrentQuantity && string.IsNullOrWhiteSpace(request.Note))
            return BadRequest(ApiResponse<object>.Fail("Stok miktarı düzeltmesi için açıklama zorunludur.", "ADJUSTMENT_REASON_REQUIRED"));

        if (!item.MaterialId.HasValue && request.CurrentQuantity != item.CurrentQuantity)
        {
            var difference = request.CurrentQuantity - item.CurrentQuantity;
            _db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(), StockItemId = item.Id,
                MovementType = difference > 0 ? "Sayım Girişi" : "Sayım Çıkışı",
                Quantity = Math.Abs(difference), MovementDate = DateTime.UtcNow,
                SourceType = "StockAdjustment", Note = request.Note!.Trim()
            });
        }

        item.Name = request.Name;
        item.Code = request.Code ?? item.Code;
        item.Category = request.Category ?? item.Category;
        item.Unit = request.Unit ?? item.Unit;
        item.CurrentQuantity = request.CurrentQuantity;
        item.CriticalQuantity = request.CriticalQuantity;
        item.MinimumQuantity = request.MinimumQuantity;
        item.MaximumQuantity = request.MaximumQuantity;
        item.LastPurchasePrice = request.LastPurchasePrice;
        item.Currency = request.Currency ?? item.Currency;
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
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(item, "Stok kartı güncellendi."));
    }

    private async Task LockStockWrites(CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational())
            await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81005)", cancellationToken);
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
