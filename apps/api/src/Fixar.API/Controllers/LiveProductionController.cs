using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

// Legacy/experimental live production flow.
// This controller must not be used as the production source of truth.
// The active production source is StationAssignment + InjectionStation + OrderItem.
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/live-production")]
public class LiveProductionController : ControllerBase
{
    private static readonly string[] SessionStatuses = { "Planned", "Ready", "Running", "Paused", "Completed", "Cancelled" };
    private static readonly string[] StationStatuses = { "Empty", "Ready", "Running", "Curing", "ReleaseDue", "MoldChange", "Cleaning", "Fault", "Paused", "Completed" };
    private static readonly string[] DowntimeReasonTypes =
    {
        "MachineFault",
        "MoldFault",
        "MaterialShortage",
        "OperatorBreak",
        "Cleaning",
        "MoldChange",
        "QualityHold",
        "PowerFailure",
        "PlannedStop",
        "Other"
    };

    private readonly ApplicationDbContext _db;

    public LiveProductionController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var sessions = await _db.ProductionSessions
            .Include(x => x.Stations)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(sessions.Select(ToSessionListResponse)));
    }

    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var session = await LoadSessionDetail(id, cancellationToken);

        if (session is null)
            return NotFound(ApiResponse<object>.Fail("Üretim oturumu bulunamadı.", "SESSION_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(ToSessionDetailResponse(session)));
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateProductionSessionRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var validation = await ValidateCreateSession(request, cancellationToken);

        if (validation is not null)
            return validation;

        var product = await _db.Products.FirstAsync(x => x.Id == request.ProductId, cancellationToken);
        var machine = await _db.Machines.FirstAsync(x => x.Id == request.MachineId, cancellationToken);
        var operatorEntity = await _db.Operators.FirstAsync(x => x.Id == request.OperatorId, cancellationToken);
        var utcNow = DateTime.UtcNow;

        var session = new ProductionSession
        {
            SessionNumber = await GenerateSessionNumber(utcNow, cancellationToken),
            WorkOrderId = request.WorkOrderId,
            WorkOrderNumber = request.WorkOrderNumber,
            ProductId = product.Id,
            ProductCode = product.Code,
            ProductName = product.Name,
            CustomerName = product.CustomerName,
            FoamType = product.FoamType,
            MachineId = machine.Id,
            MachineCode = machine.Code,
            MachineName = machine.Name,
            OperatorId = operatorEntity.Id,
            OperatorCode = operatorEntity.Code,
            OperatorName = operatorEntity.FullName,
            Shift = request.Shift,
            Status = "Planned",
            PlannedPairs = request.PlannedPairs,
            TargetPairWeight = product.AverageWeight,
            TargetDensity = product.TargetDensity,
            TargetCycleTimeSeconds = product.StandardCycleTime.HasValue ? Convert.ToInt32(product.StandardCycleTime.Value) : machine.DefaultCycleTimeSeconds,
            ProductionNote = request.ProductionNote,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        _db.ProductionSessions.Add(session);

        var stationCount = Math.Clamp(machine.StationCount ?? 24, 1, 24);
        for (var stationNumber = 1; stationNumber <= stationCount; stationNumber++)
        {
            _db.ProductionStations.Add(new ProductionStation
            {
                ProductionSessionId = session.Id,
                StationNumber = stationNumber,
                Status = "Empty",
                IsActive = true,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var created = await LoadSessionDetail(session.Id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToSessionDetailResponse(created!), "Canlı üretim oturumu oluşturuldu."));
    }

    [HttpPut("sessions/{id:guid}")]
    public async Task<IActionResult> UpdateSession(Guid id, [FromBody] UpdateProductionSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await _db.ProductionSessions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (session is null)
            return NotFound(ApiResponse<object>.Fail("Üretim oturumu bulunamadı.", "SESSION_NOT_FOUND"));

        if (IsClosed(session))
            return BadRequest(ApiResponse<object>.Fail("Tamamlanan veya iptal edilen oturum güncellenemez.", "SESSION_CLOSED"));

        if (request.PlannedPairs.HasValue && request.PlannedPairs.Value <= 0)
            return BadRequest(ApiResponse<object>.Fail("Planlanan çift 0'dan büyük olmalıdır.", "INVALID_PLANNED_PAIRS"));

        if (request.Shift.HasValue && !IsValidShift(request.Shift.Value))
            return BadRequest(ApiResponse<object>.Fail("Vardiya 1, 2 veya 3 olmalıdır.", "INVALID_SHIFT"));

        if (!string.IsNullOrWhiteSpace(request.Status) && !SessionStatuses.Contains(request.Status.Trim()))
            return BadRequest(ApiResponse<object>.Fail("Session status geçersiz.", "INVALID_SESSION_STATUS"));

        session.WorkOrderId = request.WorkOrderId ?? session.WorkOrderId;
        session.WorkOrderNumber = request.WorkOrderNumber ?? session.WorkOrderNumber;
        session.Shift = request.Shift ?? session.Shift;
        session.PlannedPairs = request.PlannedPairs ?? session.PlannedPairs;
        session.ProductionNote = request.ProductionNote ?? session.ProductionNote;
        session.QualityNote = request.QualityNote ?? session.QualityNote;
        session.EstimatedMaterialCost = request.EstimatedMaterialCost ?? session.EstimatedMaterialCost;
        session.ActualMaterialCost = request.ActualMaterialCost ?? session.ActualMaterialCost;
        session.Status = string.IsNullOrWhiteSpace(request.Status) ? session.Status : request.Status.Trim();
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var updated = await LoadSessionDetail(id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToSessionDetailResponse(updated!), "Üretim oturumu güncellendi."));
    }

    [HttpPost("sessions/{id:guid}/start")]
    public async Task<IActionResult> StartSession(Guid id, CancellationToken cancellationToken)
    {
        var session = await _db.ProductionSessions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (session is null)
            return NotFound(ApiResponse<object>.Fail("Üretim oturumu bulunamadı.", "SESSION_NOT_FOUND"));

        if (!session.IsActive || session.Status is not ("Planned" or "Ready"))
            return BadRequest(ApiResponse<object>.Fail("Sadece Planned veya Ready oturum başlatılabilir.", "INVALID_SESSION_STATUS"));

        var runningExists = await _db.ProductionSessions
            .AnyAsync(x => x.Id != id && x.IsActive && x.MachineId == session.MachineId && x.Status == "Running", cancellationToken);

        if (runningExists)
            return BadRequest(ApiResponse<object>.Fail("Bu makinede çalışan başka bir canlı üretim oturumu var.", "MACHINE_HAS_RUNNING_SESSION"));

        var machine = await _db.Machines.FirstAsync(x => x.Id == session.MachineId, cancellationToken);
        var operatorEntity = await _db.Operators.FirstAsync(x => x.Id == session.OperatorId, cancellationToken);
        var utcNow = DateTime.UtcNow;

        session.Status = "Running";
        session.StartTime ??= utcNow;
        session.UpdatedAt = utcNow;
        machine.CurrentStatus = "Running";
        machine.CurrentWorkOrderId = session.WorkOrderId;
        machine.CurrentOperatorName = session.OperatorName;
        machine.UpdatedAt = utcNow;
        operatorEntity.CurrentStatus = "Working";
        operatorEntity.CurrentMachineId = machine.Id;
        operatorEntity.CurrentMachineCode = machine.Code;
        operatorEntity.CurrentMachineName = machine.Name;
        operatorEntity.CurrentWorkOrderId = session.WorkOrderId;
        operatorEntity.CurrentWorkOrderNumber = session.WorkOrderNumber;
        operatorEntity.UpdatedAt = utcNow;

        AddEvent(session.Id, null, "SessionStarted", utcNow, operatorEntity, null, null);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToSessionDetailResponse((await LoadSessionDetail(id, cancellationToken))!), "Üretim oturumu başlatıldı."));
    }

    [HttpPost("sessions/{id:guid}/pause")]
    public async Task<IActionResult> PauseSession(Guid id, [FromBody] SessionReasonRequest? request, CancellationToken cancellationToken)
    {
        var session = await _db.ProductionSessions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (session is null)
            return NotFound(ApiResponse<object>.Fail("Üretim oturumu bulunamadı.", "SESSION_NOT_FOUND"));

        if (session.Status != "Running")
            return BadRequest(ApiResponse<object>.Fail("Sadece Running oturum duraklatılabilir.", "INVALID_SESSION_STATUS"));

        var utcNow = DateTime.UtcNow;
        session.Status = "Paused";
        session.UpdatedAt = utcNow;

        _db.ProductionDowntimes.Add(new ProductionDowntime
        {
            ProductionSessionId = session.Id,
            ReasonType = "PlannedStop",
            Reason = request?.Reason,
            StartTime = utcNow,
            IsOpen = true,
            OperatorId = session.OperatorId,
            OperatorName = session.OperatorName,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });

        AddEvent(session.Id, null, "SessionPaused", utcNow, null, request?.Reason, request?.Note);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToSessionDetailResponse((await LoadSessionDetail(id, cancellationToken))!), "Üretim oturumu duraklatıldı."));
    }

    [HttpPost("sessions/{id:guid}/resume")]
    public async Task<IActionResult> ResumeSession(Guid id, [FromBody] SessionReasonRequest? request, CancellationToken cancellationToken)
    {
        var session = await _db.ProductionSessions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (session is null)
            return NotFound(ApiResponse<object>.Fail("Üretim oturumu bulunamadı.", "SESSION_NOT_FOUND"));

        if (session.Status != "Paused")
            return BadRequest(ApiResponse<object>.Fail("Sadece Paused oturum devam ettirilebilir.", "INVALID_SESSION_STATUS"));

        var utcNow = DateTime.UtcNow;
        await CloseOpenSessionDowntimes(session.Id, utcNow, cancellationToken);
        session.Status = "Running";
        session.UpdatedAt = utcNow;

        AddEvent(session.Id, null, "SessionResumed", utcNow, null, request?.Reason, request?.Note);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToSessionDetailResponse((await LoadSessionDetail(id, cancellationToken))!), "Üretim oturumu devam ettirildi."));
    }

    [HttpPost("sessions/{id:guid}/complete")]
    public async Task<IActionResult> CompleteSession(Guid id, [FromBody] SessionReasonRequest? request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var session = await _db.ProductionSessions
            .Include(x => x.Stations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (session is null)
            return NotFound(ApiResponse<object>.Fail("Üretim oturumu bulunamadı.", "SESSION_NOT_FOUND"));

        if (session.Status is not ("Running" or "Paused"))
            return BadRequest(ApiResponse<object>.Fail("Sadece Running veya Paused oturum tamamlanabilir.", "INVALID_SESSION_STATUS"));

        var blockingStation = session.Stations
            .OrderBy(x => x.StationNumber)
            .FirstOrDefault(x => x.IsActive && x.Status is "Curing" or "Running" or "Fault" or "MoldChange" or "Cleaning" or "ReleaseDue");

        if (blockingStation is not null)
            return BadRequest(ApiResponse<object>.Fail(
                $"Session tamamlanamaz. {blockingStation.StationNumber}. istasyon {blockingStation.Status} durumunda.",
                "SESSION_HAS_ACTIVE_STATION"));

        var unfinishedCycleStation = session.Stations
            .OrderBy(x => x.StationNumber)
            .FirstOrDefault(HasUnfinishedCycle);

        if (unfinishedCycleStation is not null)
            return BadRequest(ApiResponse<object>.Fail(
                $"Session tamamlanamaz. {unfinishedCycleStation.StationNumber}. istasyonda başlatılmış ancak tamamlanmamış cycle var.",
                "SESSION_HAS_UNFINISHED_CYCLE"));

        var openDowntimes = await _db.ProductionDowntimes
            .Where(x => x.ProductionSessionId == id && x.IsOpen)
            .ToListAsync(cancellationToken);

        if (openDowntimes.Count > 0)
            return BadRequest(ApiResponse<object>.Fail("Açık duruş kayıtları kapatılmadan oturum tamamlanamaz.", "OPEN_DOWNTIME_EXISTS"));

        var machine = await _db.Machines.FirstAsync(x => x.Id == session.MachineId, cancellationToken);
        var operatorEntity = await _db.Operators.FirstAsync(x => x.Id == session.OperatorId, cancellationToken);
        var utcNow = DateTime.UtcNow;

        RecalculateSessionCounters(session);
        session.TotalDowntimeMinutes = await CalculateSessionDowntimeMinutes(session.Id, cancellationToken);

        if (session.ProducedPairs <= 0 || session.TotalCycleCount <= 0)
            return BadRequest(ApiResponse<object>.Fail(
                "Üretim kaydı bulunmayan session tamamlanamaz. Üretimsiz kapatma için İptal işlemini kullanın.",
                "SESSION_HAS_NO_PRODUCTION"));

        session.Status = "Completed";
        session.EndTime = utcNow;
        session.UpdatedAt = utcNow;
        foreach (var station in session.Stations.Where(IsUsedStation))
        {
            station.Status = "Completed";
            station.UpdatedAt = utcNow;
        }

        machine.CurrentStatus = "Idle";
        machine.CurrentWorkOrderId = null;
        machine.CurrentOperatorName = null;
        machine.UpdatedAt = utcNow;
        operatorEntity.CurrentStatus = "Available";
        operatorEntity.CurrentWorkOrderId = null;
        operatorEntity.CurrentWorkOrderNumber = null;
        operatorEntity.CurrentStationNumber = null;
        operatorEntity.UpdatedAt = utcNow;

        AddEvent(session.Id, null, "SessionCompleted", utcNow, operatorEntity, request?.Reason, request?.Note);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToSessionDetailResponse((await LoadSessionDetail(id, cancellationToken))!), "Üretim oturumu tamamlandı."));
    }

    [HttpPost("sessions/{id:guid}/cancel")]
    public async Task<IActionResult> CancelSession(Guid id, [FromBody] SessionReasonRequest? request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var session = await _db.ProductionSessions
            .Include(x => x.Stations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (session is null)
            return NotFound(ApiResponse<object>.Fail("Üretim oturumu bulunamadı.", "SESSION_NOT_FOUND"));

        if (IsClosed(session))
            return BadRequest(ApiResponse<object>.Fail("Tamamlanan veya iptal edilen oturum tekrar iptal edilemez.", "SESSION_CLOSED"));

        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == session.MachineId, cancellationToken);
        var operatorEntity = await _db.Operators.FirstOrDefaultAsync(x => x.Id == session.OperatorId, cancellationToken);
        var utcNow = DateTime.UtcNow;

        await CloseOpenSessionDowntimes(session.Id, utcNow, cancellationToken);
        session.Status = "Cancelled";
        session.IsActive = false;
        session.EndTime = utcNow;
        session.UpdatedAt = utcNow;
        foreach (var station in session.Stations)
        {
            if (IsUsedStation(station))
            {
                station.Status = "Empty";
                station.IsActive = false;
            }
            else
            {
                station.Status = "Empty";
            }

            station.CuringStartedAt = null;
            station.CuringExpectedEndAt = null;
            station.UpdatedAt = utcNow;
        }

        var moldIds = session.Stations
            .Where(x => x.MoldId.HasValue)
            .Select(x => x.MoldId!.Value)
            .Distinct()
            .ToList();

        if (moldIds.Count > 0)
        {
            var molds = await _db.Molds
                .Where(x => moldIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

            foreach (var mold in molds)
            {
                mold.CurrentStationNumber = null;
                mold.UpdatedAt = utcNow;
            }
        }

        if (machine is not null)
        {
            machine.CurrentStatus = "Idle";
            machine.CurrentWorkOrderId = null;
            machine.CurrentOperatorName = null;
            machine.UpdatedAt = utcNow;
        }

        if (operatorEntity is not null)
        {
            operatorEntity.CurrentStatus = "Available";
            operatorEntity.CurrentWorkOrderId = null;
            operatorEntity.CurrentWorkOrderNumber = null;
            operatorEntity.CurrentStationNumber = null;
            operatorEntity.UpdatedAt = utcNow;
        }

        var reason = string.IsNullOrWhiteSpace(request?.Reason) ? "Kullanıcı iptali" : request.Reason;
        AddEvent(session.Id, null, "SessionCancelled", utcNow, operatorEntity, reason, request?.Note);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToSessionDetailResponse((await LoadSessionDetail(id, cancellationToken))!), "Üretim oturumu iptal edildi."));
    }

    [HttpGet("sessions/{sessionId:guid}/stations")]
    public async Task<IActionResult> GetStations(Guid sessionId, CancellationToken cancellationToken)
    {
        var sessionExists = await _db.ProductionSessions.AnyAsync(x => x.Id == sessionId, cancellationToken);
        if (!sessionExists)
            return NotFound(ApiResponse<object>.Fail("Üretim oturumu bulunamadı.", "SESSION_NOT_FOUND"));

        var stations = await _db.ProductionStations
            .Where(x => x.ProductionSessionId == sessionId)
            .OrderBy(x => x.StationNumber)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(stations.Select(ToStationResponse)));
    }

    [HttpGet("stations/{id:guid}")]
    public async Task<IActionResult> GetStation(Guid id, CancellationToken cancellationToken)
    {
        var station = await _db.ProductionStations
            .Include(x => x.ProductionSession)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station)));
    }

    [HttpPut("stations/{id:guid}")]
    public async Task<IActionResult> UpdateStation(Guid id, [FromBody] UpdateProductionStationRequest request, CancellationToken cancellationToken)
    {
        var station = await _db.ProductionStations
            .Include(x => x.ProductionSession)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        if (station.ProductionSession is null || IsClosed(station.ProductionSession))
            return BadRequest(ApiResponse<object>.Fail("Kapalı oturum istasyonu güncellenemez.", "SESSION_CLOSED"));

        if (!string.IsNullOrWhiteSpace(request.Status) && !StationStatuses.Contains(request.Status.Trim()))
            return BadRequest(ApiResponse<object>.Fail("Station status geçersiz.", "INVALID_STATION_STATUS"));

        station.Status = string.IsNullOrWhiteSpace(request.Status) ? station.Status : request.Status.Trim();
        station.LastNote = request.LastNote ?? station.LastNote;
        station.TargetPairWeight = request.TargetPairWeight ?? station.TargetPairWeight;
        station.TargetDensity = request.TargetDensity ?? station.TargetDensity;
        station.TargetCuringTimeSeconds = request.TargetCuringTimeSeconds ?? station.TargetCuringTimeSeconds;
        station.TargetCycleTimeSeconds = request.TargetCycleTimeSeconds ?? station.TargetCycleTimeSeconds;
        station.ReleaseFrequencyCycles = request.ReleaseFrequencyCycles ?? station.ReleaseFrequencyCycles;
        station.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Üretim istasyonu güncellendi."));
    }

    [HttpPost("stations/{id:guid}/assign-mold")]
    public async Task<IActionResult> AssignMold(Guid id, [FromBody] AssignStationMoldRequest request, CancellationToken cancellationToken)
    {
        if (!request.MoldId.HasValue || request.MoldId.Value == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail("Kalıp seçimi zorunludur.", "MOLD_REQUIRED"));

        var station = await _db.ProductionStations
            .Include(x => x.ProductionSession)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        if (!IsValidStation(station.StationNumber))
            return BadRequest(ApiResponse<object>.Fail("İstasyon numarası 1 ile 24 arasında olmalıdır.", "INVALID_STATION_NUMBER"));

        var sessionValidation = ValidateStationSession(station, requireRunning: false);
        if (sessionValidation is not null)
            return sessionValidation;

        var mold = await _db.Molds.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == request.MoldId.Value, cancellationToken);
        if (mold is null)
            return NotFound(ApiResponse<object>.Fail("Kalıp bulunamadı.", "MOLD_NOT_FOUND"));

        if (!mold.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif kalıp canlı üretime atanamaz.", "MOLD_INACTIVE"));

        var duplicateMold = await _db.ProductionStations
            .AnyAsync(x => x.Id != id && x.ProductionSessionId == station.ProductionSessionId && x.IsActive && x.MoldId == mold.Id, cancellationToken);

        if (duplicateMold)
            return BadRequest(ApiResponse<object>.Fail("Bu kalıp aynı oturumda başka bir istasyona atanmış.", "MOLD_ALREADY_ASSIGNED"));

        var product = request.ProductId.HasValue
            ? await _db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId.Value, cancellationToken)
            : mold.Product;

        if (request.ProductId.HasValue && product is null)
            return NotFound(ApiResponse<object>.Fail("Ürün bulunamadı.", "PRODUCT_NOT_FOUND"));

        if (product is not null && !product.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif ürün canlı üretime atanamaz.", "PRODUCT_INACTIVE"));

        Operator? operatorEntity = null;
        if (request.OperatorId.HasValue)
        {
            operatorEntity = await _db.Operators.FirstOrDefaultAsync(x => x.Id == request.OperatorId.Value, cancellationToken);
            if (operatorEntity is null)
                return NotFound(ApiResponse<object>.Fail("Operatör bulunamadı.", "OPERATOR_NOT_FOUND"));
            if (!operatorEntity.IsActive)
                return BadRequest(ApiResponse<object>.Fail("Pasif operatör canlı üretime atanamaz.", "OPERATOR_INACTIVE"));
        }

        var utcNow = DateTime.UtcNow;
        station.MoldId = mold.Id;
        station.MoldCode = mold.Code;
        station.MoldName = mold.Name;
        station.ProductId = product?.Id ?? station.ProductionSession!.ProductId;
        station.ProductCode = product?.Code ?? station.ProductionSession!.ProductCode;
        station.ProductName = product?.Name ?? station.ProductionSession!.ProductName;
        station.OperatorId = operatorEntity?.Id ?? station.OperatorId;
        station.OperatorCode = operatorEntity?.Code ?? station.OperatorCode;
        station.OperatorName = operatorEntity?.FullName ?? station.OperatorName;
        station.TargetPairWeight = mold.TargetPairWeight ?? product?.AverageWeight;
        station.TargetDensity = mold.TargetDensity ?? product?.TargetDensity;
        station.TargetCuringTimeSeconds = mold.StandardCuringTimeSeconds;
        station.TargetCycleTimeSeconds = mold.StandardCycleTimeSeconds ?? (product?.StandardCycleTime.HasValue == true ? Convert.ToInt32(product.StandardCycleTime.Value) : null);
        station.ReleaseFrequencyCycles = mold.ReleaseFrequencyCycles;
        station.Status = "Ready";
        station.UpdatedAt = utcNow;
        mold.CurrentStationNumber = station.StationNumber;
        mold.UpdatedAt = utcNow;

        AddEvent(station.ProductionSessionId, station.Id, "MoldChanged", utcNow, operatorEntity, null, $"Kalıp atandı: {mold.Code}");
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Kalıp istasyona atandı."));
    }

    [HttpPost("stations/{id:guid}/assign-operator")]
    public async Task<IActionResult> AssignOperator(Guid id, [FromBody] AssignStationOperatorRequest request, CancellationToken cancellationToken)
    {
        if (!request.OperatorId.HasValue || request.OperatorId.Value == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail("Operatör seçimi zorunludur.", "OPERATOR_REQUIRED"));

        var station = await _db.ProductionStations
            .Include(x => x.ProductionSession)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        var sessionValidation = ValidateStationSession(station, requireRunning: false);
        if (sessionValidation is not null)
            return sessionValidation;

        var operatorEntity = await _db.Operators.FirstOrDefaultAsync(x => x.Id == request.OperatorId.Value, cancellationToken);
        if (operatorEntity is null)
            return NotFound(ApiResponse<object>.Fail("Operatör bulunamadı.", "OPERATOR_NOT_FOUND"));
        if (!operatorEntity.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif operatör canlı üretime atanamaz.", "OPERATOR_INACTIVE"));

        var utcNow = DateTime.UtcNow;
        station.OperatorId = operatorEntity.Id;
        station.OperatorCode = operatorEntity.Code;
        station.OperatorName = operatorEntity.FullName;
        station.UpdatedAt = utcNow;
        operatorEntity.CurrentStationNumber = station.StationNumber;
        operatorEntity.UpdatedAt = utcNow;

        AddEvent(station.ProductionSessionId, station.Id, "OperatorChanged", utcNow, operatorEntity, null, null);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Operatör istasyona atandı."));
    }

    [HttpPost("stations/{id:guid}/start-cycle")]
    public async Task<IActionResult> StartCycle(Guid id, CancellationToken cancellationToken)
    {
        var station = await _db.ProductionStations
            .Include(x => x.ProductionSession)
            .Include(x => x.Mold)
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        var validation = ValidateStationCanCycle(station);
        if (validation is not null)
            return validation;

        var machineId = station.ProductionSession?.MachineId;
        var machine = machineId.HasValue
            ? await _db.Machines.FirstOrDefaultAsync(x => x.Id == machineId.Value, cancellationToken)
            : null;
        var curingTimeSeconds = ResolveCuringTimeSeconds(station, machine);

        if (!curingTimeSeconds.HasValue)
            return BadRequest(ApiResponse<object>.Fail(
                "Pişme süresi tanımlı değil. Kalıp, ürün veya makine kartında pişme süresi tanımlayın.",
                "CURING_TIME_REQUIRED"));

        var utcNow = DateTime.UtcNow;
        station.CurrentCycleNumber += 1;
        station.LastCycleStartedAt = utcNow;
        station.CuringStartedAt = utcNow;
        station.TargetCuringTimeSeconds = curingTimeSeconds.Value;
        station.CuringExpectedEndAt = utcNow.AddSeconds(curingTimeSeconds.Value);
        station.Status = "Curing";
        station.UpdatedAt = utcNow;

        AddEvent(station.ProductionSessionId, station.Id, "CycleStarted", utcNow, null, null, null, station.CurrentCycleNumber);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Çevrim başlatıldı."));
    }

    [HttpPost("stations/{id:guid}/complete-cycle")]
    public async Task<IActionResult> CompleteCycle(Guid id, [FromBody] CompleteCycleRequest request, CancellationToken cancellationToken)
    {
        if (request.ProducedPairs < 0 || request.GoodPairs < 0 || request.FirePairs < 0)
            return BadRequest(ApiResponse<object>.Fail("Üretim ve fire sayaçları negatif olamaz.", "INVALID_COUNTER"));

        if (request.GoodPairs + request.FirePairs != request.ProducedPairs)
            return BadRequest(ApiResponse<object>.Fail("GoodPairs + FirePairs, ProducedPairs değerine eşit olmalıdır.", "INVALID_COUNTER_TOTAL"));

        if (request.ActualCuringTimeSeconds < 0 || request.ActualCycleTimeSeconds < 0)
            return BadRequest(ApiResponse<object>.Fail("Süre değerleri negatif olamaz.", "INVALID_TIME"));

        if (request.Weight.HasValue && request.Weight.Value <= 0)
            return BadRequest(ApiResponse<object>.Fail("Ağırlık 0'dan büyük olmalıdır.", "INVALID_WEIGHT"));

        if (request.Density.HasValue && request.Density.Value <= 0)
            return BadRequest(ApiResponse<object>.Fail("Yoğunluk 0'dan büyük olmalıdır.", "INVALID_DENSITY"));

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var station = await _db.ProductionStations
            .Include(x => x.ProductionSession)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        if (station.ProductionSession is null || station.ProductionSession.Status != "Running")
            return BadRequest(ApiResponse<object>.Fail("Çevrim tamamlamak için oturum Running olmalıdır.", "SESSION_NOT_RUNNING"));

        if (station.Status != "Curing" || !station.LastCycleStartedAt.HasValue)
            return BadRequest(ApiResponse<object>.Fail("Başlatılmamış veya tamamlanmış çevrim tekrar tamamlanamaz.", "CYCLE_NOT_ACTIVE"));

        var mold = station.MoldId.HasValue ? await _db.Molds.FirstOrDefaultAsync(x => x.Id == station.MoldId.Value, cancellationToken) : null;
        var operatorEntity = station.OperatorId.HasValue ? await _db.Operators.FirstOrDefaultAsync(x => x.Id == station.OperatorId.Value, cancellationToken) : null;
        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == station.ProductionSession.MachineId, cancellationToken);
        var utcNow = DateTime.UtcNow;

        station.ProducedPairs += request.ProducedPairs;
        station.GoodPairs += request.GoodPairs;
        station.FirePairs += request.FirePairs;
        station.LastCycleCompletedAt = utcNow;
        station.CuringStartedAt = null;
        station.CuringExpectedEndAt = null;
        station.ActualLastCuringTimeSeconds = request.ActualCuringTimeSeconds;
        station.ActualLastCycleTimeSeconds = request.ActualCycleTimeSeconds;
        station.LastPairWeight = request.Weight ?? station.LastPairWeight;
        station.LastDensity = request.Density ?? station.LastDensity;
        station.CyclesSinceLastRelease += 1;
        station.Status = station.ReleaseFrequencyCycles.HasValue && station.ReleaseFrequencyCycles.Value > 0 && station.CyclesSinceLastRelease >= station.ReleaseFrequencyCycles.Value
            ? "ReleaseDue"
            : "Ready";
        station.LastNote = request.Note ?? station.LastNote;
        station.UpdatedAt = utcNow;

        station.ProductionSession.ProducedPairs += request.ProducedPairs;
        station.ProductionSession.GoodPairs += request.GoodPairs;
        station.ProductionSession.FirePairs += request.FirePairs;
        station.ProductionSession.TotalCycleCount += 1;
        station.ProductionSession.ActualAveragePairWeight = CalculateAverage(station.ProductionSession.ActualAveragePairWeight, request.Weight, station.ProductionSession.TotalCycleCount);
        station.ProductionSession.ActualAverageDensity = CalculateAverage(station.ProductionSession.ActualAverageDensity, request.Density, station.ProductionSession.TotalCycleCount);
        station.ProductionSession.ActualAverageCuringTimeSeconds = CalculateAverage(station.ProductionSession.ActualAverageCuringTimeSeconds, request.ActualCuringTimeSeconds, station.ProductionSession.TotalCycleCount);
        station.ProductionSession.ActualAverageCycleTimeSeconds = CalculateAverage(station.ProductionSession.ActualAverageCycleTimeSeconds, request.ActualCycleTimeSeconds, station.ProductionSession.TotalCycleCount);
        station.ProductionSession.UpdatedAt = utcNow;

        if (mold is not null)
        {
            mold.TotalCycleCount += 1;
            mold.TotalProducedPairs += request.ProducedPairs;
            mold.UpdatedAt = utcNow;
        }

        if (operatorEntity is not null)
        {
            operatorEntity.TotalProducedPairs += request.ProducedPairs;
            operatorEntity.TotalFirePairs += request.FirePairs;
            operatorEntity.AverageFirePercent = CalculateFirePercent(operatorEntity.TotalProducedPairs, operatorEntity.TotalFirePairs);
            operatorEntity.UpdatedAt = utcNow;
        }

        if (machine is not null)
        {
            machine.TotalProducedPairs += request.ProducedPairs;
            machine.UpdatedAt = utcNow;
        }

        AddEvent(
            station.ProductionSessionId,
            station.Id,
            "CycleCompleted",
            utcNow,
            operatorEntity,
            null,
            request.Note,
            station.CurrentCycleNumber,
            request.ProducedPairs,
            request.GoodPairs,
            request.FirePairs,
            request.Weight,
            request.Density,
            request.ActualCuringTimeSeconds,
            request.ActualCycleTimeSeconds);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Çevrim tamamlandı."));
    }

    [HttpPost("stations/{id:guid}/record-fire")]
    public async Task<IActionResult> RecordFire(Guid id, [FromBody] RecordFireRequest request, CancellationToken cancellationToken)
    {
        if (request.FirePairs < 0)
            return BadRequest(ApiResponse<object>.Fail("Fire negatif olamaz.", "INVALID_FIRE"));

        var station = await LoadWritableStation(id, cancellationToken);
        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        var validation = ValidateStationSession(station, requireRunning: false);
        if (validation is not null)
            return validation;

        var utcNow = DateTime.UtcNow;
        station.FirePairs += request.FirePairs;
        station.ProductionSession!.FirePairs += request.FirePairs;
        station.UpdatedAt = utcNow;
        station.ProductionSession.UpdatedAt = utcNow;
        AddEvent(station.ProductionSessionId, station.Id, "FireRecorded", utcNow, null, null, request.Note, station.CurrentCycleNumber, null, null, request.FirePairs);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Fire kaydı işlendi."));
    }

    [HttpPost("stations/{id:guid}/record-weight")]
    public async Task<IActionResult> RecordWeight(Guid id, [FromBody] RecordWeightRequest request, CancellationToken cancellationToken)
    {
        if (request.Weight <= 0)
            return BadRequest(ApiResponse<object>.Fail("Ağırlık 0'dan büyük olmalıdır.", "INVALID_WEIGHT"));

        var station = await LoadWritableStation(id, cancellationToken);
        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        var validation = ValidateStationSession(station, requireRunning: false);
        if (validation is not null)
            return validation;

        var utcNow = DateTime.UtcNow;
        station.LastPairWeight = request.Weight;
        station.UpdatedAt = utcNow;
        AddEvent(station.ProductionSessionId, station.Id, "WeightRecorded", utcNow, null, null, request.Note, station.CurrentCycleNumber, weight: request.Weight);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Ağırlık kaydı işlendi."));
    }

    [HttpPost("stations/{id:guid}/record-density")]
    public async Task<IActionResult> RecordDensity(Guid id, [FromBody] RecordDensityRequest request, CancellationToken cancellationToken)
    {
        if (request.Density <= 0)
            return BadRequest(ApiResponse<object>.Fail("Yoğunluk 0'dan büyük olmalıdır.", "INVALID_DENSITY"));

        var station = await LoadWritableStation(id, cancellationToken);
        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        var validation = ValidateStationSession(station, requireRunning: false);
        if (validation is not null)
            return validation;

        var utcNow = DateTime.UtcNow;
        station.LastDensity = request.Density;
        station.UpdatedAt = utcNow;
        AddEvent(station.ProductionSessionId, station.Id, "DensityRecorded", utcNow, null, null, request.Note, station.CurrentCycleNumber, density: request.Density);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Yoğunluk kaydı işlendi."));
    }

    [HttpPost("stations/{id:guid}/apply-release")]
    public async Task<IActionResult> ApplyRelease(Guid id, [FromBody] NoteRequest? request, CancellationToken cancellationToken)
    {
        var station = await LoadWritableStation(id, cancellationToken);
        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        var validation = ValidateStationSession(station, requireRunning: false);
        if (validation is not null)
            return validation;

        var utcNow = DateTime.UtcNow;
        station.LastReleaseAt = utcNow;
        station.CyclesSinceLastRelease = 0;
        station.Status = "Ready";
        station.LastNote = request?.Note ?? station.LastNote;
        station.UpdatedAt = utcNow;
        AddEvent(station.ProductionSessionId, station.Id, "ReleaseApplied", utcNow, null, null, request?.Note, station.CurrentCycleNumber);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Release uygulandı."));
    }

    [HttpPost("stations/{id:guid}/start-fault")]
    public async Task<IActionResult> StartFault(Guid id, [FromBody] StartFaultRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReasonType) || !DowntimeReasonTypes.Contains(request.ReasonType.Trim()))
            return BadRequest(ApiResponse<object>.Fail("ReasonType geçersiz.", "INVALID_REASON_TYPE"));

        var station = await LoadWritableStation(id, cancellationToken);
        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        var validation = ValidateStationSession(station, requireRunning: false);
        if (validation is not null)
            return validation;

        var openExists = await _db.ProductionDowntimes.AnyAsync(x => x.ProductionStationId == id && x.IsOpen, cancellationToken);
        if (openExists)
            return BadRequest(ApiResponse<object>.Fail("Bu istasyonda zaten açık duruş kaydı var.", "OPEN_DOWNTIME_EXISTS"));

        var utcNow = DateTime.UtcNow;
        _db.ProductionDowntimes.Add(new ProductionDowntime
        {
            ProductionSessionId = station.ProductionSessionId,
            ProductionStationId = station.Id,
            ReasonType = request.ReasonType.Trim(),
            Reason = request.Reason,
            StartTime = utcNow,
            IsOpen = true,
            OperatorId = station.OperatorId,
            OperatorName = station.OperatorName,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });
        station.Status = "Fault";
        station.LastFaultReason = request.Reason;
        station.LastNote = request.Note ?? station.LastNote;
        station.UpdatedAt = utcNow;

        AddEvent(station.ProductionSessionId, station.Id, "FaultStarted", utcNow, null, request.Reason, request.Note, station.CurrentCycleNumber);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Arıza/duruş başlatıldı."));
    }

    [HttpPost("stations/{id:guid}/end-fault")]
    public async Task<IActionResult> EndFault(Guid id, [FromBody] NoteRequest? request, CancellationToken cancellationToken)
    {
        var station = await LoadWritableStation(id, cancellationToken);
        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        var downtime = await _db.ProductionDowntimes
            .FirstOrDefaultAsync(x => x.ProductionStationId == id && x.IsOpen, cancellationToken);

        if (downtime is null)
            return BadRequest(ApiResponse<object>.Fail("Açık duruş kaydı bulunamadı.", "OPEN_DOWNTIME_NOT_FOUND"));

        var utcNow = DateTime.UtcNow;
        CloseDowntime(downtime, utcNow);
        station.ProductionSession!.TotalDowntimeMinutes += downtime.DurationMinutes ?? 0;
        station.Status = "Ready";
        station.LastNote = request?.Note ?? station.LastNote;
        station.UpdatedAt = utcNow;
        station.ProductionSession.UpdatedAt = utcNow;

        AddEvent(station.ProductionSessionId, station.Id, "FaultEnded", utcNow, null, downtime.Reason, request?.Note, station.CurrentCycleNumber);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Arıza/duruş kapatıldı."));
    }

    [HttpPost("stations/{id:guid}/start-mold-change")]
    public async Task<IActionResult> StartMoldChange(Guid id, [FromBody] NoteRequest? request, CancellationToken cancellationToken)
    {
        return await StartStationProcess(id, "MoldChange", "MoldChanged", request?.Note, "Kalıp değişimi başlatıldı.", cancellationToken);
    }

    [HttpPost("stations/{id:guid}/complete-mold-change")]
    public async Task<IActionResult> CompleteMoldChange(Guid id, [FromBody] NoteRequest? request, CancellationToken cancellationToken)
    {
        var station = await LoadWritableStation(id, cancellationToken);
        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        var validation = ValidateStationSession(station, requireRunning: false);
        if (validation is not null)
            return validation;

        var utcNow = DateTime.UtcNow;
        station.Status = station.MoldId.HasValue ? "Ready" : "Empty";
        station.LastNote = request?.Note ?? station.LastNote;
        station.UpdatedAt = utcNow;
        AddEvent(station.ProductionSessionId, station.Id, "MoldChanged", utcNow, null, null, request?.Note, station.CurrentCycleNumber);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), "Kalıp değişimi tamamlandı."));
    }

    private async Task<IActionResult> StartStationProcess(Guid id, string status, string eventType, string? note, string message, CancellationToken cancellationToken)
    {
        var station = await LoadWritableStation(id, cancellationToken);
        if (station is null)
            return NotFound(ApiResponse<object>.Fail("Üretim istasyonu bulunamadı.", "STATION_NOT_FOUND"));

        var validation = ValidateStationSession(station, requireRunning: false);
        if (validation is not null)
            return validation;

        var utcNow = DateTime.UtcNow;
        station.Status = status;
        station.LastNote = note ?? station.LastNote;
        station.UpdatedAt = utcNow;
        AddEvent(station.ProductionSessionId, station.Id, eventType, utcNow, null, null, note, station.CurrentCycleNumber);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToStationResponse(station), message));
    }

    private async Task<IActionResult?> ValidateCreateSession(CreateProductionSessionRequest request, CancellationToken cancellationToken)
    {
        if (request.PlannedPairs <= 0)
            return BadRequest(ApiResponse<object>.Fail("Planlanan çift 0'dan büyük olmalıdır.", "INVALID_PLANNED_PAIRS"));

        if (!IsValidShift(request.Shift))
            return BadRequest(ApiResponse<object>.Fail("Vardiya 1, 2 veya 3 olmalıdır.", "INVALID_SHIFT"));

        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == request.MachineId, cancellationToken);
        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Makine bulunamadı.", "MACHINE_NOT_FOUND"));
        if (!machine.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif makine canlı üretimde kullanılamaz.", "MACHINE_INACTIVE"));

        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
            return NotFound(ApiResponse<object>.Fail("Ürün bulunamadı.", "PRODUCT_NOT_FOUND"));
        if (!product.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif ürün canlı üretimde kullanılamaz.", "PRODUCT_INACTIVE"));

        var operatorEntity = await _db.Operators.FirstOrDefaultAsync(x => x.Id == request.OperatorId, cancellationToken);
        if (operatorEntity is null)
            return NotFound(ApiResponse<object>.Fail("Operatör bulunamadı.", "OPERATOR_NOT_FOUND"));
        if (!operatorEntity.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif operatör canlı üretimde kullanılamaz.", "OPERATOR_INACTIVE"));

        var hasActiveSession = await _db.ProductionSessions
            .AnyAsync(x =>
                x.IsActive &&
                (x.Status == "Planned" || x.Status == "Running" || x.Status == "Paused") &&
                (x.MachineId == request.MachineId || x.OperatorId == request.OperatorId),
                cancellationToken);

        if (hasActiveSession)
            return BadRequest(ApiResponse<object>.Fail(
                "Seçilen makine veya operatör başka bir aktif üretim session'ında kullanılıyor.",
                "LIVE_PRODUCTION_RESOURCE_IN_USE"));

        var utcNow = DateTime.UtcNow;

        if (machine.CurrentStatus == "Running")
        {
            machine.CurrentStatus = "Idle";
            machine.CurrentWorkOrderId = null;
            machine.CurrentOperatorName = null;
            machine.UpdatedAt = utcNow;
        }

        if (operatorEntity.CurrentStatus == "Working")
        {
            operatorEntity.CurrentStatus = "Available";
            operatorEntity.CurrentWorkOrderId = null;
            operatorEntity.CurrentWorkOrderNumber = null;
            operatorEntity.CurrentStationNumber = null;
            operatorEntity.UpdatedAt = utcNow;
        }

        return null;
    }

    private async Task<ProductionSession?> LoadSessionDetail(Guid id, CancellationToken cancellationToken)
    {
        return await _db.ProductionSessions
            .Include(x => x.Machine)
            .Include(x => x.Product)
            .Include(x => x.Operator)
            .Include(x => x.Stations.OrderBy(s => s.StationNumber))
            .Include(x => x.Events.OrderByDescending(e => e.EventTime).Take(25))
                .ThenInclude(x => x.ProductionStation)
            .Include(x => x.Downtimes)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private async Task<ProductionStation?> LoadWritableStation(Guid id, CancellationToken cancellationToken)
    {
        return await _db.ProductionStations
            .Include(x => x.ProductionSession)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private IActionResult? ValidateStationSession(ProductionStation station, bool requireRunning)
    {
        if (station.ProductionSession is null)
            return BadRequest(ApiResponse<object>.Fail("İstasyona bağlı üretim oturumu bulunamadı.", "SESSION_NOT_FOUND"));

        if (IsClosed(station.ProductionSession))
            return BadRequest(ApiResponse<object>.Fail("Tamamlanan veya iptal edilen oturumda işlem yapılamaz.", "SESSION_CLOSED"));

        if (requireRunning && station.ProductionSession.Status != "Running")
            return BadRequest(ApiResponse<object>.Fail("Bu işlem için oturum Running olmalıdır.", "SESSION_NOT_RUNNING"));

        return null;
    }

    private IActionResult? ValidateStationCanCycle(ProductionStation station)
    {
        var sessionValidation = ValidateStationSession(station, requireRunning: true);
        if (sessionValidation is not null)
            return sessionValidation;

        if (!station.MoldId.HasValue)
            return BadRequest(ApiResponse<object>.Fail("Çevrim başlatmak için istasyona kalıp atanmalıdır.", "MOLD_REQUIRED"));

        if (station.Status == "ReleaseDue")
            return BadRequest(ApiResponse<object>.Fail("Release bekleyen istasyonda release uygulanmadan yeni çevrim başlatılamaz.", "RELEASE_REQUIRED"));

        if (station.Status == "Fault")
            return BadRequest(ApiResponse<object>.Fail("Arızalı istasyonda çevrim başlatılamaz.", "STATION_FAULT"));

        if (station.Status is not ("Ready" or "Running" or "Completed"))
            return BadRequest(ApiResponse<object>.Fail("İstasyon durumu çevrim başlatmaya uygun değil.", "INVALID_STATION_STATUS"));

        return null;
    }

    private static object ToSessionListResponse(ProductionSession session)
    {
        var firePercent = CalculateFirePercent(session.GoodPairs, session.FirePairs);
        var activeStationCount = session.Stations.Count(x => x.Status is "Ready" or "Running" or "Curing" or "ReleaseDue");
        var releaseDueStationCount = session.Stations.Count(x => x.Status == "ReleaseDue");
        var faultStationCount = session.Stations.Count(x => x.Status == "Fault");
        var completionPercent = CalculateCompletionPercent(session);

        return new
        {
            session.Id,
            session.SessionNumber,
            session.WorkOrderId,
            session.WorkOrderNumber,
            session.ProductId,
            session.ProductCode,
            session.ProductName,
            session.CustomerName,
            session.Size,
            session.FoamType,
            session.MachineId,
            session.MachineCode,
            session.MachineName,
            session.OperatorId,
            session.OperatorCode,
            session.OperatorName,
            session.Shift,
            session.Status,
            session.StartTime,
            session.EndTime,
            session.PlannedPairs,
            session.ProducedPairs,
            session.GoodPairs,
            session.FirePairs,
            FirePercent = firePercent,
            session.TotalCycleCount,
            session.TotalDowntimeMinutes,
            ActiveStationCount = activeStationCount,
            ReleaseDueStationCount = releaseDueStationCount,
            FaultStationCount = faultStationCount,
            CompletionPercent = completionPercent,
            session.IsActive,
            session.CreatedAt,
            session.UpdatedAt
        };
    }

    private static object ToSessionDetailResponse(ProductionSession session)
    {
        var firePercent = CalculateFirePercent(session.GoodPairs, session.FirePairs);
        var completionPercent = CalculateCompletionPercent(session);
        var metrics = new
        {
            session.ProducedPairs,
            session.GoodPairs,
            session.FirePairs,
            FirePercent = firePercent,
            session.TotalCycleCount,
            session.TotalDowntimeMinutes,
            ActiveStationCount = session.Stations.Count(x => x.Status is "Ready" or "Running" or "Curing" or "ReleaseDue"),
            ReleaseDueStationCount = session.Stations.Count(x => x.Status == "ReleaseDue"),
            FaultStationCount = session.Stations.Count(x => x.Status == "Fault"),
            CompletionPercent = completionPercent
        };

        return new
        {
            Session = ToSessionListResponse(session),
            Machine = ToMachineSummaryResponse(session.Machine),
            Operator = ToOperatorSummaryResponse(session.Operator),
            Product = ToProductSummaryResponse(session.Product),
            Stations = session.Stations.OrderBy(x => x.StationNumber).Select(ToStationResponse),
            Metrics = metrics,
            Totals = metrics,
            RecentEvents = session.Events.OrderByDescending(x => x.EventTime).Take(25).Select(ToEventResponse),
            Downtimes = session.Downtimes.OrderByDescending(x => x.StartTime).Select(ToDowntimeResponse),
            FinishedGoodsStockNote = "Bitmiş ürün stok girişi Warehouse/Finished Goods modülü hazır olduğunda buraya bağlanacaktır."
        };
    }

    private static object? ToMachineSummaryResponse(Machine? machine)
    {
        if (machine is null)
            return null;

        return new
        {
            machine.Id,
            machine.Code,
            machine.Name,
            machine.MachineType,
            machine.Model,
            machine.Manufacturer,
            machine.StationCount,
            machine.DefaultCycleTimeSeconds,
            machine.MaximumDailyCapacity,
            machine.WorkingHoursPerDay,
            machine.CurrentStatus,
            machine.CurrentOperatorName,
            machine.Location,
            machine.TotalRunningHours,
            machine.TotalProducedPairs,
            machine.AvailabilityPercent,
            machine.PerformancePercent,
            machine.QualityPercent,
            machine.OEE,
            machine.IsActive
        };
    }

    private static object? ToOperatorSummaryResponse(Operator? operatorEntity)
    {
        if (operatorEntity is null)
            return null;

        return new
        {
            operatorEntity.Id,
            operatorEntity.Code,
            operatorEntity.FirstName,
            operatorEntity.LastName,
            operatorEntity.FullName,
            operatorEntity.Department,
            operatorEntity.Role,
            operatorEntity.Shift,
            operatorEntity.CurrentStatus,
            operatorEntity.CurrentMachineId,
            operatorEntity.CurrentMachineCode,
            operatorEntity.CurrentMachineName,
            operatorEntity.CurrentStationNumber,
            operatorEntity.IsActive
        };
    }

    private static object? ToProductSummaryResponse(Product? product)
    {
        if (product is null)
            return null;

        return new
        {
            product.Id,
            product.Code,
            product.Name,
            product.CustomerName,
            product.ModelCode,
            product.FoamType,
            product.ProductType,
            Size = (string?)null,
            Number = (string?)null,
            product.AverageWeight,
            product.TargetDensity,
            product.StandardCycleTime,
            product.IsActive
        };
    }

    private static object ToStationResponse(ProductionStation station)
    {
        return new
        {
            station.Id,
            station.ProductionSessionId,
            station.StationNumber,
            station.MoldId,
            station.MoldCode,
            station.MoldName,
            station.ProductId,
            station.ProductCode,
            station.ProductName,
            station.OperatorId,
            station.OperatorCode,
            station.OperatorName,
            station.Status,
            station.CurrentCycleNumber,
            station.ProducedPairs,
            station.GoodPairs,
            station.FirePairs,
            station.LastCycleStartedAt,
            station.LastCycleCompletedAt,
            station.CuringStartedAt,
            station.CuringExpectedEndAt,
            station.LastReleaseAt,
            station.CyclesSinceLastRelease,
            station.ReleaseFrequencyCycles,
            station.TargetPairWeight,
            station.LastPairWeight,
            station.TargetDensity,
            station.LastDensity,
            station.TargetCuringTimeSeconds,
            station.ActualLastCuringTimeSeconds,
            station.TargetCycleTimeSeconds,
            station.ActualLastCycleTimeSeconds,
            station.LastFaultReason,
            station.LastNote,
            station.IsActive,
            station.CreatedAt,
            station.UpdatedAt
        };
    }

    private static object ToEventResponse(ProductionEvent productionEvent)
    {
        return new
        {
            productionEvent.Id,
            productionEvent.EventType,
            productionEvent.EventTime,
            StationId = productionEvent.ProductionStationId,
            StationNumber = productionEvent.ProductionStation?.StationNumber,
            productionEvent.CycleNumber,
            productionEvent.ProducedPairs,
            productionEvent.GoodPairs,
            productionEvent.FirePairs,
            productionEvent.Weight,
            productionEvent.Density,
            productionEvent.CuringTimeSeconds,
            productionEvent.CycleTimeSeconds,
            productionEvent.Reason,
            productionEvent.Note,
            productionEvent.OperatorId,
            productionEvent.OperatorCode,
            productionEvent.OperatorName,
            productionEvent.CreatedAt
        };
    }

    private static object ToDowntimeResponse(ProductionDowntime downtime)
    {
        return new
        {
            downtime.Id,
            downtime.ProductionSessionId,
            downtime.ProductionStationId,
            downtime.ReasonType,
            downtime.Reason,
            downtime.StartTime,
            downtime.EndTime,
            downtime.DurationMinutes,
            downtime.IsOpen,
            downtime.OperatorId,
            downtime.OperatorName,
            downtime.CreatedAt,
            downtime.UpdatedAt
        };
    }

    private async Task<string> GenerateSessionNumber(DateTime utcNow, CancellationToken cancellationToken)
    {
        var prefix = $"LP-{utcNow:yyyyMMdd}";
        var count = await _db.ProductionSessions.CountAsync(x => x.SessionNumber.StartsWith(prefix), cancellationToken) + 1;
        return $"{prefix}-{count:0000}";
    }

    private void AddEvent(
        Guid sessionId,
        Guid? stationId,
        string eventType,
        DateTime utcNow,
        Operator? operatorEntity,
        string? reason,
        string? note,
        long? cycleNumber = null,
        long? producedPairs = null,
        long? goodPairs = null,
        long? firePairs = null,
        decimal? weight = null,
        decimal? density = null,
        int? curingTimeSeconds = null,
        int? cycleTimeSeconds = null)
    {
        _db.ProductionEvents.Add(new ProductionEvent
        {
            ProductionSessionId = sessionId,
            ProductionStationId = stationId,
            EventType = eventType,
            EventTime = utcNow,
            CycleNumber = cycleNumber,
            ProducedPairs = producedPairs,
            GoodPairs = goodPairs,
            FirePairs = firePairs,
            Weight = weight,
            Density = density,
            CuringTimeSeconds = curingTimeSeconds,
            CycleTimeSeconds = cycleTimeSeconds,
            Reason = reason,
            Note = note,
            OperatorId = operatorEntity?.Id,
            OperatorCode = operatorEntity?.Code,
            OperatorName = operatorEntity?.FullName,
            CreatedAt = utcNow
        });
    }

    private async Task CloseOpenSessionDowntimes(Guid sessionId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var downtimes = await _db.ProductionDowntimes
            .Where(x => x.ProductionSessionId == sessionId && x.IsOpen)
            .ToListAsync(cancellationToken);

        foreach (var downtime in downtimes)
            CloseDowntime(downtime, utcNow);
    }

    private static void CloseDowntime(ProductionDowntime downtime, DateTime utcNow)
    {
        downtime.EndTime = utcNow;
        downtime.DurationMinutes = Math.Round((decimal)(utcNow - downtime.StartTime).TotalMinutes, 2);
        downtime.IsOpen = false;
        downtime.UpdatedAt = utcNow;
    }

    private static void RecalculateSessionCounters(ProductionSession session)
    {
        session.ProducedPairs = session.Stations.Sum(x => x.ProducedPairs);
        session.GoodPairs = session.Stations.Sum(x => x.GoodPairs);
        session.FirePairs = session.Stations.Sum(x => x.FirePairs);
        session.TotalCycleCount = session.Stations.Sum(x => x.CurrentCycleNumber);
    }

    private async Task<decimal> CalculateSessionDowntimeMinutes(Guid sessionId, CancellationToken cancellationToken)
    {
        return await _db.ProductionDowntimes
            .Where(x => x.ProductionSessionId == sessionId && !x.IsOpen)
            .SumAsync(x => x.DurationMinutes ?? 0, cancellationToken);
    }

    private static bool HasUnfinishedCycle(ProductionStation station)
    {
        if (!station.LastCycleStartedAt.HasValue)
            return false;

        return !station.LastCycleCompletedAt.HasValue || station.LastCycleCompletedAt.Value < station.LastCycleStartedAt.Value;
    }

    private static bool IsUsedStation(ProductionStation station)
    {
        return station.MoldId.HasValue || station.CurrentCycleNumber > 0 || station.ProducedPairs > 0;
    }

    private static int? ResolveCuringTimeSeconds(ProductionStation station, Machine? machine)
    {
        if (station.TargetCuringTimeSeconds is > 0)
            return station.TargetCuringTimeSeconds.Value;

        if (station.Mold?.StandardCuringTimeSeconds is > 0)
            return station.Mold.StandardCuringTimeSeconds.Value;

        if (station.Product?.StandardCycleTime is > 0)
            return Convert.ToInt32(station.Product.StandardCycleTime.Value);

        if (machine?.DefaultCycleTimeSeconds is > 0)
            return machine.DefaultCycleTimeSeconds.Value;

        return null;
    }

    private static decimal? CalculateAverage(decimal? currentAverage, decimal? newValue, long count)
    {
        if (!newValue.HasValue || count <= 0)
            return currentAverage;

        if (!currentAverage.HasValue || count == 1)
            return newValue.Value;

        return Math.Round(((currentAverage.Value * (count - 1)) + newValue.Value) / count, 2);
    }

    private static decimal? CalculateAverage(decimal? currentAverage, int? newValue, long count)
    {
        return CalculateAverage(currentAverage, newValue.HasValue ? newValue.Value : null, count);
    }

    private static decimal CalculateFirePercent(long goodPairs, long firePairs)
    {
        var total = goodPairs + firePairs;
        return total == 0 ? 0 : Math.Round((decimal)firePairs / total * 100m, 2);
    }

    private static decimal CalculateCompletionPercent(ProductionSession session)
    {
        return session.PlannedPairs > 0
            ? Math.Round((decimal)session.ProducedPairs / session.PlannedPairs * 100m, 2)
            : 0;
    }

    private static bool IsClosed(ProductionSession session)
    {
        return session.Status is "Completed" or "Cancelled";
    }

    private static bool IsValidShift(int shift)
    {
        return shift is >= 1 and <= 3;
    }

    private static bool IsValidStation(int stationNumber)
    {
        return stationNumber is >= 1 and <= 24;
    }
}

public record CreateProductionSessionRequest(
    Guid? WorkOrderId,
    string? WorkOrderNumber,
    Guid ProductId,
    Guid MachineId,
    Guid OperatorId,
    int Shift,
    long PlannedPairs,
    string? ProductionNote
);

public record UpdateProductionSessionRequest(
    Guid? WorkOrderId,
    string? WorkOrderNumber,
    int? Shift,
    long? PlannedPairs,
    string? Status,
    decimal? EstimatedMaterialCost,
    decimal? ActualMaterialCost,
    string? ProductionNote,
    string? QualityNote
);

public record SessionReasonRequest(string? Reason, string? Note);

public record AssignStationMoldRequest(Guid? MoldId, Guid? ProductId, Guid? OperatorId);

public record AssignStationOperatorRequest(Guid? OperatorId);

public record UpdateProductionStationRequest(
    string? Status,
    decimal? TargetPairWeight,
    decimal? TargetDensity,
    int? TargetCuringTimeSeconds,
    int? TargetCycleTimeSeconds,
    int? ReleaseFrequencyCycles,
    string? LastNote
);

public record CompleteCycleRequest(
    long ProducedPairs,
    long GoodPairs,
    long FirePairs,
    int? ActualCuringTimeSeconds,
    int? ActualCycleTimeSeconds,
    decimal? Weight,
    decimal? Density,
    string? Note
);

public record RecordFireRequest(long FirePairs, string? Note);

public record RecordWeightRequest(decimal Weight, string? Note);

public record RecordDensityRequest(decimal Density, string? Note);

public record StartFaultRequest(string ReasonType, string? Reason, string? Note);

public record NoteRequest(string? Note);
