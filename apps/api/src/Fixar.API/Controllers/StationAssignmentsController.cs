using System.Text.Json;
using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
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
[Route("api/v{version:apiVersion}/station-assignments")]
public class StationAssignmentsController : ControllerBase
{
    private static readonly string[] FireReasonTypes =
    {
        "Eksik Döküm",
        "Hava Kabarcığı",
        "Yırtık",
        "Kumaş Kayması",
        "Gramaj Hatası",
        "Yoğunluk Hatası",
        "Pişme Hatası",
        "Renk Hatası",
        "Kalıp Kaynaklı",
        "Operatör Kaynaklı",
        "Hammadde Kaynaklı",
        "Diğer"
    };

    private static readonly string[] DowntimeTypes =
    {
        "Makine Arızası",
        "Kalıp Arızası",
        "Hammadde Bekleme",
        "Kumaş Bekleme",
        "Kalıp Değişimi",
        "Temizlik",
        "Bakım",
        "Elektrik Kesintisi",
        "Kompresör Arızası",
        "Operatör Molası",
        "Planlı Duruş",
        "Diğer"
    };

    private readonly ApplicationDbContext _db;

    public StationAssignmentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("live-summary")]
    public async Task<IActionResult> GetLiveSummary(CancellationToken cancellationToken)
    {
        var activeAssignments = await QueryAssignments()
            .Where(x => x.FinishedAt == null)
            .OrderBy(x => x.StationNumberSnapshot)
            .ToListAsync(cancellationToken);

        var assignmentIds = activeAssignments.Select(x => x.Id).ToList();
        var firePairsByAssignment = await _db.StationAssignmentFires
            .Where(x => assignmentIds.Contains(x.StationAssignmentId) && !x.IsCancelled)
            .GroupBy(x => x.StationAssignmentId)
            .Select(x => new { AssignmentId = x.Key, FirePairs = x.Sum(y => y.FirePairs) })
            .ToDictionaryAsync(x => x.AssignmentId, x => x.FirePairs, cancellationToken);

        var openDowntimes = await _db.StationAssignmentDowntimes
            .Where(x => assignmentIds.Contains(x.StationAssignmentId) && x.IsOpen && !x.IsCancelled)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);

        var openDowntimeByAssignment = openDowntimes
            .GroupBy(x => x.StationAssignmentId)
            .ToDictionary(x => x.Key, x => x.First());

        var assignmentsByStation = activeAssignments
            .GroupBy(x => x.StationNumberSnapshot)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.StartedAt).First());

        var stationSummaries = Enumerable.Range(1, 24).Select(stationNumber =>
        {
            assignmentsByStation.TryGetValue(stationNumber, out var assignment);
            if (assignment is null)
                return ToEmptyStationResponse(stationNumber);

            firePairsByAssignment.TryGetValue(assignment.Id, out var firePairs);
            openDowntimeByAssignment.TryGetValue(assignment.Id, out var downtime);
            return ToLiveStationResponse(assignment, firePairs, downtime);
        }).ToList();

        var lastTurnEvent = await _db.StationAssignmentEvents
            .Where(x => x.EventType == "Tur Eklendi")
            .OrderByDescending(x => x.EventTime)
            .FirstOrDefaultAsync(cancellationToken);

        var turkeyZone = ResolveTurkeyTimeZone();
        var todayStartLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyZone).Date;
        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(todayStartLocal, turkeyZone);
        var todayProducedPairs = await _db.StationAssignmentEvents
            .Where(x => x.EventType == "Tur Eklendi" && x.EventTime >= todayStartUtc)
            .SumAsync(x => x.Quantity ?? 0, cancellationToken);
        var todayTurnCount = await _db.StationAssignmentEvents
            .CountAsync(x => x.EventType == "Tur Eklendi" && x.EventTime >= todayStartUtc, cancellationToken);
        var todayFirePairs = await _db.StationAssignmentFires
            .Where(x => !x.IsCancelled && x.RecordedAt >= todayStartUtc)
            .SumAsync(x => x.FirePairs, cancellationToken);

        var producedPairs = activeAssignments.Sum(x => x.ProducedPairs);
        var firePairs = stationSummaries.Sum(x => x.FirePairs);
        var goodPairs = Math.Max(producedPairs - firePairs, 0);
        var summary = new
        {
            TotalStationCount = 24,
            ActiveStationCount = stationSummaries.Count(x => x.Status == "Üretimde"),
            EmptyStationCount = stationSummaries.Count(x => x.Status == "Boş"),
            PausedStationCount = stationSummaries.Count(x => x.Status == "Duraklatıldı"),
            OpenDowntimeCount = openDowntimes.Count,
            OpenFaultCount = openDowntimes.Count(x => IsFaultType(x.DowntimeType)),
            ActiveJobsProducedPairs = producedPairs,
            DailyProducedPairs = producedPairs,
            GoodPairs = goodPairs,
            FirePairs = firePairs,
            FirePercent = producedPairs > 0 ? Math.Round((decimal)firePairs / producedPairs * 100, 2) : 0,
            ReleaseDueStationCount = stationSummaries.Count(x => x.ReleaseDue),
            LastTurnAt = lastTurnEvent?.EventTime,
            LastTurnAddedPairs = lastTurnEvent?.Quantity,
            TodayProducedPairs = todayProducedPairs,
            TodayFirePairs = todayFirePairs,
            TodayTurnCount = todayTurnCount,
            Stations = stationSummaries
        };

        return Ok(ApiResponse<object>.SuccessResponse(summary));
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var assignments = await QueryAssignments()
            .Where(x => x.FinishedAt == null)
            .OrderBy(x => x.StationNumberSnapshot)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(assignments.Select(ToAssignmentListResponse)));
    }

    [HttpGet("station/{stationNumber:int}")]
    public async Task<IActionResult> GetByStation(int stationNumber, CancellationToken cancellationToken)
    {
        var assignment = await QueryAssignments()
            .Where(x => x.FinishedAt == null && x.StationNumberSnapshot == stationNumber)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Bu istasyonda aktif üretim yok.", "STATION_EMPTY"));

        var firePairs = await GetFirePairs(assignment.Id, cancellationToken);
        var openDowntime = await GetOpenDowntime(assignment.Id, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToAssignmentDetailResponse(assignment, firePairs, openDowntime)));
    }

    [HttpPost("assign")]
    [Authorize(Policy = AuthorizationPolicies.CanPlanProduction)]
    public async Task<IActionResult> Assign([FromBody] AssignStationRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var station = await _db.InjectionStations
            .FirstOrDefaultAsync(x => x.StationNumber == request.StationNumber, cancellationToken);

        if (station is null)
            return NotFound(ApiResponse<object>.Fail("İstasyon bulunamadı.", "STATION_NOT_FOUND"));

        var alreadyActive = await _db.StationAssignments
            .AnyAsync(x => x.StationNumberSnapshot == request.StationNumber && x.FinishedAt == null, cancellationToken);

        if (alreadyActive)
            return BadRequest(ApiResponse<object>.Fail("Bu istasyonda zaten aktif iş var.", "STATION_ALREADY_ACTIVE"));

        WorkOrder? workOrder = null;
        var orderItemId = request.OrderItemId;

        if (request.WorkOrderId.HasValue)
        {
            workOrder = await _db.WorkOrders
                .FirstOrDefaultAsync(x => x.Id == request.WorkOrderId.Value, cancellationToken);

            if (workOrder is null)
                return NotFound(ApiResponse<object>.Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"));

            if (workOrder.Status is not ("Planned" or "Ready"))
                return BadRequest(ApiResponse<object>.Fail("Yalnızca Planlandı veya Hazır durumundaki iş emirleri planlamaya alınabilir.", "WORK_ORDER_NOT_AVAILABLE"));

            if (workOrder.IsCancelled || !workOrder.IsActive)
                return BadRequest(ApiResponse<object>.Fail("Pasif veya iptal edilmiş iş emri planlamaya alınamaz.", "WORK_ORDER_INACTIVE"));

            var assignedPairs = await _db.StationAssignments
                .Where(x => x.WorkOrderId == workOrder.Id)
                .SumAsync(x => x.PlannedPairs, cancellationToken);
            var remainingToAssign = workOrder.PlannedPairs - assignedPairs;
            var requestedPairs = request.PlannedPairs ?? remainingToAssign;

            if (requestedPairs <= 0 || requestedPairs > remainingToAssign)
                return BadRequest(ApiResponse<object>.Fail("Atama miktarı iş emrinin kalan atanabilir miktarını aşıyor.", "WORK_ORDER_ASSIGNMENT_LIMIT"));

            orderItemId = workOrder.OrderItemId;
        }

        var orderItem = await _db.OrderItems
            .FirstOrDefaultAsync(x => x.Id == orderItemId, cancellationToken);

        if (orderItem is null)
            return NotFound(ApiResponse<object>.Fail("Sipariş kalemi bulunamadı.", "ORDER_ITEM_NOT_FOUND"));

        Mold? mold = null;
        if (request.MoldId.HasValue)
        {
            mold = await _db.Molds.FirstOrDefaultAsync(x => x.Id == request.MoldId.Value, cancellationToken);
            if (mold is null)
                return NotFound(ApiResponse<object>.Fail("Kalıp bulunamadı.", "MOLD_NOT_FOUND"));
        }

        var utcNow = DateTime.UtcNow;
        var assignment = new StationAssignment
        {
            InjectionStationId = station.Id,
            OrderItemId = orderItemId,
            WorkOrderId = workOrder?.Id,
            MoldId = request.MoldId,
            StationNumberSnapshot = request.StationNumber,
            OperatorName = request.OperatorName,
            StartedAt = utcNow,
            PlannedPairs = request.PlannedPairs ?? workOrder?.PlannedPairs ?? orderItem.QuantityPairs,
            Status = "Üretimde",
            ProducedPairs = 0,
            FirePairs = 0,
            TotalTurns = 0,
            TurnsSinceLastRelease = 0,
            ReleaseFrequencyTurns = mold?.ReleaseFrequencyCycles,
            Note = request.Note
        };

        station.Status = "Üretimde";

        _db.StationAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);
        await AddEvent(assignment.Id, "İş Başlatıldı", utcNow, null, null, request.Note, GetActor(), null, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var created = await QueryAssignments().FirstAsync(x => x.Id == assignment.Id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToAssignmentListResponse(created), "İş başlatıldı."));
    }

    [HttpPost("add-production")]
    [Authorize(Policy = AuthorizationPolicies.CanRecordProduction), Idempotent]
    public async Task<IActionResult> AddProduction([FromBody] AddProductionRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (_db.Database.IsRelational())
            await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81003)", cancellationToken);

        var assignment = await QueryAssignments()
            .FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId && x.FinishedAt == null, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        if (request.ProducedPairs <= 0)
            return BadRequest(ApiResponse<object>.Fail("Üretilen çift 0'dan büyük olmalı.", "INVALID_QUANTITY"));

        if (request.ProducedPairs > assignment.PlannedPairs - assignment.ProducedPairs ||
            request.ProducedPairs > assignment.OrderItem.QuantityPairs - assignment.OrderItem.ProducedPairs)
            return Conflict(ApiResponse<object>.Fail("Üretim miktarı istasyon veya sipariş kalemi kalan miktarını aşamaz.", "PRODUCTION_EXCEEDS_REMAINING"));

        var hasOpenDowntime = await HasOpenDowntime(assignment.Id, cancellationToken);
        if (hasOpenDowntime)
            return BadRequest(ApiResponse<object>.Fail("Açık duruş bulunan istasyona üretim eklenemez.", "OPEN_DOWNTIME_EXISTS"));

        assignment.ProducedPairs += request.ProducedPairs;
        assignment.OrderItem.ProducedPairs += request.ProducedPairs;
        await AddEvent(assignment.Id, "Manuel Üretim Eklendi", DateTime.UtcNow, request.ProducedPairs, null, null, GetActor(), null, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToAssignmentListResponse(assignment), "Üretim adedi eklendi."));
    }

    [HttpPost("add-turn")]
    [Authorize(Policy = AuthorizationPolicies.CanRecordProduction), Idempotent]
    public async Task<IActionResult> AddTurn([FromBody] AddTurnRequest request, CancellationToken cancellationToken)
    {
        if (request.TurnCount <= 0)
            return BadRequest(ApiResponse<object>.Fail("Tur adedi 0'dan büyük olmalı.", "INVALID_TURN_COUNT"));

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (_db.Database.IsRelational())
            await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81003)", cancellationToken);

        var actor = GetActor();
        if (!string.IsNullOrWhiteSpace(request.RequestId))
        {
            var requestToken = $"\"requestId\":\"{request.RequestId.Trim()}\"";
            var duplicate = await _db.StationAssignmentEvents
                .AnyAsync(x => x.EventType == "Tur Eklendi" && x.MetadataJson != null && x.MetadataJson.Contains(requestToken), cancellationToken);

            if (duplicate)
            {
                var duplicateResult = new
                {
                    TurnCount = request.TurnCount,
                    ActiveStationCount = 0,
                    SkippedStationCount = 0,
                    TotalAddedPairs = 0,
                    AddedAt = DateTime.UtcNow,
                    Duplicate = true,
                    Stations = Array.Empty<object>(),
                    SkippedStations = Array.Empty<object>(),
                    ReleaseDueStations = Array.Empty<object>()
                };

                return Ok(ApiResponse<object>.SuccessResponse(duplicateResult, "Bu tur isteği daha önce işlendi."));
            }
        }

        var candidates = await QueryAssignments()
            .Where(x => x.FinishedAt == null && (x.Status == "Üretimde" || x.Status == "Duraklatıldı"))
            .OrderBy(x => x.StationNumberSnapshot)
            .ToListAsync(cancellationToken);

        var candidateIds = candidates.Select(x => x.Id).ToList();
        var openDowntimeIds = await _db.StationAssignmentDowntimes
            .Where(x => candidateIds.Contains(x.StationAssignmentId) && x.IsOpen && !x.IsCancelled)
            .Select(x => x.StationAssignmentId)
            .ToListAsync(cancellationToken);

        var openDowntimeSet = openDowntimeIds.ToHashSet();
        var eligible = candidates
            .Where(x => x.Status == "Üretimde" && !openDowntimeSet.Contains(x.Id))
            .ToList();
        var skipped = candidates
            .Where(x => x.Status != "Üretimde" || openDowntimeSet.Contains(x.Id))
            .Select(x => new
            {
                StationNumber = x.StationNumberSnapshot,
                AssignmentId = x.Id,
                Reason = openDowntimeSet.Contains(x.Id) ? "Açık duruş var." : "İstasyon üretimde değil."
            })
            .ToList();

        if (!eligible.Any())
            return BadRequest(ApiResponse<object>.Fail("Aktif üretimde tur eklenecek istasyon yok.", "NO_ACTIVE_STATION"));

        var utcNow = DateTime.UtcNow;
        var stationResults = new List<object>();
        var processed = new List<StationAssignment>();

        foreach (var assignment in eligible)
        {
            var assignmentRemaining = assignment.PlannedPairs - assignment.ProducedPairs;
            var orderRemaining = assignment.OrderItem.QuantityPairs - assignment.OrderItem.ProducedPairs;
            if (request.TurnCount > assignmentRemaining || request.TurnCount > orderRemaining)
            {
                skipped.Add(new
                {
                    StationNumber = assignment.StationNumberSnapshot,
                    AssignmentId = assignment.Id,
                    Reason = "İstasyon veya sipariş kalemi hedefi doldu."
                });
                continue;
            }

            assignment.ProducedPairs += request.TurnCount;
            assignment.OrderItem.ProducedPairs += request.TurnCount;
            assignment.TotalTurns += request.TurnCount;
            assignment.TurnsSinceLastRelease += request.TurnCount;
            assignment.LastTurnAt = utcNow;

            if (!string.IsNullOrWhiteSpace(request.Note))
                assignment.Note = request.Note;

            var metadata = JsonSerializer.Serialize(new { requestId = request.RequestId, assignment.TotalTurns });
            await AddEvent(assignment.Id, "Tur Eklendi", utcNow, request.TurnCount, null, request.Note, actor, metadata, cancellationToken);
            processed.Add(assignment);

            stationResults.Add(new
            {
                StationNumber = assignment.StationNumberSnapshot,
                AssignmentId = assignment.Id,
                AddedPairs = request.TurnCount,
                assignment.ProducedPairs,
                assignment.TotalTurns,
                assignment.TurnsSinceLastRelease,
                assignment.ReleaseFrequencyTurns,
                ReleaseDue = IsReleaseDue(assignment)
            });
        }

        if (processed.Count == 0)
            return Conflict(ApiResponse<object>.Fail("Aktif istasyonların kalan üretim hedefi bulunmuyor.", "PRODUCTION_TARGET_REACHED"));

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var result = new
        {
            TurnCount = request.TurnCount,
            ActiveStationCount = processed.Count,
            SkippedStationCount = skipped.Count,
            TotalAddedPairs = processed.Count * request.TurnCount,
            AddedAt = utcNow,
            Stations = stationResults,
            SkippedStations = skipped,
            ReleaseDueStations = processed
                .Where(IsReleaseDue)
                .Select(x => new { StationNumber = x.StationNumberSnapshot, AssignmentId = x.Id })
                .ToList()
        };

        return Ok(ApiResponse<object>.SuccessResponse(result, "Tur üretimi eklendi."));
    }

    [HttpPost("pause")]
    [Authorize(Policy = AuthorizationPolicies.CanRecordProduction), Idempotent]
    public async Task<IActionResult> Pause([FromBody] AssignmentActionRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _db.StationAssignments
            .FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId && x.FinishedAt == null, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        assignment.Status = "Duraklatıldı";
        assignment.Note = request.Note ?? assignment.Note;
        await AddEvent(assignment.Id, "İş Duraklatıldı", DateTime.UtcNow, null, null, request.Note, GetActor(), null, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { assignment.Id, assignment.Status }, "İş duraklatıldı."));
    }

    [HttpPost("resume")]
    [Authorize(Policy = AuthorizationPolicies.CanRecordProduction), Idempotent]
    public async Task<IActionResult> Resume([FromBody] AssignmentActionRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _db.StationAssignments
            .FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId && x.FinishedAt == null, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        if (await HasOpenDowntime(assignment.Id, cancellationToken))
            return BadRequest(ApiResponse<object>.Fail("Açık duruş bitmeden iş devam ettirilemez.", "OPEN_DOWNTIME_EXISTS"));

        assignment.Status = "Üretimde";
        assignment.Note = request.Note ?? assignment.Note;
        await AddEvent(assignment.Id, "İş Devam Ettirildi", DateTime.UtcNow, null, null, request.Note, GetActor(), null, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { assignment.Id, assignment.Status }, "İş devam ettirildi."));
    }

    [HttpPost("finish")]
    [Authorize(Policy = AuthorizationPolicies.CanPlanProduction), Idempotent]
    public async Task<IActionResult> Finish([FromBody] AssignmentActionRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _db.StationAssignments
            .Include(x => x.InjectionStation)
            .FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId && x.FinishedAt == null, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        if (await HasOpenDowntime(assignment.Id, cancellationToken))
            return BadRequest(ApiResponse<object>.Fail("Açık duruş bitmeden iş tamamlanamaz.", "OPEN_DOWNTIME_EXISTS"));

        assignment.Status = "Tamamlandı";
        assignment.FinishedAt = DateTime.UtcNow;
        assignment.Note = request.Note ?? assignment.Note;
        assignment.InjectionStation.Status = "Boş";
        await AddEvent(assignment.Id, "İş Tamamlandı", DateTime.UtcNow, null, null, request.Note, GetActor(), null, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { assignment.Id, assignment.Status }, "İş tamamlandı."));
    }

    [HttpPost("{assignmentId:guid}/fires")]
    [Authorize(Policy = AuthorizationPolicies.CanRecordProduction), Idempotent]
    public async Task<IActionResult> CreateFire(Guid assignmentId, [FromBody] CreateFireRequest request, CancellationToken cancellationToken)
    {
        if (request.FirePairs <= 0)
            return BadRequest(ApiResponse<object>.Fail("Fire çift adedi 0'dan büyük olmalıdır.", "INVALID_FIRE_PAIRS"));

        if (!FireReasonTypes.Contains(request.ReasonType))
            return BadRequest(ApiResponse<object>.Fail("Fire nedeni geçersiz.", "INVALID_FIRE_REASON"));

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var assignment = await QueryAssignments()
            .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        if (assignment.FinishedAt is not null)
            return BadRequest(ApiResponse<object>.Fail("Tamamlanmış iş için fire kaydı girilemez.", "ASSIGNMENT_CLOSED"));

        if (assignment.FirePairs + request.FirePairs > assignment.ProducedPairs)
            return BadRequest(ApiResponse<object>.Fail("Toplam fire, üretilen çift adedini aşamaz.", "FIRE_EXCEEDS_PRODUCTION"));

        var utcNow = DateTime.UtcNow;
        var actor = GetActor();
        var fire = CreateFireEntity(assignment, request, utcNow, actor);
        assignment.FirePairs += request.FirePairs;
        _db.StationAssignmentFires.Add(fire);
        await AddEvent(assignment.Id, "Fire Kaydedildi", utcNow, request.FirePairs, request.ReasonType, request.Note, actor, null, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToFireResponse(fire), "Fire kaydı oluşturuldu."));
    }

    [HttpGet("{assignmentId:guid}/fires")]
    public async Task<IActionResult> GetFires(Guid assignmentId, CancellationToken cancellationToken)
    {
        var exists = await _db.StationAssignments.AnyAsync(x => x.Id == assignmentId, cancellationToken);
        if (!exists)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        var fireEntities = await _db.StationAssignmentFires
            .Where(x => x.StationAssignmentId == assignmentId)
            .OrderByDescending(x => x.RecordedAt)
            .ToListAsync(cancellationToken);
        var fires = fireEntities.Select(ToFireResponse).ToList();

        return Ok(ApiResponse<object>.SuccessResponse(fires));
    }

    [HttpPost("{assignmentId:guid}/fires/{fireId:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.CanOverrideProductionRules), Idempotent]
    public async Task<IActionResult> CancelFire(Guid assignmentId, Guid fireId, [FromBody] CancelFireRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var assignment = await _db.StationAssignments.FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        var fire = await _db.StationAssignmentFires
            .FirstOrDefaultAsync(x => x.Id == fireId && x.StationAssignmentId == assignmentId, cancellationToken);

        if (fire is null)
            return NotFound(ApiResponse<object>.Fail("Fire kaydı bulunamadı.", "FIRE_NOT_FOUND"));

        if (fire.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("İptal edilmiş fire kaydı tekrar iptal edilemez.", "FIRE_ALREADY_CANCELLED"));

        var utcNow = DateTime.UtcNow;
        var actor = GetActor();
        fire.IsCancelled = true;
        fire.CancelledAt = utcNow;
        fire.CancelledBy = actor;
        fire.CancellationReason = request.CancellationReason;
        fire.UpdatedAt = utcNow;
        assignment.FirePairs = Math.Max(assignment.FirePairs - fire.FirePairs, 0);
        await AddEvent(assignment.Id, "Fire İptal Edildi", utcNow, fire.FirePairs, request.CancellationReason, fire.Note, actor, null, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToFireResponse(fire), "Fire kaydı iptal edildi."));
    }

    [HttpPost("{assignmentId:guid}/downtimes/start")]
    [Authorize(Policy = AuthorizationPolicies.CanRecordProduction), Idempotent]
    public async Task<IActionResult> StartDowntime(Guid assignmentId, [FromBody] StartDowntimeRequest request, CancellationToken cancellationToken)
    {
        if (!DowntimeTypes.Contains(request.DowntimeType))
            return BadRequest(ApiResponse<object>.Fail("Duruş türü geçersiz.", "INVALID_DOWNTIME_TYPE"));

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var assignment = await QueryAssignments().FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        if (assignment.FinishedAt is not null)
            return BadRequest(ApiResponse<object>.Fail("Tamamlanmış iş için duruş başlatılamaz.", "ASSIGNMENT_CLOSED"));

        if (await HasOpenDowntime(assignmentId, cancellationToken))
            return BadRequest(ApiResponse<object>.Fail("Bu iş için zaten açık duruş var.", "OPEN_DOWNTIME_EXISTS"));

        var utcNow = DateTime.UtcNow;
        var actor = GetActor();
        var downtime = CreateDowntimeEntity(assignment, request, utcNow, actor);
        assignment.Status = "Duraklatıldı";
        _db.StationAssignmentDowntimes.Add(downtime);
        await AddEvent(assignment.Id, "Duruş Başladı", utcNow, null, request.DowntimeType, request.Note, actor, null, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToDowntimeResponse(downtime), "Duruş başlatıldı."));
    }

    [HttpPost("{assignmentId:guid}/downtimes/{downtimeId:guid}/finish")]
    [Authorize(Policy = AuthorizationPolicies.CanRecordProduction), Idempotent]
    public async Task<IActionResult> FinishDowntime(Guid assignmentId, Guid downtimeId, [FromBody] FinishDowntimeRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var assignment = await _db.StationAssignments.FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        var downtime = await _db.StationAssignmentDowntimes
            .FirstOrDefaultAsync(x => x.Id == downtimeId && x.StationAssignmentId == assignmentId, cancellationToken);

        if (downtime is null)
            return NotFound(ApiResponse<object>.Fail("Duruş kaydı bulunamadı.", "DOWNTIME_NOT_FOUND"));

        if (!downtime.IsOpen || downtime.IsCancelled)
            return BadRequest(ApiResponse<object>.Fail("Bu duruş zaten kapatılmış.", "DOWNTIME_ALREADY_CLOSED"));

        var utcNow = DateTime.UtcNow;
        var actor = GetActor();
        downtime.IsOpen = false;
        downtime.EndedAt = utcNow;
        downtime.DurationMinutes = Math.Round((decimal)(utcNow - downtime.StartedAt).TotalMinutes, 2);
        downtime.EndedBy = actor;
        downtime.Note = request.Note ?? downtime.Note;
        downtime.UpdatedAt = utcNow;
        assignment.Status = string.IsNullOrWhiteSpace(downtime.PreviousAssignmentStatus) || downtime.PreviousAssignmentStatus == "Duraklatıldı"
            ? "Üretimde"
            : downtime.PreviousAssignmentStatus;
        await AddEvent(assignment.Id, "Duruş Bitti", utcNow, null, downtime.DowntimeType, request.Note, actor, null, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToDowntimeResponse(downtime), "Duruş bitirildi."));
    }

    [HttpGet("{assignmentId:guid}/downtimes")]
    public async Task<IActionResult> GetDowntimes(Guid assignmentId, CancellationToken cancellationToken)
    {
        var exists = await _db.StationAssignments.AnyAsync(x => x.Id == assignmentId, cancellationToken);
        if (!exists)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        var downtimeEntities = await _db.StationAssignmentDowntimes
            .Where(x => x.StationAssignmentId == assignmentId)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);
        var downtimes = downtimeEntities.Select(ToDowntimeResponse).ToList();

        return Ok(ApiResponse<object>.SuccessResponse(downtimes));
    }

    [HttpPost("{assignmentId:guid}/release-applied")]
    [Authorize(Policy = AuthorizationPolicies.CanRecordProduction), Idempotent]
    public async Task<IActionResult> ApplyRelease(Guid assignmentId, [FromBody] ReleaseAppliedRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var assignment = await _db.StationAssignments.FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        if (assignment is null || assignment.FinishedAt is not null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        if (!assignment.MoldId.HasValue)
            return BadRequest(ApiResponse<object>.Fail("Kalıp atanmamış işte kalıp ayırıcı kaydı yapılamaz.", "MOLD_REQUIRED"));

        var utcNow = DateTime.UtcNow;
        assignment.TurnsSinceLastRelease = 0;
        assignment.LastReleaseAt = utcNow;
        assignment.LastReleaseTurn = assignment.TotalTurns;
        await AddEvent(assignment.Id, "Kalıp Ayırıcı Uygulandı", utcNow, null, null, request.Note, GetActor(), null, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var warning = assignment.ReleaseFrequencyTurns is null or <= 0
            ? "Kalıp ayırıcı uygulandı. Uyarı: Kalıp ayırıcı sıklığı kalıp kartında tanımlı değil."
            : "Kalıp ayırıcı uygulandı.";

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            assignment.Id,
            assignment.TurnsSinceLastRelease,
            assignment.LastReleaseAt,
            assignment.LastReleaseTurn,
            assignment.ReleaseFrequencyTurns,
            ReleaseDue = IsReleaseDue(assignment)
        }, warning));
    }

    [HttpGet("{assignmentId:guid}/events")]
    public async Task<IActionResult> GetEvents(Guid assignmentId, CancellationToken cancellationToken)
    {
        var exists = await _db.StationAssignments.AnyAsync(x => x.Id == assignmentId, cancellationToken);
        if (!exists)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        var eventEntities = await _db.StationAssignmentEvents
            .Where(x => x.StationAssignmentId == assignmentId)
            .OrderByDescending(x => x.EventTime)
            .Take(100)
            .ToListAsync(cancellationToken);
        var events = eventEntities.Select(ToEventResponse).ToList();

        return Ok(ApiResponse<object>.SuccessResponse(events));
    }

    private IQueryable<StationAssignment> QueryAssignments()
    {
        return _db.StationAssignments
            .Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Customer)
            .Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Product)
            .Include(x => x.OrderItem).ThenInclude(x => x.Product)
            .Include(x => x.WorkOrder)
            .Include(x => x.Mold)
            .Include(x => x.InjectionStation);
    }

    private async Task<int> GetFirePairs(Guid assignmentId, CancellationToken cancellationToken)
    {
        return await _db.StationAssignmentFires
            .Where(x => x.StationAssignmentId == assignmentId && !x.IsCancelled)
            .SumAsync(x => x.FirePairs, cancellationToken);
    }

    private async Task<bool> HasOpenDowntime(Guid assignmentId, CancellationToken cancellationToken)
    {
        return await _db.StationAssignmentDowntimes
            .AnyAsync(x => x.StationAssignmentId == assignmentId && x.IsOpen && !x.IsCancelled, cancellationToken);
    }

    private async Task<StationAssignmentDowntime?> GetOpenDowntime(Guid assignmentId, CancellationToken cancellationToken)
    {
        return await _db.StationAssignmentDowntimes
            .Where(x => x.StationAssignmentId == assignmentId && x.IsOpen && !x.IsCancelled)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task AddEvent(
        Guid assignmentId,
        string eventType,
        DateTime eventTime,
        int? quantity,
        string? reason,
        string? note,
        string? recordedBy,
        string? metadataJson,
        CancellationToken cancellationToken)
    {
        var assignment = await QueryAssignments().FirstAsync(x => x.Id == assignmentId, cancellationToken);
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
    }

    private StationAssignmentFire CreateFireEntity(StationAssignment assignment, CreateFireRequest request, DateTime utcNow, string actor)
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
            FirePairs = request.FirePairs,
            ReasonType = request.ReasonType,
            Reason = request.Reason,
            Note = request.Note,
            RecordedAt = utcNow,
            RecordedBy = actor,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    private StationAssignmentDowntime CreateDowntimeEntity(StationAssignment assignment, StartDowntimeRequest request, DateTime utcNow, string actor)
    {
        return new StationAssignmentDowntime
        {
            StationAssignmentId = assignment.Id,
            OrderItemId = assignment.OrderItemId,
            InjectionStationId = assignment.InjectionStationId,
            StationNumberSnapshot = assignment.StationNumberSnapshot,
            OperatorNameSnapshot = assignment.OperatorName,
            DowntimeType = request.DowntimeType,
            Reason = request.Reason,
            Note = request.Note,
            PreviousAssignmentStatus = assignment.Status,
            StartedAt = utcNow,
            IsOpen = true,
            StartedBy = actor,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    private static object ToAssignmentListResponse(StationAssignment assignment)
    {
        var product = assignment.OrderItem.Product ?? assignment.OrderItem.Order.Product;
        return new
        {
            assignment.Id,
            assignment.StationNumberSnapshot,
            assignment.Status,
            assignment.OperatorName,
            assignment.ProducedPairs,
            assignment.FirePairs,
            GoodPairs = Math.Max(assignment.ProducedPairs - assignment.FirePairs, 0),
            assignment.TotalTurns,
            assignment.TurnsSinceLastRelease,
            assignment.ReleaseFrequencyTurns,
            ReleaseDue = IsReleaseDue(assignment),
            CustomerName = assignment.OrderItem.Order.Customer.CompanyName ?? assignment.OrderItem.Order.Customer.Name,
            ProductName = product?.Name,
            ProductCode = product?.Code,
            assignment.WorkOrderId,
            WorkOrderNumber = assignment.WorkOrder?.WorkOrderNumber,
            assignment.PlannedPairs,
            MoldName = assignment.Mold?.Name,
            MoldCode = assignment.Mold?.Code,
            assignment.StartedAt,
            assignment.LastTurnAt,
            assignment.LastReleaseAt
        };
    }

    private static object ToAssignmentDetailResponse(StationAssignment assignment, int firePairs, StationAssignmentDowntime? openDowntime)
    {
        var product = assignment.OrderItem.Product ?? assignment.OrderItem.Order.Product;
        return new
        {
            assignment.Id,
            assignment.StationNumberSnapshot,
            assignment.Status,
            assignment.OperatorName,
            assignment.ProducedPairs,
            FirePairs = firePairs,
            GoodPairs = Math.Max(assignment.ProducedPairs - firePairs, 0),
            QuantityPairs = assignment.OrderItem.QuantityPairs,
            assignment.WorkOrderId,
            WorkOrderNumber = assignment.WorkOrder?.WorkOrderNumber,
            AssignmentPlannedPairs = assignment.PlannedPairs,
            OrderItemProducedPairs = assignment.OrderItem.ProducedPairs,
            RemainingPairs = assignment.OrderItem.QuantityPairs - assignment.OrderItem.ProducedPairs,
            ProductionType = assignment.OrderItem.ProductionType,
            FabricColor = assignment.OrderItem.FabricColor,
            CustomerName = assignment.OrderItem.Order.Customer.CompanyName ?? assignment.OrderItem.Order.Customer.Name,
            ProductName = product?.Name,
            ProductCode = product?.Code,
            MoldName = assignment.Mold?.Name,
            MoldCode = assignment.Mold?.Code,
            assignment.StartedAt,
            assignment.TotalTurns,
            assignment.TurnsSinceLastRelease,
            assignment.ReleaseFrequencyTurns,
            ReleaseDue = IsReleaseDue(assignment),
            assignment.LastReleaseAt,
            assignment.LastTurnAt,
            OpenDowntime = openDowntime is null ? null : ToDowntimeResponse(openDowntime),
            assignment.Note
        };
    }

    private static LiveStationResponse ToEmptyStationResponse(int stationNumber)
    {
        return new LiveStationResponse(
            StationNumber: stationNumber,
            AssignmentId: null,
            Status: "Boş",
            CustomerName: null,
            ProductName: null,
            ProductCode: null,
            MoldName: null,
            MoldCode: null,
            OperatorName: null,
            ProducedPairs: 0,
            GoodPairs: 0,
            FirePairs: 0,
            TotalTurns: 0,
            OpenDowntime: false,
            DowntimeType: null,
            DowntimeStartedAt: null,
            TurnsSinceLastRelease: 0,
            ReleaseFrequencyTurns: null,
            ReleaseDue: false,
            LastReleaseAt: null,
            LastTurnAt: null,
            OrderPlannedPairs: 0,
            OrderProducedPairs: 0,
            OrderRemainingPairs: 0,
            StartedAt: null,
            PausedAt: null,
            FinishedAt: null,
            WorkOrderNumber: null);
    }

    private static LiveStationResponse ToLiveStationResponse(StationAssignment assignment, int firePairs, StationAssignmentDowntime? downtime)
    {
        var product = assignment.OrderItem.Product ?? assignment.OrderItem.Order.Product;
        var plannedPairs = assignment.OrderItem.QuantityPairs;
        var orderProducedPairs = assignment.OrderItem.ProducedPairs;

        return new LiveStationResponse(
            StationNumber: assignment.StationNumberSnapshot,
            AssignmentId: assignment.Id,
            Status: assignment.Status,
            CustomerName: assignment.OrderItem.Order.Customer.CompanyName ?? assignment.OrderItem.Order.Customer.Name,
            ProductName: product?.Name,
            ProductCode: product?.Code,
            MoldName: assignment.Mold?.Name,
            MoldCode: assignment.Mold?.Code,
            OperatorName: assignment.OperatorName,
            ProducedPairs: assignment.ProducedPairs,
            GoodPairs: Math.Max(assignment.ProducedPairs - firePairs, 0),
            FirePairs: firePairs,
            TotalTurns: assignment.TotalTurns,
            OpenDowntime: downtime is not null,
            DowntimeType: downtime?.DowntimeType,
            DowntimeStartedAt: downtime?.StartedAt,
            TurnsSinceLastRelease: assignment.TurnsSinceLastRelease,
            ReleaseFrequencyTurns: assignment.ReleaseFrequencyTurns,
            ReleaseDue: IsReleaseDue(assignment),
            LastReleaseAt: assignment.LastReleaseAt,
            LastTurnAt: assignment.LastTurnAt,
            OrderPlannedPairs: plannedPairs,
            OrderProducedPairs: orderProducedPairs,
            OrderRemainingPairs: Math.Max(0, plannedPairs - orderProducedPairs),
            StartedAt: assignment.StartedAt,
            PausedAt: assignment.Status == "Duraklatıldı" ? assignment.LastModified : null,
            FinishedAt: assignment.FinishedAt,
            WorkOrderNumber: assignment.WorkOrder?.WorkOrderNumber);
    }

    private static object ToFireResponse(StationAssignmentFire fire)
    {
        return new
        {
            fire.Id,
            fire.StationAssignmentId,
            fire.StationNumberSnapshot,
            fire.FirePairs,
            fire.ReasonType,
            fire.Reason,
            fire.Note,
            fire.RecordedAt,
            fire.RecordedBy,
            fire.IsCancelled,
            fire.CancelledAt,
            fire.CancelledBy,
            fire.CancellationReason
        };
    }

    private static object ToDowntimeResponse(StationAssignmentDowntime downtime)
    {
        return new
        {
            downtime.Id,
            downtime.StationAssignmentId,
            downtime.StationNumberSnapshot,
            downtime.DowntimeType,
            downtime.Reason,
            downtime.Note,
            downtime.PreviousAssignmentStatus,
            downtime.StartedAt,
            downtime.EndedAt,
            downtime.DurationMinutes,
            downtime.IsOpen,
            downtime.StartedBy,
            downtime.EndedBy,
            downtime.IsCancelled
        };
    }

    private static object ToEventResponse(StationAssignmentEvent stationEvent)
    {
        return new
        {
            stationEvent.Id,
            stationEvent.EventType,
            stationEvent.EventTime,
            stationEvent.Quantity,
            stationEvent.Reason,
            stationEvent.Note,
            stationEvent.RecordedBy,
            StationNumber = stationEvent.StationNumberSnapshot,
            OperatorName = stationEvent.OperatorNameSnapshot
        };
    }

    private static bool IsReleaseDue(StationAssignment assignment)
    {
        return assignment.ReleaseFrequencyTurns.HasValue &&
            assignment.ReleaseFrequencyTurns.Value > 0 &&
            assignment.TurnsSinceLastRelease >= assignment.ReleaseFrequencyTurns.Value;
    }

    private static bool IsFaultType(string downtimeType)
    {
        return downtimeType.Contains("Arıza", StringComparison.OrdinalIgnoreCase);
    }

    private static TimeZoneInfo ResolveTurkeyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
    }

    private string GetActor()
    {
        return User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name)
            ? User.Identity.Name
            : "system";
    }
}

