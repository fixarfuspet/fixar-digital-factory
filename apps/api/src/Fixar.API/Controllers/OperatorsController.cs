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
[Route("api/v{version:apiVersion}/operators")]
public class OperatorsController : ControllerBase
{
    private static readonly string[] Departments =
    {
        "Injection",
        "Cutting",
        "Packaging",
        "Warehouse",
        "Quality",
        "Maintenance",
        "ProductionManagement"
    };

    private static readonly string[] Roles =
    {
        "InjectionOperator",
        "CuttingOperator",
        "PackagingOperator",
        "WarehouseOperator",
        "QualityOperator",
        "MaintenanceOperator",
        "ProductionManager"
    };

    private static readonly string[] Statuses =
    {
        "Available",
        "Working",
        "Break",
        "Leave",
        "Absent",
        "Inactive"
    };

    private static readonly string[] MachineAssignmentTypes =
    {
        "Default",
        "Current"
    };

    private readonly ApplicationDbContext _db;

    public OperatorsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var operators = await _db.Operators
            .Include(x => x.DefaultMachine)
            .Include(x => x.CurrentMachine)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Department)
            .ThenBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(operators.Select(ToResponse)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var operatorEntity = await _db.Operators
            .Include(x => x.DefaultMachine)
            .Include(x => x.CurrentMachine)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (operatorEntity is null)
            return NotFound(ApiResponse<object>.Fail("Operatör bulunamadı.", "OPERATOR_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOperatorRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(request, null, cancellationToken);

        if (validation is not null)
            return validation;

        var operatorEntity = new Operator();
        await ApplyRequest(operatorEntity, request, DateTime.UtcNow, cancellationToken);

        _db.Operators.Add(operatorEntity);
        await _db.SaveChangesAsync(cancellationToken);

        await LoadMachines(operatorEntity, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity), "Operatör oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateOperatorRequest request, CancellationToken cancellationToken)
    {
        var operatorEntity = await _db.Operators
            .Include(x => x.DefaultMachine)
            .Include(x => x.CurrentMachine)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (operatorEntity is null)
            return NotFound(ApiResponse<object>.Fail("Operatör bulunamadı.", "OPERATOR_NOT_FOUND"));

        var validation = await ValidateRequest(request, id, cancellationToken);

        if (validation is not null)
            return validation;

        await ApplyRequest(operatorEntity, request, DateTime.UtcNow, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await LoadMachines(operatorEntity, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity), "Operatör güncellendi."));
    }

    [HttpPost("{id:guid}/assign-machine")]
    public async Task<IActionResult> AssignMachine(Guid id, [FromBody] AssignOperatorMachineRequest request, CancellationToken cancellationToken)
    {
        if (request.MachineId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail("Makine seçimi zorunludur.", "MACHINE_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.AssignmentType) || !MachineAssignmentTypes.Contains(request.AssignmentType.Trim()))
            return BadRequest(ApiResponse<object>.Fail("assignmentType Default veya Current olmalıdır.", "INVALID_ASSIGNMENT_TYPE"));

        var operatorEntity = await _db.Operators.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (operatorEntity is null)
            return NotFound(ApiResponse<object>.Fail("Operatör bulunamadı.", "OPERATOR_NOT_FOUND"));

        if (!operatorEntity.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif operatöre makine atanamaz.", "OPERATOR_INACTIVE"));

        var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == request.MachineId, cancellationToken);

        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Makine bulunamadı.", "MACHINE_NOT_FOUND"));

        if (!machine.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif makine atanamaz.", "MACHINE_INACTIVE"));

        if (request.AssignmentType.Trim() == "Default")
        {
            operatorEntity.DefaultMachineId = machine.Id;
            operatorEntity.DefaultMachineCode = machine.Code;
            operatorEntity.DefaultMachineName = machine.Name;
        }
        else
        {
            operatorEntity.CurrentMachineId = machine.Id;
            operatorEntity.CurrentMachineCode = machine.Code;
            operatorEntity.CurrentMachineName = machine.Name;
        }

        operatorEntity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await LoadMachines(operatorEntity, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity), "Operatör makine ataması güncellendi."));
    }

    [HttpPost("{id:guid}/assign-work-order")]
    public async Task<IActionResult> AssignWorkOrder(Guid id, [FromBody] AssignOperatorWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var operatorEntity = await GetActiveOperator(id, cancellationToken);

        if (operatorEntity.Result is not null)
            return operatorEntity.Result;

        operatorEntity.Value!.CurrentWorkOrderId = request.WorkOrderId;
        operatorEntity.Value.CurrentWorkOrderNumber = request.WorkOrderNumber;
        operatorEntity.Value.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await LoadMachines(operatorEntity.Value, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity.Value), "Operatör iş emri ataması güncellendi."));
    }

    [HttpPost("{id:guid}/assign-station")]
    public async Task<IActionResult> AssignStation(Guid id, [FromBody] AssignOperatorStationRequest request, CancellationToken cancellationToken)
    {
        if (!IsValidStation(request.StationNumber))
            return BadRequest(ApiResponse<object>.Fail("İstasyon numarası 1 ile 24 arasında olmalıdır.", "INVALID_STATION_NUMBER"));

        var operatorEntity = await GetActiveOperator(id, cancellationToken);

        if (operatorEntity.Result is not null)
            return operatorEntity.Result;

        if (!operatorEntity.Value!.CanUseInjectionMachine)
            return BadRequest(ApiResponse<object>.Fail("Injection yetkisi olmayan operatör istasyona atanamaz.", "INJECTION_PERMISSION_REQUIRED"));

        operatorEntity.Value.CurrentStationNumber = request.StationNumber;
        operatorEntity.Value.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await LoadMachines(operatorEntity.Value, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity.Value), "Operatör istasyon ataması güncellendi."));
    }

    [HttpPost("{id:guid}/start-work")]
    public async Task<IActionResult> StartWork(Guid id, CancellationToken cancellationToken)
    {
        var operatorEntity = await GetActiveOperator(id, cancellationToken);

        if (operatorEntity.Result is not null)
            return operatorEntity.Result;

        operatorEntity.Value!.CurrentStatus = "Working";
        operatorEntity.Value.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await LoadMachines(operatorEntity.Value, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity.Value), "Operatör çalışmaya başladı."));
    }

    [HttpPost("{id:guid}/stop-work")]
    public async Task<IActionResult> StopWork(Guid id, CancellationToken cancellationToken)
    {
        var operatorEntity = await GetActiveOperator(id, cancellationToken);

        if (operatorEntity.Result is not null)
            return operatorEntity.Result;

        operatorEntity.Value!.CurrentStatus = "Available";
        operatorEntity.Value.CurrentWorkOrderId = null;
        operatorEntity.Value.CurrentWorkOrderNumber = null;
        operatorEntity.Value.CurrentStationNumber = null;
        operatorEntity.Value.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await LoadMachines(operatorEntity.Value, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity.Value), "Operatör çalışmayı durdurdu."));
    }

    [HttpPost("{id:guid}/start-break")]
    public async Task<IActionResult> StartBreak(Guid id, CancellationToken cancellationToken)
    {
        var operatorEntity = await GetActiveOperator(id, cancellationToken);

        if (operatorEntity.Result is not null)
            return operatorEntity.Result;

        operatorEntity.Value!.CurrentStatus = "Break";
        operatorEntity.Value.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await LoadMachines(operatorEntity.Value, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity.Value), "Operatör mola durumuna alındı."));
    }

    [HttpPost("{id:guid}/end-break")]
    public async Task<IActionResult> EndBreak(Guid id, CancellationToken cancellationToken)
    {
        var operatorEntity = await GetActiveOperator(id, cancellationToken);

        if (operatorEntity.Result is not null)
            return operatorEntity.Result;

        operatorEntity.Value!.CurrentStatus = "Working";
        operatorEntity.Value.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await LoadMachines(operatorEntity.Value, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity.Value), "Operatör moladan döndü."));
    }

    [HttpPost("{id:guid}/record-production")]
    public async Task<IActionResult> RecordProduction(Guid id, [FromBody] OperatorProductionRequest request, CancellationToken cancellationToken)
    {
        if (request.ProducedPairs < 0)
            return BadRequest(ApiResponse<object>.Fail("Üretilen çift negatif olamaz.", "INVALID_PRODUCED_PAIRS"));

        if (request.WorkingHours < 0)
            return BadRequest(ApiResponse<object>.Fail("Çalışma saati negatif olamaz.", "INVALID_WORKING_HOURS"));

        if (request.FirePairs < 0)
            return BadRequest(ApiResponse<object>.Fail("Fire çift negatif olamaz.", "INVALID_FIRE_PAIRS"));

        var operatorEntity = await GetActiveOperator(id, cancellationToken);

        if (operatorEntity.Result is not null)
            return operatorEntity.Result;

        operatorEntity.Value!.TotalProducedPairs += request.ProducedPairs;
        operatorEntity.Value.TotalWorkingHours += request.WorkingHours;
        operatorEntity.Value.TotalFirePairs += request.FirePairs;
        operatorEntity.Value.AverageFirePercent = CalculateFirePercent(operatorEntity.Value.TotalProducedPairs, operatorEntity.Value.TotalFirePairs);
        operatorEntity.Value.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await LoadMachines(operatorEntity.Value, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity.Value), "Operatör üretim kaydı işlendi."));
    }

    [HttpPost("{id:guid}/record-performance")]
    public async Task<IActionResult> RecordPerformance(Guid id, [FromBody] OperatorPerformanceRequest request, CancellationToken cancellationToken)
    {
        if (!IsPercent(request.PerformancePercent))
            return BadRequest(ApiResponse<object>.Fail("PerformancePercent 0 ile 100 arasında olmalıdır.", "INVALID_PERFORMANCE_PERCENT"));

        if (!IsPercent(request.QualityScore))
            return BadRequest(ApiResponse<object>.Fail("QualityScore 0 ile 100 arasında olmalıdır.", "INVALID_QUALITY_SCORE"));

        var operatorEntity = await GetActiveOperator(id, cancellationToken);

        if (operatorEntity.Result is not null)
            return operatorEntity.Result;

        operatorEntity.Value!.PerformancePercent = request.PerformancePercent;
        operatorEntity.Value.QualityScore = request.QualityScore;
        operatorEntity.Value.LastPerformanceUpdate = NormalizeDate(request.UpdateDate) ?? DateTime.UtcNow;
        operatorEntity.Value.Notes = AppendNote(operatorEntity.Value.Notes, request.Note, "Performans");
        operatorEntity.Value.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await LoadMachines(operatorEntity.Value, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(operatorEntity.Value), "Operatör performans kaydı işlendi."));
    }

    private async Task<ActionResult<Operator>> GetActiveOperator(Guid id, CancellationToken cancellationToken)
    {
        var operatorEntity = await _db.Operators.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (operatorEntity is null)
            return NotFound(ApiResponse<object>.Fail("Operatör bulunamadı.", "OPERATOR_NOT_FOUND"));

        if (!operatorEntity.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif operatörde bu işlem yapılamaz.", "OPERATOR_INACTIVE"));

        return operatorEntity;
    }

    private async Task<IActionResult?> ValidateRequest(CreateOperatorRequest request, Guid? operatorId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse<object>.Fail("Operatör kodu zorunludur.", "CODE_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.FirstName))
            return BadRequest(ApiResponse<object>.Fail("Operatör adı zorunludur.", "FIRST_NAME_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(ApiResponse<object>.Fail("Operatör soyadı zorunludur.", "LAST_NAME_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.Department) || !Departments.Contains(request.Department.Trim()))
            return BadRequest(ApiResponse<object>.Fail("Department geçersiz.", "INVALID_DEPARTMENT"));

        if (string.IsNullOrWhiteSpace(request.Role) || !Roles.Contains(request.Role.Trim()))
            return BadRequest(ApiResponse<object>.Fail("Role geçersiz.", "INVALID_ROLE"));

        if (request.Shift is < 1 or > 3)
            return BadRequest(ApiResponse<object>.Fail("Shift sadece 1, 2 veya 3 olabilir.", "INVALID_SHIFT"));

        if (string.IsNullOrWhiteSpace(request.CurrentStatus) || !Statuses.Contains(request.CurrentStatus.Trim()))
            return BadRequest(ApiResponse<object>.Fail("CurrentStatus geçersiz.", "INVALID_STATUS"));

        if (request.CurrentStationNumber.HasValue && !IsValidStation(request.CurrentStationNumber.Value))
            return BadRequest(ApiResponse<object>.Fail("CurrentStationNumber 1 ile 24 arasında olmalıdır.", "INVALID_STATION_NUMBER"));

        if (request.TotalProducedPairs < 0)
            return BadRequest(ApiResponse<object>.Fail("TotalProducedPairs negatif olamaz.", "INVALID_TOTAL_PRODUCED_PAIRS"));

        if (request.TotalWorkingHours < 0)
            return BadRequest(ApiResponse<object>.Fail("TotalWorkingHours negatif olamaz.", "INVALID_TOTAL_WORKING_HOURS"));

        if (request.TotalFirePairs < 0)
            return BadRequest(ApiResponse<object>.Fail("TotalFirePairs negatif olamaz.", "INVALID_TOTAL_FIRE_PAIRS"));

        if (request.PerformancePercent.HasValue && !IsPercent(request.PerformancePercent.Value))
            return BadRequest(ApiResponse<object>.Fail("PerformancePercent 0 ile 100 arasında olmalıdır.", "INVALID_PERFORMANCE_PERCENT"));

        if (request.QualityScore.HasValue && !IsPercent(request.QualityScore.Value))
            return BadRequest(ApiResponse<object>.Fail("QualityScore 0 ile 100 arasında olmalıdır.", "INVALID_QUALITY_SCORE"));

        var code = request.Code.Trim();
        var codeExists = await _db.Operators
            .AnyAsync(x => x.IsActive && x.Code == code && (!operatorId.HasValue || x.Id != operatorId.Value), cancellationToken);

        if (codeExists)
            return BadRequest(ApiResponse<object>.Fail("Bu kod ile aktif bir operatör zaten var.", "OPERATOR_CODE_EXISTS"));

        if (!string.IsNullOrWhiteSpace(request.EmployeeNumber))
        {
            var employeeNumber = request.EmployeeNumber.Trim();
            var employeeExists = await _db.Operators
                .AnyAsync(x => x.IsActive && x.EmployeeNumber == employeeNumber && (!operatorId.HasValue || x.Id != operatorId.Value), cancellationToken);

            if (employeeExists)
                return BadRequest(ApiResponse<object>.Fail("Bu sicil numarası ile aktif bir operatör zaten var.", "EMPLOYEE_NUMBER_EXISTS"));
        }

        if (request.DefaultMachineId.HasValue && !await MachineExists(request.DefaultMachineId.Value, cancellationToken))
            return BadRequest(ApiResponse<object>.Fail("Varsayılan makine bulunamadı.", "DEFAULT_MACHINE_NOT_FOUND"));

        if (request.CurrentMachineId.HasValue && !await MachineExists(request.CurrentMachineId.Value, cancellationToken))
            return BadRequest(ApiResponse<object>.Fail("Mevcut makine bulunamadı.", "CURRENT_MACHINE_NOT_FOUND"));

        return null;
    }

    private async Task ApplyRequest(Operator operatorEntity, CreateOperatorRequest request, DateTime utcNow, CancellationToken cancellationToken)
    {
        operatorEntity.Code = request.Code.Trim();
        operatorEntity.FirstName = request.FirstName.Trim();
        operatorEntity.LastName = request.LastName.Trim();
        operatorEntity.FullName = $"{operatorEntity.FirstName} {operatorEntity.LastName}".Trim();
        operatorEntity.NationalId = request.NationalId;
        operatorEntity.EmployeeNumber = request.EmployeeNumber;
        operatorEntity.Department = request.Department.Trim();
        operatorEntity.Role = request.Role.Trim();
        operatorEntity.Phone = request.Phone;
        operatorEntity.Email = request.Email;
        operatorEntity.HireDate = NormalizeDate(request.HireDate);
        operatorEntity.TerminationDate = NormalizeDate(request.TerminationDate);
        operatorEntity.Shift = request.Shift;
        operatorEntity.DefaultMachineId = request.DefaultMachineId;
        operatorEntity.CanUseInjectionMachine = request.CanUseInjectionMachine;
        operatorEntity.CanUseGezerKafa = request.CanUseGezerKafa;
        operatorEntity.CanUseDonerKafa = request.CanUseDonerKafa;
        operatorEntity.CanUseDtfMachine = request.CanUseDtfMachine;
        operatorEntity.CanPerformQualityControl = request.CanPerformQualityControl;
        operatorEntity.CanPerformMaintenance = request.CanPerformMaintenance;
        operatorEntity.CanApproveWorkOrder = request.CanApproveWorkOrder;
        operatorEntity.CurrentMachineId = request.CurrentMachineId;
        operatorEntity.CurrentWorkOrderId = request.CurrentWorkOrderId;
        operatorEntity.CurrentWorkOrderNumber = request.CurrentWorkOrderNumber;
        operatorEntity.CurrentStationNumber = request.CurrentStationNumber;
        operatorEntity.CurrentStatus = request.CurrentStatus.Trim();
        operatorEntity.TotalProducedPairs = request.TotalProducedPairs ?? operatorEntity.TotalProducedPairs;
        operatorEntity.TotalWorkingHours = request.TotalWorkingHours ?? operatorEntity.TotalWorkingHours;
        operatorEntity.TotalFirePairs = request.TotalFirePairs ?? operatorEntity.TotalFirePairs;
        operatorEntity.AverageFirePercent = CalculateFirePercent(operatorEntity.TotalProducedPairs, operatorEntity.TotalFirePairs);
        operatorEntity.PerformancePercent = request.PerformancePercent;
        operatorEntity.QualityScore = request.QualityScore;
        operatorEntity.LastPerformanceUpdate = NormalizeDate(request.LastPerformanceUpdate);
        operatorEntity.PhotoPath = request.PhotoPath;
        operatorEntity.QrCode = request.QrCode;
        operatorEntity.Barcode = request.Barcode;
        operatorEntity.Notes = request.Notes;
        operatorEntity.IsActive = request.IsActive ?? operatorEntity.IsActive;

        await SyncMachineSnapshots(operatorEntity, cancellationToken);

        if (operatorEntity.CreatedAt == default)
            operatorEntity.CreatedAt = utcNow;

        operatorEntity.UpdatedAt = utcNow;
    }

    private async Task SyncMachineSnapshots(Operator operatorEntity, CancellationToken cancellationToken)
    {
        if (operatorEntity.DefaultMachineId.HasValue)
        {
            var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == operatorEntity.DefaultMachineId.Value, cancellationToken);
            operatorEntity.DefaultMachineCode = machine?.Code;
            operatorEntity.DefaultMachineName = machine?.Name;
        }
        else
        {
            operatorEntity.DefaultMachineCode = null;
            operatorEntity.DefaultMachineName = null;
        }

        if (operatorEntity.CurrentMachineId.HasValue)
        {
            var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == operatorEntity.CurrentMachineId.Value, cancellationToken);
            operatorEntity.CurrentMachineCode = machine?.Code;
            operatorEntity.CurrentMachineName = machine?.Name;
        }
        else
        {
            operatorEntity.CurrentMachineCode = null;
            operatorEntity.CurrentMachineName = null;
        }
    }

    private async Task LoadMachines(Operator operatorEntity, CancellationToken cancellationToken)
    {
        await _db.Entry(operatorEntity).Reference(x => x.DefaultMachine).LoadAsync(cancellationToken);
        await _db.Entry(operatorEntity).Reference(x => x.CurrentMachine).LoadAsync(cancellationToken);
    }

    private async Task<bool> MachineExists(Guid machineId, CancellationToken cancellationToken)
    {
        return await _db.Machines.AnyAsync(x => x.Id == machineId, cancellationToken);
    }

    private static object ToResponse(Operator operatorEntity)
    {
        return new
        {
            operatorEntity.Id,
            operatorEntity.Code,
            operatorEntity.FirstName,
            operatorEntity.LastName,
            operatorEntity.FullName,
            operatorEntity.NationalId,
            operatorEntity.EmployeeNumber,
            operatorEntity.Department,
            operatorEntity.Role,
            operatorEntity.Phone,
            operatorEntity.Email,
            operatorEntity.HireDate,
            operatorEntity.TerminationDate,
            operatorEntity.Shift,
            operatorEntity.DefaultMachineId,
            DefaultMachineCode = operatorEntity.DefaultMachine?.Code ?? operatorEntity.DefaultMachineCode,
            DefaultMachineName = operatorEntity.DefaultMachine?.Name ?? operatorEntity.DefaultMachineName,
            operatorEntity.CanUseInjectionMachine,
            operatorEntity.CanUseGezerKafa,
            operatorEntity.CanUseDonerKafa,
            operatorEntity.CanUseDtfMachine,
            operatorEntity.CanPerformQualityControl,
            operatorEntity.CanPerformMaintenance,
            operatorEntity.CanApproveWorkOrder,
            operatorEntity.CurrentMachineId,
            CurrentMachineCode = operatorEntity.CurrentMachine?.Code ?? operatorEntity.CurrentMachineCode,
            CurrentMachineName = operatorEntity.CurrentMachine?.Name ?? operatorEntity.CurrentMachineName,
            operatorEntity.CurrentWorkOrderId,
            operatorEntity.CurrentWorkOrderNumber,
            operatorEntity.CurrentStationNumber,
            operatorEntity.CurrentStatus,
            operatorEntity.TotalProducedPairs,
            operatorEntity.TotalWorkingHours,
            operatorEntity.TotalFirePairs,
            operatorEntity.AverageFirePercent,
            operatorEntity.PerformancePercent,
            operatorEntity.QualityScore,
            operatorEntity.LastPerformanceUpdate,
            operatorEntity.PhotoPath,
            operatorEntity.QrCode,
            operatorEntity.Barcode,
            operatorEntity.Notes,
            operatorEntity.IsActive,
            operatorEntity.CreatedAt,
            operatorEntity.UpdatedAt
        };
    }

    private static bool IsValidStation(int stationNumber)
    {
        return stationNumber is >= 1 and <= 24;
    }

    private static bool IsPercent(decimal value)
    {
        return value is >= 0 and <= 100;
    }

    private static decimal? CalculateFirePercent(long totalProducedPairs, long totalFirePairs)
    {
        var total = totalProducedPairs + totalFirePairs;

        if (total <= 0)
            return null;

        return Math.Round(totalFirePairs / (decimal)total * 100m, 2);
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

public record CreateOperatorRequest(
    string Code,
    string FirstName,
    string LastName,
    string? NationalId,
    string? EmployeeNumber,
    string Department,
    string Role,
    string? Phone,
    string? Email,
    DateTime? HireDate,
    DateTime? TerminationDate,
    int Shift,
    Guid? DefaultMachineId,
    bool CanUseInjectionMachine,
    bool CanUseGezerKafa,
    bool CanUseDonerKafa,
    bool CanUseDtfMachine,
    bool CanPerformQualityControl,
    bool CanPerformMaintenance,
    bool CanApproveWorkOrder,
    Guid? CurrentMachineId,
    Guid? CurrentWorkOrderId,
    string? CurrentWorkOrderNumber,
    int? CurrentStationNumber,
    string CurrentStatus,
    long? TotalProducedPairs,
    decimal? TotalWorkingHours,
    long? TotalFirePairs,
    decimal? PerformancePercent,
    decimal? QualityScore,
    DateTime? LastPerformanceUpdate,
    string? PhotoPath,
    string? QrCode,
    string? Barcode,
    string? Notes,
    bool? IsActive
);

public record AssignOperatorMachineRequest(Guid MachineId, string AssignmentType);

public record AssignOperatorWorkOrderRequest(Guid? WorkOrderId, string? WorkOrderNumber);

public record AssignOperatorStationRequest(int StationNumber);

public record OperatorProductionRequest(long ProducedPairs, decimal WorkingHours, long FirePairs);

public record OperatorPerformanceRequest(decimal PerformancePercent, decimal QualityScore, DateTime? UpdateDate, string? Note);
