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
[Route("api/v{version:apiVersion}/cutting-records")]
public class CuttingRecordsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CuttingRecordsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? stationAssignmentId,
        [FromQuery] Guid? workOrderId,
        [FromQuery] Guid? orderItemId,
        [FromQuery] Guid? productId,
        [FromQuery] Guid? cuttingMachineId,
        [FromQuery] Guid? operatorId,
        [FromQuery] string? status,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var records = await ProjectRecords(
                stationAssignmentId,
                workOrderId,
                orderItemId,
                productId,
                cuttingMachineId,
                operatorId,
                status,
                dateFrom,
                dateTo,
                isActive,
                search)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(records));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var record = await ProjectRecords(id: id).FirstOrDefaultAsync(cancellationToken);
        if (record is null)
            return NotFound(ApiResponse<object>.Fail("Kesim kaydı bulunamadı.", "CUTTING_RECORD_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(record));
    }

    [HttpGet("available-station-assignments")]
    public async Task<IActionResult> GetAvailableStationAssignments(CancellationToken cancellationToken)
    {
        var assignments = await QueryAssignments()
            .Where(x => x.ProducedPairs > 0)
            .OrderBy(x => x.StationNumberSnapshot)
            .ToListAsync(cancellationToken);

        var assignmentIds = assignments.Select(x => x.Id).ToList();
        var cutInputs = await _db.CuttingRecords
            .Where(x => x.StationAssignmentId.HasValue && assignmentIds.Contains(x.StationAssignmentId.Value) && !x.IsCancelled)
            .GroupBy(x => x.StationAssignmentId!.Value)
            .Select(x => new { AssignmentId = x.Key, InputPairs = x.Sum(y => y.InputPairs > 0 ? y.InputPairs : y.CutPairs) })
            .ToDictionaryAsync(x => x.AssignmentId, x => x.InputPairs, cancellationToken);

        var result = assignments
            .Select(x =>
            {
                cutInputs.TryGetValue(x.Id, out var alreadyCut);
                var available = CalculateAvailableAfterInjection(x);
                var remaining = Math.Max(available - alreadyCut, 0);
                var product = x.OrderItem.Product ?? x.OrderItem.Order.Product;
                return new AvailableCuttingAssignmentDto(
                    x.Id,
                    x.StationNumberSnapshot,
                    x.WorkOrderId,
                    x.WorkOrder?.WorkOrderNumber,
                    x.OrderItemId,
                    x.OrderItem.OrderId,
                    x.OrderItem.Order.Customer.CompanyName ?? x.OrderItem.Order.Customer.Name,
                    product?.Id,
                    product?.Code,
                    product?.Name,
                    x.ProducedPairs,
                    x.FirePairs,
                    alreadyCut,
                    remaining,
                    x.Status);
            })
            .Where(x => x.RemainingForCutting > 0)
            .ToList();

        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanRecordCutting), Idempotent]
    public async Task<IActionResult> Create([FromBody] UpsertCuttingRecordRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockCuttingWrites(cancellationToken);

        var validation = await ValidateRequest(request, null, cancellationToken);
        if (validation is not null)
            return validation;

        var assignment = await QueryAssignments().FirstAsync(x => x.Id == request.StationAssignmentId, cancellationToken);
        var machine = await _db.CuttingMachines.FirstAsync(x => x.Id == request.CuttingMachineId, cancellationToken);
        var operatorEntity = request.OperatorId.HasValue
            ? await _db.Operators.FirstOrDefaultAsync(x => x.Id == request.OperatorId.Value, cancellationToken)
            : null;
        var product = assignment.OrderItem.Product ?? assignment.OrderItem.Order.Product;
        var utcNow = DateTime.UtcNow;

        var record = new CuttingRecord
        {
            RecordNumber = await GenerateRecordNumber(utcNow, cancellationToken),
            CuttingMachineId = machine.Id,
            OrderId = assignment.OrderItem.OrderId,
            StationAssignmentId = assignment.Id,
            WorkOrderId = assignment.WorkOrderId,
            OrderItemId = assignment.OrderItemId,
            ProductId = product?.Id,
            OperatorId = operatorEntity?.Id,
            RecordDate = request.RecordDate.HasValue ? NormalizeUtc(request.RecordDate.Value) : utcNow,
            StartTime = utcNow,
            Shift = request.Shift,
            InputPairs = request.InputPairs,
            GoodPairs = request.GoodPairs,
            RejectedPairs = request.RejectedPairs,
            ReworkPairs = request.ReworkPairs,
            CutPairs = request.GoodPairs,
            Notes = request.Notes,
            Status = "Draft",
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            CreatedByName = GetActor(),
            UpdatedByName = GetActor()
        };

        _db.CuttingRecords.Add(record);
        AddAudit("Cutting Record Created", record.Id, new { record.RecordNumber, record.StationAssignmentId, record.InputPairs });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dto = await ProjectRecords(id: record.Id).FirstAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(dto, "Kesim kaydı oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanOverrideProductionRules), Idempotent]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertCuttingRecordRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockCuttingWrites(cancellationToken);

        var record = await _db.CuttingRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record is null)
            return NotFound(ApiResponse<object>.Fail("Kesim kaydı bulunamadı.", "CUTTING_RECORD_NOT_FOUND"));
        if (record.Status == "Completed")
            return BadRequest(ApiResponse<object>.Fail("Tamamlanmış kesim kaydı düzenlenemez.", "CUTTING_COMPLETED_LOCKED"));
        if (record.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("İptal edilmiş kesim kaydı düzenlenemez.", "CUTTING_CANCELLED_LOCKED"));

        var validation = await ValidateRequest(request, id, cancellationToken);
        if (validation is not null)
            return validation;

        var assignment = await QueryAssignments().FirstAsync(x => x.Id == request.StationAssignmentId, cancellationToken);
        var product = assignment.OrderItem.Product ?? assignment.OrderItem.Order.Product;
        record.CuttingMachineId = request.CuttingMachineId;
        record.StationAssignmentId = assignment.Id;
        record.WorkOrderId = assignment.WorkOrderId;
        record.OrderItemId = assignment.OrderItemId;
        record.OrderId = assignment.OrderItem.OrderId;
        record.ProductId = product?.Id;
        record.OperatorId = request.OperatorId;
        record.RecordDate = request.RecordDate.HasValue ? NormalizeUtc(request.RecordDate.Value) : record.RecordDate;
        record.Shift = request.Shift;
        record.InputPairs = request.InputPairs;
        record.GoodPairs = request.GoodPairs;
        record.RejectedPairs = request.RejectedPairs;
        record.ReworkPairs = request.ReworkPairs;
        record.CutPairs = request.GoodPairs;
        record.Notes = request.Notes;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedByName = GetActor();
        AddAudit("Cutting Record Updated", record.Id, new { record.RecordNumber });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dto = await ProjectRecords(id: record.Id).FirstAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(dto, "Kesim kaydı güncellendi."));
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = AuthorizationPolicies.CanRecordCutting), Idempotent]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockCuttingWrites(cancellationToken);

        var record = await _db.CuttingRecords
            .Include(x => x.OrderItem)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record is null)
            return NotFound(ApiResponse<object>.Fail("Kesim kaydı bulunamadı.", "CUTTING_RECORD_NOT_FOUND"));
        if (record.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("İptal edilmiş kesim kaydı tamamlanamaz.", "CUTTING_CANCELLED"));
        if (record.Status == "Completed")
            return BadRequest(ApiResponse<object>.Fail("Kesim kaydı zaten tamamlanmış.", "CUTTING_ALREADY_COMPLETED"));
        if (record.OrderItem is null)
            return Conflict(ApiResponse<object>.Fail("Kesim kaydının sipariş kalemi bulunamadı.", "CUTTING_ORDER_ITEM_MISSING"));
        if (record.GoodPairs > record.OrderItem.ProducedPairs - record.OrderItem.CutPairs)
            return Conflict(ApiResponse<object>.Fail("Sağlam kesim miktarı sipariş kaleminin kalan üretilmiş miktarını aşıyor.", "CUTTING_EXCEEDS_ORDER_REMAINING"));

        record.Status = "Completed";
        record.EndTime = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedByName = GetActor();
        record.OrderItem.CutPairs += record.GoodPairs;
        AddAudit("Cutting Record Completed", record.Id, new { record.RecordNumber });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dto = await ProjectRecords(id: record.Id).FirstAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(dto, "Kesim kaydı tamamlandı."));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.CanOverrideProductionRules), Idempotent]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelCuttingRecordRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockCuttingWrites(cancellationToken);

        var record = await _db.CuttingRecords
            .Include(x => x.OrderItem)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record is null)
            return NotFound(ApiResponse<object>.Fail("Kesim kaydı bulunamadı.", "CUTTING_RECORD_NOT_FOUND"));
        if (record.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("Kesim kaydı zaten iptal edilmiş.", "CUTTING_ALREADY_CANCELLED"));

        var hasBoxes = await _db.ProductionBoxes.AnyAsync(x => x.CuttingRecordId == id && !x.IsCancelled, cancellationToken);
        if (hasBoxes)
            return BadRequest(ApiResponse<object>.Fail("Kolilenmiş kesim kaydı iptal edilemez.", "CUTTING_HAS_BOXES"));

        var wasCompleted = record.Status == "Completed";
        record.IsCancelled = true;
        record.IsActive = false;
        record.Status = "Cancelled";
        record.CancellationReason = string.IsNullOrWhiteSpace(request.CancellationReason) ? "Kullanıcı iptali" : request.CancellationReason.Trim();
        record.CancelledAt = DateTime.UtcNow;
        record.CancelledBy = GetActor();
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedByName = GetActor();
        if (wasCompleted && record.OrderItem is not null)
            record.OrderItem.CutPairs = Math.Max(record.OrderItem.CutPairs - record.GoodPairs, 0);
        AddAudit("Cutting Record Cancelled", record.Id, new { record.RecordNumber, record.CancellationReason });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { record.Id, record.Status }, "Kesim kaydı iptal edildi."));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var records = await _db.CuttingRecords.AsNoTracking().Where(x => x.IsActive && !x.IsCancelled).ToListAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        var boxed = await _db.ProductionBoxes.AsNoTracking().Where(x => x.CuttingRecordId.HasValue && !x.IsCancelled).ToListAsync(cancellationToken);
        var summary = new
        {
            TotalInputPairs = records.Sum(x => x.InputPairs),
            TodayCutPairs = records.Where(x => x.RecordDate >= today).Sum(x => x.GoodPairs),
            GoodPairs = records.Sum(x => x.GoodPairs),
            RejectedPairs = records.Sum(x => x.RejectedPairs),
            ReworkPairs = records.Sum(x => x.ReworkPairs),
            WaitingForPacking = Math.Max(records.Sum(x => x.GoodPairs) - boxed.Sum(x => x.PairCount > 0 ? x.PairCount : x.QuantityPairs), 0),
            DraftCount = records.Count(x => x.Status == "Draft"),
            CompletedCount = records.Count(x => x.Status == "Completed")
        };

        return Ok(ApiResponse<object>.SuccessResponse(summary));
    }

    private IQueryable<CuttingRecordListDto> ProjectRecords(
        Guid? stationAssignmentId = null,
        Guid? workOrderId = null,
        Guid? orderItemId = null,
        Guid? productId = null,
        Guid? cuttingMachineId = null,
        Guid? operatorId = null,
        string? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        bool? isActive = null,
        string? search = null,
        Guid? id = null)
    {
        var boxedQuery = _db.ProductionBoxes.AsNoTracking()
            .Where(b => b.CuttingRecordId.HasValue && !b.IsCancelled)
            .GroupBy(b => b.CuttingRecordId!.Value)
            .Select(g => new { CuttingRecordId = g.Key, BoxedPairs = g.Sum(b => b.PairCount > 0 ? b.PairCount : b.QuantityPairs) });

        var query = _db.CuttingRecords.AsNoTracking()
            .Include(x => x.CuttingMachine)
            .Include(x => x.Operator)
            .Include(x => x.WorkOrder)
            .Include(x => x.StationAssignment)
            .Include(x => x.OrderItem!).ThenInclude(x => x.Order).ThenInclude(x => x.Customer)
            .Include(x => x.Product)
            .GroupJoin(boxedQuery, r => r.Id, b => b.CuttingRecordId, (record, boxed) => new { record, boxed = boxed.FirstOrDefault() });

        if (id.HasValue) query = query.Where(x => x.record.Id == id.Value);
        if (stationAssignmentId.HasValue) query = query.Where(x => x.record.StationAssignmentId == stationAssignmentId.Value);
        if (workOrderId.HasValue) query = query.Where(x => x.record.WorkOrderId == workOrderId.Value);
        if (orderItemId.HasValue) query = query.Where(x => x.record.OrderItemId == orderItemId.Value);
        if (productId.HasValue) query = query.Where(x => x.record.ProductId == productId.Value);
        if (cuttingMachineId.HasValue) query = query.Where(x => x.record.CuttingMachineId == cuttingMachineId.Value);
        if (operatorId.HasValue) query = query.Where(x => x.record.OperatorId == operatorId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.record.Status == status);
        if (dateFrom.HasValue) query = query.Where(x => x.record.RecordDate >= NormalizeUtc(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(x => x.record.RecordDate <= NormalizeUtc(dateTo.Value));
        if (isActive.HasValue) query = query.Where(x => x.record.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                (x.record.RecordNumber != null && x.record.RecordNumber.ToLower().Contains(term)) ||
                (x.record.WorkOrder != null && x.record.WorkOrder.WorkOrderNumber.ToLower().Contains(term)) ||
                (x.record.Product != null && x.record.Product.Name.ToLower().Contains(term)) ||
                x.record.Order.Customer.Name.ToLower().Contains(term) ||
                (x.record.Order.Customer.CompanyName != null && x.record.Order.Customer.CompanyName.ToLower().Contains(term)));
        }

        return query
            .OrderByDescending(x => x.record.RecordDate)
            .Select(x => new CuttingRecordListDto(
            x.record.Id,
            x.record.RecordNumber ?? $"CUT-{x.record.Id.ToString().Substring(0, 8).ToUpper()}",
            x.record.RecordDate,
            x.record.StationAssignmentId,
            x.record.StationAssignment != null ? x.record.StationAssignment.StationNumberSnapshot : null,
            x.record.WorkOrderId,
            x.record.WorkOrder != null ? x.record.WorkOrder.WorkOrderNumber : null,
            x.record.Order.Customer.CompanyName ?? x.record.Order.Customer.Name,
            x.record.Product != null ? x.record.Product.Code : x.record.Order.Product.Code,
            x.record.Product != null ? x.record.Product.Name : x.record.Order.Product.Name,
            x.record.CuttingMachineId,
            x.record.CuttingMachine.Name,
            x.record.OperatorId,
            x.record.Operator != null ? x.record.Operator.FullName : x.record.CuttingMachine.OperatorName,
            x.record.InputPairs > 0 ? x.record.InputPairs : x.record.CutPairs,
            x.record.GoodPairs > 0 ? x.record.GoodPairs : x.record.CutPairs,
            x.record.RejectedPairs,
            x.record.ReworkPairs,
            x.boxed != null ? x.boxed.BoxedPairs : 0,
            Math.Max((x.record.GoodPairs > 0 ? x.record.GoodPairs : x.record.CutPairs) - (x.boxed != null ? x.boxed.BoxedPairs : 0), 0),
            x.record.Status,
            x.record.CreatedAt,
            x.record.UpdatedAt));
    }

    private async Task<IActionResult?> ValidateRequest(UpsertCuttingRecordRequest request, Guid? currentId, CancellationToken cancellationToken)
    {
        if (request.StationAssignmentId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail("Seçilen istasyon ataması bulunamadı.", "STATION_ASSIGNMENT_REQUIRED"));
        if (request.CuttingMachineId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail("Kesim makinesi zorunludur.", "CUTTING_MACHINE_REQUIRED"));
        if (request.InputPairs <= 0)
            return BadRequest(ApiResponse<object>.Fail("Girdi çift adedi 0'dan büyük olmalıdır.", "INVALID_INPUT_PAIRS"));
        if (request.GoodPairs < 0 || request.RejectedPairs < 0 || request.ReworkPairs < 0 || request.GoodPairs + request.RejectedPairs + request.ReworkPairs != request.InputPairs)
            return BadRequest(ApiResponse<object>.Fail("Sağlam, fire ve yeniden işlem toplamı girdi miktarına eşit olmalıdır.", "CUTTING_TOTAL_MISMATCH"));

        var machineExists = await _db.CuttingMachines.AnyAsync(x => x.Id == request.CuttingMachineId && x.IsActive, cancellationToken);
        if (!machineExists)
            return BadRequest(ApiResponse<object>.Fail("Aktif kesim makinesi bulunamadı.", "CUTTING_MACHINE_NOT_FOUND"));

        var assignment = await QueryAssignments().FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId, cancellationToken);
        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Seçilen istasyon ataması bulunamadı.", "STATION_ASSIGNMENT_NOT_FOUND"));

        var remaining = await CalculateRemainingForCutting(assignment, currentId, cancellationToken);
        if (request.InputPairs > remaining)
            return BadRequest(ApiResponse<object>.Fail("Kesim miktarı üretimden kalan kesilebilir miktarı aşıyor.", "CUTTING_EXCEEDS_REMAINING"));

        return null;
    }

    private async Task<int> CalculateRemainingForCutting(StationAssignment assignment, Guid? currentId, CancellationToken cancellationToken)
    {
        var alreadyCut = await _db.CuttingRecords
            .Where(x => x.StationAssignmentId == assignment.Id && !x.IsCancelled && (!currentId.HasValue || x.Id != currentId.Value))
            .SumAsync(x => x.InputPairs > 0 ? x.InputPairs : x.CutPairs, cancellationToken);

        return Math.Max(CalculateAvailableAfterInjection(assignment) - alreadyCut, 0);
    }

    private static int CalculateAvailableAfterInjection(StationAssignment assignment)
    {
        return Math.Max(assignment.ProducedPairs - assignment.FirePairs, 0);
    }

    private IQueryable<StationAssignment> QueryAssignments()
    {
        return _db.StationAssignments
            .Include(x => x.WorkOrder)
            .Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Customer)
            .Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Product)
            .Include(x => x.OrderItem).ThenInclude(x => x.Product);
    }

    private async Task<string> GenerateRecordNumber(DateTime utcNow, CancellationToken cancellationToken)
    {
        var prefix = $"CUT-{utcNow:yyyyMMdd}-";
        var last = await _db.CuttingRecords
            .Where(x => x.RecordNumber != null && x.RecordNumber.StartsWith(prefix))
            .OrderByDescending(x => x.RecordNumber)
            .Select(x => x.RecordNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (!string.IsNullOrWhiteSpace(last) && int.TryParse(last[^4..], out var parsed))
            next = parsed + 1;
        return prefix + next.ToString("0000");
    }

    private async Task LockCuttingWrites(CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational())
            await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81004)", cancellationToken);
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
            IpAddress = HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private string GetActor() => User?.Identity?.Name ?? "system";

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

