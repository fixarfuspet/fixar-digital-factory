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
[Authorize]
[Route("api/v{version:apiVersion}/machines")]
public class MachineController : ControllerBase
{
    private static readonly string[] MachineTypes =
    {
        "Injection",
        "Cutting",
        "Packaging",
        "DTF",
        "Warehouse",
        "Quality"
    };

    private static readonly string[] MachineStatuses =
    {
        "Idle",
        "Running",
        "Maintenance",
        "Stopped"
    };

    private readonly ApplicationDbContext _db;

    public MachineController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var machines = await _db.Machines
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.MachineType)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(machines));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Makine bulunamadı.", "MACHINE_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(machine));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMachineRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(request, null, cancellationToken);

        if (validation is not null)
            return validation;

        var machine = new Machine();
        ApplyRequest(machine, request, DateTime.UtcNow);

        _db.Machines.Add(machine);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(machine, "Makine oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateMachineRequest request, CancellationToken cancellationToken)
    {
        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Makine bulunamadı.", "MACHINE_NOT_FOUND"));

        var validation = await ValidateRequest(request, id, cancellationToken);

        if (validation is not null)
            return validation;

        ApplyRequest(machine, request, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(machine, "Makine güncellendi."));
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, [FromBody] StartMachineRequest? request, CancellationToken cancellationToken)
    {
        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Makine bulunamadı.", "MACHINE_NOT_FOUND"));

        if (!machine.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif makine başlatılamaz.", "MACHINE_INACTIVE"));

        machine.CurrentStatus = "Running";
        machine.CurrentWorkOrderId = request?.CurrentWorkOrderId ?? machine.CurrentWorkOrderId;
        machine.CurrentOperatorName = request?.CurrentOperatorName ?? machine.CurrentOperatorName;
        machine.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(machine, "Makine çalıştırıldı."));
    }

    [HttpPost("{id:guid}/stop")]
    public async Task<IActionResult> Stop(Guid id, [FromBody] StopMachineRequest? request, CancellationToken cancellationToken)
    {
        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Makine bulunamadı.", "MACHINE_NOT_FOUND"));

        machine.CurrentStatus = "Idle";
        machine.CurrentWorkOrderId = null;
        machine.CurrentOperatorName = null;
        machine.Notes = AppendNote(machine.Notes, request?.Note, "Stop");
        machine.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(machine, "Makine durduruldu."));
    }

    [HttpPost("{id:guid}/maintenance")]
    public async Task<IActionResult> Maintenance(Guid id, [FromBody] MachineMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Makine bulunamadı.", "MACHINE_NOT_FOUND"));

        machine.CurrentStatus = "Maintenance";
        machine.LastMaintenanceDate = NormalizeDate(request.MaintenanceDate) ?? DateTime.UtcNow;
        machine.NextMaintenanceDate = NormalizeDate(request.NextMaintenanceDate);
        machine.Notes = AppendNote(machine.Notes, request.Note, "Bakım");
        machine.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(machine, "Makine bakım kaydı işlendi."));
    }

    [HttpPost("{id:guid}/cleaning")]
    public async Task<IActionResult> Cleaning(Guid id, [FromBody] MachineCleaningRequest request, CancellationToken cancellationToken)
    {
        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Makine bulunamadı.", "MACHINE_NOT_FOUND"));

        machine.LastCleaningDate = NormalizeDate(request.CleaningDate) ?? DateTime.UtcNow;
        machine.NextCleaningDate = NormalizeDate(request.NextCleaningDate);
        machine.Notes = AppendNote(machine.Notes, request.Note, "Temizlik");
        machine.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(machine, "Makine temizlik kaydı işlendi."));
    }

    [HttpPost("{id:guid}/calibration")]
    public async Task<IActionResult> Calibration(Guid id, [FromBody] MachineCalibrationRequest request, CancellationToken cancellationToken)
    {
        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Makine bulunamadı.", "MACHINE_NOT_FOUND"));

        machine.LastCalibrationDate = NormalizeDate(request.CalibrationDate) ?? DateTime.UtcNow;
        machine.NextCalibrationDate = NormalizeDate(request.NextCalibrationDate);
        machine.Notes = AppendNote(machine.Notes, request.Note, "Kalibrasyon");
        machine.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(machine, "Makine kalibrasyon kaydı işlendi."));
    }

    [HttpPost("{id:guid}/production")]
    public async Task<IActionResult> Production(Guid id, [FromBody] MachineProductionRequest request, CancellationToken cancellationToken)
    {
        if (request.ProducedPairs < 0)
            return BadRequest(ApiResponse<object>.Fail("Üretilen çift negatif olamaz.", "INVALID_PRODUCED_PAIRS"));

        if (request.RunningHours < 0)
            return BadRequest(ApiResponse<object>.Fail("Çalışma saati negatif olamaz.", "INVALID_RUNNING_HOURS"));

        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Makine bulunamadı.", "MACHINE_NOT_FOUND"));

        machine.TotalProducedPairs += request.ProducedPairs;
        machine.TotalRunningHours += request.RunningHours;
        machine.AvailabilityPercent = request.AvailabilityPercent ?? machine.AvailabilityPercent;
        machine.PerformancePercent = request.PerformancePercent ?? machine.PerformancePercent;
        machine.QualityPercent = request.QualityPercent ?? machine.QualityPercent;
        machine.OEE = request.OEE ?? CalculateOee(machine.AvailabilityPercent, machine.PerformancePercent, machine.QualityPercent) ?? machine.OEE;
        machine.Notes = AppendNote(machine.Notes, request.Note, "Üretim");
        machine.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(machine, "Makine üretim kaydı işlendi."));
    }

    private async Task<IActionResult?> ValidateRequest(CreateMachineRequest request, Guid? machineId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse<object>.Fail("Makine kodu zorunludur.", "CODE_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Makine adı zorunludur.", "NAME_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.MachineType) || !MachineTypes.Contains(request.MachineType.Trim()))
            return BadRequest(ApiResponse<object>.Fail("MachineType geçersiz.", "INVALID_MACHINE_TYPE"));

        if (!string.IsNullOrWhiteSpace(request.CurrentStatus) && !MachineStatuses.Contains(request.CurrentStatus.Trim()))
            return BadRequest(ApiResponse<object>.Fail("CurrentStatus geçersiz.", "INVALID_MACHINE_STATUS"));

        if (request.MachineType.Trim() == "Injection")
        {
            if ((request.StationCount ?? 0) <= 0)
                return BadRequest(ApiResponse<object>.Fail("Injection makinesi için StationCount 0'dan büyük olmalıdır.", "INVALID_STATION_COUNT"));

            if ((request.DefaultCycleTimeSeconds ?? 0) <= 0)
                return BadRequest(ApiResponse<object>.Fail("Injection makinesi için DefaultCycleTimeSeconds 0'dan büyük olmalıdır.", "INVALID_CYCLE_TIME"));

            if ((request.MaximumDailyCapacity ?? 0) <= 0)
                return BadRequest(ApiResponse<object>.Fail("Injection makinesi için MaximumDailyCapacity 0'dan büyük olmalıdır.", "INVALID_DAILY_CAPACITY"));
        }

        if (request.StationCount.HasValue && request.StationCount.Value < 0)
            return BadRequest(ApiResponse<object>.Fail("StationCount negatif olamaz.", "INVALID_STATION_COUNT"));

        if (request.DefaultCycleTimeSeconds.HasValue && request.DefaultCycleTimeSeconds.Value < 0)
            return BadRequest(ApiResponse<object>.Fail("DefaultCycleTimeSeconds negatif olamaz.", "INVALID_CYCLE_TIME"));

        if (request.MaximumDailyCapacity.HasValue && request.MaximumDailyCapacity.Value < 0)
            return BadRequest(ApiResponse<object>.Fail("MaximumDailyCapacity negatif olamaz.", "INVALID_DAILY_CAPACITY"));

        if (request.WorkingHoursPerDay.HasValue && request.WorkingHoursPerDay.Value < 0)
            return BadRequest(ApiResponse<object>.Fail("WorkingHoursPerDay negatif olamaz.", "INVALID_WORKING_HOURS"));

        if (request.EnergyConsumption.HasValue && request.EnergyConsumption.Value < 0)
            return BadRequest(ApiResponse<object>.Fail("EnergyConsumption negatif olamaz.", "INVALID_ENERGY_CONSUMPTION"));

        if (request.TotalRunningHours.HasValue && request.TotalRunningHours.Value < 0)
            return BadRequest(ApiResponse<object>.Fail("TotalRunningHours negatif olamaz.", "INVALID_TOTAL_RUNNING_HOURS"));

        if (request.TotalProducedPairs.HasValue && request.TotalProducedPairs.Value < 0)
            return BadRequest(ApiResponse<object>.Fail("TotalProducedPairs negatif olamaz.", "INVALID_TOTAL_PRODUCED_PAIRS"));

        var code = request.Code.Trim();
        var duplicateCode = await _db.Machines
            .AnyAsync(x => x.IsActive && x.Code == code && (!machineId.HasValue || x.Id != machineId.Value), cancellationToken);

        if (duplicateCode)
            return BadRequest(ApiResponse<object>.Fail("Bu kod ile aktif bir makine zaten var.", "MACHINE_CODE_EXISTS"));

        return null;
    }

    private static void ApplyRequest(Machine machine, CreateMachineRequest request, DateTime utcNow)
    {
        machine.Code = request.Code.Trim();
        machine.Name = request.Name.Trim();
        machine.Description = request.Description;
        machine.MachineType = request.MachineType.Trim();
        machine.Model = request.Model;
        machine.Manufacturer = request.Manufacturer;
        machine.SerialNumber = request.SerialNumber;
        machine.Year = request.Year;
        machine.StationCount = request.StationCount;
        machine.DefaultCycleTimeSeconds = request.DefaultCycleTimeSeconds;
        machine.MaximumDailyCapacity = request.MaximumDailyCapacity;
        machine.WorkingHoursPerDay = request.WorkingHoursPerDay;
        machine.EnergyConsumption = request.EnergyConsumption;
        machine.Location = request.Location;
        machine.CurrentStatus = string.IsNullOrWhiteSpace(request.CurrentStatus) ? machine.CurrentStatus : request.CurrentStatus.Trim();
        machine.CurrentWorkOrderId = request.CurrentWorkOrderId;
        machine.CurrentOperatorName = request.CurrentOperatorName;
        machine.LastMaintenanceDate = NormalizeDate(request.LastMaintenanceDate);
        machine.NextMaintenanceDate = NormalizeDate(request.NextMaintenanceDate);
        machine.LastCleaningDate = NormalizeDate(request.LastCleaningDate);
        machine.NextCleaningDate = NormalizeDate(request.NextCleaningDate);
        machine.LastCalibrationDate = NormalizeDate(request.LastCalibrationDate);
        machine.NextCalibrationDate = NormalizeDate(request.NextCalibrationDate);
        machine.TotalRunningHours = request.TotalRunningHours ?? machine.TotalRunningHours;
        machine.TotalProducedPairs = request.TotalProducedPairs ?? machine.TotalProducedPairs;
        machine.AvailabilityPercent = request.AvailabilityPercent;
        machine.PerformancePercent = request.PerformancePercent;
        machine.QualityPercent = request.QualityPercent;
        machine.OEE = request.OEE ?? CalculateOee(request.AvailabilityPercent, request.PerformancePercent, request.QualityPercent);
        machine.Notes = request.Notes;
        machine.IsActive = request.IsActive ?? machine.IsActive;

        if (machine.CreatedAt == default)
            machine.CreatedAt = utcNow;

        machine.UpdatedAt = utcNow;
    }

    private static DateTime? NormalizeDate(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

    private static decimal? CalculateOee(decimal? availability, decimal? performance, decimal? quality)
    {
        if (!availability.HasValue || !performance.HasValue || !quality.HasValue)
            return null;

        return Math.Round((availability.Value * performance.Value * quality.Value) / 10000m, 2);
    }

    private static string? AppendNote(string? currentNotes, string? note, string title)
    {
        if (string.IsNullOrWhiteSpace(note))
            return currentNotes;

        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC] {title}: {note.Trim()}";

        return string.IsNullOrWhiteSpace(currentNotes)
            ? entry
            : currentNotes + Environment.NewLine + entry;
    }
}

