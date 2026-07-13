using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Mold : BaseAuditableEntity
{
    public Guid? ProductId { get; set; }

    public Product? Product { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    public string? ProductModel { get; set; }

    public string? ModelCode { get; set; }

    public string Size { get; set; } = string.Empty;

    public string? SizeGroup { get; set; }

    public string? SizeRange { get; set; }

    public string? Description { get; set; }

    public string MoldType { get; set; } = "Pair";

    public int CavityCount { get; set; } = 1;

    public bool IsRightLeftCombined { get; set; }

    public string FoamType { get; set; } = "10100";

    public string ProductType { get; set; } = "Normal";

    public decimal? XCoordinate { get; set; }

    public decimal? YCoordinate { get; set; }

    public decimal? TargetPairWeight { get; set; }

    public decimal? MinimumPairWeight { get; set; }

    public decimal? MaximumPairWeight { get; set; }

    public decimal? TargetDensity { get; set; }

    public decimal? MinimumDensity { get; set; }

    public decimal? MaximumDensity { get; set; }

    public int? StandardCuringTimeSeconds { get; set; }

    public decimal? StandardMoldTemperature { get; set; }

    public int? StandardCycleTimeSeconds { get; set; }

    public int? ReleaseFrequencyCycles { get; set; }

    public decimal? MoldWeightKg { get; set; }

    public string? MachineName { get; set; }

    public string? CompatibleMachineCode { get; set; }

    public int? CurrentStationNumber { get; set; }

    public string? StorageLocation { get; set; }

    public string? ShelfCode { get; set; }

    public long TotalCycleCount { get; set; }

    public long TotalProducedPairs { get; set; }

    public DateTime? LastCleaningDate { get; set; }

    public DateTime? NextCleaningDate { get; set; }

    public DateTime? LastMaintenanceDate { get; set; }

    public DateTime? NextMaintenanceDate { get; set; }

    public long? EstimatedLifeCycles { get; set; }

    public string? PhotoPath { get; set; }

    public string? TechnicalDocumentPath { get; set; }

    public string? CadFilePath { get; set; }

    public string? QrCode { get; set; }

    public string? Barcode { get; set; }

    public string OwnerType { get; set; } = "Fixar";

    public string? OwnerCustomerName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProductionRecord> ProductionRecords { get; set; } = new List<ProductionRecord>();
}