public record UpsertCuttingRecordRequest(Guid StationAssignmentId, Guid CuttingMachineId, Guid? OperatorId, DateTime? RecordDate, int? Shift, int InputPairs, int GoodPairs, int RejectedPairs, int ReworkPairs, string? Notes);
public record CancelCuttingRecordRequest(string? CancellationReason);
public record AvailableCuttingAssignmentDto(Guid StationAssignmentId, int StationNumber, Guid? WorkOrderId, string? WorkOrderNumber, Guid OrderItemId, Guid OrderId, string CustomerName, Guid? ProductId, string? ProductCode, string? ProductName, int ProducedPairs, int InjectionFirePairs, int AlreadyCutInputPairs, int RemainingForCutting, string Status);
public record CuttingRecordListDto(Guid Id, string RecordNumber, DateTime RecordDate, Guid? StationAssignmentId, int? StationNumber, Guid? WorkOrderId, string? WorkOrderNumber, string? CustomerName, string? ProductCode, string? ProductName, Guid CuttingMachineId, string CuttingMachineName, Guid? OperatorId, string? OperatorName, int InputPairs, int GoodPairs, int RejectedPairs, int ReworkPairs, int BoxedPairs, int RemainingForPacking, string Status, DateTime CreatedAt, DateTime UpdatedAt);
