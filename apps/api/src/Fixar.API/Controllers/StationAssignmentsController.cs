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
[Route("api/v{version:apiVersion}/station-assignments")]
public class StationAssignmentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StationAssignmentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var assignments = await _db.StationAssignments
            .Include(x => x.InjectionStation)
            .Include(x => x.OrderItem)
                .ThenInclude(x => x.Order)
                    .ThenInclude(x => x.Customer)
            .Include(x => x.OrderItem)
                .ThenInclude(x => x.Product)
            .Include(x => x.Mold)
            .Where(x => x.FinishedAt == null)
            .OrderBy(x => x.StationNumberSnapshot)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(assignments));
    }

    [HttpGet("station/{stationNumber:int}")]
    public async Task<IActionResult> GetByStation(int stationNumber, CancellationToken cancellationToken)
    {
        var assignment = await _db.StationAssignments
            .Include(x => x.InjectionStation)
            .Include(x => x.OrderItem)
                .ThenInclude(x => x.Order)
                    .ThenInclude(x => x.Customer)
            .Include(x => x.OrderItem)
                .ThenInclude(x => x.Product)
            .Include(x => x.Mold)
            .Where(x => x.FinishedAt == null && x.StationNumberSnapshot == stationNumber)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Bu istasyonda aktif üretim yok.", "STATION_EMPTY"));

        return Ok(ApiResponse<object>.SuccessResponse(assignment));
    }

    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignStationRequest request, CancellationToken cancellationToken)
    {
        var station = await _db.InjectionStations
            .FirstOrDefaultAsync(x => x.StationNumber == request.StationNumber, cancellationToken);

        if (station is null)
            return NotFound(ApiResponse<object>.Fail("İstasyon bulunamadı.", "STATION_NOT_FOUND"));

        var orderItem = await _db.OrderItems
            .FirstOrDefaultAsync(x => x.Id == request.OrderItemId, cancellationToken);

        if (orderItem is null)
            return NotFound(ApiResponse<object>.Fail("Sipariş kalemi bulunamadı.", "ORDER_ITEM_NOT_FOUND"));

        var activeAssignment = await _db.StationAssignments
            .FirstOrDefaultAsync(x => x.InjectionStationId == station.Id && x.FinishedAt == null, cancellationToken);

        if (activeAssignment is not null)
            return BadRequest(ApiResponse<object>.Fail("Bu istasyonda zaten aktif üretim var. Önce mevcut işi bitirin.", "STATION_ALREADY_ACTIVE"));

        var assignment = new StationAssignment
        {
            InjectionStationId = station.Id,
            OrderItemId = request.OrderItemId,
            MoldId = request.MoldId,
            StationNumberSnapshot = station.StationNumber,
            OperatorName = request.OperatorName,
            StartedAt = DateTime.UtcNow,
            Status = "Üretimde",
            Note = request.Note
        };

        station.Status = "Üretimde";

        _db.StationAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(assignment, "İstasyon ataması yapıldı."));
    }

    [HttpPost("finish")]
    public async Task<IActionResult> Finish([FromBody] FinishStationAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _db.StationAssignments
            .Include(x => x.InjectionStation)
            .Include(x => x.OrderItem)
            .FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("İstasyon ataması bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        if (assignment.FinishedAt is not null)
            return BadRequest(ApiResponse<object>.Fail("Bu iş zaten tamamlanmış.", "ASSIGNMENT_ALREADY_FINISHED"));

        assignment.ProducedPairs = request.ProducedPairs;
        assignment.FinishedAt = DateTime.UtcNow;
        assignment.Status = "Tamamlandı";
        assignment.Note = request.Note ?? assignment.Note;

        assignment.OrderItem.ProducedPairs += request.ProducedPairs;

        assignment.InjectionStation.Status = "Boş";

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(assignment, "İstasyondaki iş tamamlandı."));
    }

    [HttpPost("change-status")]
    public async Task<IActionResult> ChangeStatus([FromBody] ChangeStationStatusRequest request, CancellationToken cancellationToken)
    {
        var station = await _db.InjectionStations
            .FirstOrDefaultAsync(x => x.StationNumber == request.StationNumber, cancellationToken);

        if (station is null)
            return NotFound(ApiResponse<object>.Fail("İstasyon bulunamadı.", "STATION_NOT_FOUND"));

        station.Status = request.Status;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(station, "İstasyon durumu güncellendi."));
    }
}

public record AssignStationRequest(
    int StationNumber,
    Guid OrderItemId,
    Guid? MoldId,
    string? OperatorName,
    string? Note
);

public record FinishStationAssignmentRequest(
    Guid StationAssignmentId,
    int ProducedPairs,
    string? Note
);

public record ChangeStationStatusRequest(
    int StationNumber,
    string Status
);