public record AssignStationRequest(
    int StationNumber,
    Guid OrderItemId,
    Guid? MoldId,
    Guid? WorkOrderId,
    int? PlannedPairs,
    string? OperatorName,
    string? Note
);

public record AddProductionRequest(
    Guid StationAssignmentId,
    int ProducedPairs
);

public record AddTurnRequest(
    int TurnCount,
    string? Note,
    string? RequestId
);

public record AssignmentActionRequest(
    Guid StationAssignmentId,
    string? Note
);

public record CreateFireRequest(
    int FirePairs,
    string ReasonType,
    string? Reason,
    string? Note
);

public record CancelFireRequest(string? CancellationReason);

public record StartDowntimeRequest(
    string DowntimeType,
    string? Reason,
    string? Note
);

public record FinishDowntimeRequest(string? Note);

public record ReleaseAppliedRequest(string? Note);

public record LiveStationResponse(
    int StationNumber,
    Guid? AssignmentId,
    string Status,
    string? CustomerName,
    string? ProductName,
    string? ProductCode,
    string? MoldName,
    string? MoldCode,
    string? OperatorName,
    int ProducedPairs,
    int GoodPairs,
    int FirePairs,
    int TotalTurns,
    bool OpenDowntime,
    string? DowntimeType,
    DateTime? DowntimeStartedAt,
    int TurnsSinceLastRelease,
    int? ReleaseFrequencyTurns,
    bool ReleaseDue,
    DateTime? LastReleaseAt,
    DateTime? LastTurnAt,
    int OrderPlannedPairs,
    int OrderProducedPairs,
    int OrderRemainingPairs,
    DateTime? StartedAt,
    DateTime? PausedAt,
    DateTime? FinishedAt,
    string? WorkOrderNumber
);