public record CreateMachineRequest(
    string Code,
    string Name,
    string? Description,
    string MachineType,
    string? Model,
    string? Manufacturer,
    string? SerialNumber,
    int? Year,
    int? StationCount,
    int? DefaultCycleTimeSeconds,
    int? MaximumDailyCapacity,
    decimal? WorkingHoursPerDay,
    decimal? EnergyConsumption,
    string? Location,
    string? CurrentStatus,
    Guid? CurrentWorkOrderId,
    string? CurrentOperatorName,
    DateTime? LastMaintenanceDate,
    DateTime? NextMaintenanceDate,
    DateTime? LastCleaningDate,
    DateTime? NextCleaningDate,
    DateTime? LastCalibrationDate,
    DateTime? NextCalibrationDate,
    decimal? TotalRunningHours,
    long? TotalProducedPairs,
    decimal? AvailabilityPercent,
    decimal? PerformancePercent,
    decimal? QualityPercent,
    decimal? OEE,
    string? Notes,
    bool? IsActive
);

public record StartMachineRequest(Guid? CurrentWorkOrderId, string? CurrentOperatorName);

public record StopMachineRequest(string? Note);

public record MachineMaintenanceRequest(DateTime? MaintenanceDate, DateTime? NextMaintenanceDate, string? Note);

public record MachineCleaningRequest(DateTime? CleaningDate, DateTime? NextCleaningDate, string? Note);

public record MachineCalibrationRequest(DateTime? CalibrationDate, DateTime? NextCalibrationDate, string? Note);

public record MachineProductionRequest(
    long ProducedPairs,
    decimal RunningHours,
    decimal? AvailabilityPercent,
    decimal? PerformancePercent,
    decimal? QualityPercent,
    decimal? OEE,
    string? Note
);
