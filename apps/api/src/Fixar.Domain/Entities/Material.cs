using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Material : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? SubCategory { get; set; }

    public string? Description { get; set; }

    public string MaterialType { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public Guid? DefaultSupplierId { get; set; }

    public string? DefaultSupplierName { get; set; }

    public string? Currency { get; set; }

    public decimal? LastPurchasePrice { get; set; }

    public decimal? MinimumStock { get; set; }

    public decimal? MaximumStock { get; set; }

    public decimal? CriticalStock { get; set; }

    public string? WarehouseName { get; set; }

    public string? LocationCode { get; set; }

    public bool LotTrackingEnabled { get; set; }

    public bool ExpiryTrackingEnabled { get; set; }

    public string? TechnicalSpecification { get; set; }

    public string? SafetyInformation { get; set; }

    public string? ChemicalRole { get; set; }

    public decimal? Density { get; set; }

    public decimal? MixingRatio { get; set; }

    public decimal? ContainerWeight { get; set; }

    public bool AddedToPoliolBatch { get; set; }

    public string? CrosskimApplicationNote { get; set; }

    public string? FabricType { get; set; }

    public decimal? FabricWeightGsm { get; set; }

    public string? FabricColor { get; set; }

    public decimal? FabricWidth { get; set; }

    public decimal? FabricRollLength { get; set; }

    public string? AdhesiveType { get; set; }

    public string? CustomerName { get; set; }

    public string? DtfCode { get; set; }

    public string? DtfName { get; set; }

    public decimal? DtfWidth { get; set; }

    public decimal? DtfHeight { get; set; }

    public string? ApplicationPosition { get; set; }

    public string? ApplicationNote { get; set; }

    public string? PackagingType { get; set; }

    public int? BoxPairCapacity { get; set; }

    public string? BoxDimensions { get; set; }

    public decimal? BoxWeight { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();

    public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}
