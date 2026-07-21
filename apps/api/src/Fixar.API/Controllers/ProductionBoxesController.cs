using System.Data;
using System.Text.Json;
using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Domain.Enums;
using Fixar.Infrastructure.Persistence;
using Fixar.Infrastructure.Identity;
using Fixar.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/production-boxes")]
public class ProductionBoxesController : ControllerBase
{
    private static readonly string[] BoxStatuses = { "Packed", "InWarehouse", "ReadyForShipment", "Shipped", "Cancelled" };
    private readonly ApplicationDbContext _db;

    public ProductionBoxesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] Guid? cuttingRecordId,
        [FromQuery] Guid? workOrderId,
        [FromQuery] Guid? orderItemId,
        [FromQuery] Guid? productId,
        [FromQuery] Guid? customerId,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var boxes = await ProjectBoxes(status, cuttingRecordId, workOrderId, orderItemId, productId, customerId, isActive, search)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(boxes));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var box = await ProjectBoxes(id: id).FirstOrDefaultAsync(cancellationToken);
        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Koli bulunamadı.", "BOX_NOT_FOUND"));

        var events = await _db.ProductionBoxEvents.AsNoTracking()
            .Where(x => x.ProductionBoxId == id)
            .OrderByDescending(x => x.EventTime)
            .Select(x => new ProductionBoxEventDto(x.Id, x.EventType, x.FromLocation, x.ToLocation, x.OperatorName, x.QuantityPairs, x.Note, x.EventTime))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { Box = box, Events = events }));
    }

    [HttpGet("{boxCode}")]
    public async Task<IActionResult> GetByCode(string boxCode, CancellationToken cancellationToken)
    {
        var box = await ProjectBoxes(search: boxCode).FirstOrDefaultAsync(x => x.BoxNumber == boxCode || x.BoxCode == boxCode, cancellationToken);
        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Koli bulunamadı.", "BOX_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(box));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanManageBoxes), Idempotent]
    public async Task<IActionResult> Create([FromBody] CreateProductionBoxRequest request, CancellationToken cancellationToken)
    {
        if (!request.CuttingRecordId.HasValue)
            return BadRequest(ApiResponse<object>.Fail("Koli oluşturmak için tamamlanmış kesim kaydı seçilmelidir.", "CUTTING_RECORD_REQUIRED"));
        if (request.PairCount <= 0)
            return BadRequest(ApiResponse<object>.Fail("Koli çift adedi 0'dan büyük olmalıdır.", "INVALID_PAIR_COUNT"));

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var cutting = await QueryCuttingRecords()
            .FirstOrDefaultAsync(x => x.Id == request.CuttingRecordId.Value, cancellationToken);
        if (cutting is null)
            return NotFound(ApiResponse<object>.Fail("Kesim kaydı bulunamadı.", "CUTTING_RECORD_NOT_FOUND"));
        if (cutting.Status != "Completed" || cutting.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("Cancelled veya tamamlanmamış kesimden koli oluşturulamaz.", "CUTTING_NOT_COMPLETED"));

        var remaining = await CalculateRemainingForPacking(cutting.Id, cutting.GoodPairs > 0 ? cutting.GoodPairs : cutting.CutPairs, null, cancellationToken);
        if (request.PairCount > remaining)
            return BadRequest(ApiResponse<object>.Fail("Koli miktarı kolilenebilir kalan miktarı aşıyor.", "PACKING_EXCEEDS_REMAINING"));

        var utcNow = DateTime.UtcNow;
        var product = cutting.Product ?? cutting.Order.Product;
        var boxNumber = await GenerateBoxNumber(utcNow, cancellationToken);
        var box = new ProductionBox
        {
            BoxCode = boxNumber,
            BoxNumber = boxNumber,
            Barcode = boxNumber,
            TraceabilityCode = Guid.NewGuid().ToString("N"),
            CuttingRecordId = cutting.Id,
            StationAssignmentId = cutting.StationAssignmentId,
            WorkOrderId = cutting.WorkOrderId,
            OrderItemId = cutting.OrderItemId,
            OrderId = cutting.OrderId,
            CustomerId = cutting.Order.CustomerId,
            ProductId = product?.Id,
            PairCount = request.PairCount,
            QuantityPairs = request.PairCount,
            Status = "Packed",
            CurrentStatus = "Packed",
            CurrentLocation = "Paketleme",
            PackedAt = request.PackedAt.HasValue ? NormalizeUtc(request.PackedAt.Value) : utcNow,
            CustomerName = cutting.Order.Customer.CompanyName ?? cutting.Order.Customer.Name,
            CustomerNameSnapshot = cutting.Order.Customer.CompanyName ?? cutting.Order.Customer.Name,
            ProductCodeSnapshot = product?.Code,
            ProductNameSnapshot = product?.Name,
            WorkOrderNumberSnapshot = cutting.WorkOrder?.WorkOrderNumber,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            CreatedByName = GetActor(),
            UpdatedByName = GetActor()
        };

        _db.ProductionBoxes.Add(box);
        AddBoxEvent(box, "BoxCreated", null, "Paketleme", request.Note, utcNow);
        AddAudit("Production Box Created", box.Id, new { box.BoxNumber, box.PairCount, box.CuttingRecordId });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dto = await ProjectBoxes(id: box.Id).FirstAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(dto, "Koli oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanOverrideProductionRules)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductionBoxRequest request, CancellationToken cancellationToken)
    {
        var box = await _db.ProductionBoxes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Koli bulunamadı.", "BOX_NOT_FOUND"));
        if (box.Status == "Shipped")
            return BadRequest(ApiResponse<object>.Fail("Sevk edilmiş koli güncellenemez.", "BOX_SHIPPED_LOCKED"));
        if (box.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("İptal edilmiş koli güncellenemez.", "BOX_CANCELLED_LOCKED"));

        box.WarehouseLocation = request.WarehouseLocation ?? box.WarehouseLocation;
        box.RackCode = request.RackCode ?? box.RackCode;
        box.UpdatedAt = DateTime.UtcNow;
        box.UpdatedByName = GetActor();
        AddBoxEvent(box, "BoxUpdated", box.CurrentLocation, box.CurrentLocation, request.Note, DateTime.UtcNow);
        AddAudit("Production Box Updated", box.Id, new { box.BoxNumber });
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await ProjectBoxes(id: box.Id).FirstAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(dto, "Koli güncellendi."));
    }

    [HttpPost("{id:guid}/receive-to-warehouse")]
    [Authorize(Policy = AuthorizationPolicies.CanManageWarehouse), Idempotent]
    public Task<IActionResult> ReceiveToWarehouse(Guid id, [FromBody] WarehouseTransitionRequest request, CancellationToken cancellationToken)
        => Transition(id, "Packed", "InWarehouse", request.WarehouseLocation, request.RackCode, "ReceivedToWarehouse", request.Note, null, null, cancellationToken);

    [HttpPost("{id:guid}/mark-ready-for-shipment")]
    [Authorize(Policy = AuthorizationPolicies.CanManageWarehouse), Idempotent]
    public Task<IActionResult> MarkReadyForShipment(Guid id, [FromBody] WarehouseTransitionRequest request, CancellationToken cancellationToken)
        => Transition(id, "InWarehouse", "ReadyForShipment", request.WarehouseLocation, request.RackCode, "MarkedReadyForShipment", request.Note, null, null, cancellationToken);

    [HttpPost("{id:guid}/ship")]
    [Authorize(Policy = AuthorizationPolicies.CanManageShipments), Idempotent]
    public Task<IActionResult> Ship(Guid id, [FromBody] ShipBoxRequest request, CancellationToken cancellationToken)
        => Transition(id, "ReadyForShipment", "Shipped", null, null, "Shipped", request.Notes, request.ShipmentReference, request.ShipmentDate, cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.CanOverrideProductionRules), Idempotent]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelProductionBoxRequest request, CancellationToken cancellationToken)
    {
        var box = await _db.ProductionBoxes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Koli bulunamadı.", "BOX_NOT_FOUND"));
        if (box.Status == "Shipped")
            return BadRequest(ApiResponse<object>.Fail("Sevk edilmiş koli iptal edilemez.", "BOX_SHIPPED_TERMINAL"));
        if (box.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("Koli zaten iptal edilmiş.", "BOX_ALREADY_CANCELLED"));

        var fromStatus = box.Status;
        var utcNow = DateTime.UtcNow;
        box.Status = "Cancelled";
        box.CurrentStatus = "Cancelled";
        box.IsCancelled = true;
        box.IsActive = false;
        box.CancellationReason = string.IsNullOrWhiteSpace(request.CancellationReason) ? "Kullanıcı iptali" : request.CancellationReason.Trim();
        box.CancelledAt = utcNow;
        box.UpdatedAt = utcNow;
        box.UpdatedByName = GetActor();
        AddBoxEvent(box, "Cancelled", fromStatus, "Cancelled", box.CancellationReason, utcNow);
        AddAudit("Production Box Cancelled", box.Id, new { box.BoxNumber, box.CancellationReason });
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { box.Id, box.Status }, "Koli iptal edildi."));
    }

    [HttpGet("available-for-shipment")]
    public async Task<IActionResult> AvailableForShipment(CancellationToken cancellationToken)
    {
        var boxes = await ProjectBoxes(status: "ReadyForShipment", isActive: true)
            .ToListAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(boxes));
    }

    [HttpPost("bulk-ship")]
    [Authorize(Policy = AuthorizationPolicies.CanManageShipments), Idempotent]
    public async Task<IActionResult> BulkShip([FromBody] BulkShipRequest request, CancellationToken cancellationToken)
    {
        if (request.BoxIds.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("Sevk edilecek koli seçilmelidir.", "BOX_SELECTION_REQUIRED"));
        if (string.IsNullOrWhiteSpace(request.ShipmentReference))
            return BadRequest(ApiResponse<object>.Fail("Sevkiyat referansı zorunludur.", "SHIPMENT_REFERENCE_REQUIRED"));

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var boxes = await _db.ProductionBoxes.Where(x => request.BoxIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (boxes.Count != request.BoxIds.Count)
            return NotFound(ApiResponse<object>.Fail("Seçilen kolilerden biri bulunamadı.", "BOX_NOT_FOUND"));
        if (boxes.Any(x => x.Status != "ReadyForShipment" || x.IsCancelled))
            return BadRequest(ApiResponse<object>.Fail("Yalnızca sevkiyata hazır koliler sevk edilebilir.", "BOX_NOT_READY"));

        var utcNow = request.ShipmentDate.HasValue ? NormalizeUtc(request.ShipmentDate.Value) : DateTime.UtcNow;
        foreach (var box in boxes)
        {
            box.Status = "Shipped";
            box.CurrentStatus = "Shipped";
            box.CurrentLocation = "Sevkiyat";
            box.ShippedAt = utcNow;
            box.ShipmentReference = request.ShipmentReference.Trim();
            box.ShipmentNotes = request.Notes;
            box.UpdatedAt = utcNow;
            box.UpdatedByName = GetActor();
            AddBoxEvent(box, "Shipped", "ReadyForShipment", "Sevkiyat", request.Notes, utcNow);
        }
        AddAudit("Bulk Shipment Completed", Guid.NewGuid(), new { request.ShipmentReference, BoxCount = boxes.Count, PairCount = boxes.Sum(x => x.PairCount > 0 ? x.PairCount : x.QuantityPairs) });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { request.ShipmentReference, BoxCount = boxes.Count, PairCount = boxes.Sum(x => x.PairCount > 0 ? x.PairCount : x.QuantityPairs) }, "Toplu sevkiyat tamamlandı."));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var boxes = await _db.ProductionBoxes.AsNoTracking().ToListAsync(cancellationToken);
        var active = boxes.Where(x => !x.IsCancelled).ToList();
        var summary = new
        {
            TotalBoxes = active.Count,
            TotalPairs = active.Sum(PairCount),
            PackedBoxes = active.Count(x => x.Status == "Packed"),
            WarehouseBoxes = active.Count(x => x.Status == "InWarehouse"),
            ReadyBoxes = active.Count(x => x.Status == "ReadyForShipment"),
            ShippedBoxes = active.Count(x => x.Status == "Shipped"),
            CancelledBoxes = boxes.Count(x => x.IsCancelled),
            PackedPairs = active.Where(x => x.Status == "Packed").Sum(PairCount),
            WarehousePairs = active.Where(x => x.Status == "InWarehouse").Sum(PairCount),
            ReadyPairs = active.Where(x => x.Status == "ReadyForShipment").Sum(PairCount),
            ShippedPairs = active.Where(x => x.Status == "Shipped").Sum(PairCount),
            PairsByProduct = active.GroupBy(x => x.ProductNameSnapshot ?? "-").Select(x => new { Name = x.Key, Pairs = x.Sum(PairCount) }).OrderByDescending(x => x.Pairs).Take(10),
            PairsByCustomer = active.GroupBy(x => x.CustomerNameSnapshot ?? x.CustomerName ?? "-").Select(x => new { Name = x.Key, Pairs = x.Sum(PairCount) }).OrderByDescending(x => x.Pairs).Take(10),
            PairsByLocation = active.GroupBy(x => x.WarehouseLocation ?? x.CurrentLocation ?? "-").Select(x => new { Name = x.Key, Pairs = x.Sum(PairCount) }).OrderByDescending(x => x.Pairs).Take(10)
        };
        return Ok(ApiResponse<object>.SuccessResponse(summary));
    }

    [HttpGet("{id:guid}/events")]
    public async Task<IActionResult> GetEvents(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _db.ProductionBoxes.AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
            return NotFound(ApiResponse<object>.Fail("Koli bulunamadı.", "BOX_NOT_FOUND"));

        var events = await _db.ProductionBoxEvents.AsNoTracking()
            .Where(x => x.ProductionBoxId == id)
            .OrderByDescending(x => x.EventTime)
            .Select(x => new ProductionBoxEventDto(x.Id, x.EventType, x.FromLocation, x.ToLocation, x.OperatorName, x.QuantityPairs, x.Note, x.EventTime))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(events));
    }

    [HttpPost("create")]
    [Authorize(Policy = AuthorizationPolicies.CanManageBoxes), Idempotent]
    public async Task<IActionResult> LegacyCreate([FromBody] LegacyCreateBoxRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BoxCode))
            return BadRequest(ApiResponse<object>.Fail("Kasa kodu zorunludur.", "BOX_CODE_REQUIRED"));
        var exists = await _db.ProductionBoxes.AnyAsync(x => x.BoxCode == request.BoxCode || x.BoxNumber == request.BoxCode, cancellationToken);
        if (exists)
            return BadRequest(ApiResponse<object>.Fail("Bu kasa kodu zaten var.", "BOX_ALREADY_EXISTS"));

        var now = DateTime.UtcNow;
        var box = new ProductionBox
        {
            BoxCode = request.BoxCode.Trim(),
            BoxNumber = request.BoxCode.Trim(),
            Barcode = request.BoxCode.Trim(),
            TraceabilityCode = Guid.NewGuid().ToString("N"),
            CurrentStatus = "Boş",
            Status = "Packed",
            CurrentLocation = "Boş Kasa Alanı",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByName = GetActor(),
            UpdatedByName = GetActor()
        };
        _db.ProductionBoxes.Add(box);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(await ProjectBoxes(id: box.Id).FirstAsync(cancellationToken), "Kasa oluşturuldu."));
    }

    [HttpPost("fill")]
    [Authorize(Policy = AuthorizationPolicies.CanManageBoxes), Idempotent]
    public async Task<IActionResult> Fill([FromBody] FillBoxRequest request, CancellationToken cancellationToken)
    {
        var box = await _db.ProductionBoxes.FirstOrDefaultAsync(x => x.BoxCode == request.BoxCode || x.BoxNumber == request.BoxCode, cancellationToken);
        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Kasa bulunamadı.", "BOX_NOT_FOUND"));

        box.OrderId = request.OrderId;
        box.ProductId = request.ProductId;
        box.MoldId = request.MoldId;
        box.CustomerName = request.CustomerName;
        box.CustomerNameSnapshot = request.CustomerName;
        box.ProductionType = request.ProductionType;
        box.FabricColor = request.FabricColor;
        box.QuantityPairs = request.QuantityPairs;
        box.PairCount = request.QuantityPairs;
        box.CurrentStatus = "Üretimden Çıktı";
        box.Status = "Packed";
        box.CurrentLocation = request.Location;
        box.OperatorName = request.OperatorName;
        box.FilledAt = DateTime.UtcNow;
        box.PackedAt = DateTime.UtcNow;
        box.UpdatedAt = DateTime.UtcNow;
        AddBoxEvent(box, "BoxCreated", null, request.Location, request.Note, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(await ProjectBoxes(id: box.Id).FirstAsync(cancellationToken), "Kasa dolduruldu."));
    }

    [HttpPost("start-cutting")]
    [Authorize(Policy = AuthorizationPolicies.CanRecordCutting), Idempotent]
    public Task<IActionResult> StartCutting([FromBody] BoxMoveRequest request, CancellationToken cancellationToken) => LegacyMove(request, "Kesime Başladı", "Kesim", cancellationToken);

    [HttpPost("finish-cutting")]
    [Authorize(Policy = AuthorizationPolicies.CanRecordCutting), Idempotent]
    public Task<IActionResult> FinishCutting([FromBody] BoxMoveRequest request, CancellationToken cancellationToken) => LegacyMove(request, "Kesim Bitti", "Kesim Tamamlandı", cancellationToken);

    [HttpPost("move-to-warehouse")]
    [Authorize(Policy = AuthorizationPolicies.CanManageWarehouse), Idempotent]
    public Task<IActionResult> MoveToWarehouse([FromBody] BoxMoveRequest request, CancellationToken cancellationToken) => LegacyMove(request, "Depoya Girdi", request.ToLocation ?? "Depo", cancellationToken);

    [HttpPost("ship")]
    [Authorize(Policy = AuthorizationPolicies.CanManageShipments), Idempotent]
    public Task<IActionResult> LegacyShip([FromBody] BoxMoveRequest request, CancellationToken cancellationToken) => LegacyMove(request, "Sevk Edildi", "Sevkiyat", cancellationToken);

    [HttpPost("empty")]
    [Authorize(Policy = AuthorizationPolicies.CanOverrideProductionRules), Idempotent]
    public async Task<IActionResult> EmptyBox([FromBody] BoxMoveRequest request, CancellationToken cancellationToken)
    {
        var box = await _db.ProductionBoxes.FirstOrDefaultAsync(x => x.BoxCode == request.BoxCode || x.BoxNumber == request.BoxCode, cancellationToken);
        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Kasa bulunamadı.", "BOX_NOT_FOUND"));

        var from = box.CurrentLocation;
        box.QuantityPairs = 0;
        box.PairCount = 0;
        box.CurrentStatus = "Boş";
        box.CurrentLocation = "Boş Kasa Alanı";
        box.OperatorName = request.OperatorName;
        box.FilledAt = null;
        AddBoxEvent(box, "Kasa Boşaltıldı", from, "Boş Kasa Alanı", request.Note, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(await ProjectBoxes(id: box.Id).FirstAsync(cancellationToken), "Kasa boşaltıldı."));
    }

    private async Task<IActionResult> Transition(Guid id, string expectedStatus, string nextStatus, string? warehouseLocation, string? rackCode, string eventType, string? note, string? shipmentReference, DateTime? shipmentDate, CancellationToken cancellationToken)
    {
        var box = await _db.ProductionBoxes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Koli bulunamadı.", "BOX_NOT_FOUND"));
        if (box.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("İptal koli işleme alınamaz.", "BOX_CANCELLED"));
        if (box.Status == "Shipped")
            return BadRequest(ApiResponse<object>.Fail("Bu koli daha önce sevk edilmiş.", "BOX_ALREADY_SHIPPED"));
        if (box.Status != expectedStatus)
            return BadRequest(ApiResponse<object>.Fail(GetTransitionError(expectedStatus), "INVALID_BOX_STATUS"));
        if (nextStatus == "Shipped" && string.IsNullOrWhiteSpace(shipmentReference))
            return BadRequest(ApiResponse<object>.Fail("Sevkiyat referansı zorunludur.", "SHIPMENT_REFERENCE_REQUIRED"));

        var from = box.Status;
        var now = shipmentDate.HasValue ? NormalizeUtc(shipmentDate.Value) : DateTime.UtcNow;
        box.Status = nextStatus;
        box.CurrentStatus = nextStatus;
        box.UpdatedAt = now;
        box.UpdatedByName = GetActor();
        if (nextStatus == "InWarehouse")
        {
            box.WarehouseLocation = warehouseLocation;
            box.RackCode = rackCode;
            box.CurrentLocation = string.Join(" / ", new[] { warehouseLocation, rackCode }.Where(x => !string.IsNullOrWhiteSpace(x)));
            box.ReceivedToWarehouseAt = now;
            box.ReceivedToWarehouseBy = GetActor();
        }
        else if (nextStatus == "ReadyForShipment")
        {
            box.WarehouseLocation = warehouseLocation ?? box.WarehouseLocation;
            box.RackCode = rackCode ?? box.RackCode;
            box.ReadyForShipmentAt = now;
        }
        else if (nextStatus == "Shipped")
        {
            box.ShippedAt = now;
            box.ShipmentReference = shipmentReference!.Trim();
            box.ShipmentNotes = note;
            box.CurrentLocation = "Sevkiyat";
        }

        AddBoxEvent(box, eventType, from, nextStatus, note, now);
        AddAudit(eventType switch
        {
            "ReceivedToWarehouse" => "Production Box Received To Warehouse",
            "MarkedReadyForShipment" => "Production Box Ready For Shipment",
            "Shipped" => "Production Box Shipped",
            _ => "Production Box Updated"
        }, box.Id, new { box.BoxNumber, box.Status, box.ShipmentReference });
        await _db.SaveChangesAsync(cancellationToken);
        var dto = await ProjectBoxes(id: box.Id).FirstAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(dto, GetTransitionMessage(nextStatus)));
    }

    private async Task<IActionResult> LegacyMove(BoxMoveRequest request, string eventType, string defaultLocation, CancellationToken cancellationToken)
    {
        var box = await _db.ProductionBoxes.FirstOrDefaultAsync(x => x.BoxCode == request.BoxCode || x.BoxNumber == request.BoxCode, cancellationToken);
        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Kasa bulunamadı.", "BOX_NOT_FOUND"));

        var from = box.CurrentLocation;
        var to = request.ToLocation ?? defaultLocation;
        box.CurrentStatus = eventType;
        box.CurrentLocation = to;
        box.OperatorName = request.OperatorName;
        box.UpdatedAt = DateTime.UtcNow;
        AddBoxEvent(box, eventType, from, to, request.Note, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(await ProjectBoxes(id: box.Id).FirstAsync(cancellationToken), eventType));
    }

    private IQueryable<ProductionBoxListDto> ProjectBoxes(string? status = null, Guid? cuttingRecordId = null, Guid? workOrderId = null, Guid? orderItemId = null, Guid? productId = null, Guid? customerId = null, bool? isActive = null, string? search = null, Guid? id = null)
    {
        var query = _db.ProductionBoxes.AsNoTracking()
            .Include(x => x.CuttingRecord)
            .Include(x => x.StationAssignment)
            .Include(x => x.WorkOrder)
            .Include(x => x.Order)
            .Include(x => x.Customer)
            .Include(x => x.Product)
            .AsQueryable();

        if (id.HasValue) query = query.Where(x => x.Id == id.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (cuttingRecordId.HasValue) query = query.Where(x => x.CuttingRecordId == cuttingRecordId.Value);
        if (workOrderId.HasValue) query = query.Where(x => x.WorkOrderId == workOrderId.Value);
        if (orderItemId.HasValue) query = query.Where(x => x.OrderItemId == orderItemId.Value);
        if (productId.HasValue) query = query.Where(x => x.ProductId == productId.Value);
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.BoxCode.ToLower().Contains(term) ||
                (x.BoxNumber != null && x.BoxNumber.ToLower().Contains(term)) ||
                (x.CustomerNameSnapshot != null && x.CustomerNameSnapshot.ToLower().Contains(term)) ||
                (x.ProductNameSnapshot != null && x.ProductNameSnapshot.ToLower().Contains(term)) ||
                (x.WorkOrderNumberSnapshot != null && x.WorkOrderNumberSnapshot.ToLower().Contains(term)));
        }

        return query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ProductionBoxListDto(
            x.Id,
            x.BoxNumber ?? x.BoxCode,
            x.BoxCode,
            x.Barcode ?? x.BoxCode,
            x.TraceabilityCode,
            x.CuttingRecordId,
            x.CuttingRecord != null ? x.CuttingRecord.RecordNumber : null,
            x.StationAssignmentId,
            x.StationAssignment != null ? x.StationAssignment.StationNumberSnapshot : null,
            x.WorkOrderId,
            x.WorkOrderNumberSnapshot ?? (x.WorkOrder != null ? x.WorkOrder.WorkOrderNumber : null),
            x.OrderId,
            x.OrderId.HasValue ? "ORD-" + x.OrderId.Value.ToString().Substring(0, 8).ToUpper() : null,
            x.CustomerId,
            x.CustomerNameSnapshot ?? x.CustomerName ?? (x.Customer != null ? x.Customer.CompanyName ?? x.Customer.Name : null),
            x.ProductId,
            x.ProductCodeSnapshot ?? (x.Product != null ? x.Product.Code : null),
            x.ProductNameSnapshot ?? (x.Product != null ? x.Product.Name : null),
            x.PairCount > 0 ? x.PairCount : x.QuantityPairs,
            x.Status,
            x.CurrentStatus,
            x.WarehouseLocation,
            x.RackCode,
            x.PackedAt ?? x.FilledAt,
            x.ReceivedToWarehouseAt,
            x.ReadyForShipmentAt,
            x.ShippedAt,
            x.ShipmentReference,
            x.IsCancelled,
            x.CreatedAt,
            x.UpdatedAt));
    }

    private async Task<int> CalculateRemainingForPacking(Guid cuttingRecordId, int goodPairs, Guid? currentBoxId, CancellationToken cancellationToken)
    {
        var boxed = await _db.ProductionBoxes
            .Where(x => x.CuttingRecordId == cuttingRecordId && !x.IsCancelled && (!currentBoxId.HasValue || x.Id != currentBoxId.Value))
            .SumAsync(x => x.PairCount > 0 ? x.PairCount : x.QuantityPairs, cancellationToken);
        return Math.Max(goodPairs - boxed, 0);
    }

    private IQueryable<CuttingRecord> QueryCuttingRecords()
    {
        return _db.CuttingRecords
            .Include(x => x.CuttingMachine)
            .Include(x => x.WorkOrder)
            .Include(x => x.Order).ThenInclude(x => x.Customer)
            .Include(x => x.Order).ThenInclude(x => x.Product)
            .Include(x => x.Product);
    }

    private async Task<string> GenerateBoxNumber(DateTime utcNow, CancellationToken cancellationToken)
    {
        var prefix = $"BOX-{utcNow:yyyyMMdd}-";
        var last = await _db.ProductionBoxes
            .Where(x => x.BoxNumber != null && x.BoxNumber.StartsWith(prefix))
            .OrderByDescending(x => x.BoxNumber)
            .Select(x => x.BoxNumber)
            .FirstOrDefaultAsync(cancellationToken);
        var next = 1;
        if (!string.IsNullOrWhiteSpace(last) && int.TryParse(last[^4..], out var parsed))
            next = parsed + 1;
        return prefix + next.ToString("0000");
    }

    private static int PairCount(ProductionBox box) => box.PairCount > 0 ? box.PairCount : box.QuantityPairs;

    private void AddBoxEvent(ProductionBox box, string eventType, string? from, string? to, string? note, DateTime eventTime)
    {
        _db.ProductionBoxEvents.Add(new ProductionBoxEvent
        {
            ProductionBox = box,
            EventType = eventType,
            FromLocation = from,
            ToLocation = to,
            OperatorName = GetActor(),
            QuantityPairs = PairCount(box),
            Note = note,
            EventTime = eventTime
        });
    }

    private void AddAudit(string eventName, Guid entityId, object payload)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserName = GetActor(),
            Action = AuditAction.Update,
            EntityName = eventName,
            EntityId = entityId.ToString(),
            NewValues = JsonSerializer.Serialize(payload),
            Timestamp = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
    }

    private string GetActor() => User?.Identity?.Name ?? "system";

    private static string GetTransitionError(string expectedStatus) => expectedStatus switch
    {
        "Packed" => "Yalnızca kolilenmiş ürünler depoya kabul edilebilir.",
        "InWarehouse" => "Yalnızca depodaki koliler sevkiyata hazır yapılabilir.",
        "ReadyForShipment" => "Yalnızca sevkiyata hazır koliler sevk edilebilir.",
        _ => "Geçersiz koli durumu."
    };

    private static string GetTransitionMessage(string status) => status switch
    {
        "InWarehouse" => "Koli depoya kabul edildi.",
        "ReadyForShipment" => "Koli sevkiyata hazırlandı.",
        "Shipped" => "Koli sevk edildi.",
        _ => "Koli güncellendi."
    };

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

