using System.Data;
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
[Route("api/v{version:apiVersion}/stock-reservations")]
public sealed class StockReservationsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid? workOrderId, Guid? materialId, string? status, DateTime? dateFrom, DateTime? dateTo, bool? isActive, string? search, CancellationToken ct)
    {
        var q = db.StockReservations.AsNoTracking().AsQueryable();
        if (workOrderId.HasValue) q = q.Where(x => x.WorkOrderId == workOrderId); if (materialId.HasValue) q = q.Where(x => x.Lines.Any(l => l.MaterialId == materialId)); if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status); if (dateFrom.HasValue) q = q.Where(x => x.ReservationDate >= dateFrom); if (dateTo.HasValue) q = q.Where(x => x.ReservationDate <= dateTo); if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive);
        if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim().ToLower(); q = q.Where(x => x.ReservationNumber.ToLower().Contains(s) || x.WorkOrder.WorkOrderNumber.ToLower().Contains(s) || (x.WorkOrder.CustomerNameSnapshot != null && x.WorkOrder.CustomerNameSnapshot.ToLower().Contains(s))); }
        var rows = await q.OrderByDescending(x => x.ReservationDate).Select(x => new { x.Id, x.ReservationNumber, x.WorkOrderId, x.WorkOrder.WorkOrderNumber, CustomerName = x.WorkOrder.CustomerNameSnapshot, ProductCode = x.WorkOrder.ProductCodeSnapshot, ProductName = x.WorkOrder.ProductNameSnapshot, x.ReservationDate, x.ExpiresAt, x.Status, LineCount = x.Lines.Count, MaterialCount = x.Lines.Select(l => l.MaterialId).Distinct().Count(), TotalReservedLines = x.Lines.Sum(l => l.ReservedQuantity - l.ReleasedQuantity), x.IsActive, x.CreatedAt, x.UpdatedAt }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) { var row = await Detail(id).FirstOrDefaultAsync(ct); return row is null ? NotFound(Fail("Rezervasyon bulunamadı.", "RESERVATION_NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(row)); }
    [HttpGet("work-order/{workOrderId:guid}")] public async Task<IActionResult> ByWorkOrder(Guid workOrderId, CancellationToken ct) => Ok(ApiResponse<object>.SuccessResponse(await db.StockReservations.AsNoTracking().Where(x => x.WorkOrderId == workOrderId).OrderByDescending(x => x.ReservationDate).Select(x => new { x.Id, x.ReservationNumber, x.Status, x.ReservationDate, x.ExpiresAt, ActiveReservedQuantity = x.Lines.Sum(l => l.ReservedQuantity - l.ReleasedQuantity), x.IsActive }).ToListAsync(ct)));

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var q = db.StockReservations.AsNoTracking();
        var activeWorkOrders = await q.Where(x => x.Status == "Active").Select(x => x.WorkOrderId).Distinct().CountAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { draft = await q.CountAsync(x => x.Status == "Draft", ct), active = await q.CountAsync(x => x.Status == "Active", ct), fullyReservedWorkOrders = activeWorkOrders, incomplete = await q.CountAsync(x => x.Status == "Draft", ct), released = await q.CountAsync(x => x.Status == "Released", ct), totalReservedQuantity = await q.Where(x => x.Status == "Active").SelectMany(x => x.Lines).SumAsync(x => x.ReservedQuantity - x.ReleasedQuantity, ct) }));
    }

    [HttpGet("work-order/{workOrderId:guid}/suggest")]
    public async Task<IActionResult> Suggest(Guid workOrderId, CancellationToken ct)
    {
        var result = await BuildSuggestion(workOrderId, ct);
        return result.Error is not null ? result.Error : Ok(ApiResponse<object>.SuccessResponse(result.Value!));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReservationRequest request, CancellationToken ct)
    {
        var built = await BuildDraft(request, null, ct); if (built.Error is not null) return built.Error;
        await using var tx = await db.Database.BeginTransactionAsync(ct); var now = DateTime.UtcNow;
        var reservation = new StockReservation { Id = Guid.NewGuid(), ReservationNumber = await GenerateNumber(now, ct), WorkOrderId = request.WorkOrderId, ReservationDate = now, ExpiresAt = Utc(request.ExpiresAt), Notes = Clean(request.Notes), Status = "Draft", IsActive = true, CreatedAt = now, UpdatedAt = now, CreatedByName = Actor(), UpdatedByName = Actor() };
        var sequence = 1; foreach (var line in built.Lines!) { line.Id = Guid.NewGuid(); line.StockReservationId = reservation.Id; line.Sequence = sequence++; line.CreatedAt = now; line.UpdatedAt = now; reservation.Lines.Add(line); }
        db.StockReservations.Add(reservation); Audit("Stock Reservation Created", reservation.Id, new { reservation.ReservationNumber, reservation.WorkOrderId, LineCount = reservation.Lines.Count }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = reservation.Id, version = "1" }, ApiResponse<object>.SuccessResponse(await Detail(reservation.Id).FirstAsync(ct), "Stok rezervasyonu taslak olarak oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ReservationRequest request, CancellationToken ct)
    {
        var reservation = await db.StockReservations.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct); if (reservation is null) return NotFound(Fail("Rezervasyon bulunamadı.", "RESERVATION_NOT_FOUND")); if (reservation.Status != "Draft") return BadRequest(Fail("Yalnızca taslak rezervasyon düzenlenebilir.", "RESERVATION_NOT_DRAFT")); if (request.WorkOrderId != reservation.WorkOrderId) return BadRequest(Fail("Rezervasyonun iş emri değiştirilemez.", "WORK_ORDER_CHANGE_NOT_ALLOWED"));
        var built = await BuildDraft(request, id, ct); if (built.Error is not null) return built.Error; db.StockReservationLines.RemoveRange(reservation.Lines); reservation.Lines.Clear(); var now = DateTime.UtcNow; var sequence = 1;
        foreach (var line in built.Lines!) { line.Id = Guid.NewGuid(); line.StockReservationId = id; line.Sequence = sequence++; line.CreatedAt = now; line.UpdatedAt = now; reservation.Lines.Add(line); } reservation.ExpiresAt = Utc(request.ExpiresAt); reservation.Notes = Clean(request.Notes); reservation.UpdatedAt = now; reservation.UpdatedByName = Actor(); Audit("Stock Reservation Updated", id, new { LineCount = reservation.Lines.Count }); await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(await Detail(id).FirstAsync(ct), "Rezervasyon güncellendi."));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var reservation = await db.StockReservations.Include(x => x.WorkOrder).Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct); if (reservation is null) return NotFound(Fail("Rezervasyon bulunamadı.", "RESERVATION_NOT_FOUND")); if (reservation.Status != "Draft") return BadRequest(Fail("Yalnızca taslak rezervasyon aktifleştirilebilir.", "RESERVATION_NOT_DRAFT")); if (Terminal(reservation.WorkOrder)) return BadRequest(Fail("Tamamlanmış veya iptal edilmiş iş emri için rezervasyon oluşturulamaz.", "WORK_ORDER_TERMINAL"));
        foreach (var line in reservation.Lines.OrderBy(x => x.Sequence))
        {
            var lot = await db.MaterialLots.FirstAsync(x => x.Id == line.MaterialLotId, ct); var container = line.MaterialContainerId.HasValue ? await db.MaterialContainers.FirstOrDefaultAsync(x => x.Id == line.MaterialContainerId, ct) : null;
            var error = ValidateAsset(lot, container, line.ReservedQuantity); if (error is not null) return error; lot.ReservedQuantity += line.ReservedQuantity; lot.UpdatedAt = DateTime.UtcNow; lot.UpdatedByName = Actor(); if (container is not null) { container.ReservedQuantity += line.ReservedQuantity; container.UpdatedAt = DateTime.UtcNow; container.UpdatedByName = Actor(); }
            if (line.IsFifoOverride) Audit("FIFO Override", reservation.Id, new { line.MaterialId, line.MaterialLotId, line.MaterialContainerId, line.FifoOverrideReason });
        }
        reservation.Status = "Active"; reservation.ActivatedAt = DateTime.UtcNow; reservation.ActivatedByName = Actor(); reservation.UpdatedAt = DateTime.UtcNow; reservation.UpdatedByName = Actor(); Audit("Stock Reservation Activated", id, new { reservation.ReservationNumber }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(await Detail(id).FirstAsync(ct), "Rezervasyon aktifleştirildi."));
    }

    [HttpPost("{id:guid}/release")]
    public async Task<IActionResult> Release(Guid id, [FromBody] ReservationActionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(Fail("Serbest bırakma gerekçesi zorunludur.", "REASON_REQUIRED")); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var r = await db.StockReservations.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct); if (r is null) return NotFound(Fail("Rezervasyon bulunamadı.", "RESERVATION_NOT_FOUND")); if (r.Status != "Active") return BadRequest(Fail("Yalnızca aktif rezervasyon serbest bırakılabilir.", "RESERVATION_NOT_ACTIVE")); await ReleaseLines(r, ct); r.Status = "Released"; r.ReleasedAt = DateTime.UtcNow; r.ReleasedByName = Actor(); r.IsActive = false; r.UpdatedAt = DateTime.UtcNow; Audit("Stock Reservation Released", id, new { Reason = request.Reason.Trim() }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(await Detail(id).FirstAsync(ct), "Rezervasyon serbest bırakıldı."));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] ReservationActionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(Fail("İptal gerekçesi zorunludur.", "REASON_REQUIRED")); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var r = await db.StockReservations.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct); if (r is null) return NotFound(Fail("Rezervasyon bulunamadı.", "RESERVATION_NOT_FOUND")); if (r.Status is "Cancelled" or "Released") return BadRequest(Fail("Bu rezervasyon yeniden iptal edilemez.", "RESERVATION_TERMINAL")); if (r.Status == "Active") await ReleaseLines(r, ct); r.Status = "Cancelled"; r.IsCancelled = true; r.IsActive = false; r.CancellationReason = request.Reason.Trim(); r.CancelledAt = DateTime.UtcNow; r.CancelledByName = Actor(); r.UpdatedAt = DateTime.UtcNow; Audit("Stock Reservation Cancelled", id, new { r.CancellationReason }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(await Detail(id).FirstAsync(ct), "Rezervasyon iptal edildi."));
    }

    private async Task ReleaseLines(StockReservation r, CancellationToken ct)
    {
        foreach (var line in r.Lines) { var active = line.ReservedQuantity - line.ReleasedQuantity; if (active <= 0) continue; var lot = await db.MaterialLots.FirstAsync(x => x.Id == line.MaterialLotId, ct); if (lot.ReservedQuantity < active) throw new InvalidOperationException("Lot rezerve miktarı tutarsız."); lot.ReservedQuantity -= active; lot.UpdatedAt = DateTime.UtcNow; if (line.MaterialContainerId.HasValue) { var c = await db.MaterialContainers.FirstAsync(x => x.Id == line.MaterialContainerId, ct); if (c.ReservedQuantity < active) throw new InvalidOperationException("Container rezerve miktarı tutarsız."); c.ReservedQuantity -= active; c.UpdatedAt = DateTime.UtcNow; } line.ReleasedQuantity += active; line.UpdatedAt = DateTime.UtcNow; }
    }

    private async Task<(object? Value, IActionResult? Error)> BuildSuggestion(Guid workOrderId, CancellationToken ct)
    {
        var workOrder = await db.WorkOrders.AsNoTracking().Include(x => x.Product).Include(x => x.Recipe).ThenInclude(x => x!.Items).ThenInclude(x => x.Material).FirstOrDefaultAsync(x => x.Id == workOrderId, ct); if (workOrder is null) return (null, NotFound(Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"))); if (workOrder.Recipe is null || !workOrder.Recipe.IsActive) return (null, BadRequest(Fail("İş emri için aktif reçete bulunamadı.", "RECIPE_REQUIRED")));
        var activeReserved = await db.StockReservationLines.AsNoTracking().Where(x => x.StockReservation.WorkOrderId == workOrderId && x.StockReservation.Status == "Active").GroupBy(x => x.MaterialId).Select(x => new { MaterialId = x.Key, Quantity = x.Sum(y => y.ReservedQuantity - y.ReleasedQuantity) }).ToDictionaryAsync(x => x.MaterialId, x => x.Quantity, ct);
        var materials = new List<object>(); var warnings = new List<string>(); var full = 0; var sequence = 1;
        foreach (var item in workOrder.Recipe.Items.Where(x => !x.IsOptional).OrderBy(x => x.Sequence))
        {
            var stock = await db.StockItems.AsNoTracking().Where(x => x.MaterialId == item.MaterialId).OrderBy(x => x.Id).FirstOrDefaultAsync(ct); if (stock is null) { warnings.Add($"{item.Material.Code}: bağlı stok kartı yok."); continue; }
            var conversion = Convert(item.Unit, stock.Unit); if (conversion is null) { warnings.Add($"{item.Material.Code}: birim dönüşümü desteklenmiyor."); continue; } var required = Round((item.Quantity * workOrder.PlannedPairs / (workOrder.Recipe.OutputQuantity > 0 ? workOrder.Recipe.OutputQuantity : 1)) * (1 + item.WastePercent / 100) * conversion.Value); var already = activeReserved.GetValueOrDefault(item.MaterialId); var remaining = Math.Max(required - already, 0);
            var assets = await db.MaterialContainers.AsNoTracking().Where(c => c.MaterialId == item.MaterialId && c.StockItemId == stock.Id && c.IsActive && !c.IsBlocked && !c.IsDamaged && c.Status != "Empty" && c.Status != "Cancelled" && c.MaterialLot.IsActive && !c.MaterialLot.IsBlocked && (c.MaterialLot.QualityStatus == "Approved" || c.MaterialLot.QualityStatus == "Conditional") && (c.MaterialLot.ExpiryDate == null || c.MaterialLot.ExpiryDate >= DateTime.UtcNow)).OrderBy(c => c.OpenedAt == null).ThenBy(c => c.MaterialLot.ExpiryDate == null).ThenBy(c => c.MaterialLot.ExpiryDate).ThenBy(c => c.MaterialLot.ReceivedDate).ThenBy(c => c.MaterialLot.LotNumber).ThenBy(c => c.ContainerCode).Select(c => new { Container = c, Lot = c.MaterialLot }).ToListAsync(ct);
            var needed = remaining; var suggested = new List<object>(); var fifo = 1; foreach (var a in assets) { var available = Math.Max(a.Container.CurrentQuantity - a.Container.ReservedQuantity, 0); if (available <= 0 || needed <= 0) continue; var take = Math.Min(available, needed); suggested.Add(new { materialLotId = a.Lot.Id, a.Lot.LotNumber, a.Lot.ExpiryDate, a.Lot.ReceivedDate, materialContainerId = a.Container.Id, a.Container.ContainerCode, containerStatus = a.Container.Status, availableQuantity = available, suggestedQuantity = take, unit = stock.Unit, isOpenedContainer = a.Container.OpenedAt.HasValue, fifoOrder = fifo++, warning = a.Lot.QualityStatus == "Conditional" ? "Koşullu kalite onayı" : null }); needed -= take; }
            var totalContainer = assets.Sum(a => Math.Max(a.Container.CurrentQuantity - a.Container.ReservedQuantity, 0)); var totalLot = await db.MaterialLots.AsNoTracking().Where(l => l.MaterialId == item.MaterialId && l.StockItemId == stock.Id && l.IsActive && !l.IsBlocked && (l.QualityStatus == "Approved" || l.QualityStatus == "Conditional") && (l.ExpiryDate == null || l.ExpiryDate >= DateTime.UtcNow)).SumAsync(l => l.CurrentQuantity - l.ReservedQuantity, ct); var can = needed <= 0; if (can) full++;
            materials.Add(new { item.MaterialId, MaterialCode = item.Material.Code, MaterialName = item.Material.Name, requiredQuantity = required, alreadyReservedQuantity = already, remainingToReserve = remaining, unit = stock.Unit, totalAvailableLotQuantity = totalLot, totalAvailableContainerQuantity = totalContainer, canFullyReserve = can, suggestedLines = suggested, sequence = sequence++ });
        }
        var count = materials.Count; return (new { workOrderId, workOrder.WorkOrderNumber, canFullyReserve = count > 0 && full == count, requiredMaterialCount = count, fullyAvailableMaterialCount = full, shortageMaterialCount = count - full, warnings, materials }, null);
    }

    private async Task<(List<StockReservationLine>? Lines, IActionResult? Error)> BuildDraft(ReservationRequest request, Guid? existingId, CancellationToken ct)
    {
        var wo = await db.WorkOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.WorkOrderId, ct); if (wo is null) return (null, NotFound(Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"))); if (Terminal(wo)) return (null, BadRequest(Fail("Tamamlanmış veya iptal edilmiş iş emri için rezervasyon oluşturulamaz.", "WORK_ORDER_TERMINAL"))); if (request.Lines.Count == 0) return (null, BadRequest(Fail("En az bir rezervasyon satırı zorunludur.", "LINES_REQUIRED"))); if (request.Lines.Where(x => x.MaterialContainerId.HasValue).GroupBy(x => x.MaterialContainerId).Any(x => x.Count() > 1)) return (null, BadRequest(Fail("Aynı varil aynı rezervasyonda birden fazla kez kullanılamaz.", "DUPLICATE_CONTAINER")));
        var suggestionResult = await BuildSuggestion(request.WorkOrderId, ct); if (suggestionResult.Error is not null) return (null, suggestionResult.Error); var json = JsonSerializer.Serialize(suggestionResult.Value); using var doc = JsonDocument.Parse(json); var suggestedPairs = doc.RootElement.GetProperty("materials").EnumerateArray().SelectMany(m => m.GetProperty("suggestedLines").EnumerateArray().Select(l => (MaterialId: m.GetProperty("MaterialId").GetGuid(), LotId: l.GetProperty("materialLotId").GetGuid(), ContainerId: l.GetProperty("materialContainerId").GetGuid(), Required: m.GetProperty("requiredQuantity").GetDecimal()))).ToList();
        var lines = new List<StockReservationLine>(); foreach (var r in request.Lines) { if (r.ReservedQuantity <= 0) return (null, BadRequest(Fail("Rezervasyon miktarı sıfırdan büyük olmalıdır.", "INVALID_QUANTITY"))); var lot = await db.MaterialLots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.MaterialLotId, ct); var container = r.MaterialContainerId.HasValue ? await db.MaterialContainers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.MaterialContainerId, ct) : null; if (lot is null) return (null, BadRequest(Fail("Malzeme lotu bulunamadı.", "LOT_NOT_FOUND"))); if (lot.MaterialId != r.MaterialId || (container != null && (container.MaterialLotId != lot.Id || container.MaterialId != r.MaterialId))) return (null, BadRequest(Fail("Malzeme, lot ve container ilişkileri uyumlu değil.", "ASSET_MISMATCH"))); var error = ValidateAsset(lot, container, r.ReservedQuantity); if (error is not null) return (null, error); if (container is null && string.IsNullOrWhiteSpace(r.Notes)) return (null, BadRequest(Fail("Container olmadan lot rezervasyonu için açıklama zorunludur.", "LOT_LEVEL_REASON_REQUIRED")));
            var fifo = suggestedPairs.Any(x => x.MaterialId == r.MaterialId && x.LotId == r.MaterialLotId && x.ContainerId == r.MaterialContainerId); if (!fifo && string.IsNullOrWhiteSpace(r.FifoOverrideReason)) return (null, BadRequest(Fail("FIFO önerisinden farklı seçim için gerekçe zorunludur.", "FIFO_OVERRIDE_REASON_REQUIRED"))); var required = suggestedPairs.Where(x => x.MaterialId == r.MaterialId).Select(x => x.Required).FirstOrDefault(); lines.Add(new StockReservationLine { MaterialId = r.MaterialId, StockItemId = lot.StockItemId, MaterialLotId = lot.Id, MaterialContainerId = r.MaterialContainerId, RequiredQuantity = required, ReservedQuantity = r.ReservedQuantity, Unit = lot.Unit, IsFifoSuggested = fifo, IsFifoOverride = !fifo, FifoOverrideReason = Clean(r.FifoOverrideReason), Notes = Clean(r.Notes) }); }
        var activeByMaterial = await db.StockReservationLines.AsNoTracking().Where(x => x.StockReservation.WorkOrderId == request.WorkOrderId && x.StockReservation.Status == "Active").GroupBy(x => x.MaterialId).Select(x => new { x.Key, Quantity = x.Sum(y => y.ReservedQuantity - y.ReleasedQuantity) }).ToDictionaryAsync(x => x.Key, x => x.Quantity, ct); foreach (var group in lines.GroupBy(x => x.MaterialId)) if (group.Sum(x => x.ReservedQuantity) + activeByMaterial.GetValueOrDefault(group.Key) > group.First().RequiredQuantity) return (null, BadRequest(Fail("Rezervasyon miktarı ihtiyaçtan kalan miktarı aşıyor.", "OVER_REQUIREMENT"))); return (lines, null);
    }

    private IActionResult? ValidateAsset(MaterialLot lot, MaterialContainer? c, decimal q) { if (!lot.IsActive || lot.IsBlocked) return BadRequest(Fail("Blokeli lot rezerve edilemez.", "LOT_BLOCKED")); if (lot.QualityStatus == "Rejected") return BadRequest(Fail("Kalite reddi bulunan lot rezerve edilemez.", "LOT_REJECTED")); if (lot.ExpiryDate < DateTime.UtcNow) return BadRequest(Fail("Süresi geçmiş lot rezerve edilemez.", "LOT_EXPIRED")); if (lot.CurrentQuantity - lot.ReservedQuantity < q) return BadRequest(Fail("Lot kullanılabilir miktarı rezervasyon için yetersiz.", "LOT_INSUFFICIENT")); if (c is not null) { if (!c.IsActive || c.IsBlocked || c.IsDamaged || c.Status is "Empty" or "Cancelled") return BadRequest(Fail("Hasarlı veya blokeli varil rezerve edilemez.", "CONTAINER_UNAVAILABLE")); if (c.CurrentQuantity - c.ReservedQuantity < q) return BadRequest(Fail("Varil kullanılabilir miktarı rezervasyon için yetersiz.", "CONTAINER_INSUFFICIENT")); } return null; }
    private IQueryable<object> Detail(Guid id) => db.StockReservations.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.ReservationNumber, x.WorkOrderId, WorkOrder = new { x.WorkOrder.WorkOrderNumber, x.WorkOrder.CustomerNameSnapshot, x.WorkOrder.ProductCodeSnapshot, x.WorkOrder.ProductNameSnapshot, x.WorkOrder.Status }, x.ReservationDate, x.ExpiresAt, x.Status, x.Notes, x.IsActive, x.IsCancelled, x.CancellationReason, x.CancelledAt, x.CancelledByName, x.ActivatedAt, x.ActivatedByName, x.ReleasedAt, x.ReleasedByName, x.CreatedAt, x.UpdatedAt, Lines = x.Lines.OrderBy(l => l.Sequence).Select(l => new { l.Id, l.MaterialId, MaterialCode = l.Material.Code, MaterialName = l.Material.Name, l.StockItemId, StockCode = l.StockItem.Code, l.MaterialLotId, LotNumber = l.MaterialLot.LotNumber, l.MaterialContainerId, ContainerCode = l.MaterialContainer != null ? l.MaterialContainer.ContainerCode : null, l.RequiredQuantity, l.ReservedQuantity, l.ReleasedQuantity, ActiveReservedQuantity = l.ReservedQuantity - l.ReleasedQuantity, l.Unit, l.IsFifoSuggested, l.IsFifoOverride, l.FifoOverrideReason, LotExpiryDate = l.MaterialLot.ExpiryDate, ContainerStatus = l.MaterialContainer != null ? l.MaterialContainer.Status : null, l.Notes }) });
    private async Task<string> GenerateNumber(DateTime now, CancellationToken ct) { var prefix = $"RES-{now:yyyyMMdd}-"; await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [prefix], ct); var last = await db.StockReservations.Where(x => x.ReservationNumber.StartsWith(prefix)).OrderByDescending(x => x.ReservationNumber).Select(x => x.ReservationNumber).FirstOrDefaultAsync(ct); var n = last?.Length >= 4 && int.TryParse(last[^4..], out var parsed) ? parsed + 1 : 1; return prefix + n.ToString("0000"); }
    private static decimal? Convert(string from, string to) { static string N(string x) => x.Trim().ToLowerInvariant() switch { "gr" or "gram" => "g", "lt" or "l" or "liter" => "litre", _ => x.Trim().ToLowerInvariant() }; var a = N(from); var b = N(to); if (a == b) return 1; return (a, b) switch { ("g", "kg") => .001m, ("kg", "g") => 1000m, ("ml", "litre") => .001m, ("litre", "ml") => 1000m, _ => null }; }
    private static bool Terminal(WorkOrder x) => x.IsCancelled || x.Status is "Completed" or "Cancelled"; private static decimal Round(decimal x) => Math.Round(x, 4); private static string? Clean(string? x) => string.IsNullOrWhiteSpace(x) ? null : x.Trim(); private static DateTime? Utc(DateTime? x) => x?.ToUniversalTime(); private string Actor() => User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name) ? User.Identity.Name : "system"; private static ApiResponse<object> Fail(string message, string code) => ApiResponse<object>.Fail(message, code); private void Audit(string name, Guid id, object payload) => db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), UserName = Actor(), Action = AuditAction.Update, EntityName = name, EntityId = id.ToString(), NewValues = JsonSerializer.Serialize(payload), Timestamp = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
}

public sealed record ReservationRequest(Guid WorkOrderId, DateTime? ExpiresAt, string? Notes, List<ReservationLineRequest> Lines);
public sealed record ReservationLineRequest(Guid MaterialId, Guid MaterialLotId, Guid? MaterialContainerId, decimal ReservedQuantity, string Unit, string? FifoOverrideReason, string? Notes);
public sealed record ReservationActionRequest(string? Reason);
