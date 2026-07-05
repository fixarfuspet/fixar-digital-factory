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
[Route("api/v{version:apiVersion}/production-boxes")]
public class ProductionBoxesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProductionBoxesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var boxes = await _db.ProductionBoxes
            .OrderBy(x => x.BoxCode)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(boxes));
    }

    [HttpGet("{boxCode}")]
    public async Task<IActionResult> GetByCode(string boxCode, CancellationToken cancellationToken)
    {
        var box = await _db.ProductionBoxes
            .Include(x => x.Events.OrderByDescending(e => e.EventTime))
            .FirstOrDefaultAsync(x => x.BoxCode == boxCode, cancellationToken);

        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Kasa bulunamadı.", "BOX_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(box));
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateBoxRequest request, CancellationToken cancellationToken)
    {
        var exists = await _db.ProductionBoxes.AnyAsync(x => x.BoxCode == request.BoxCode, cancellationToken);

        if (exists)
            return BadRequest(ApiResponse<object>.Fail("Bu kasa kodu zaten var.", "BOX_ALREADY_EXISTS"));

        var box = new ProductionBox
        {
            BoxCode = request.BoxCode,
            CurrentStatus = "Boş",
            CurrentLocation = "Boş Kasa Alanı"
        };

        _db.ProductionBoxes.Add(box);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(box, "Kasa oluşturuldu."));
    }

    [HttpPost("fill")]
    public async Task<IActionResult> Fill([FromBody] FillBoxRequest request, CancellationToken cancellationToken)
    {
        var box = await _db.ProductionBoxes.FirstOrDefaultAsync(x => x.BoxCode == request.BoxCode, cancellationToken);

        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Kasa bulunamadı.", "BOX_NOT_FOUND"));

        box.OrderId = request.OrderId;
        box.ProductId = request.ProductId;
        box.MoldId = request.MoldId;
        box.CustomerName = request.CustomerName;
        box.ProductionType = request.ProductionType;
        box.FabricColor = request.FabricColor;
        box.QuantityPairs = request.QuantityPairs;
        box.CurrentStatus = "Üretimden Çıktı";
        box.CurrentLocation = request.Location;
        box.OperatorName = request.OperatorName;
        box.FilledAt = DateTime.UtcNow;

        _db.ProductionBoxEvents.Add(new ProductionBoxEvent
        {
            ProductionBox = box,
            EventType = "Üretimden Çıktı",
            ToLocation = request.Location,
            OperatorName = request.OperatorName,
            QuantityPairs = request.QuantityPairs,
            Note = request.Note,
            EventTime = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(box, "Kasa dolduruldu."));
    }

    [HttpPost("start-cutting")]
    public Task<IActionResult> StartCutting([FromBody] BoxMoveRequest request, CancellationToken cancellationToken)
        => MoveBox(request, "Kesime Başladı", "Kesim", cancellationToken);

    [HttpPost("finish-cutting")]
    public Task<IActionResult> FinishCutting([FromBody] BoxMoveRequest request, CancellationToken cancellationToken)
        => MoveBox(request, "Kesim Bitti", "Kesim Tamamlandı", cancellationToken);

    [HttpPost("move-to-warehouse")]
    public Task<IActionResult> MoveToWarehouse([FromBody] BoxMoveRequest request, CancellationToken cancellationToken)
        => MoveBox(request, "Depoya Girdi", request.ToLocation ?? "Depo", cancellationToken);

    [HttpPost("ship")]
    public Task<IActionResult> Ship([FromBody] BoxMoveRequest request, CancellationToken cancellationToken)
        => MoveBox(request, "Sevk Edildi", "Sevkiyat", cancellationToken);

    [HttpPost("empty")]
    public async Task<IActionResult> EmptyBox([FromBody] BoxMoveRequest request, CancellationToken cancellationToken)
    {
        var box = await _db.ProductionBoxes.FirstOrDefaultAsync(x => x.BoxCode == request.BoxCode, cancellationToken);

        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Kasa bulunamadı.", "BOX_NOT_FOUND"));

        var fromLocation = box.CurrentLocation;

        box.OrderId = null;
        box.ProductId = null;
        box.MoldId = null;
        box.CustomerName = null;
        box.ProductionType = null;
        box.FabricColor = null;
        box.QuantityPairs = 0;
        box.CurrentStatus = "Boş";
        box.CurrentLocation = "Boş Kasa Alanı";
        box.OperatorName = request.OperatorName;
        box.FilledAt = null;

        _db.ProductionBoxEvents.Add(new ProductionBoxEvent
        {
            ProductionBox = box,
            EventType = "Kasa Boşaltıldı",
            FromLocation = fromLocation,
            ToLocation = "Boş Kasa Alanı",
            OperatorName = request.OperatorName,
            Note = request.Note,
            EventTime = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(box, "Kasa boşaltıldı."));
    }

    private async Task<IActionResult> MoveBox(BoxMoveRequest request, string eventType, string defaultLocation, CancellationToken cancellationToken)
    {
        var box = await _db.ProductionBoxes.FirstOrDefaultAsync(x => x.BoxCode == request.BoxCode, cancellationToken);

        if (box is null)
            return NotFound(ApiResponse<object>.Fail("Kasa bulunamadı.", "BOX_NOT_FOUND"));

        var fromLocation = box.CurrentLocation;
        var toLocation = request.ToLocation ?? defaultLocation;

        box.CurrentStatus = eventType;
        box.CurrentLocation = toLocation;
        box.OperatorName = request.OperatorName;

        _db.ProductionBoxEvents.Add(new ProductionBoxEvent
        {
            ProductionBox = box,
            EventType = eventType,
            FromLocation = fromLocation,
            ToLocation = toLocation,
            OperatorName = request.OperatorName,
            QuantityPairs = box.QuantityPairs,
            Note = request.Note,
            EventTime = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(box, eventType));
    }
}

public record CreateBoxRequest(string BoxCode);

public record FillBoxRequest(
    string BoxCode,
    Guid? OrderId,
    Guid? ProductId,
    Guid? MoldId,
    string? CustomerName,
    string? ProductionType,
    string? FabricColor,
    int QuantityPairs,
    string? Location,
    string? OperatorName,
    string? Note
);

public record BoxMoveRequest(
    string BoxCode,
    string? ToLocation,
    string? OperatorName,
    string? Note
);