public record CreateProductionBoxRequest(Guid? CuttingRecordId, int PairCount, DateTime? PackedAt, string? Note);
public record UpdateProductionBoxRequest(string? WarehouseLocation, string? RackCode, string? Note);
public record WarehouseTransitionRequest(string? WarehouseLocation, string? RackCode, string? Note);
public record ShipBoxRequest(string ShipmentReference, DateTime? ShipmentDate, string? Notes);
public record BulkShipRequest(IReadOnlyCollection<Guid> BoxIds, string ShipmentReference, DateTime? ShipmentDate, string? Notes);
public record CancelProductionBoxRequest(string? CancellationReason);
public record LegacyCreateBoxRequest(string BoxCode);
public record FillBoxRequest(string BoxCode, Guid? OrderId, Guid? ProductId, Guid? MoldId, string? CustomerName, string? ProductionType, string? FabricColor, int QuantityPairs, string? Location, string? OperatorName, string? Note);
public record BoxMoveRequest(string BoxCode, string? ToLocation, string? OperatorName, string? Note);
public record ProductionBoxEventDto(Guid Id, string EventType, string? FromLocation, string? ToLocation, string? OperatorName, int? QuantityPairs, string? Note, DateTime EventTime);
public record ProductionBoxListDto(Guid Id, string BoxNumber, string BoxCode, string Barcode, string? TraceabilityCode, Guid? CuttingRecordId, string? CuttingRecordNumber, Guid? StationAssignmentId, int? StationNumber, Guid? WorkOrderId, string? WorkOrderNumber, Guid? OrderId, string? OrderNumber, Guid? CustomerId, string? CustomerName, Guid? ProductId, string? ProductCode, string? ProductName, int PairCount, string Status, string CurrentStatus, string? WarehouseLocation, string? RackCode, DateTime? PackedAt, DateTime? ReceivedToWarehouseAt, DateTime? ReadyForShipmentAt, DateTime? ShippedAt, string? ShipmentReference, bool IsCancelled, DateTime CreatedAt, DateTime UpdatedAt);
