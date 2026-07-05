using Asp.Versioning;
using Fixar.Application.Common.Models;
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
    [AllowAnonymous]
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
            .Select(x => new
            {
                x.Id,
                x.StationNumberSnapshot,
                x.Status,
                x.OperatorName,
                x.ProducedPairs,
                CustomerName = x.OrderItem.Order.Customer.Name,
                ProductName = x.OrderItem.Product != null ? x.OrderItem.Product.Name : "-",
                MoldName = x.Mold != null ? x.Mold.Name : "-",
                StartedAt = x.StartedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(assignments));
    }

    [HttpPost("change-status")]
    [AllowAnonymous]
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

public record ChangeStationStatusRequest(
    int StationNumber,
    string Status
);