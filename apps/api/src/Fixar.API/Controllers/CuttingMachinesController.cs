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
[Route("api/v{version:apiVersion}/cutting-machines")]
public class CuttingMachinesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CuttingMachinesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var machines = await _db.CuttingMachines
            .AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .Select(x => new CuttingMachineDto(x.Id, x.Name, x.MachineType, x.OperatorName, x.Status, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(machines));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var machine = await _db.CuttingMachines
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CuttingMachineDto(x.Id, x.Name, x.MachineType, x.OperatorName, x.Status, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Kesim makinesi bulunamadı.", "CUTTING_MACHINE_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(machine));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertCuttingMachineRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Makine adı zorunludur.", "NAME_REQUIRED"));

        var machine = new CuttingMachine();
        Apply(machine, request);
        _db.CuttingMachines.Add(machine);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new CuttingMachineDto(machine.Id, machine.Name, machine.MachineType, machine.OperatorName, machine.Status, machine.IsActive), "Kesim makinesi oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertCuttingMachineRequest request, CancellationToken cancellationToken)
    {
        var machine = await _db.CuttingMachines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Kesim makinesi bulunamadı.", "CUTTING_MACHINE_NOT_FOUND"));

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Makine adı zorunludur.", "NAME_REQUIRED"));

        Apply(machine, request);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new CuttingMachineDto(machine.Id, machine.Name, machine.MachineType, machine.OperatorName, machine.Status, machine.IsActive), "Kesim makinesi güncellendi."));
    }

    [HttpPost("{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) => SetActive(id, true, cancellationToken);

    [HttpPost("{id:guid}/deactivate")]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) => SetActive(id, false, cancellationToken);

    private async Task<IActionResult> SetActive(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var machine = await _db.CuttingMachines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (machine is null)
            return NotFound(ApiResponse<object>.Fail("Kesim makinesi bulunamadı.", "CUTTING_MACHINE_NOT_FOUND"));

        machine.IsActive = isActive;
        machine.Status = isActive ? "Çalışıyor" : "Pasif";
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { machine.Id, machine.IsActive, machine.Status }, isActive ? "Kesim makinesi aktifleştirildi." : "Kesim makinesi pasifleştirildi."));
    }

    private static void Apply(CuttingMachine machine, UpsertCuttingMachineRequest request)
    {
        machine.Name = request.Name.Trim();
        machine.MachineType = string.IsNullOrWhiteSpace(request.MachineType) ? "Gezer Kafa" : request.MachineType.Trim();
        machine.OperatorName = request.OperatorName?.Trim() ?? string.Empty;
        machine.Status = string.IsNullOrWhiteSpace(request.Status) ? "Çalışıyor" : request.Status.Trim();
        machine.IsActive = request.IsActive;
    }
}

public record UpsertCuttingMachineRequest(string Name, string? MachineType, string? OperatorName, string? Status, bool IsActive);
public record CuttingMachineDto(Guid Id, string Name, string MachineType, string OperatorName, string Status, bool IsActive);
