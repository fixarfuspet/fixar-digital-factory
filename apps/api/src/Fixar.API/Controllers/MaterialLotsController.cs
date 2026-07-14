using System.Text.Json;
using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Domain.Enums;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), AllowAnonymous]
[Route("api/v{version:apiVersion}/material-lots")]
public sealed class MaterialLotsController(ApplicationDbContext db) : ControllerBase
{
    private static readonly string[] Statuses = ["Available", "PartiallyUsed", "FullyUsed", "Blocked", "Expired", "Cancelled"];
    private static readonly string[] QualityStatuses = ["Pending", "Approved", "Conditional", "Rejected"];

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid? materialId, Guid? stockItemId, Guid? supplierId, string? status, string? qualityStatus, bool? isBlocked, bool? isActive, DateTime? expiringBefore, string? search, CancellationToken ct)
    {
        var query = db.MaterialLots.AsNoTracking().AsQueryable();
        if (materialId.HasValue) query = query.Where(x => x.MaterialId == materialId);
        if (stockItemId.HasValue) query = query.Where(x => x.StockItemId == stockItemId);
        if (supplierId.HasValue) query = query.Where(x => x.SupplierId == supplierId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(qualityStatus)) query = query.Where(x => x.QualityStatus == qualityStatus);
        if (isBlocked.HasValue) query = query.Where(x => x.IsBlocked == isBlocked);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive);
        if (expiringBefore.HasValue) query = query.Where(x => x.ExpiryDate <= expiringBefore);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.LotNumber.ToLower().Contains(term) || (x.SupplierLotNumber != null && x.SupplierLotNumber.ToLower().Contains(term)) || (x.BatchNumber != null && x.BatchNumber.ToLower().Contains(term)) || x.Material.Name.ToLower().Contains(term));
        }

        var rows = await query.OrderBy(x => x.ExpiryDate ?? DateTime.MaxValue).ThenBy(x => x.ReceivedDate).Select(x => new
        {
            x.Id, x.LotNumber, x.SupplierLotNumber, x.BatchNumber, x.MaterialId, MaterialCode = x.Material.Code, MaterialName = x.Material.Name,
            x.StockItemId, StockCode = x.StockItem.Code, x.SupplierId, SupplierName = x.Supplier != null ? x.Supplier.Name : null,
            x.ReceivedDate, x.ProductionDate, x.ExpiryDate, x.InitialQuantity, x.CurrentQuantity, x.ReservedQuantity,
            AvailableQuantity = x.CurrentQuantity - x.ReservedQuantity, x.Unit, x.UnitPrice, x.Currency, x.Warehouse, x.Location, x.RackCode,
            x.Status, x.QualityStatus, x.IsBlocked, x.IsActive, IsExpired = x.ExpiryDate.HasValue && x.ExpiryDate < DateTime.UtcNow,
            ContainerCount = x.Containers.Count(c => c.IsActive && c.Status != "Cancelled"), OpenContainerCount = x.Containers.Count(c => c.IsActive && (c.Status == "Open" || c.Status == "PartiallyUsed")),
            x.CreatedAt, x.UpdatedAt
        }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var row = await DetailQuery(id).FirstOrDefaultAsync(ct);
        return row is null ? NotFound(Fail("Malzeme lotu bulunamadı.", "LOT_NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(row));
    }

    [HttpGet("{id:guid}/containers")]
    public async Task<IActionResult> GetContainers(Guid id, CancellationToken ct)
    {
        if (!await db.MaterialLots.AnyAsync(x => x.Id == id, ct)) return NotFound(Fail("Malzeme lotu bulunamadı.", "LOT_NOT_FOUND"));
        var rows = await db.MaterialContainers.AsNoTracking().Where(x => x.MaterialLotId == id).OrderBy(x => x.ContainerCode).Select(MaterialContainersController.ListProjection()).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var now = DateTime.UtcNow; var soon = now.AddDays(30);
        var q = db.MaterialLots.AsNoTracking();
        var result = new { total = await q.CountAsync(ct), available = await q.CountAsync(x => x.IsActive && !x.IsBlocked && x.Status == "Available" && x.QualityStatus != "Rejected", ct), qualityPending = await q.CountAsync(x => x.IsActive && x.QualityStatus == "Pending", ct), blocked = await q.CountAsync(x => x.IsActive && x.IsBlocked, ct), expired = await q.CountAsync(x => x.IsActive && x.ExpiryDate < now, ct), expiringSoon = await q.CountAsync(x => x.IsActive && x.ExpiryDate >= now && x.ExpiryDate <= soon, ct), depleted = await q.CountAsync(x => x.IsActive && x.CurrentQuantity == 0, ct) };
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MaterialLotRequest request, CancellationToken ct)
    {
        var validation = await Validate(request, null, ct); if (validation != null) return validation;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var stock = await db.StockItems.AsNoTracking().FirstAsync(x => x.Id == request.StockItemId, ct);
        var purchaseLine = request.PurchaseOrderLineId.HasValue ? await db.PurchaseOrderLines.AsNoTracking().Include(x => x.PurchaseOrder).FirstOrDefaultAsync(x => x.Id == request.PurchaseOrderLineId, ct) : null;
        var now = DateTime.UtcNow; var actor = Actor();
        var lot = new MaterialLot
        {
            Id = Guid.NewGuid(), MaterialId = request.MaterialId, StockItemId = request.StockItemId, SupplierId = request.SupplierId,
            PurchaseOrderId = purchaseLine?.PurchaseOrderId ?? request.PurchaseOrderId, PurchaseOrderLineId = request.PurchaseOrderLineId,
            LotNumber = string.IsNullOrWhiteSpace(request.LotNumber) ? await GenerateLotNumber(now, ct) : request.LotNumber.Trim(), SupplierLotNumber = Clean(request.SupplierLotNumber), BatchNumber = Clean(request.BatchNumber),
            ReceivedDate = Utc(request.ReceivedDate ?? now), ProductionDate = Utc(request.ProductionDate), ExpiryDate = Utc(request.ExpiryDate), InitialQuantity = request.InitialQuantity,
            CurrentQuantity = request.InitialQuantity, ReservedQuantity = 0, Unit = stock.Unit, UnitPrice = purchaseLine?.UnitPrice ?? request.UnitPrice, Currency = purchaseLine?.PurchaseOrder.Currency ?? Clean(request.Currency) ?? stock.Currency ?? "TRY",
            Warehouse = Clean(request.Warehouse) ?? stock.WarehouseName, Location = Clean(request.Location) ?? stock.LocationCode, RackCode = Clean(request.RackCode), Status = "Available", QualityStatus = ValidQuality(request.QualityStatus) ? request.QualityStatus! : "Pending",
            Notes = Clean(request.Notes), IsActive = true, CreatedAt = now, UpdatedAt = now, CreatedByName = actor, UpdatedByName = actor
        };
        db.MaterialLots.Add(lot); Audit("Material Lot Created", lot.Id, new { lot.LotNumber, lot.MaterialId, lot.StockItemId });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = lot.Id, version = "1" }, ApiResponse<object>.SuccessResponse(await DetailQuery(lot.Id).FirstAsync(ct), "Malzeme lotu oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] MaterialLotRequest request, CancellationToken ct)
    {
        var lot = await db.MaterialLots.FirstOrDefaultAsync(x => x.Id == id, ct); if (lot is null) return NotFound(Fail("Malzeme lotu bulunamadı.", "LOT_NOT_FOUND"));
        var validation = await Validate(request, id, ct); if (validation != null) return validation;
        var allocated = await db.MaterialContainers.Where(x => x.MaterialLotId == id && x.Status != "Cancelled").SumAsync(x => x.InitialQuantity, ct);
        if (request.InitialQuantity < allocated) return BadRequest(Fail("Lot başlangıç miktarı mevcut container dağılımından küçük olamaz.", "LOT_ALLOCATION_CONFLICT"));
        var stock = await db.StockItems.AsNoTracking().FirstAsync(x => x.Id == request.StockItemId, ct);
        lot.MaterialId = request.MaterialId; lot.StockItemId = request.StockItemId; lot.SupplierId = request.SupplierId; lot.PurchaseOrderId = request.PurchaseOrderId; lot.PurchaseOrderLineId = request.PurchaseOrderLineId;
        lot.LotNumber = request.LotNumber!.Trim(); lot.SupplierLotNumber = Clean(request.SupplierLotNumber); lot.BatchNumber = Clean(request.BatchNumber); lot.ReceivedDate = Utc(request.ReceivedDate ?? lot.ReceivedDate); lot.ProductionDate = Utc(request.ProductionDate); lot.ExpiryDate = Utc(request.ExpiryDate);
        lot.InitialQuantity = request.InitialQuantity; lot.CurrentQuantity = request.CurrentQuantity ?? lot.CurrentQuantity; lot.ReservedQuantity = request.ReservedQuantity ?? lot.ReservedQuantity; lot.Unit = stock.Unit; lot.UnitPrice = request.UnitPrice; lot.Currency = Clean(request.Currency) ?? lot.Currency;
        lot.Warehouse = Clean(request.Warehouse); lot.Location = Clean(request.Location); lot.RackCode = Clean(request.RackCode); lot.QualityStatus = ValidQuality(request.QualityStatus) ? request.QualityStatus! : lot.QualityStatus; lot.Notes = Clean(request.Notes); lot.UpdatedAt = DateTime.UtcNow; lot.UpdatedByName = Actor();
        RecalculateStatus(lot); Audit("Material Lot Updated", lot.Id, new { lot.LotNumber }); await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(await DetailQuery(id).FirstAsync(ct), "Malzeme lotu güncellendi."));
    }

    [HttpPost("{id:guid}/activate")] public Task<IActionResult> Activate(Guid id, CancellationToken ct) => Change(id, "Material Lot Activated", (x, _) => x.IsActive = true, ct);
    [HttpPost("{id:guid}/deactivate")] public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) => Change(id, "Material Lot Deactivated", (x, _) => x.IsActive = false, ct);
    [HttpPost("{id:guid}/block")] public Task<IActionResult> Block(Guid id, [FromBody] ReasonRequest? request, CancellationToken ct) => Change(id, "Material Lot Blocked", (x, now) => { x.IsBlocked = true; x.BlockReason = Clean(request?.Reason); x.Status = "Blocked"; }, ct);
    [HttpPost("{id:guid}/unblock")] public Task<IActionResult> Unblock(Guid id, CancellationToken ct) => Change(id, "Material Lot Unblocked", (x, now) => { x.IsBlocked = false; x.BlockReason = null; RecalculateStatus(x); }, ct);
    [HttpPost("{id:guid}/approve-quality")] public Task<IActionResult> Approve(Guid id, CancellationToken ct) => Change(id, "Material Lot Quality Approved", (x, _) => x.QualityStatus = "Approved", ct);
    [HttpPost("{id:guid}/reject-quality")] public Task<IActionResult> Reject(Guid id, [FromBody] ReasonRequest? request, CancellationToken ct) => Change(id, "Material Lot Quality Rejected", (x, _) => { x.QualityStatus = "Rejected"; x.IsBlocked = true; x.BlockReason = Clean(request?.Reason) ?? "Kalite reddi"; x.Status = "Blocked"; }, ct);

    private async Task<IActionResult> Change(Guid id, string auditEvent, Action<MaterialLot, DateTime> action, CancellationToken ct)
    {
        var lot = await db.MaterialLots.FirstOrDefaultAsync(x => x.Id == id, ct); if (lot is null) return NotFound(Fail("Malzeme lotu bulunamadı.", "LOT_NOT_FOUND"));
        var now = DateTime.UtcNow; action(lot, now); lot.UpdatedAt = now; lot.UpdatedByName = Actor(); Audit(auditEvent, id, new { lot.Status, lot.QualityStatus, lot.IsBlocked, lot.IsActive }); await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(await DetailQuery(id).FirstAsync(ct), "İşlem tamamlandı."));
    }

    private async Task<IActionResult?> Validate(MaterialLotRequest r, Guid? existingId, CancellationToken ct)
    {
        if (r.MaterialId == Guid.Empty) return BadRequest(Fail("Malzeme zorunludur.", "MATERIAL_REQUIRED"));
        if (r.StockItemId == Guid.Empty) return BadRequest(Fail("Stok kartı zorunludur.", "STOCK_REQUIRED"));
        if (r.InitialQuantity <= 0) return BadRequest(Fail("Lot başlangıç miktarı sıfırdan büyük olmalıdır.", "INVALID_INITIAL_QUANTITY"));
        var current = existingId.HasValue ? r.CurrentQuantity : r.InitialQuantity; var reserved = existingId.HasValue ? r.ReservedQuantity : 0;
        if (current < 0) return BadRequest(Fail("Lot mevcut miktarı negatif olamaz.", "INVALID_CURRENT_QUANTITY"));
        if (reserved < 0 || reserved > current) return BadRequest(Fail("Rezerve miktar mevcut miktarı aşamaz.", "INVALID_RESERVED_QUANTITY"));
        var received = r.ReceivedDate ?? DateTime.UtcNow; if (r.ExpiryDate.HasValue && r.ExpiryDate < received) return BadRequest(Fail("Son kullanma tarihi geliş tarihinden önce olamaz.", "INVALID_EXPIRY_DATE"));
        var stock = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.StockItemId, ct); if (stock is null) return BadRequest(Fail("Stok kartı bulunamadı.", "STOCK_NOT_FOUND"));
        if (stock.MaterialId != r.MaterialId) return BadRequest(Fail("Seçilen stok kartı bu malzemeye ait değil.", "STOCK_MATERIAL_MISMATCH"));
        if (!string.IsNullOrWhiteSpace(r.Unit) && !string.Equals(r.Unit.Trim(), stock.Unit, StringComparison.OrdinalIgnoreCase)) return BadRequest(Fail("Lot birimi stok kartı birimiyle uyumlu olmalıdır.", "UNIT_MISMATCH"));
        if (!string.IsNullOrWhiteSpace(r.LotNumber) && await db.MaterialLots.AnyAsync(x => x.LotNumber == r.LotNumber.Trim() && x.Id != existingId, ct)) return Conflict(Fail("Bu lot numarası zaten kullanılıyor.", "LOT_NUMBER_EXISTS"));
        if (r.PurchaseOrderLineId.HasValue)
        {
            var line = await db.PurchaseOrderLines.AsNoTracking().Include(x => x.StockItem).FirstOrDefaultAsync(x => x.Id == r.PurchaseOrderLineId, ct);
            if (line is null) return BadRequest(Fail("Satın alma satırı bulunamadı.", "PURCHASE_LINE_NOT_FOUND"));
            if (line.StockItemId != r.StockItemId || line.StockItem.MaterialId != r.MaterialId) return BadRequest(Fail("Satın alma satırı seçilen malzeme ve stok kartıyla uyumlu değil.", "PURCHASE_LINE_MISMATCH"));
            if (r.PurchaseOrderId.HasValue && line.PurchaseOrderId != r.PurchaseOrderId) return BadRequest(Fail("Satın alma siparişi ile satırı uyumlu değil.", "PURCHASE_ORDER_MISMATCH"));
        }
        return null;
    }

    private IQueryable<object> DetailQuery(Guid id) => db.MaterialLots.AsNoTracking().Where(x => x.Id == id).Select(x => new
    {
        x.Id, x.LotNumber, x.SupplierLotNumber, x.BatchNumber, x.MaterialId, Material = new { x.Material.Code, x.Material.Name }, x.StockItemId, Stock = new { x.StockItem.Code, x.StockItem.Name, x.StockItem.Unit },
        x.SupplierId, Supplier = x.Supplier == null ? null : new { x.Supplier.Name, x.Supplier.Code }, x.PurchaseOrderId, x.PurchaseOrderLineId,
        Purchase = x.PurchaseOrder == null ? null : new { x.PurchaseOrder.DocumentNo, x.PurchaseOrder.OrderDate, x.PurchaseOrder.Currency },
        x.ReceivedDate, x.ProductionDate, x.ExpiryDate, x.InitialQuantity, x.CurrentQuantity, x.ReservedQuantity, AvailableQuantity = x.CurrentQuantity - x.ReservedQuantity, x.Unit, x.UnitPrice, x.Currency, x.Warehouse, x.Location, x.RackCode,
        x.Status, x.QualityStatus, x.IsBlocked, x.BlockReason, x.Notes, x.IsActive, x.CreatedAt, x.UpdatedAt, x.CreatedByName, x.UpdatedByName,
        ContainerCount = x.Containers.Count(c => c.IsActive && c.Status != "Cancelled"), ContainerAllocatedQuantity = x.Containers.Where(c => c.Status != "Cancelled").Sum(c => (decimal?)c.InitialQuantity) ?? 0,
        UnallocatedQuantity = x.InitialQuantity - (x.Containers.Where(c => c.Status != "Cancelled").Sum(c => (decimal?)c.InitialQuantity) ?? 0), ContainerCurrentQuantity = x.Containers.Where(c => c.Status != "Cancelled").Sum(c => (decimal?)c.CurrentQuantity) ?? 0
    });

    private async Task<string> GenerateLotNumber(DateTime now, CancellationToken ct)
    {
        var prefix = $"LOT-{now:yyyyMMdd}-"; await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [prefix], ct);
        var last = await db.MaterialLots.Where(x => x.LotNumber.StartsWith(prefix)).OrderByDescending(x => x.LotNumber).Select(x => x.LotNumber).FirstOrDefaultAsync(ct);
        return prefix + Next(last).ToString("0000");
    }

    private static int Next(string? last) => last?.Length >= 4 && int.TryParse(last[^4..], out var n) ? n + 1 : 1;
    private static void RecalculateStatus(MaterialLot x) { if (x.IsBlocked) x.Status = "Blocked"; else if (x.ExpiryDate < DateTime.UtcNow) x.Status = "Expired"; else if (x.CurrentQuantity <= 0) x.Status = "FullyUsed"; else if (x.CurrentQuantity < x.InitialQuantity) x.Status = "PartiallyUsed"; else x.Status = "Available"; }
    private static bool ValidQuality(string? value) => value != null && QualityStatuses.Contains(value);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    private static DateTime? Utc(DateTime? value) => value.HasValue ? Utc(value.Value) : null;
    private string Actor() => User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name) ? User.Identity.Name : "system";
    private static ApiResponse<object> Fail(string message, string code) => ApiResponse<object>.Fail(message, code);
    private void Audit(string name, Guid id, object payload) => db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), UserName = Actor(), Action = AuditAction.Update, EntityName = name, EntityId = id.ToString(), NewValues = JsonSerializer.Serialize(payload), Timestamp = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
}

public sealed record MaterialLotRequest(Guid MaterialId, Guid StockItemId, Guid? SupplierId, Guid? PurchaseOrderId, Guid? PurchaseOrderLineId, string? LotNumber, string? SupplierLotNumber, string? BatchNumber, DateTime? ReceivedDate, DateTime? ProductionDate, DateTime? ExpiryDate, decimal InitialQuantity, decimal? CurrentQuantity, decimal? ReservedQuantity, string? Unit, decimal? UnitPrice, string? Currency, string? Warehouse, string? Location, string? RackCode, string? QualityStatus, string? Notes);
public sealed record ReasonRequest(string? Reason);
