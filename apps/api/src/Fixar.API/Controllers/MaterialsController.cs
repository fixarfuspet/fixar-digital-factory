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
[Route("api/v{version:apiVersion}/materials")]
public class MaterialsController : ControllerBase
{
    private static readonly string[] MaterialTypes =
    {
        "Chemical",
        "Fabric",
        "Adhesive",
        "DTF",
        "Packaging",
        "Auxiliary"
    };

    private static readonly string[] ChemicalRoles =
    {
        "Poliol",
        "Izosiyanat",
        "Crosskim",
        "Pigment",
        "Solvent",
        "Kalıp Ayırıcı",
        "Diğer"
    };

    private static readonly string[] FabricTypes =
    {
        "Interlok",
        "Lacoste",
        "Mesh",
        "Keçe",
        "Diğer"
    };

    private static readonly string[] AdhesiveTypes =
    {
        "Normal",
        "Polibond",
        "Diğer"
    };

    private static readonly string[] PackagingTypes =
    {
        "Koli",
        "Poşet",
        "Etiket",
        "Shrink",
        "Diğer"
    };

    private readonly ApplicationDbContext _db;

    public MaterialsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var materials = await _db.Materials
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(materials));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var material = await _db.Materials.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (material is null)
            return NotFound(ApiResponse<object>.Fail("Malzeme bulunamadı.", "MATERIAL_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(material));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMaterialRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(request, null, null, cancellationToken);

        if (validation is not null)
            return validation;

        var material = new Material();
        ApplyRequest(material, request, true);

        _db.Materials.Add(material);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(material, "Malzeme oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateMaterialRequest request, CancellationToken cancellationToken)
    {
        var material = await _db.Materials.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (material is null)
            return NotFound(ApiResponse<object>.Fail("Malzeme bulunamadı.", "MATERIAL_NOT_FOUND"));

        var validation = await ValidateRequest(request, id, material, cancellationToken);

        if (validation is not null)
            return validation;

        ApplyRequest(material, request, false);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(material, "Malzeme güncellendi."));
    }

    private async Task<IActionResult?> ValidateRequest(CreateMaterialRequest request, Guid? materialId, Material? currentMaterial, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse<object>.Fail("Malzeme kodu zorunludur.", "CODE_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Malzeme adı zorunludur.", "NAME_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.MaterialType) || !MaterialTypes.Contains(request.MaterialType.Trim()))
            return BadRequest(ApiResponse<object>.Fail("MaterialType geçersiz.", "INVALID_MATERIAL_TYPE"));

        if (string.IsNullOrWhiteSpace(request.Unit))
            return BadRequest(ApiResponse<object>.Fail("Birim zorunludur.", "UNIT_REQUIRED"));

        var code = request.Code.Trim();
        var targetIsActive = request.IsActive ?? currentMaterial?.IsActive ?? true;
        var hasActiveCode = targetIsActive && await _db.Materials.AnyAsync(
            x => x.IsActive && x.Code == code && (!materialId.HasValue || x.Id != materialId.Value),
            cancellationToken);

        if (hasActiveCode)
            return BadRequest(ApiResponse<object>.Fail("Bu kod ile aktif bir malzeme zaten var.", "MATERIAL_CODE_EXISTS"));

        var materialType = request.MaterialType.Trim();

        if (materialType == "Chemical" && !IsNullOrAllowed(request.ChemicalRole, ChemicalRoles))
            return BadRequest(ApiResponse<object>.Fail("ChemicalRole geçersiz.", "INVALID_CHEMICAL_ROLE"));

        if (materialType == "Fabric" && !IsNullOrAllowed(request.FabricType, FabricTypes))
            return BadRequest(ApiResponse<object>.Fail("FabricType geçersiz.", "INVALID_FABRIC_TYPE"));

        if (materialType == "Adhesive" && !IsNullOrAllowed(request.AdhesiveType, AdhesiveTypes))
            return BadRequest(ApiResponse<object>.Fail("AdhesiveType geçersiz.", "INVALID_ADHESIVE_TYPE"));

        if (materialType == "Packaging" && !IsNullOrAllowed(request.PackagingType, PackagingTypes))
            return BadRequest(ApiResponse<object>.Fail("PackagingType geçersiz.", "INVALID_PACKAGING_TYPE"));

        return null;
    }

    private static bool IsNullOrAllowed(string? value, string[] allowedValues)
    {
        return string.IsNullOrWhiteSpace(value) || allowedValues.Contains(value.Trim());
    }

    private static void ApplyRequest(Material material, CreateMaterialRequest request, bool isCreate)
    {
        var now = DateTime.UtcNow;

        material.Code = request.Code.Trim();
        material.Name = request.Name.Trim();
        material.Category = TrimToNull(request.Category);
        material.SubCategory = TrimToNull(request.SubCategory);
        material.Description = TrimToNull(request.Description);
        material.MaterialType = request.MaterialType.Trim();
        material.Unit = request.Unit.Trim();
        material.DefaultSupplierId = request.DefaultSupplierId;
        material.DefaultSupplierName = TrimToNull(request.DefaultSupplierName);
        material.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim();
        material.LastPurchasePrice = request.LastPurchasePrice;
        material.MinimumStock = request.MinimumStock;
        material.MaximumStock = request.MaximumStock;
        material.CriticalStock = request.CriticalStock;
        material.WarehouseName = TrimToNull(request.WarehouseName);
        material.LocationCode = TrimToNull(request.LocationCode);
        material.LotTrackingEnabled = request.LotTrackingEnabled;
        material.ExpiryTrackingEnabled = request.ExpiryTrackingEnabled;
        material.TechnicalSpecification = TrimToNull(request.TechnicalSpecification);
        material.SafetyInformation = TrimToNull(request.SafetyInformation);
        material.ChemicalRole = TrimToNull(request.ChemicalRole);
        material.Density = request.Density;
        material.MixingRatio = request.MixingRatio;
        material.ContainerWeight = request.ContainerWeight;
        material.AddedToPoliolBatch = request.AddedToPoliolBatch;
        material.CrosskimApplicationNote = TrimToNull(request.CrosskimApplicationNote);
        material.FabricType = TrimToNull(request.FabricType);
        material.FabricWeightGsm = request.FabricWeightGsm;
        material.FabricColor = TrimToNull(request.FabricColor);
        material.FabricWidth = request.FabricWidth;
        material.FabricRollLength = request.FabricRollLength;
        material.AdhesiveType = TrimToNull(request.AdhesiveType);
        material.CustomerName = TrimToNull(request.CustomerName);
        material.DtfCode = TrimToNull(request.DtfCode);
        material.DtfName = TrimToNull(request.DtfName);
        material.DtfWidth = request.DtfWidth;
        material.DtfHeight = request.DtfHeight;
        material.ApplicationPosition = TrimToNull(request.ApplicationPosition);
        material.ApplicationNote = TrimToNull(request.ApplicationNote);
        material.PackagingType = TrimToNull(request.PackagingType);
        material.BoxPairCapacity = request.BoxPairCapacity;
        material.BoxDimensions = TrimToNull(request.BoxDimensions);
        material.BoxWeight = request.BoxWeight;
        material.IsActive = request.IsActive ?? material.IsActive;
        material.UpdatedAt = now;

        if (isCreate)
        {
            material.IsActive = request.IsActive ?? true;
            material.CreatedAt = now;
        }
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public record CreateMaterialRequest(
    string Code,
    string Name,
    string? Category,
    string? SubCategory,
    string? Description,
    string MaterialType,
    string Unit,
    Guid? DefaultSupplierId,
    string? DefaultSupplierName,
    string? Currency,
    decimal? LastPurchasePrice,
    decimal? MinimumStock,
    decimal? MaximumStock,
    decimal? CriticalStock,
    string? WarehouseName,
    string? LocationCode,
    bool LotTrackingEnabled,
    bool ExpiryTrackingEnabled,
    string? TechnicalSpecification,
    string? SafetyInformation,
    string? ChemicalRole,
    decimal? Density,
    decimal? MixingRatio,
    decimal? ContainerWeight,
    bool AddedToPoliolBatch,
    string? CrosskimApplicationNote,
    string? FabricType,
    decimal? FabricWeightGsm,
    string? FabricColor,
    decimal? FabricWidth,
    decimal? FabricRollLength,
    string? AdhesiveType,
    string? CustomerName,
    string? DtfCode,
    string? DtfName,
    decimal? DtfWidth,
    decimal? DtfHeight,
    string? ApplicationPosition,
    string? ApplicationNote,
    string? PackagingType,
    int? BoxPairCapacity,
    string? BoxDimensions,
    decimal? BoxWeight,
    bool? IsActive
);
