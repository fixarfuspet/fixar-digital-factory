using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class InjectionStation : BaseAuditableEntity
{
    public int StationNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    // Aktif, Pasif, Bakım
    public string Status { get; set; } = "Aktif";

    public bool IsActive { get; set; } = true;

    public ICollection<ProductionRecord> ProductionRecords { get; set; }
        = new List<ProductionRecord>();
}