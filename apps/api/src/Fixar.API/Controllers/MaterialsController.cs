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
        var materials = await ProjectMaterialList()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(materials));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var material = await ProjectMaterialDetail()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

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

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.Materials.Add(material);
        var stockValidation = await SyncStockItemForMaterial(material, cancellationToken);

        if (stockValidation is not null)
            return stockValidation;

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var created = await ProjectMaterialDetail().FirstAsync(x => x.Id == material.Id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(created, "Malzeme oluşturuldu."));
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

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        ApplyRequest(material, request, false);
        var stockValidation = await SyncStockItemForMaterial(material, cancellationToken);

        if (stockValidation is not null)
            return stockValidation;

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var updated = await ProjectMaterialDetail().FirstAsync(x => x.Id == id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(updated, "Malzeme güncellendi."));
    }

    private IQueryable<MaterialListDto> ProjectMaterialList()
    {
        return _db.Materials
            .AsNoTracking()
            .Select(x => new MaterialListDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                Category = x.Category,
                SubCategory = x.SubCategory,
                MaterialType = x.MaterialType,
                Unit = x.Unit,
                Density = x.Density,
                DefaultSupplierId = x.DefaultSupplierId,
                DefaultSupplierName = x.DefaultSupplierName,
                PreferredSupplierId = x.DefaultSupplierId,
                PreferredSupplierName = x.DefaultSupplierName,
                Currency = x.Currency,
                LastPurchasePrice = x.LastPurchasePrice,
                MinimumStock = x.MinimumStock,
                MaximumStock = x.MaximumStock,
                CriticalStock = x.CriticalStock,
                WarehouseName = x.WarehouseName,
                LocationCode = x.LocationCode,
                LotTrackingEnabled = x.LotTrackingEnabled,
                ExpiryTrackingEnabled = x.ExpiryTrackingEnabled,
                TechnicalSpecification = x.TechnicalSpecification,
                SafetyInformation = x.SafetyInformation,
                StockItemId = x.StockItems
                    .OrderByDescending(s => s.IsActive)
                    .ThenBy(s => s.Name)
                    .Select(s => (Guid?)s.Id)
                    .FirstOrDefault(),
                StockCode = x.StockItems
                    .OrderByDescending(s => s.IsActive)
                    .ThenBy(s => s.Name)
                    .Select(s => s.Code)
                    .FirstOrDefault(),
                CurrentQuantity = x.StockItems.Sum(s => s.CurrentQuantity),
                MinimumStockLevel = x.StockItems
                    .OrderByDescending(s => s.IsActive)
                    .ThenBy(s => s.Name)
                    .Select(s => s.MinimumQuantity)
                    .FirstOrDefault(),
                MaximumStockLevel = x.StockItems
                    .OrderByDescending(s => s.IsActive)
                    .ThenBy(s => s.Name)
                    .Select(s => s.MaximumQuantity)
                    .FirstOrDefault(),
                IsLowStock = x.StockItems.Any(s => s.CriticalQuantity > 0 && s.CurrentQuantity <= s.CriticalQuantity),
                RecipeUsageCount = x.RecipeItems.Count,
                ActiveRecipeUsageCount = x.RecipeItems.Count(i => i.Recipe.IsActive),
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            });
    }

    private IQueryable<MaterialDetailDto> ProjectMaterialDetail()
    {
        return _db.Materials
            .AsNoTracking()
            .Select(x => new MaterialDetailDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                Category = x.Category,
                SubCategory = x.SubCategory,
                MaterialType = x.MaterialType,
                Unit = x.Unit,
                Density = x.Density,
                DefaultSupplierId = x.DefaultSupplierId,
                DefaultSupplierName = x.DefaultSupplierName,
                PreferredSupplierId = x.DefaultSupplierId,
                PreferredSupplierName = x.DefaultSupplierName,
                Currency = x.Currency,
                LastPurchasePrice = x.LastPurchasePrice,
                MinimumStock = x.MinimumStock,
                MaximumStock = x.MaximumStock,
                CriticalStock = x.CriticalStock,
                WarehouseName = x.WarehouseName,
                LocationCode = x.LocationCode,
                LotTrackingEnabled = x.LotTrackingEnabled,
                ExpiryTrackingEnabled = x.ExpiryTrackingEnabled,
                TechnicalSpecification = x.TechnicalSpecification,
                SafetyInformation = x.SafetyInformation,
                ChemicalRole = x.ChemicalRole,
                MixingRatio = x.MixingRatio,
                ContainerWeight = x.ContainerWeight,
                AddedToPoliolBatch = x.AddedToPoliolBatch,
                CrosskimApplicationNote = x.CrosskimApplicationNote,
                FabricType = x.FabricType,
                FabricWeightGsm = x.FabricWeightGsm,
                FabricColor = x.FabricColor,
                FabricWidth = x.FabricWidth,
                FabricRollLength = x.FabricRollLength,
                AdhesiveType = x.AdhesiveType,
                CustomerName = x.CustomerName,
                DtfCode = x.DtfCode,
                DtfName = x.DtfName,
                DtfWidth = x.DtfWidth,
                DtfHeight = x.DtfHeight,
                ApplicationPosition = x.ApplicationPosition,
                ApplicationNote = x.ApplicationNote,
                PackagingType = x.PackagingType,
                BoxPairCapacity = x.BoxPairCapacity,
                BoxDimensions = x.BoxDimensions,
                BoxWeight = x.BoxWeight,
                StockItemId = x.StockItems
                    .OrderByDescending(s => s.IsActive)
                    .ThenBy(s => s.Name)
                    .Select(s => (Guid?)s.Id)
                    .FirstOrDefault(),
                StockCode = x.StockItems
                    .OrderByDescending(s => s.IsActive)
                    .ThenBy(s => s.Name)
                    .Select(s => s.Code)
                    .FirstOrDefault(),
                CurrentQuantity = x.StockItems.Sum(s => s.CurrentQuantity),
                MinimumStockLevel = x.StockItems
                    .OrderByDescending(s => s.IsActive)
                    .ThenBy(s => s.Name)
                    .Select(s => s.MinimumQuantity)
                    .FirstOrDefault(),
                MaximumStockLevel = x.StockItems
                    .OrderByDescending(s => s.IsActive)
                    .ThenBy(s => s.Name)
                    .Select(s => s.MaximumQuantity)
                    .FirstOrDefault(),
                IsLowStock = x.StockItems.Any(s => s.CriticalQuantity > 0 && s.CurrentQuantity <= s.CriticalQuantity),
                RecipeUsageCount = x.RecipeItems.Count,
                ActiveRecipeUsageCount = x.RecipeItems.Count(i => i.Recipe.IsActive),
                SupplierSummary = new MaterialSupplierSummaryDto
                {
                    PreferredSupplierId = x.DefaultSupplierId,
                    PreferredSupplierName = x.DefaultSupplierName
                },
                StockSummary = x.StockItems
                    .OrderByDescending(s => s.IsActive)
                    .ThenBy(s => s.Name)
                    .Select(s => new MaterialStockSummaryDto
                    {
                        StockItemId = s.Id,
                        StockCode = s.Code,
                        StockName = s.Name,
                        CurrentQuantity = s.CurrentQuantity,
                        Unit = s.Unit,
                        MinimumStockLevel = s.MinimumQuantity,
                        MaximumStockLevel = s.MaximumQuantity,
                        CriticalStockLevel = s.CriticalQuantity,
                        WarehouseName = s.WarehouseName,
                        LocationCode = s.LocationCode,
                        LastPurchasePrice = s.LastPurchasePrice,
                        Currency = s.Currency,
                        IsLowStock = s.CriticalQuantity > 0 && s.CurrentQuantity <= s.CriticalQuantity,
                        IsActive = s.IsActive
                    })
                    .FirstOrDefault(),
                LastPurchaseSummary = x.StockItems
                    .SelectMany(s => s.Movements)
                    .Where(m => m.UnitPrice.HasValue)
                    .OrderByDescending(m => m.MovementDate)
                    .Select(m => new MaterialPurchaseSummaryDto
                    {
                        UnitPrice = m.UnitPrice,
                        MovementDate = m.MovementDate,
                        SourceType = m.SourceType,
                        SourceDocumentNo = m.SourceDocumentNo
                    })
                    .FirstOrDefault(),
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            });
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

    private async Task<IActionResult?> SyncStockItemForMaterial(Material material, CancellationToken cancellationToken)
    {
        var linkedStock = await _db.StockItems
            .FirstOrDefaultAsync(x => x.MaterialId == material.Id, cancellationToken);

        if (linkedStock is not null)
        {
            var hasSecondActiveStockForMaterial = await _db.StockItems
                .AnyAsync(
                    x => x.Id != linkedStock.Id
                        && x.MaterialId == material.Id
                        && x.IsActive,
                    cancellationToken);

            if (hasSecondActiveStockForMaterial)
                return BadRequest(ApiResponse<object>.Fail("Bu malzeme için ikinci aktif stok kartı oluşturulamaz.", "MATERIAL_ACTIVE_STOCK_EXISTS"));

            var conflictingLinkedStock = await _db.StockItems
                .FirstOrDefaultAsync(
                    x => x.Id != linkedStock.Id
                        && x.IsActive
                        && x.Code == material.Code
                        && x.MaterialId.HasValue
                        && x.MaterialId.Value != material.Id,
                    cancellationToken);

            if (conflictingLinkedStock is not null)
                return BadRequest(ApiResponse<object>.Fail("Bu stok kartı başka bir malzemeye bağlı.", "STOCK_ALREADY_LINKED_TO_MATERIAL"));

            ApplyMaterialToStockItem(linkedStock, material, preserveCurrentQuantity: true);
            return null;
        }

        var codeMatchedStock = await _db.StockItems
            .FirstOrDefaultAsync(x => x.Code == material.Code, cancellationToken);

        if (codeMatchedStock is not null)
        {
            if (codeMatchedStock.MaterialId.HasValue && codeMatchedStock.MaterialId.Value != material.Id)
                return BadRequest(ApiResponse<object>.Fail("Bu stok kartı başka bir malzemeye bağlı.", "STOCK_ALREADY_LINKED_TO_MATERIAL"));

            ApplyMaterialToStockItem(codeMatchedStock, material, preserveCurrentQuantity: true);
            return null;
        }

        var stockItem = new StockItem
        {
            CurrentQuantity = 0
        };

        ApplyMaterialToStockItem(stockItem, material, preserveCurrentQuantity: false);
        _db.StockItems.Add(stockItem);

        return null;
    }

    private static void ApplyMaterialToStockItem(StockItem stockItem, Material material, bool preserveCurrentQuantity)
    {
        var currentQuantity = stockItem.CurrentQuantity;

        stockItem.MaterialId = material.Id;
        stockItem.Code = material.Code;
        stockItem.Name = material.Name;
        stockItem.Category = material.Category ?? "Genel";
        stockItem.Unit = material.Unit;
        stockItem.SupplierName = material.DefaultSupplierName;
        stockItem.LastPurchasePrice = material.LastPurchasePrice;
        stockItem.MinimumQuantity = material.MinimumStock;
        stockItem.MaximumQuantity = material.MaximumStock;
        stockItem.CriticalQuantity = material.CriticalStock ?? 0;
        stockItem.WarehouseName = material.WarehouseName;
        stockItem.LocationCode = material.LocationCode;
        stockItem.Currency = material.Currency ?? stockItem.Currency;
        stockItem.IsActive = material.IsActive;

        if (preserveCurrentQuantity)
            stockItem.CurrentQuantity = currentQuantity;
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public class MaterialListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? SubCategory { get; init; }
    public string MaterialType { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public decimal? Density { get; init; }
    public Guid? DefaultSupplierId { get; init; }
    public string? DefaultSupplierName { get; init; }
    public Guid? PreferredSupplierId { get; init; }
    public string? PreferredSupplierName { get; init; }
    public string? Currency { get; init; }
    public decimal? LastPurchasePrice { get; init; }
    public decimal? MinimumStock { get; init; }
    public decimal? MaximumStock { get; init; }
    public decimal? CriticalStock { get; init; }
    public string? WarehouseName { get; init; }
    public string? LocationCode { get; init; }
    public bool LotTrackingEnabled { get; init; }
    public bool ExpiryTrackingEnabled { get; init; }
    public string? TechnicalSpecification { get; init; }
    public string? SafetyInformation { get; init; }
    public Guid? StockItemId { get; init; }
    public string? StockCode { get; init; }
    public decimal CurrentQuantity { get; init; }
    public decimal? MinimumStockLevel { get; init; }
    public decimal? MaximumStockLevel { get; init; }
    public bool IsLowStock { get; init; }
    public int RecipeUsageCount { get; init; }
    public int ActiveRecipeUsageCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class MaterialDetailDto : MaterialListDto
{
    public string? TechnicalName { get; init; }
    public string? Manufacturer { get; init; }
    public string? Brand { get; init; }
    public string? CountryOfOrigin { get; init; }
    public int? LeadTimeDays { get; init; }
    public int? ShelfLifeDays { get; init; }
    public string? StorageConditions { get; init; }
    public string? SafetyNotes { get; init; }
    public string? Notes { get; init; }
    public string? ChemicalRole { get; init; }
    public decimal? MixingRatio { get; init; }
    public decimal? ContainerWeight { get; init; }
    public bool AddedToPoliolBatch { get; init; }
    public string? CrosskimApplicationNote { get; init; }
    public string? FabricType { get; init; }
    public decimal? FabricWeightGsm { get; init; }
    public string? FabricColor { get; init; }
    public decimal? FabricWidth { get; init; }
    public decimal? FabricRollLength { get; init; }
    public string? AdhesiveType { get; init; }
    public string? CustomerName { get; init; }
    public string? DtfCode { get; init; }
    public string? DtfName { get; init; }
    public decimal? DtfWidth { get; init; }
    public decimal? DtfHeight { get; init; }
    public string? ApplicationPosition { get; init; }
    public string? ApplicationNote { get; init; }
    public string? PackagingType { get; init; }
    public int? BoxPairCapacity { get; init; }
    public string? BoxDimensions { get; init; }
    public decimal? BoxWeight { get; init; }
    public MaterialSupplierSummaryDto? SupplierSummary { get; init; }
    public MaterialStockSummaryDto? StockSummary { get; init; }
    public MaterialPurchaseSummaryDto? LastPurchaseSummary { get; init; }
}

public class MaterialStockSummaryDto
{
    public Guid StockItemId { get; init; }
    public string? StockCode { get; init; }
    public string StockName { get; init; } = string.Empty;
    public decimal CurrentQuantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal? MinimumStockLevel { get; init; }
    public decimal? MaximumStockLevel { get; init; }
    public decimal CriticalStockLevel { get; init; }
    public string? WarehouseName { get; init; }
    public string? LocationCode { get; init; }
    public decimal? LastPurchasePrice { get; init; }
    public string Currency { get; init; } = "TRY";
    public bool IsLowStock { get; init; }
    public bool IsActive { get; init; }
}

public class MaterialSupplierSummaryDto
{
    public Guid? PreferredSupplierId { get; init; }
    public string? PreferredSupplierName { get; init; }
}

public class MaterialPurchaseSummaryDto
{
    public decimal? UnitPrice { get; init; }
    public DateTime MovementDate { get; init; }
    public string? SourceType { get; init; }
    public string? SourceDocumentNo { get; init; }
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
