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
[AllowAnonymous]
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
            .Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Customer)
            .Include(x => x.OrderItem).ThenInclude(x => x.Product)
            .Include(x => x.Mold)
            .Where(x => x.FinishedAt == null)
            .OrderBy(x => x.StationNumberSnapshot)
            .Select(x => new
            {
                x.Id,
                x.StationNumberSnapshot,
                x.Status,
                x.OperatorName,
                x.ProducedPairs,
                CustomerName = x.OrderItem.Order.Customer.CompanyName ?? x.OrderItem.Order.Customer.Name,
                ProductName = x.OrderItem.Product != null ? x.OrderItem.Product.Name : x.OrderItem.Order.Product.Name,
                MoldName = x.Mold != null ? x.Mold.Name : "-",
                x.StartedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(assignments));
    }

    [HttpGet("station/{stationNumber:int}")]
    public async Task<IActionResult> GetByStation(int stationNumber, CancellationToken cancellationToken)
    {
        var assignment = await _db.StationAssignments
            .Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Customer)
            .Include(x => x.OrderItem).ThenInclude(x => x.Product)
            .Include(x => x.Mold)
            .Where(x => x.FinishedAt == null && x.StationNumberSnapshot == stationNumber)
            .OrderByDescending(x => x.StartedAt)
            .Select(x => new
            {
                x.Id,
                x.StationNumberSnapshot,
                x.Status,
                x.OperatorName,
                x.ProducedPairs,
                CustomerName = x.OrderItem.Order.Customer.CompanyName ?? x.OrderItem.Order.Customer.Name,
                ProductName = x.OrderItem.Product != null ? x.OrderItem.Product.Name : x.OrderItem.Order.Product.Name,
                MoldName = x.Mold != null ? x.Mold.Name : "-",
                QuantityPairs = x.OrderItem.QuantityPairs,
                OrderItemProducedPairs = x.OrderItem.ProducedPairs,
                RemainingPairs = x.OrderItem.QuantityPairs - x.OrderItem.ProducedPairs,
                ProductionType = x.OrderItem.ProductionType,
                FabricColor = x.OrderItem.FabricColor,
                x.StartedAt,
                x.Note
            })
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

        var alreadyActive = await _db.StationAssignments
            .AnyAsync(x => x.StationNumberSnapshot == request.StationNumber && x.FinishedAt == null, cancellationToken);

        if (alreadyActive)
            return BadRequest(ApiResponse<object>.Fail("Bu istasyonda zaten aktif iş var.", "STATION_ALREADY_ACTIVE"));

        var orderItem = await _db.OrderItems
            .FirstOrDefaultAsync(x => x.Id == request.OrderItemId, cancellationToken);

        if (orderItem is null)
            return NotFound(ApiResponse<object>.Fail("Sipariş kalemi bulunamadı.", "ORDER_ITEM_NOT_FOUND"));

        var assignment = new StationAssignment
        {
            InjectionStationId = station.Id,
            OrderItemId = request.OrderItemId,
            MoldId = request.MoldId,
            StationNumberSnapshot = request.StationNumber,
            OperatorName = request.OperatorName,
            StartedAt = DateTime.UtcNow,
            Status = "Üretimde",
            ProducedPairs = 0,
            Note = request.Note
        };

        station.Status = "Üretimde";

        _db.StationAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(assignment, "İş başlatıldı."));
    }

    [HttpPost("add-production")]
    public async Task<IActionResult> AddProduction([FromBody] AddProductionRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _db.StationAssignments
            .Include(x => x.OrderItem)
            .FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId && x.FinishedAt == null, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        if (request.ProducedPairs <= 0)
            return BadRequest(ApiResponse<object>.Fail("Üretilen çift 0'dan büyük olmalı.", "INVALID_QUANTITY"));

        assignment.ProducedPairs += request.ProducedPairs;
        assignment.OrderItem.ProducedPairs += request.ProducedPairs;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(assignment, "Üretim adedi eklendi."));
    }

    [HttpPost("add-turn")]
    public async Task<IActionResult> AddTurn([FromBody] AddTurnRequest request, CancellationToken cancellationToken)
    {
        if (request.TurnCount <= 0)
            return BadRequest(ApiResponse<object>.Fail("Tur adedi 0'dan büyük olmalı.", "INVALID_TURN_COUNT"));

        var assignments = await _db.StationAssignments
            .Include(x => x.OrderItem)
            .Where(x => x.FinishedAt == null && x.Status == "Üretimde")
            .OrderBy(x => x.StationNumberSnapshot)
            .ToListAsync(cancellationToken);

        if (!assignments.Any())
            return BadRequest(ApiResponse<object>.Fail("Aktif üretimde istasyon yok.", "NO_ACTIVE_STATION"));

        foreach (var assignment in assignments)
        {
            assignment.ProducedPairs += request.TurnCount;
            assignment.OrderItem.ProducedPairs += request.TurnCount;

            if (!string.IsNullOrWhiteSpace(request.Note))
                assignment.Note = request.Note;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var result = new
        {
            TurnCount = request.TurnCount,
            ActiveStationCount = assignments.Count,
            TotalAddedPairs = assignments.Count * request.TurnCount,
            Stations = assignments.Select(x => x.StationNumberSnapshot).ToList(),
            AddedAt = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.SuccessResponse(result, "Tur üretimi eklendi."));
    }

    [HttpPost("pause")]
    public async Task<IActionResult> Pause([FromBody] AssignmentActionRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _db.StationAssignments
            .FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId && x.FinishedAt == null, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        assignment.Status = "Duraklatıldı";
        assignment.Note = request.Note ?? assignment.Note;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(assignment, "İş duraklatıldı."));
    }

    [HttpPost("resume")]
    public async Task<IActionResult> Resume([FromBody] AssignmentActionRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _db.StationAssignments
            .FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId && x.FinishedAt == null, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        assignment.Status = "Üretimde";
        assignment.Note = request.Note ?? assignment.Note;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(assignment, "İş devam ettirildi."));
    }

    [HttpPost("finish")]
    public async Task<IActionResult> Finish([FromBody] AssignmentActionRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _db.StationAssignments
            .Include(x => x.InjectionStation)
            .FirstOrDefaultAsync(x => x.Id == request.StationAssignmentId && x.FinishedAt == null, cancellationToken);

        if (assignment is null)
            return NotFound(ApiResponse<object>.Fail("Aktif iş bulunamadı.", "ASSIGNMENT_NOT_FOUND"));

        assignment.Status = "Tamamlandı";
        assignment.FinishedAt = DateTime.UtcNow;
        assignment.Note = request.Note ?? assignment.Note;

        assignment.InjectionStation.Status = "Boş";

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(assignment, "İş tamamlandı."));
    }
}

public record AssignStationRequest(
    int StationNumber,
    Guid OrderItemId,
    Guid? MoldId,
    string? OperatorName,
    string? Note
);

public record AddProductionRequest(
    Guid StationAssignmentId,
    int ProducedPairs
);

public record AddTurnRequest(
    int TurnCount,
    string? Note
);

public record AssignmentActionRequest(
    Guid StationAssignmentId,
    string? Note
);