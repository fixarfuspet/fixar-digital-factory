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

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/quality-inspections")]
public class QualityInspectionsController : ControllerBase
{
    private static readonly string[] InspectionTypes = { "StartUp", "InProcess", "Final", "Random", "ComplaintReview" };
    private static readonly string[] Results = { "Pending", "Passed", "Conditional", "Failed", "Cancelled" };
    private static readonly string[] CheckResults = { "NotChecked", "Passed", "Failed", "Warning" };
    private static readonly string[] Severities = { "Minor", "Major", "Critical" };

    private readonly ApplicationDbContext _db;

    public QualityInspectionsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? stationAssignmentId,
        [FromQuery] Guid? workOrderId,
        [FromQuery] Guid? orderItemId,
        [FromQuery] Guid? productId,
        [FromQuery] Guid? moldId,
        [FromQuery] Guid? machineId,
        [FromQuery] Guid? operatorId,
        [FromQuery] string? inspectionType,
        [FromQuery] string? result,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = _db.QualityInspections.AsNoTracking().AsQueryable();

        if (stationAssignmentId.HasValue) query = query.Where(x => x.StationAssignmentId == stationAssignmentId.Value);
        if (workOrderId.HasValue) query = query.Where(x => x.WorkOrderId == workOrderId.Value);
        if (orderItemId.HasValue) query = query.Where(x => x.OrderItemId == orderItemId.Value);
        if (productId.HasValue) query = query.Where(x => x.ProductId == productId.Value);
        if (moldId.HasValue) query = query.Where(x => x.MoldId == moldId.Value);
        if (machineId.HasValue) query = query.Where(x => x.MachineId == machineId.Value);
        if (operatorId.HasValue) query = query.Where(x => x.OperatorId == operatorId.Value);
        if (!string.IsNullOrWhiteSpace(inspectionType)) query = query.Where(x => x.InspectionType == inspectionType);
        if (!string.IsNullOrWhiteSpace(result)) query = query.Where(x => x.Result == result);
        if (dateFrom.HasValue) query = query.Where(x => x.InspectionDate >= NormalizeUtc(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(x => x.InspectionDate <= NormalizeUtc(dateTo.Value));
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.InspectionNumber.ToLower().Contains(term) ||
                (x.CustomerNameSnapshot != null && x.CustomerNameSnapshot.ToLower().Contains(term)) ||
                (x.ProductCodeSnapshot != null && x.ProductCodeSnapshot.ToLower().Contains(term)) ||
                (x.ProductNameSnapshot != null && x.ProductNameSnapshot.ToLower().Contains(term)) ||
                (x.WorkOrderNumberSnapshot != null && x.WorkOrderNumberSnapshot.ToLower().Contains(term)) ||
                (x.MoldCodeSnapshot != null && x.MoldCodeSnapshot.ToLower().Contains(term)) ||
                (x.OperatorNameSnapshot != null && x.OperatorNameSnapshot.ToLower().Contains(term)));
        }

