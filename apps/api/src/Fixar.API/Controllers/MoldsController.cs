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
[Route("api/v{version:apiVersion}/molds")]
public class MoldsController : ControllerBase
{
    private static readonly string[] MoldTypes =
    {
        "Single",
        "Pair",
        "Right",
        "Left",
        "Combined"
    };

    private static readonly string[] FoamTypes =
    {
        "10100",
        "10900"
    };

    private static readonly string[] ProductTypes =
    {
        "Normal",
        "Memory Foam"
    };

    private static readonly string[] OwnerTypes =
    {
        "Fixar",
        "Customer"
    };

    private readonly ApplicationDbContext _db;

    public MoldsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var molds = await _db.Molds
            .Include(x => x.Product)
            .OrderBy(x => x.CustomerName)
            .ThenBy(x => x.ProductModel)
            .ThenBy(x => x.Size)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(molds.Select(ToResponse)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var mold = await _db.Molds
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (mold is null)
            return NotFound(ApiResponse<object>.Fail("Kalıp bulunamadı.", "MOLD_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(mold)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMoldRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(request, null, cancellationToken);

        if (validation is not null)
            return validation;

        var mold = new Mold();
        ApplyRequest(mold, request, DateTime.UtcNow);

        _db.Molds.Add(mold);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(mold).Reference(x => x.Product).LoadAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(mold), "Kalıp oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateMoldRequest request, CancellationToken cancellationToken)
    {
        var mold = await _db.Molds
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (mold is null)
            return NotFound(ApiResponse<object>.Fail("Kalıp bulunamadı.", "MOLD_NOT_FOUND"));

        var validation = await ValidateRequest(request, id, cancellationToken);

        if (validation is not null)
            return validation;

        ApplyRequest(mold, request, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(mold).Reference(x => x.Product).LoadAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(mold), "Kalıp güncellendi."));
    }

    [HttpPost("{id:guid}/assign-station")]
    public async Task<IActionResult> AssignStation(Guid id, [FromBody] AssignMoldStationRequest request, CancellationToken cancellationToken)
    {
        if (!IsValidStation(request.StationNumber))
            return BadRequest(ApiResponse<object>.Fail("İstasyon numarası 1 ile 24 arasında olmalıdır.", "INVALID_STATION_NUMBER"));

        var mold = await _db.Molds
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (mold is null)
            return NotFound(ApiResponse<object>.Fail("Kalıp bulunamadı.", "MOLD_NOT_FOUND"));

        if (!mold.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif kalıp istasyona atanamaz.", "MOLD_INACTIVE"));

        var stationOccupied = await _db.Molds
            .AnyAsync(x => x.Id != id && x.IsActive && x.CurrentStationNumber == request.StationNumber, cancellationToken);

        if (stationOccupied)
            return BadRequest(ApiResponse<object>.Fail("Bu istasyonda aktif başka bir kalıp var.", "STATION_ALREADY_HAS_MOLD"));

        mold.CurrentStationNumber = request.StationNumber;
        mold.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(mold), "Kalıp istasyona atandı."));
    }

    [HttpPost("{id:guid}/record-cycle")]
    public async Task<IActionResult> RecordCycle(Guid id, [FromBody] RecordMoldCycleRequest request, CancellationToken cancellationToken)
    {
        if (request.CycleCount <= 0)
            return BadRequest(ApiResponse<object>.Fail("Çevrim adedi 0'dan büyük olmalıdır.", "INVALID_CYCLE_COUNT"));

        if (request.ProducedPairs < 0)
            return BadRequest(ApiResponse<object>.Fail("Üretilen çift negatif olamaz.", "INVALID_PRODUCED_PAIRS"));

        var mold = await _db.Molds
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (mold is null)
            return NotFound(ApiResponse<object>.Fail("Kalıp bulunamadı.", "MOLD_NOT_FOUND"));

        mold.TotalCycleCount += request.CycleCount;
        mold.TotalProducedPairs += request.ProducedPairs;
        mold.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var lifeWarning = mold.EstimatedLifeCycles.HasValue && mold.TotalCycleCount >= mold.EstimatedLifeCycles.Value
            ? "Kalıp tahmini ömür çevrimini aşmış veya ulaşmış olabilir."
            : null;

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            Mold = ToResponse(mold),
            LifeWarning = lifeWarning
        }, "Kalıp çevrim kaydı eklendi."));
    }

    [HttpPost("{id:guid}/record-cleaning")]
    public async Task<IActionResult> RecordCleaning(Guid id, [FromBody] RecordMoldCleaningRequest request, CancellationToken cancellationToken)
    {
        var mold = await _db.Molds
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (mold is null)
            return NotFound(ApiResponse<object>.Fail("Kalıp bulunamadı.", "MOLD_NOT_FOUND"));

        mold.LastCleaningDate = NormalizeDate(request.CleaningDate) ?? DateTime.UtcNow;
        mold.NextCleaningDate = NormalizeDate(request.NextCleaningDate);
        mold.Description = AppendNote(mold.Description, request.Note, "Temizlik");
        mold.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(mold), "Kalıp temizlik kaydı işlendi."));
    }

    [HttpPost("{id:guid}/record-maintenance")]
    public async Task<IActionResult> RecordMaintenance(Guid id, [FromBody] RecordMoldMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var mold = await _db.Molds
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (mold is null)
            return NotFound(ApiResponse<object>.Fail("Kalıp bulunamadı.", "MOLD_NOT_FOUND"));

        mold.LastMaintenanceDate = NormalizeDate(request.MaintenanceDate) ?? DateTime.UtcNow;
        mold.NextMaintenanceDate = NormalizeDate(request.NextMaintenanceDate);
        mold.Description = AppendNote(mold.Description, request.Note, "Bakım");
        mold.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToResponse(mold), "Kalıp bakım kaydı işlendi."));
    }

    private async Task<IActionResult?> ValidateRequest(CreateMoldRequest request, Guid? moldId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse<object>.Fail("Kalıp kodu zorunludur.", "CODE_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Kalıp adı zorunludur.", "NAME_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.Size))
            return BadRequest(ApiResponse<object>.Fail("Numara zorunludur.", "SIZE_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.FoamType) || !FoamTypes.Contains(request.FoamType.Trim()))
            return BadRequest(ApiResponse<object>.Fail("FoamType sadece 10100 veya 10900 olabilir.", "INVALID_FOAM_TYPE"));

        if (string.IsNullOrWhiteSpace(request.MoldType) || !MoldTypes.Contains(request.MoldType.Trim()))
            return BadRequest(ApiResponse<object>.Fail("MoldType geçersiz.", "INVALID_MOLD_TYPE"));

        if (!string.IsNullOrWhiteSpace(request.ProductType) && !ProductTypes.Contains(request.ProductType.Trim()))
            return BadRequest(ApiResponse<object>.Fail("ProductType geçersiz.", "INVALID_PRODUCT_TYPE"));

        if (!string.IsNullOrWhiteSpace(request.OwnerType) && !OwnerTypes.Contains(request.OwnerType.Trim()))
            return BadRequest(ApiResponse<object>.Fail("OwnerType geçersiz.", "INVALID_OWNER_TYPE"));

        if (request.CavityCount <= 0)
            return BadRequest(ApiResponse<object>.Fail("CavityCount 0'dan büyük olmalıdır.", "INVALID_CAVITY_COUNT"));

        if (request.XCoordinate < 0)
            return BadRequest(ApiResponse<object>.Fail("XCoordinate negatif olamaz.", "INVALID_X_COORDINATE"));

        if (request.YCoordinate < 0)
            return BadRequest(ApiResponse<object>.Fail("YCoordinate negatif olamaz.", "INVALID_Y_COORDINATE"));

        if (request.TargetPairWeight < 0)
            return BadRequest(ApiResponse<object>.Fail("TargetPairWeight negatif olamaz.", "INVALID_TARGET_PAIR_WEIGHT"));

        if (request.StandardCuringTimeSeconds < 0)
            return BadRequest(ApiResponse<object>.Fail("StandardCuringTimeSeconds negatif olamaz.", "INVALID_CURING_TIME"));

        if (request.StandardCycleTimeSeconds < 0)
            return BadRequest(ApiResponse<object>.Fail("StandardCycleTimeSeconds negatif olamaz.", "INVALID_CYCLE_TIME"));

        if (request.CurrentStationNumber.HasValue && !IsValidStation(request.CurrentStationNumber.Value))
            return BadRequest(ApiResponse<object>.Fail("CurrentStationNumber 1 ile 24 arasında olmalıdır.", "INVALID_STATION_NUMBER"));

        var code = request.Code.Trim();
        var duplicateCode = await _db.Molds
            .AnyAsync(x => x.IsActive && x.Code == code && (!moldId.HasValue || x.Id != moldId.Value), cancellationToken);

        if (duplicateCode)
            return BadRequest(ApiResponse<object>.Fail("Bu kod ile aktif bir kalıp zaten var.", "MOLD_CODE_EXISTS"));

        if (request.ProductId.HasValue)
        {
            var productExists = await _db.Products
                .AnyAsync(x => x.Id == request.ProductId.Value, cancellationToken);

            if (!productExists)
                return BadRequest(ApiResponse<object>.Fail("Bağlanacak ürün bulunamadı.", "PRODUCT_NOT_FOUND"));
        }

        if (request.CurrentStationNumber.HasValue)
        {
            var stationOccupied = await _db.Molds
                .AnyAsync(x => x.IsActive && x.CurrentStationNumber == request.CurrentStationNumber.Value && (!moldId.HasValue || x.Id != moldId.Value), cancellationToken);

            if (stationOccupied)
                return BadRequest(ApiResponse<object>.Fail("Bu istasyonda aktif başka bir kalıp var.", "STATION_ALREADY_HAS_MOLD"));
        }

        return null;
    }

    private static void ApplyRequest(Mold mold, CreateMoldRequest request, DateTime utcNow)
    {
        mold.ProductId = request.ProductId;
        mold.Code = request.Code.Trim();
        mold.Name = request.Name.Trim();
        mold.CustomerName = request.CustomerName;
        mold.ProductModel = request.ProductModel;
        mold.ModelCode = request.ModelCode;
        mold.Size = request.Size.Trim();
        mold.SizeRange = request.Size.Trim();
        mold.SizeGroup = request.SizeGroup;
        mold.Description = request.Description;
        mold.MoldType = request.MoldType.Trim();
        mold.CavityCount = request.CavityCount;
        mold.IsRightLeftCombined = request.IsRightLeftCombined;
        mold.FoamType = request.FoamType.Trim();
        mold.ProductType = string.IsNullOrWhiteSpace(request.ProductType) ? "Normal" : request.ProductType.Trim();
        mold.XCoordinate = request.XCoordinate;
        mold.YCoordinate = request.YCoordinate;
        mold.TargetPairWeight = request.TargetPairWeight;
        mold.MinimumPairWeight = request.MinimumPairWeight;
        mold.MaximumPairWeight = request.MaximumPairWeight;
        mold.TargetDensity = request.TargetDensity;
        mold.MinimumDensity = request.MinimumDensity;
        mold.MaximumDensity = request.MaximumDensity;
        mold.StandardCuringTimeSeconds = request.StandardCuringTimeSeconds;
        mold.StandardMoldTemperature = request.StandardMoldTemperature;
        mold.StandardCycleTimeSeconds = request.StandardCycleTimeSeconds;
        mold.ReleaseFrequencyCycles = request.ReleaseFrequencyCycles;
        mold.MoldWeightKg = request.MoldWeightKg;
        mold.MachineName = request.MachineName;
        mold.CompatibleMachineCode = request.CompatibleMachineCode;
        mold.CurrentStationNumber = request.CurrentStationNumber;
        mold.StorageLocation = request.StorageLocation;
        mold.ShelfCode = request.ShelfCode;
        mold.TotalCycleCount = request.TotalCycleCount ?? mold.TotalCycleCount;
        mold.TotalProducedPairs = request.TotalProducedPairs ?? mold.TotalProducedPairs;
        mold.LastCleaningDate = NormalizeDate(request.LastCleaningDate);
        mold.NextCleaningDate = NormalizeDate(request.NextCleaningDate);
        mold.LastMaintenanceDate = NormalizeDate(request.LastMaintenanceDate);
        mold.NextMaintenanceDate = NormalizeDate(request.NextMaintenanceDate);
        mold.EstimatedLifeCycles = request.EstimatedLifeCycles;
        mold.PhotoPath = request.PhotoPath;
        mold.TechnicalDocumentPath = request.TechnicalDocumentPath;
        mold.CadFilePath = request.CadFilePath;
        mold.QrCode = request.QrCode;
        mold.Barcode = request.Barcode;
        mold.OwnerType = string.IsNullOrWhiteSpace(request.OwnerType) ? "Fixar" : request.OwnerType.Trim();
        mold.OwnerCustomerName = request.OwnerCustomerName;
        mold.IsActive = request.IsActive ?? mold.IsActive;

        if (mold.CreatedAt == default)
            mold.CreatedAt = utcNow;

        mold.UpdatedAt = utcNow;
    }

    private static object ToResponse(Mold mold)
    {
        return new
        {
            mold.Id,
            mold.ProductId,
            ProductCode = mold.Product != null ? mold.Product.Code : null,
            ProductName = mold.Product != null ? mold.Product.Name : null,
            mold.Code,
            mold.Name,
            mold.CustomerName,
            mold.ProductModel,
            mold.ModelCode,
            mold.Size,
            mold.SizeGroup,
            mold.SizeRange,
            mold.Description,
            mold.MoldType,
            mold.CavityCount,
            mold.IsRightLeftCombined,
            mold.FoamType,
            mold.ProductType,
            mold.XCoordinate,
            mold.YCoordinate,
            mold.TargetPairWeight,
            mold.MinimumPairWeight,
            mold.MaximumPairWeight,
            mold.TargetDensity,
            mold.MinimumDensity,
            mold.MaximumDensity,
            mold.StandardCuringTimeSeconds,
            mold.StandardMoldTemperature,
            mold.StandardCycleTimeSeconds,
            mold.ReleaseFrequencyCycles,
            mold.MoldWeightKg,
            mold.MachineName,
            mold.CompatibleMachineCode,
            mold.CurrentStationNumber,
            mold.StorageLocation,
            mold.ShelfCode,
            mold.TotalCycleCount,
            mold.TotalProducedPairs,
            mold.LastCleaningDate,
            mold.NextCleaningDate,
            mold.LastMaintenanceDate,
            mold.NextMaintenanceDate,
            mold.EstimatedLifeCycles,
            mold.PhotoPath,
            mold.TechnicalDocumentPath,
            mold.CadFilePath,
            mold.QrCode,
            mold.Barcode,
            mold.OwnerType,
            mold.OwnerCustomerName,
            mold.IsActive,
            mold.CreatedAt,
            mold.UpdatedAt
        };
    }

    private static bool IsValidStation(int stationNumber)
    {
        return stationNumber is >= 1 and <= 24;
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

    private static string? AppendNote(string? currentDescription, string? note, string title)
    {
        if (string.IsNullOrWhiteSpace(note))
            return currentDescription;

        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC] {title}: {note.Trim()}";

        return string.IsNullOrWhiteSpace(currentDescription)
            ? entry
            : currentDescription + Environment.NewLine + entry;
    }
}

public record CreateMoldRequest(
    Guid? ProductId,
    string Code,
    string Name,
    string? CustomerName,
    string? ProductModel,
    string? ModelCode,
    string Size,
    string? SizeGroup,
    string? Description,
    string MoldType,
    int CavityCount,
    bool IsRightLeftCombined,
    string FoamType,
    string? ProductType,
    decimal? XCoordinate,
    decimal? YCoordinate,
    decimal? TargetPairWeight,
    decimal? MinimumPairWeight,
    decimal? MaximumPairWeight,
    decimal? TargetDensity,
    decimal? MinimumDensity,
    decimal? MaximumDensity,
    int? StandardCuringTimeSeconds,
    decimal? StandardMoldTemperature,
    int? StandardCycleTimeSeconds,
    int? ReleaseFrequencyCycles,
    decimal? MoldWeightKg,
    string? MachineName,
    string? CompatibleMachineCode,
    int? CurrentStationNumber,
    string? StorageLocation,
    string? ShelfCode,
    long? TotalCycleCount,
    long? TotalProducedPairs,
    DateTime? LastCleaningDate,
    DateTime? NextCleaningDate,
    DateTime? LastMaintenanceDate,
    DateTime? NextMaintenanceDate,
    long? EstimatedLifeCycles,
    string? PhotoPath,
    string? TechnicalDocumentPath,
    string? CadFilePath,
    string? QrCode,
    string? Barcode,
    string? OwnerType,
    string? OwnerCustomerName,
    bool? IsActive
);

public record AssignMoldStationRequest(int StationNumber);

public record RecordMoldCycleRequest(long CycleCount, long ProducedPairs);

public record RecordMoldCleaningRequest(DateTime? CleaningDate, DateTime? NextCleaningDate, string? Note);

public record RecordMoldMaintenanceRequest(DateTime? MaintenanceDate, DateTime? NextMaintenanceDate, string? Note);
