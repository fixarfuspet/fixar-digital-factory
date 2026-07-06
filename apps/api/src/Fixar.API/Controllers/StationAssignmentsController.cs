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
}

public record AssignStationRequest(
    int StationNumber,
    Guid OrderItemId,
    Guid? MoldId,
    string? OperatorName,
    string? Note
);