        var items = await query
            .OrderByDescending(x => x.InspectionDate)
            .ThenByDescending(x => x.Created)
            .Select(x => new QualityInspectionListDto(
                x.Id,
                x.InspectionNumber,
                x.InspectionType,
                x.InspectionDate,
                x.StationAssignment.StationNumberSnapshot,
                x.WorkOrderNumberSnapshot,
                x.CustomerNameSnapshot,
                x.ProductCodeSnapshot,
                x.ProductNameSnapshot,
                x.MoldCodeSnapshot,
                x.OperatorNameSnapshot,
                x.SampleSizePairs,
                x.CheckedPairs,
                x.AcceptedPairs,
                x.RejectedPairs,
                x.ConditionalAcceptedPairs,
                x.Result,
                x.Status,
                x.HoldProduction,
                x.Defects.Where(d => d.StationAssignmentFireId.HasValue).Sum(d => d.DefectPairs),
                x.Created,
                x.LastModified))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(items));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await ProjectDetails(id)
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
            return NotFound(ApiResponse<object>.Fail("Kalite kontrol kaydı bulunamadı.", "QUALITY_INSPECTION_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertQualityInspectionRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(request, requireCompleteFields: false);
        if (validation is not null)
            return BadRequest(validation);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var assignment = await QueryAssignments()
            .FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Seçilen istasyon ataması bulunamadı.", "STATION_ASSIGNMENT_NOT_FOUND"));

        var utcNow = DateTime.UtcNow;
        var actor = GetActor();
        var inspection = new QualityInspection
        {
            InspectionNumber = await GenerateInspectionNumber(utcNow, cancellationToken),
            Status = "Draft",
            Result = "Pending",
            CreatedByName = actor,
            UpdatedByName = actor
        };

        ApplyRequest(inspection, request, assignment, utcNow);
        _db.QualityInspections.Add(inspection);
        await AddStationEvent(assignment, "Quality Inspection Created", utcNow, null, inspection.InspectionNumber, inspection.GeneralNotes, actor, null);
        AddAuditLog("Quality Inspection Created", inspection.Id, new { inspection.InspectionNumber, inspection.StationAssignmentId });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dto = await ProjectDetails(inspection.Id).FirstAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(dto, "Kalite kontrol kaydı oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertQualityInspectionRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(request, requireCompleteFields: false);
        if (validation is not null)
            return BadRequest(validation);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var inspection = await _db.QualityInspections
            .Include(x => x.Defects)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (inspection is null)
            return NotFound(ApiResponse<object>.Fail("Kalite kontrol kaydı bulunamadı.", "QUALITY_INSPECTION_NOT_FOUND"));

        if (inspection.Status == "Completed")
            return BadRequest(ApiResponse<object>.Fail("Tamamlanmış kalite kontrolü düzenlenemez.", "QUALITY_COMPLETED_LOCKED"));

        if (inspection.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("İptal edilmiş kalite kontrolü düzenlenemez.", "QUALITY_CANCELLED_LOCKED"));

        var assignment = await QueryAssignments()
            .FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Seçilen istasyon ataması bulunamadı.", "STATION_ASSIGNMENT_NOT_FOUND"));

        ApplyRequest(inspection, request, assignment, DateTime.UtcNow);
        inspection.UpdatedByName = GetActor();

        _db.QualityDefects.RemoveRange(inspection.Defects);
        AddDefects(inspection, request.Defects, DateTime.UtcNow);
        AddAuditLog("Quality Inspection Updated", inspection.Id, new { inspection.InspectionNumber });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dto = await ProjectDetails(inspection.Id).FirstAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(dto, "Kalite kontrol kaydı güncellendi."));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var inspection = await _db.QualityInspections
            .Include(x => x.Defects)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (inspection is null)
            return NotFound(ApiResponse<object>.Fail("Kalite kontrol kaydı bulunamadı.", "QUALITY_INSPECTION_NOT_FOUND"));

        if (inspection.Status == "Completed")
            return BadRequest(ApiResponse<object>.Fail("Tamamlanmış kalite kontrolü tekrar tamamlanamaz.", "QUALITY_ALREADY_COMPLETED"));

        if (inspection.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("İptal edilmiş kalite kontrolü tamamlanamaz.", "QUALITY_CANCELLED"));

        var completionValidation = ValidateInspectionForCompletion(inspection);
        if (completionValidation is not null)
            return BadRequest(completionValidation);

        var assignment = await QueryAssignments()
            .FirstOrDefaultAsync(x => x.Id == inspection.StationAssignmentId, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Seçilen istasyon ataması bulunamadı.", "STATION_ASSIGNMENT_NOT_FOUND"));

        var utcNow = DateTime.UtcNow;
        var actor = GetActor();
        CalculateResults(inspection);
        inspection.Status = "Completed";
        inspection.CompletedAt = utcNow;
        inspection.CompletedBy = actor;
        inspection.UpdatedByName = actor;

        StationAssignmentFire? linkedFire = null;
        if (inspection.CreateFireRecord && inspection.FirePairs.GetValueOrDefault() > 0)
        {
            if (assignment.FinishedAt is not null)
                return BadRequest(ApiResponse<object>.Fail("Tamamlanmış iş için fire kaydı girilemez.", "ASSIGNMENT_CLOSED"));

            var firePairs = inspection.FirePairs.GetValueOrDefault();
            if (assignment.FirePairs + firePairs > assignment.ProducedPairs)
                return BadRequest(ApiResponse<object>.Fail("Toplam fire, üretilen çift adedini aşamaz.", "FIRE_EXCEEDS_PRODUCTION"));

            linkedFire = CreateFire(assignment, inspection, firePairs, utcNow, actor);
            assignment.FirePairs += firePairs;
            _db.StationAssignmentFires.Add(linkedFire);
            await AddStationEvent(assignment, "QualityFireCreated", utcNow, firePairs, inspection.FireReason, inspection.InspectionNumber, actor, null);
        }

        if (linkedFire is not null)
        {
            foreach (var defect in inspection.Defects.Where(x => x.IsFireRelated && x.StationAssignmentFireId is null))
            {
                defect.StationAssignmentFireId = linkedFire.Id;
                defect.UpdatedAt = utcNow;
            }
        }

        if (inspection.HoldProduction)
            await StartQualityHoldIfNeeded(assignment, inspection, utcNow, actor, cancellationToken);

        await AddStationEvent(
            assignment,
            inspection.Result == "Failed" ? "QualityFailed" : "QualityInspectionCompleted",
            utcNow,
            inspection.CheckedPairs,
            inspection.Result,
            inspection.InspectionNumber,
            actor,
            JsonSerializer.Serialize(new { inspection.WeightResult, inspection.DensityResult, inspection.DimensionResult }));

        AddAuditLog("Quality Inspection Completed", inspection.Id, new { inspection.InspectionNumber, inspection.Result, FireId = linkedFire?.Id });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dto = await ProjectDetails(inspection.Id).FirstAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(dto, "Kalite kontrolü tamamlandı."));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelQualityInspectionRequest request, CancellationToken cancellationToken)
    {
        var inspection = await _db.QualityInspections.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (inspection is null)
            return NotFound(ApiResponse<object>.Fail("Kalite kontrol kaydı bulunamadı.", "QUALITY_INSPECTION_NOT_FOUND"));

        if (inspection.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("İptal edilmiş kalite kontrolü tekrar iptal edilemez.", "QUALITY_ALREADY_CANCELLED"));

        var utcNow = DateTime.UtcNow;
        var actor = GetActor();
        inspection.IsCancelled = true;
        inspection.IsActive = false;
        inspection.Status = "Cancelled";
        inspection.Result = "Cancelled";
        inspection.CancellationReason = string.IsNullOrWhiteSpace(request.CancellationReason) ? "Kullanıcı iptali" : request.CancellationReason.Trim();
        inspection.CancelledAt = utcNow;
        inspection.CancelledBy = actor;
        inspection.UpdatedByName = actor;

        var assignment = await QueryAssignments().FirstOrDefaultAsync(x => x.Id == inspection.StationAssignmentId, cancellationToken);
        if (assignment is not null)
            await AddStationEvent(assignment, "Quality Inspection Cancelled", utcNow, null, inspection.CancellationReason, inspection.InspectionNumber, actor, null);

        AddAuditLog("Quality Inspection Cancelled", inspection.Id, new { inspection.InspectionNumber, inspection.CancellationReason });

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { inspection.Id, inspection.Status, inspection.Result }, "Kalite kontrolü iptal edildi. Bağlı fire kayıtları otomatik iptal edilmedi."));
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var source = await _db.QualityInspections
            .Include(x => x.Defects)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (source is null)
            return NotFound(ApiResponse<object>.Fail("Kalite kontrol kaydı bulunamadı.", "QUALITY_INSPECTION_NOT_FOUND"));

        var utcNow = DateTime.UtcNow;
        var copy = new QualityInspection
        {
            InspectionNumber = await GenerateInspectionNumber(utcNow, cancellationToken),
            InspectionType = source.InspectionType,
            StationAssignmentId = source.StationAssignmentId,
            WorkOrderId = source.WorkOrderId,
            OrderItemId = source.OrderItemId,
            ProductId = source.ProductId,
            MoldId = source.MoldId,
            MachineId = source.MachineId,
            OperatorId = source.OperatorId,
            InspectionDate = utcNow,
            Shift = source.Shift,
            Status = "Draft",
            Result = "Pending",
            SampleSizePairs = source.SampleSizePairs,
            TargetWeightGrams = source.TargetWeightGrams,
            WeightToleranceMinus = source.WeightToleranceMinus,
            WeightTolerancePlus = source.WeightTolerancePlus,
            TargetDensity = source.TargetDensity,
            DensityMinimum = source.DensityMinimum,
            DensityMaximum = source.DensityMaximum,
            TargetX = source.TargetX,
            TargetY = source.TargetY,
            DimensionTolerance = source.DimensionTolerance,
            ProductCodeSnapshot = source.ProductCodeSnapshot,
            ProductNameSnapshot = source.ProductNameSnapshot,
            CustomerNameSnapshot = source.CustomerNameSnapshot,
            WorkOrderNumberSnapshot = source.WorkOrderNumberSnapshot,
            MoldCodeSnapshot = source.MoldCodeSnapshot,
            OperatorNameSnapshot = source.OperatorNameSnapshot,
            CreatedByName = GetActor(),
            UpdatedByName = GetActor()
        };

        _db.QualityInspections.Add(copy);
        AddAuditLog("Quality Inspection Duplicated", copy.Id, new { SourceId = source.Id, copy.InspectionNumber });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dto = await ProjectDetails(copy.Id).FirstAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(dto, "Kalite kontrolü kopyalandı."));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken cancellationToken)
    {
        var query = _db.QualityInspections.AsNoTracking().Where(x => x.IsActive);
        if (dateFrom.HasValue) query = query.Where(x => x.InspectionDate >= NormalizeUtc(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(x => x.InspectionDate <= NormalizeUtc(dateTo.Value));

        var inspections = await query.ToListAsync(cancellationToken);
        var inspectionIds = inspections.Select(x => x.Id).ToList();
        var defectTypesRaw = await _db.QualityDefects.AsNoTracking()
            .Where(x => inspectionIds.Contains(x.QualityInspectionId))
            .GroupBy(x => x.DefectType)
            .Select(x => new { Name = x.Key, Value = x.Sum(d => d.DefectPairs) })
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToListAsync(cancellationToken);
        var defectTypes = defectTypesRaw.Select(x => new SummaryItemDto(x.Name, x.Value)).ToList();

        var firePairs = await _db.QualityDefects.AsNoTracking()
            .Where(x => x.StationAssignmentFireId.HasValue && inspectionIds.Contains(x.QualityInspectionId))
            .SumAsync(x => x.DefectPairs, cancellationToken);

        var totalChecked = inspections.Sum(x => x.CheckedPairs);
        var totalRejected = inspections.Sum(x => x.RejectedPairs);
        var summary = new QualitySummaryDto(
            inspections.Count,
            inspections.Count(x => x.Result == "Passed"),
            inspections.Count(x => x.Result == "Conditional"),
            inspections.Count(x => x.Result == "Failed"),
            inspections.Count(x => x.Result == "Pending"),
            totalChecked,
            totalRejected,
            totalChecked > 0 ? Math.Round((decimal)totalRejected / totalChecked * 100m, 2) : 0,
            firePairs,
            inspections.Count(x => x.HoldProduction),
            defectTypes,
            Enumerable.Empty<SummaryItemDto>(),
            Enumerable.Empty<SummaryItemDto>(),
            Enumerable.Empty<SummaryItemDto>(),
            new DateRangeDto(dateFrom, dateTo));

        return Ok(ApiResponse<object>.SuccessResponse(summary));
    }

    [HttpGet("station/{stationAssignmentId:guid}/summary")]
    public async Task<IActionResult> GetStationSummary(Guid stationAssignmentId, CancellationToken cancellationToken)
    {
        var inspections = await _db.QualityInspections.AsNoTracking()
            .Where(x => x.StationAssignmentId == stationAssignmentId && x.IsActive)
            .OrderByDescending(x => x.InspectionDate)
            .ToListAsync(cancellationToken);

        var latest = inspections.FirstOrDefault();
        var inspectionIds = inspections.Select(x => x.Id).ToList();
        var linkedFirePairs = await _db.QualityDefects.AsNoTracking()
            .Where(x => inspectionIds.Contains(x.QualityInspectionId) && x.StationAssignmentFireId.HasValue)
            .SumAsync(x => x.DefectPairs, cancellationToken);

        var totalChecked = inspections.Sum(x => x.CheckedPairs);
        var totalRejected = inspections.Sum(x => x.RejectedPairs);
        var dto = new StationQualitySummaryDto(
            stationAssignmentId,
            latest is null ? null : new LatestInspectionDto(latest.Id, latest.InspectionNumber, latest.Result, latest.InspectionDate),
            inspections.Count,
            inspections.Count(x => x.Result == "Failed"),
            totalChecked,
            totalRejected,
            linkedFirePairs,
            latest?.Result ?? "NotChecked");

        return Ok(ApiResponse<object>.SuccessResponse(dto));
    }

    [HttpGet("work-order/{workOrderId:guid}/summary")]
    public async Task<IActionResult> GetWorkOrderSummary(Guid workOrderId, CancellationToken cancellationToken)
    {
        var inspections = await _db.QualityInspections.AsNoTracking()
            .Where(x => x.WorkOrderId == workOrderId && x.IsActive)
            .OrderByDescending(x => x.InspectionDate)
            .ToListAsync(cancellationToken);

        var totalChecked = inspections.Sum(x => x.CheckedPairs);
        var totalRejected = inspections.Sum(x => x.RejectedPairs);
        var latest = inspections.FirstOrDefault();
        var dto = new WorkOrderQualitySummaryDto(
            workOrderId,
            inspections.Count,
            inspections.Count(x => x.Result == "Passed"),
            inspections.Count(x => x.Result == "Failed"),
            totalChecked,
            totalRejected,
            totalChecked > 0 ? Math.Round((decimal)totalRejected / totalChecked * 100m, 2) : 0,
            latest?.Result ?? "NotChecked",
            inspections.Any() && inspections.All(x => x.Result != "Failed" && x.Result != "Pending"));

        return Ok(ApiResponse<object>.SuccessResponse(dto));
    }

    private IQueryable<QualityInspectionDetailDto> ProjectDetails(Guid? id = null)
    {
        var query = _db.QualityInspections.AsNoTracking();
        if (id.HasValue)
            query = query.Where(x => x.Id == id.Value);

        return query
            .Select(x => new QualityInspectionDetailDto(
                x.Id,
                x.InspectionNumber,
                x.InspectionType,
                x.StationAssignmentId,
                x.WorkOrderId,
                x.OrderItemId,
                x.ProductId,
                x.MoldId,
                x.MachineId,
                x.OperatorId,
                x.InspectionDate,
                x.Shift,
                x.Status,
                x.Result,
                x.SampleSizePairs,
                x.CheckedPairs,
                x.AcceptedPairs,
                x.RejectedPairs,
                x.ConditionalAcceptedPairs,
                x.TargetWeightGrams,
                x.MeasuredWeightGrams,
                x.WeightToleranceMinus,
                x.WeightTolerancePlus,
                x.WeightResult,
                x.TargetDensity,
                x.MeasuredDensity,
                x.DensityMinimum,
                x.DensityMaximum,
                x.DensityResult,
                x.TargetX,
                x.MeasuredX,
                x.TargetY,
                x.MeasuredY,
                x.DimensionTolerance,
                x.DimensionResult,
                x.VisualResult,
                x.ColorResult,
                x.SurfaceResult,
                x.FabricBondingResult,
                x.GeneralNotes,
                x.CorrectiveAction,
                x.HoldProduction,
                x.CreateFireRecord,
                x.FireReason,
                x.FirePairs,
                x.IsActive,
                x.IsCancelled,
                x.CancellationReason,
                x.CancelledAt,
                x.CancelledBy,
                x.CompletedAt,
                x.CompletedBy,
                x.ProductCodeSnapshot,
                x.ProductNameSnapshot,
                x.CustomerNameSnapshot,
                x.WorkOrderNumberSnapshot,
                x.MoldCodeSnapshot,
                x.OperatorNameSnapshot,
                x.StationAssignment.StationNumberSnapshot,
                x.Defects.OrderBy(d => d.Sequence).Select(d => new QualityDefectDto(
                    d.Id,
                    d.DefectType,
                    d.DefectCode,
                    d.Description,
                    d.DefectPairs,
                    d.Severity,
                    d.IsFireRelated,
                    d.StationAssignmentFireId,
                    d.CorrectiveAction,
                    d.Sequence)).ToList(),
                x.Defects.Where(d => d.StationAssignmentFireId.HasValue).Select(d => new LinkedFireDto(
                    d.StationAssignmentFireId!.Value,
                    d.DefectPairs,
                    d.DefectType)).ToList(),
                _db.StationAssignmentDowntimes
                    .Where(d => d.StationAssignmentId == x.StationAssignmentId && d.IsOpen && !d.IsCancelled)
                    .Select(d => new LinkedDowntimeDto(d.Id, d.DowntimeType, d.StartedAt, d.Reason))
                    .FirstOrDefault(),
                _db.StationAssignmentEvents
                    .Where(e => e.StationAssignmentId == x.StationAssignmentId && e.EventType.StartsWith("Quality"))
                    .OrderByDescending(e => e.EventTime)
                    .Take(10)
                    .Select(e => new QualityEventDto(e.Id, e.EventType, e.EventTime, e.Quantity, e.Reason, e.Note))
                    .ToList(),
                x.Created,
                x.LastModified));
    }

    private IQueryable<StationAssignment> QueryAssignments()
    {
        return _db.StationAssignments
            .Include(x => x.InjectionStation)
            .Include(x => x.WorkOrder).ThenInclude(x => x!.AssignedMachine)
            .Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Customer)
            .Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Product)
            .Include(x => x.OrderItem).ThenInclude(x => x.Product)
            .Include(x => x.Mold);
    }

    private ApiResponse<object>? ValidateRequest(UpsertQualityInspectionRequest request, bool requireCompleteFields)
    {
        if (!InspectionTypes.Contains(request.InspectionType))
            return ApiResponse<object>.Fail("Kontrol türü geçersiz.", "INVALID_INSPECTION_TYPE");
        if (request.StationAssignmentId == Guid.Empty)
            return ApiResponse<object>.Fail("Seçilen istasyon ataması bulunamadı.", "STATION_ASSIGNMENT_REQUIRED");
        if (request.SampleSizePairs <= 0)
            return ApiResponse<object>.Fail("Numune adedi sıfırdan büyük olmalıdır.", "INVALID_SAMPLE_SIZE");
        if (request.CheckedPairs <= 0)
            return ApiResponse<object>.Fail("Kontrol edilen çift sayısı sıfırdan büyük olmalıdır.", "INVALID_CHECKED_PAIRS");
        if (request.AcceptedPairs < 0 || request.RejectedPairs < 0 || request.ConditionalAcceptedPairs < 0)
            return ApiResponse<object>.Fail("Kontrol adetleri negatif olamaz.", "INVALID_PAIR_COUNTS");
        if (request.AcceptedPairs + request.RejectedPairs + request.ConditionalAcceptedPairs != request.CheckedPairs)
            return ApiResponse<object>.Fail("Uygun, şartlı uygun ve uygunsuz adetlerin toplamı kontrol edilen adede eşit olmalıdır.", "PAIR_TOTAL_MISMATCH");
        if (request.FirePairs.GetValueOrDefault() < 0 || request.FirePairs.GetValueOrDefault() > request.RejectedPairs)
            return ApiResponse<object>.Fail("Fire adedi uygunsuz adet sayısını aşamaz.", "FIRE_PAIRS_INVALID");
        if (HasNegative(request.MeasuredWeightGrams, request.MeasuredDensity, request.MeasuredX, request.MeasuredY, request.TargetWeightGrams, request.TargetDensity, request.TargetX, request.TargetY))
            return ApiResponse<object>.Fail("Ölçüm ve hedef değerleri negatif olamaz.", "NEGATIVE_MEASUREMENT");
        if (!IsCheckResult(request.VisualResult) || !IsCheckResult(request.ColorResult) || !IsCheckResult(request.SurfaceResult) || !IsCheckResult(request.FabricBondingResult))
            return ApiResponse<object>.Fail("Kontrol sonucu geçersiz.", "INVALID_CHECK_RESULT");
        if (request.Defects.Sum(x => x.DefectPairs) > request.CheckedPairs)
            return ApiResponse<object>.Fail("Kusur adetleri toplamı kontrol edilen adedi aşamaz.", "DEFECT_PAIRS_EXCEED_CHECKED");
        if (request.Defects.Any(x => x.DefectPairs < 0 || !Severities.Contains(x.Severity)))
            return ApiResponse<object>.Fail("Kusur adedi veya şiddeti geçersiz.", "INVALID_DEFECT");
        if (requireCompleteFields && request.MeasuredWeightGrams.HasValue && (!request.TargetWeightGrams.HasValue || !request.WeightToleranceMinus.HasValue || !request.WeightTolerancePlus.HasValue))
            return ApiResponse<object>.Fail("Kalite kontrol hedef değerleri bulunamadı.", "QUALITY_TARGETS_REQUIRED");

        return null;
    }

    private ApiResponse<object>? ValidateInspectionForCompletion(QualityInspection inspection)
    {
        if (inspection.CheckedPairs <= 0)
            return ApiResponse<object>.Fail("Kontrol edilen çift sayısı sıfırdan büyük olmalıdır.", "INVALID_CHECKED_PAIRS");
        if (inspection.AcceptedPairs + inspection.RejectedPairs + inspection.ConditionalAcceptedPairs != inspection.CheckedPairs)
            return ApiResponse<object>.Fail("Uygun, şartlı uygun ve uygunsuz adetlerin toplamı kontrol edilen adede eşit olmalıdır.", "PAIR_TOTAL_MISMATCH");
        if (inspection.FirePairs.GetValueOrDefault() > inspection.RejectedPairs)
            return ApiResponse<object>.Fail("Fire adedi uygunsuz adet sayısını aşamaz.", "FIRE_PAIRS_INVALID");
        if (inspection.MeasuredWeightGrams.HasValue && (!inspection.TargetWeightGrams.HasValue || !inspection.WeightToleranceMinus.HasValue || !inspection.WeightTolerancePlus.HasValue))
            return ApiResponse<object>.Fail("Kalite kontrol hedef değerleri bulunamadı.", "QUALITY_TARGETS_REQUIRED");
        if (inspection.MeasuredDensity.HasValue && (!inspection.DensityMinimum.HasValue || !inspection.DensityMaximum.HasValue))
            return ApiResponse<object>.Fail("Yoğunluk hedef değerleri bulunamadı.", "DENSITY_TARGETS_REQUIRED");
        if ((inspection.MeasuredX.HasValue || inspection.MeasuredY.HasValue) && (!inspection.TargetX.HasValue || !inspection.TargetY.HasValue || !inspection.DimensionTolerance.HasValue))
            return ApiResponse<object>.Fail("Ölçü / XY hedef değerleri bulunamadı.", "DIMENSION_TARGETS_REQUIRED");

        return null;
    }

    private void ApplyRequest(QualityInspection inspection, UpsertQualityInspectionRequest request, StationAssignment assignment, DateTime utcNow)
    {
        var product = assignment.OrderItem.Product ?? assignment.OrderItem.Order.Product;
        var machine = assignment.WorkOrder?.AssignedMachine;
        var operatorEntity = FindOperator(assignment.OperatorName);

        inspection.InspectionType = request.InspectionType;
        inspection.StationAssignmentId = assignment.Id;
        inspection.WorkOrderId = assignment.WorkOrderId;
        inspection.OrderItemId = assignment.OrderItemId;
        inspection.ProductId = product?.Id;
        inspection.MoldId = assignment.MoldId;
        inspection.MachineId = machine?.Id;
        inspection.OperatorId = operatorEntity?.Id;
        inspection.InspectionDate = request.InspectionDate.HasValue ? NormalizeUtc(request.InspectionDate.Value) : utcNow;
        inspection.Shift = request.Shift;
        inspection.SampleSizePairs = request.SampleSizePairs;
        inspection.CheckedPairs = request.CheckedPairs;
        inspection.AcceptedPairs = request.AcceptedPairs;
        inspection.RejectedPairs = request.RejectedPairs;
        inspection.ConditionalAcceptedPairs = request.ConditionalAcceptedPairs;
        inspection.TargetWeightGrams = request.TargetWeightGrams ?? assignment.Mold?.TargetPairWeight ?? product?.AverageWeight;
        inspection.MeasuredWeightGrams = request.MeasuredWeightGrams;
        inspection.WeightToleranceMinus = request.WeightToleranceMinus ?? CalculateMinusTolerance(assignment.Mold);
        inspection.WeightTolerancePlus = request.WeightTolerancePlus ?? CalculatePlusTolerance(assignment.Mold);
        inspection.TargetDensity = request.TargetDensity ?? assignment.Mold?.TargetDensity ?? product?.TargetDensity;
        inspection.MeasuredDensity = request.MeasuredDensity;
        inspection.DensityMinimum = request.DensityMinimum ?? assignment.Mold?.MinimumDensity;
        inspection.DensityMaximum = request.DensityMaximum ?? assignment.Mold?.MaximumDensity;
        inspection.TargetX = request.TargetX ?? assignment.Mold?.XCoordinate;
        inspection.MeasuredX = request.MeasuredX;
        inspection.TargetY = request.TargetY ?? assignment.Mold?.YCoordinate;
        inspection.MeasuredY = request.MeasuredY;
        inspection.DimensionTolerance = request.DimensionTolerance;
        inspection.VisualResult = request.VisualResult ?? "NotChecked";
        inspection.ColorResult = request.ColorResult ?? "NotChecked";
        inspection.SurfaceResult = request.SurfaceResult ?? "NotChecked";
        inspection.FabricBondingResult = request.FabricBondingResult ?? "NotChecked";
        inspection.GeneralNotes = request.GeneralNotes;
        inspection.CorrectiveAction = request.CorrectiveAction;
        inspection.HoldProduction = request.HoldProduction;
        inspection.CreateFireRecord = request.CreateFireRecord;
        inspection.FireReason = request.FireReason;
        inspection.FirePairs = request.FirePairs;
        inspection.ProductCodeSnapshot = product?.Code;
        inspection.ProductNameSnapshot = product?.Name;
        inspection.CustomerNameSnapshot = assignment.OrderItem.Order.Customer.CompanyName ?? assignment.OrderItem.Order.Customer.Name;
        inspection.WorkOrderNumberSnapshot = assignment.WorkOrder?.WorkOrderNumber;
        inspection.MoldCodeSnapshot = assignment.Mold?.Code;
        inspection.OperatorNameSnapshot = assignment.OperatorName;

        if (inspection.Defects.Count == 0)
            AddDefects(inspection, request.Defects, utcNow);
    }

    private void AddDefects(QualityInspection inspection, IReadOnlyCollection<QualityDefectRequest> defects, DateTime utcNow)
    {
        var sequence = 1;
        foreach (var defect in defects)
        {
            inspection.Defects.Add(new QualityDefect
            {
                DefectType = defect.DefectType.Trim(),
                DefectCode = defect.DefectCode,
                Description = defect.Description,
                DefectPairs = defect.DefectPairs,
                Severity = defect.Severity,
                IsFireRelated = defect.IsFireRelated,
                CorrectiveAction = defect.CorrectiveAction,
                Sequence = sequence++,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            });
        }
    }

    private void CalculateResults(QualityInspection inspection)
    {
        inspection.WeightResult = CalculateRangeResult(inspection.MeasuredWeightGrams, inspection.TargetWeightGrams, inspection.WeightToleranceMinus, inspection.WeightTolerancePlus);
        inspection.DensityResult = CalculateMinMaxResult(inspection.MeasuredDensity, inspection.DensityMinimum, inspection.DensityMaximum);
        inspection.DimensionResult = CalculateDimensionResult(inspection);

        var checks = new[] { inspection.WeightResult, inspection.DensityResult, inspection.DimensionResult, inspection.VisualResult, inspection.ColorResult, inspection.SurfaceResult, inspection.FabricBondingResult };
        if (inspection.Defects.Any(x => x.Severity == "Critical") || checks.Contains("Failed") || inspection.RejectedPairs > 0)
            inspection.Result = "Failed";
        else if (checks.Contains("Warning") || inspection.ConditionalAcceptedPairs > 0 || inspection.Defects.Any(x => x.Severity == "Major"))
            inspection.Result = "Conditional";
        else if (checks.Any(x => x == "NotChecked") && inspection.Defects.Count == 0)
            inspection.Result = "Pending";
        else
            inspection.Result = "Passed";
    }

    private StationAssignmentFire CreateFire(StationAssignment assignment, QualityInspection inspection, int firePairs, DateTime utcNow, string actor)
    {
        var product = assignment.OrderItem.Product ?? assignment.OrderItem.Order.Product;
        return new StationAssignmentFire
        {
            StationAssignmentId = assignment.Id,
            OrderItemId = assignment.OrderItemId,
            InjectionStationId = assignment.InjectionStationId,
            StationNumberSnapshot = assignment.StationNumberSnapshot,
            ProductId = product?.Id,
            ProductCodeSnapshot = product?.Code,
            ProductNameSnapshot = product?.Name,
            MoldId = assignment.MoldId,
            MoldCodeSnapshot = assignment.Mold?.Code,
            MoldNameSnapshot = assignment.Mold?.Name,
            OperatorNameSnapshot = assignment.OperatorName,
            FirePairs = firePairs,
            ReasonType = string.IsNullOrWhiteSpace(inspection.FireReason) ? "Diğer" : inspection.FireReason.Trim(),
            Reason = inspection.GeneralNotes,
            Note = $"Kalite kontrol bağlantısı: {inspection.InspectionNumber}",
            RecordedAt = utcNow,
            RecordedBy = actor,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    private async Task StartQualityHoldIfNeeded(StationAssignment assignment, QualityInspection inspection, DateTime utcNow, string actor, CancellationToken cancellationToken)
    {
        var hasOpenDowntime = await _db.StationAssignmentDowntimes
            .AnyAsync(x => x.StationAssignmentId == assignment.Id && x.IsOpen && !x.IsCancelled, cancellationToken);

        if (hasOpenDowntime)
            return;

        _db.StationAssignmentDowntimes.Add(new StationAssignmentDowntime
        {
            StationAssignmentId = assignment.Id,
            OrderItemId = assignment.OrderItemId,
            InjectionStationId = assignment.InjectionStationId,
            StationNumberSnapshot = assignment.StationNumberSnapshot,
            OperatorNameSnapshot = assignment.OperatorName,
            DowntimeType = "Kalite Kontrol Bekletmesi",
            Reason = inspection.Result,
            Note = inspection.CorrectiveAction ?? inspection.GeneralNotes,
            PreviousAssignmentStatus = assignment.Status,
            StartedAt = utcNow,
            IsOpen = true,
            StartedBy = actor,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });
        assignment.Status = "Duraklatıldı";
        await AddStationEvent(assignment, "QualityHoldStarted", utcNow, null, inspection.Result, inspection.InspectionNumber, actor, null);
        AddAuditLog("Quality Production Hold Started", inspection.Id, new { inspection.InspectionNumber, assignment.Id });
    }

    private async Task AddStationEvent(StationAssignment assignment, string eventType, DateTime eventTime, int? quantity, string? reason, string? note, string? recordedBy, string? metadataJson)
    {
        var product = assignment.OrderItem.Product ?? assignment.OrderItem.Order.Product;
        _db.StationAssignmentEvents.Add(new StationAssignmentEvent
        {
            StationAssignmentId = assignment.Id,
            OrderItemId = assignment.OrderItemId,
            InjectionStationId = assignment.InjectionStationId,
            StationNumberSnapshot = assignment.StationNumberSnapshot,
            ProductId = product?.Id,
            ProductCodeSnapshot = product?.Code,
            ProductNameSnapshot = product?.Name,
            MoldId = assignment.MoldId,
            MoldCodeSnapshot = assignment.Mold?.Code,
            MoldNameSnapshot = assignment.Mold?.Name,
            OperatorNameSnapshot = assignment.OperatorName,
            EventType = eventType,
            EventTime = eventTime,
            Quantity = quantity,
            Reason = reason,
            Note = note,
            RecordedBy = recordedBy,
            MetadataJson = metadataJson,
            CreatedAt = eventTime
        });

        await Task.CompletedTask;
    }

    private void AddAuditLog(string eventName, Guid entityId, object payload)
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

    private async Task<string> GenerateInspectionNumber(DateTime utcNow, CancellationToken cancellationToken)
    {
        var prefix = $"QC-{utcNow:yyyyMMdd}-";
        var lastNumber = await _db.QualityInspections
            .Where(x => x.InspectionNumber.StartsWith(prefix))
            .OrderByDescending(x => x.InspectionNumber)
            .Select(x => x.InspectionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (!string.IsNullOrWhiteSpace(lastNumber) && int.TryParse(lastNumber[^4..], out var parsed))
            next = parsed + 1;

        return prefix + next.ToString("0000");
    }

    private Operator? FindOperator(string? operatorName)
    {
        if (string.IsNullOrWhiteSpace(operatorName))
            return null;

        var name = operatorName.Trim().ToLower();
        return _db.Operators.Local.FirstOrDefault(x => (x.FullName ?? string.Empty).ToLower() == name)
            ?? _db.Operators.FirstOrDefault(x => x.FullName != null && x.FullName.ToLower() == name);
    }

    private string GetActor() => User?.Identity?.Name ?? "system";

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static decimal? CalculateMinusTolerance(Mold? mold)
    {
        if (mold?.TargetPairWeight is null || mold.MinimumPairWeight is null)
            return null;
        return Math.Max(mold.TargetPairWeight.Value - mold.MinimumPairWeight.Value, 0);
    }

    private static decimal? CalculatePlusTolerance(Mold? mold)
    {
        if (mold?.TargetPairWeight is null || mold.MaximumPairWeight is null)
            return null;
        return Math.Max(mold.MaximumPairWeight.Value - mold.TargetPairWeight.Value, 0);
    }

    private static string CalculateRangeResult(decimal? measured, decimal? target, decimal? minus, decimal? plus)
    {
        if (!measured.HasValue)
            return "NotChecked";
        if (!target.HasValue || !minus.HasValue || !plus.HasValue)
            return "Warning";

        return measured.Value >= target.Value - minus.Value && measured.Value <= target.Value + plus.Value ? "Passed" : "Failed";
    }

    private static string CalculateMinMaxResult(decimal? measured, decimal? minimum, decimal? maximum)
    {
        if (!measured.HasValue)
            return "NotChecked";
        if (!minimum.HasValue || !maximum.HasValue)
            return "Warning";

        return measured.Value >= minimum.Value && measured.Value <= maximum.Value ? "Passed" : "Failed";
    }

    private static string CalculateDimensionResult(QualityInspection inspection)
    {
        if (!inspection.MeasuredX.HasValue && !inspection.MeasuredY.HasValue)
            return "NotChecked";
        if (!inspection.TargetX.HasValue || !inspection.TargetY.HasValue || !inspection.DimensionTolerance.HasValue || !inspection.MeasuredX.HasValue || !inspection.MeasuredY.HasValue)
            return "Warning";

        return Math.Abs(inspection.MeasuredX.Value - inspection.TargetX.Value) <= inspection.DimensionTolerance.Value &&
               Math.Abs(inspection.MeasuredY.Value - inspection.TargetY.Value) <= inspection.DimensionTolerance.Value
            ? "Passed"
            : "Failed";
    }

    private static bool IsCheckResult(string? value) => string.IsNullOrWhiteSpace(value) || CheckResults.Contains(value);

    private static bool HasNegative(params decimal?[] values) => values.Any(x => x.HasValue && x.Value < 0);
}

public record UpsertQualityInspectionRequest(
    string InspectionType,
    Guid StationAssignmentId,
    int SampleSizePairs,
    int CheckedPairs,
    int AcceptedPairs,
    int RejectedPairs,
    int ConditionalAcceptedPairs,
    DateTime? InspectionDate,
    int? Shift,
    decimal? TargetWeightGrams,
    decimal? MeasuredWeightGrams,
    decimal? WeightToleranceMinus,
    decimal? WeightTolerancePlus,
    decimal? TargetDensity,
    decimal? MeasuredDensity,
    decimal? DensityMinimum,
    decimal? DensityMaximum,
    decimal? TargetX,
    decimal? MeasuredX,
    decimal? TargetY,
    decimal? MeasuredY,
    decimal? DimensionTolerance,
    string? VisualResult,
    string? ColorResult,
    string? SurfaceResult,
    string? FabricBondingResult,
    string? GeneralNotes,
    string? CorrectiveAction,
    bool HoldProduction,
    bool CreateFireRecord,
    string? FireReason,
    int? FirePairs,
    IReadOnlyCollection<QualityDefectRequest> Defects);

public record QualityDefectRequest(
    string DefectType,
    string? DefectCode,
    string? Description,
    int DefectPairs,
    string Severity,
    bool IsFireRelated,
    string? CorrectiveAction);

public record CancelQualityInspectionRequest(string? CancellationReason);

public record QualityInspectionListDto(
    Guid Id,
    string InspectionNumber,
    string InspectionType,
    DateTime InspectionDate,
    int StationNumber,
    string? WorkOrderNumber,
    string? CustomerName,
    string? ProductCode,
    string? ProductName,
    string? MoldCode,
    string? OperatorName,
    int SampleSizePairs,
    int CheckedPairs,
    int AcceptedPairs,
    int RejectedPairs,
    int ConditionalAcceptedPairs,
    string Result,
    string Status,
    bool HoldProduction,
    int LinkedFirePairs,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record QualityInspectionDetailDto(
    Guid Id,
    string InspectionNumber,
    string InspectionType,
    Guid StationAssignmentId,
    Guid? WorkOrderId,
    Guid OrderItemId,
    Guid? ProductId,
    Guid? MoldId,
    Guid? MachineId,
    Guid? OperatorId,
    DateTime InspectionDate,
    int? Shift,
    string Status,
    string Result,
    int SampleSizePairs,
    int CheckedPairs,
    int AcceptedPairs,
    int RejectedPairs,
    int ConditionalAcceptedPairs,
    decimal? TargetWeightGrams,
    decimal? MeasuredWeightGrams,
    decimal? WeightToleranceMinus,
    decimal? WeightTolerancePlus,
    string WeightResult,
    decimal? TargetDensity,
    decimal? MeasuredDensity,
    decimal? DensityMinimum,
    decimal? DensityMaximum,
    string DensityResult,
    decimal? TargetX,
    decimal? MeasuredX,
    decimal? TargetY,
    decimal? MeasuredY,
    decimal? DimensionTolerance,
    string DimensionResult,
    string VisualResult,
    string ColorResult,
    string SurfaceResult,
    string FabricBondingResult,
    string? GeneralNotes,
    string? CorrectiveAction,
    bool HoldProduction,
    bool CreateFireRecord,
    string? FireReason,
    int? FirePairs,
    bool IsActive,
    bool IsCancelled,
    string? CancellationReason,
    DateTime? CancelledAt,
    string? CancelledBy,
    DateTime? CompletedAt,
    string? CompletedBy,
    string? ProductCodeSnapshot,
    string? ProductNameSnapshot,
    string? CustomerNameSnapshot,
    string? WorkOrderNumberSnapshot,
    string? MoldCodeSnapshot,
    string? OperatorNameSnapshot,
    int StationNumber,
    IReadOnlyCollection<QualityDefectDto> Defects,
    IReadOnlyCollection<LinkedFireDto> LinkedFires,
    LinkedDowntimeDto? OpenDowntime,
    IReadOnlyCollection<QualityEventDto> Events,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record QualityDefectDto(Guid Id, string DefectType, string? DefectCode, string? Description, int DefectPairs, string Severity, bool IsFireRelated, Guid? StationAssignmentFireId, string? CorrectiveAction, int Sequence);
public record LinkedFireDto(Guid FireId, int DefectPairs, string DefectType);
public record LinkedDowntimeDto(Guid DowntimeId, string DowntimeType, DateTime StartedAt, string? Reason);
public record QualityEventDto(Guid Id, string EventType, DateTime EventTime, int? Quantity, string? Reason, string? Note);
public record SummaryItemDto(string Name, int Value);
public record DateRangeDto(DateTime? DateFrom, DateTime? DateTo);
public record QualitySummaryDto(int TotalInspections, int PassedCount, int ConditionalCount, int FailedCount, int PendingCount, int TotalCheckedPairs, int TotalRejectedPairs, decimal DefectRate, int TotalFirePairsLinked, int HoldCount, IEnumerable<SummaryItemDto> TopDefectTypes, IEnumerable<SummaryItemDto> TopProductsByDefect, IEnumerable<SummaryItemDto> TopMoldsByDefect, IEnumerable<SummaryItemDto> TopOperatorsByDefect, DateRangeDto DateRange);
public record LatestInspectionDto(Guid Id, string InspectionNumber, string Result, DateTime InspectionDate);
public record StationQualitySummaryDto(Guid StationAssignmentId, LatestInspectionDto? LatestInspection, int InspectionCount, int FailedCount, int TotalChecked, int TotalRejected, int LinkedFirePairs, string CurrentQualityStatus);
public record WorkOrderQualitySummaryDto(Guid WorkOrderId, int InspectionCount, int Passed, int Failed, int TotalChecked, int TotalRejected, decimal DefectRate, string LatestResult, bool CanCloseQuality);
