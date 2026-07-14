using System.Linq.Expressions;
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
[Route("api/v{version:apiVersion}/material-containers")]
public sealed class MaterialContainersController(ApplicationDbContext db) : ControllerBase
{
    private static readonly string[] Types = ["Drum", "IBC", "Can", "Bag", "Box", "Roll", "Other"];

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid? lotId, Guid? materialId, Guid? stockItemId, string? containerType, string? status, bool? isDamaged, bool? isBlocked, bool? isActive, string? search, CancellationToken ct)
    {
        var q = db.MaterialContainers.AsNoTracking().AsQueryable();
        if (lotId.HasValue) q = q.Where(x => x.MaterialLotId == lotId); if (materialId.HasValue) q = q.Where(x => x.MaterialId == materialId); if (stockItemId.HasValue) q = q.Where(x => x.StockItemId == stockItemId);
        if (!string.IsNullOrWhiteSpace(containerType)) q = q.Where(x => x.ContainerType == containerType); if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        if (isDamaged.HasValue) q = q.Where(x => x.IsDamaged == isDamaged); if (isBlocked.HasValue) q = q.Where(x => x.IsBlocked == isBlocked); if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToLower(); q = q.Where(x => x.ContainerCode.ToLower().Contains(term) || (x.ManufacturerContainerNumber != null && x.ManufacturerContainerNumber.ToLower().Contains(term)) || x.MaterialLot.LotNumber.ToLower().Contains(term) || x.Material.Name.ToLower().Contains(term)); }
        return Ok(ApiResponse<object>.SuccessResponse(await q.OrderBy(x => x.ContainerCode).Select(ListProjection()).ToListAsync(ct)));
    }

    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) { var row = await Detail(id).FirstOrDefaultAsync(ct); return row is null ? NotFound(Fail("Container bulunamadı.", "CONTAINER_NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(row)); }
    [HttpGet("by-code/{containerCode}")] public async Task<IActionResult> ByCode(string containerCode, CancellationToken ct) { var id = await db.MaterialContainers.AsNoTracking().Where(x => x.ContainerCode == containerCode).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct); return id is null ? NotFound(Fail("Container bulunamadı.", "CONTAINER_NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(await Detail(id.Value).FirstAsync(ct))); }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var q = db.MaterialContainers.AsNoTracking().Where(x => x.IsActive);
        var result = new { total = await q.CountAsync(ct), sealedCount = await q.CountAsync(x => x.Status == "Sealed", ct), open = await q.CountAsync(x => x.Status == "Open", ct), partiallyUsed = await q.CountAsync(x => x.Status == "PartiallyUsed", ct), empty = await q.CountAsync(x => x.Status == "Empty", ct), damaged = await q.CountAsync(x => x.IsDamaged, ct), blocked = await q.CountAsync(x => x.IsBlocked, ct) };
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MaterialContainerRequest request, CancellationToken ct)
    {
        var validation = await Validate(request, null, ct); if (validation != null) return validation;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var lot = await db.MaterialLots.AsNoTracking().FirstAsync(x => x.Id == request.MaterialLotId, ct); var now = DateTime.UtcNow; var actor = Actor();
        var item = new MaterialContainer
        {
            Id = Guid.NewGuid(), MaterialLotId = lot.Id, MaterialId = lot.MaterialId, StockItemId = lot.StockItemId,
            ContainerCode = string.IsNullOrWhiteSpace(request.ContainerCode) ? await GenerateCode(request.ContainerType, now, ct) : request.ContainerCode.Trim(), ContainerType = request.ContainerType, ManufacturerContainerNumber = Clean(request.ManufacturerContainerNumber),
            InitialQuantity = request.InitialQuantity, CurrentQuantity = request.InitialQuantity, ReservedQuantity = 0, Unit = lot.Unit, Status = "Sealed", Warehouse = Clean(request.Warehouse) ?? lot.Warehouse, Location = Clean(request.Location) ?? lot.Location, RackCode = Clean(request.RackCode) ?? lot.RackCode,
            Notes = Clean(request.Notes), IsActive = true, CreatedAt = now, UpdatedAt = now, CreatedByName = actor, UpdatedByName = actor
        };
        db.MaterialContainers.Add(item); Audit("Material Container Created", item.Id, new { item.ContainerCode, item.MaterialLotId, item.InitialQuantity }); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = item.Id, version = "1" }, ApiResponse<object>.SuccessResponse(await Detail(item.Id).FirstAsync(ct), "Container oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] MaterialContainerRequest request, CancellationToken ct)
    {
        var item = await db.MaterialContainers.FirstOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound(Fail("Container bulunamadı.", "CONTAINER_NOT_FOUND"));
        var validation = await Validate(request, id, ct); if (validation != null) return validation;
        var lot = await db.MaterialLots.AsNoTracking().FirstAsync(x => x.Id == request.MaterialLotId, ct);
        item.MaterialLotId = lot.Id; item.MaterialId = lot.MaterialId; item.StockItemId = lot.StockItemId; item.ContainerCode = request.ContainerCode!.Trim(); item.ContainerType = request.ContainerType; item.ManufacturerContainerNumber = Clean(request.ManufacturerContainerNumber);
        item.InitialQuantity = request.InitialQuantity; item.CurrentQuantity = request.CurrentQuantity ?? item.CurrentQuantity; item.ReservedQuantity = request.ReservedQuantity ?? item.ReservedQuantity; item.Unit = lot.Unit; item.Warehouse = Clean(request.Warehouse); item.Location = Clean(request.Location); item.RackCode = Clean(request.RackCode); item.Notes = Clean(request.Notes); item.UpdatedAt = DateTime.UtcNow; item.UpdatedByName = Actor(); Recalculate(item);
        Audit("Material Container Updated", id, new { item.ContainerCode }); await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(await Detail(id).FirstAsync(ct), "Container güncellendi."));
    }

    [HttpPost("{id:guid}/open")]
    public async Task<IActionResult> Open(Guid id, CancellationToken ct)
    {
        var item = await Find(id, ct); if (item is null) return NotFound(Fail("Container bulunamadı.", "CONTAINER_NOT_FOUND"));
        if (item.Status == "Open" || item.OpenedAt.HasValue) return BadRequest(Fail("Bu container zaten açık.", "ALREADY_OPEN"));
        if (item.Status == "Empty") return BadRequest(Fail("Boş container açılamaz.", "EMPTY_CONTAINER")); if (item.Status == "Cancelled" || !item.IsActive) return BadRequest(Fail("İptal edilmiş container işleme alınamaz.", "CANCELLED_CONTAINER"));
        if (item.IsBlocked || item.IsDamaged) return BadRequest(Fail("Blokeli veya hasarlı container açılamaz.", "CONTAINER_UNAVAILABLE"));
        var now = DateTime.UtcNow; item.Status = "Open"; item.OpenedAt = now; item.OpenedBy = Actor(); return await SaveAction(item, "Material Container Opened", ct);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var item = await Find(id, ct); if (item is null) return NotFound(Fail("Container bulunamadı.", "CONTAINER_NOT_FOUND")); if (item.Status == "Cancelled" || !item.IsActive) return BadRequest(Fail("İptal edilmiş container işleme alınamaz.", "CANCELLED_CONTAINER"));
        item.ClosedAt = DateTime.UtcNow; item.ClosedBy = Actor(); Recalculate(item); return await SaveAction(item, "Material Container Closed", ct);
    }

    [HttpPost("{id:guid}/block")] public Task<IActionResult> Block(Guid id, [FromBody] ReasonRequest? request, CancellationToken ct) => Change(id, "Material Container Blocked", x => { x.IsBlocked = true; x.BlockReason = Clean(request?.Reason); x.Status = "Blocked"; }, ct);
    [HttpPost("{id:guid}/unblock")] public Task<IActionResult> Unblock(Guid id, CancellationToken ct) => Change(id, "Material Container Unblocked", x => { x.IsBlocked = false; x.BlockReason = null; Recalculate(x); }, ct);
    [HttpPost("{id:guid}/mark-damaged")] public Task<IActionResult> Damage(Guid id, [FromBody] ReasonRequest? request, CancellationToken ct) => Change(id, "Material Container Marked Damaged", x => { x.IsDamaged = true; x.DamageNotes = Clean(request?.Reason); x.Status = "Damaged"; }, ct);
    [HttpPost("{id:guid}/clear-damage")] public Task<IActionResult> ClearDamage(Guid id, CancellationToken ct) => Change(id, "Material Container Damage Cleared", x => { x.IsDamaged = false; x.DamageNotes = null; Recalculate(x); }, ct);
    [HttpPost("{id:guid}/deactivate")] public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) => Change(id, "Material Container Deactivated", x => x.IsActive = false, ct);

    private async Task<IActionResult> Change(Guid id, string eventName, Action<MaterialContainer> action, CancellationToken ct)
    {
        var item = await Find(id, ct); if (item is null) return NotFound(Fail("Container bulunamadı.", "CONTAINER_NOT_FOUND")); action(item); return await SaveAction(item, eventName, ct);
    }
    private async Task<IActionResult> SaveAction(MaterialContainer item, string eventName, CancellationToken ct) { item.UpdatedAt = DateTime.UtcNow; item.UpdatedByName = Actor(); Audit(eventName, item.Id, new { item.Status, item.IsBlocked, item.IsDamaged, item.IsActive }); await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(await Detail(item.Id).FirstAsync(ct), "İşlem tamamlandı.")); }
    private Task<MaterialContainer?> Find(Guid id, CancellationToken ct) => db.MaterialContainers.FirstOrDefaultAsync(x => x.Id == id, ct);

    private async Task<IActionResult?> Validate(MaterialContainerRequest r, Guid? existingId, CancellationToken ct)
    {
        if (r.MaterialLotId == Guid.Empty) return BadRequest(Fail("Malzeme lotu zorunludur.", "LOT_REQUIRED")); if (!Types.Contains(r.ContainerType)) return BadRequest(Fail("Container tipi geçersiz.", "INVALID_CONTAINER_TYPE"));
        if (r.InitialQuantity <= 0) return BadRequest(Fail("Container başlangıç miktarı sıfırdan büyük olmalıdır.", "INVALID_INITIAL_QUANTITY"));
        var current = existingId.HasValue ? r.CurrentQuantity : r.InitialQuantity; var reserved = existingId.HasValue ? r.ReservedQuantity : 0;
        if (current < 0) return BadRequest(Fail("Container mevcut miktarı negatif olamaz.", "INVALID_CURRENT_QUANTITY")); if (reserved < 0 || reserved > current) return BadRequest(Fail("Rezerve miktar mevcut miktarı aşamaz.", "INVALID_RESERVED_QUANTITY"));
        var lot = await db.MaterialLots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.MaterialLotId, ct); if (lot is null) return BadRequest(Fail("Malzeme lotu bulunamadı.", "LOT_NOT_FOUND"));
        if (lot.IsBlocked) return BadRequest(Fail("Blokeli lot için yeni varil oluşturulamaz.", "LOT_BLOCKED")); if (lot.QualityStatus == "Rejected") return BadRequest(Fail("Kalite reddi bulunan lot için yeni varil oluşturulamaz.", "LOT_REJECTED")); if (!lot.IsActive || lot.Status == "Cancelled") return BadRequest(Fail("Pasif veya iptal edilmiş lot için container oluşturulamaz.", "LOT_INACTIVE"));
        if (!string.IsNullOrWhiteSpace(r.Unit) && !string.Equals(r.Unit.Trim(), lot.Unit, StringComparison.OrdinalIgnoreCase)) return BadRequest(Fail("Container birimi lot birimiyle uyumlu olmalıdır.", "UNIT_MISMATCH"));
        if (!string.IsNullOrWhiteSpace(r.ContainerCode) && await db.MaterialContainers.AnyAsync(x => x.ContainerCode == r.ContainerCode.Trim() && x.Id != existingId, ct)) return Conflict(Fail("Bu container kodu zaten kullanılıyor.", "CONTAINER_CODE_EXISTS"));
        var allocated = await db.MaterialContainers.Where(x => x.MaterialLotId == r.MaterialLotId && x.Status != "Cancelled" && x.Id != existingId).SumAsync(x => x.InitialQuantity, ct);
        if (allocated + r.InitialQuantity > lot.InitialQuantity) return BadRequest(Fail("Container miktarı lotta kalan dağıtılabilir miktarı aşıyor.", "LOT_OVER_ALLOCATION"));
        return null;
    }

    internal static Expression<Func<MaterialContainer, object>> ListProjection() => x => new
    {
        x.Id, x.ContainerCode, x.ContainerType, x.ManufacturerContainerNumber, x.MaterialLotId, LotNumber = x.MaterialLot.LotNumber, x.MaterialId, MaterialCode = x.Material.Code, MaterialName = x.Material.Name,
        x.StockItemId, StockCode = x.StockItem.Code, x.InitialQuantity, x.CurrentQuantity, x.ReservedQuantity, AvailableQuantity = x.CurrentQuantity - x.ReservedQuantity, x.Unit, x.Status, x.OpenedAt, x.ClosedAt,
        x.Warehouse, x.Location, x.RackCode, x.IsDamaged, x.IsBlocked, x.IsActive, x.CreatedAt, x.UpdatedAt
    };
    private IQueryable<object> Detail(Guid id) => db.MaterialContainers.AsNoTracking().Where(x => x.Id == id).Select(x => new
    {
        x.Id, x.ContainerCode, x.ContainerType, x.ManufacturerContainerNumber, x.MaterialLotId, Lot = new { x.MaterialLot.LotNumber, x.MaterialLot.QualityStatus, x.MaterialLot.IsBlocked },
        x.MaterialId, Material = new { x.Material.Code, x.Material.Name }, x.StockItemId, Stock = new { x.StockItem.Code, x.StockItem.Name, x.StockItem.Unit },
        x.InitialQuantity, x.CurrentQuantity, x.ReservedQuantity, AvailableQuantity = x.CurrentQuantity - x.ReservedQuantity, x.Unit, x.OpenedAt, x.OpenedBy, x.ClosedAt, x.ClosedBy, x.Status,
        x.Warehouse, x.Location, x.RackCode, x.IsDamaged, x.DamageNotes, x.IsBlocked, x.BlockReason, x.Notes, x.IsActive, x.CreatedAt, x.UpdatedAt, x.CreatedByName, x.UpdatedByName
    });

    private async Task<string> GenerateCode(string type, DateTime now, CancellationToken ct)
    {
        var stem = type == "Drum" ? "DRM" : "CNT"; var prefix = $"{stem}-{now:yyyyMMdd}-"; await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [prefix], ct);
        var last = await db.MaterialContainers.Where(x => x.ContainerCode.StartsWith(prefix)).OrderByDescending(x => x.ContainerCode).Select(x => x.ContainerCode).FirstOrDefaultAsync(ct);
        var next = last?.Length >= 4 && int.TryParse(last[^4..], out var n) ? n + 1 : 1; return prefix + next.ToString("0000");
    }
    private static void Recalculate(MaterialContainer x) { if (x.IsDamaged) x.Status = "Damaged"; else if (x.IsBlocked) x.Status = "Blocked"; else if (x.CurrentQuantity <= 0) x.Status = "Empty"; else if (x.CurrentQuantity < x.InitialQuantity) x.Status = "PartiallyUsed"; else if (x.OpenedAt.HasValue) x.Status = "Open"; else x.Status = "Sealed"; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private string Actor() => User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name) ? User.Identity.Name : "system";
    private static ApiResponse<object> Fail(string message, string code) => ApiResponse<object>.Fail(message, code);
    private void Audit(string name, Guid id, object payload) => db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), UserName = Actor(), Action = AuditAction.Update, EntityName = name, EntityId = id.ToString(), NewValues = JsonSerializer.Serialize(payload), Timestamp = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
}

public sealed record MaterialContainerRequest(Guid MaterialLotId, string? ContainerCode, string ContainerType, string? ManufacturerContainerNumber, decimal InitialQuantity, decimal? CurrentQuantity, decimal? ReservedQuantity, string? Unit, string? Warehouse, string? Location, string? RackCode, string? Notes);
